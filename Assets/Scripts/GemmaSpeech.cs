using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>Owns a Piper CPU process and plays its PCM16 sentence chunks in Unity.</summary>
public sealed class GemmaSpeech : MonoBehaviour
{
    [Serializable] sealed class Request
    {
        public string command = "speak"; public int id; public string text;
        public float length_scale = 1.1f, pause_seconds;
    }
    [Serializable] sealed class Reply
    {
        public string kind, text, audio;
        public int id, sample_rate, channels;
    }

    readonly ConcurrentQueue<Action> pending = new ConcurrentQueue<Action>();
    readonly Queue<AudioClip> clips = new Queue<AudioClip>();
    readonly Queue<string> phrases = new Queue<string>();
    readonly SpeechTextBuffer textBuffer = new SpeechTextBuffer();
    Process worker;
    AudioSource source;
    AudioHighPassFilter telephoneHighPass;
    AudioLowPassFilter telephoneLowPass;
    AudioDistortionFilter raspFilter;
    bool telephoneEnabled = true;
    float telephoneStrength = .5f;
    float pitchSemitones;
    AudioListener ownedListener;
    AudioClip current;
    int generation, requestId;
    string diagnostics = "";
    float volume = 1f;
    bool streamOpen, requestActive;
    string pythonPath, projectPath;
    public string VoicePath { get; private set; }
    public float SpeechRate { get; set; } = .91f;
    public float PauseSeconds { get; set; } = .15f;
    public float Tremble { get; set; } = .2f;
    public float VolumeVariation { get; set; } = .2f;
    float rasp = .12f;
    public float Rasp
    {
        get => rasp;
        set
        {
            rasp = Mathf.Clamp01(value);
            if (raspFilter != null)
            {
                raspFilter.distortionLevel = rasp * .35f;
                raspFilter.enabled = rasp > 0f;
            }
        }
    }

    public float TelephoneStrength
    {
        get => telephoneStrength;
        set { telephoneStrength = Mathf.Clamp01(value); ApplyTelephone(); }
    }

    public float PitchSemitones
    {
        get => pitchSemitones;
        set
        {
            pitchSemitones = Mathf.Clamp(value, -12f, 7f);
            if (source != null) source.pitch = Mathf.Pow(2f, pitchSemitones / 12f);
        }
    }

    void ApplyTelephone()
    {
        float t = telephoneStrength;
        if (telephoneHighPass != null)
        {
            telephoneHighPass.cutoffFrequency = t <= .5f ? Mathf.Lerp(20f, 300f, t * 2f) : Mathf.Lerp(300f, 650f, (t - .5f) * 2f);
            telephoneHighPass.enabled = telephoneEnabled && t > 0f;
        }
        if (telephoneLowPass != null)
        {
            telephoneLowPass.cutoffFrequency = t <= .5f ? Mathf.Lerp(22000f, 3400f, t * 2f) : Mathf.Lerp(3400f, 2200f, (t - .5f) * 2f);
            telephoneLowPass.enabled = telephoneEnabled && t > 0f;
        }
    }

    public bool TelephoneEnabled
    {
        get => telephoneEnabled;
        set
        {
            telephoneEnabled = value;
            ApplyTelephone();
        }
    }

    public float Volume
    {
        get => volume;
        set
        {
            volume = Mathf.Clamp01(value);
            if (source != null) source.volume = volume * OfficeGameSettings.Voice;
        }
    }

    public bool Ready { get; private set; }
    public bool Synthesizing => requestActive || phrases.Count > 0;
    public bool Playing => source != null && source.isPlaying;
    public float PlaybackLevel { get; private set; }
    readonly float[] mouthSamples = new float[256];
    public bool HasPendingSpeech => streamOpen || Synthesizing || Playing || clips.Count > 0;
    public string Status { get; private set; } = "Озвучка выключена";
    public string LastText { get; private set; } = "";
    public float LastDuration { get; private set; }
    public float LastRms { get; private set; }
    public event Action<string> StatusChanged;
    public event Action PlaybackStarted;
    public event Action<string,string> TimingEvent;

    void SetStatus(string value) { Status = value; StatusChanged?.Invoke(value); }

    public void Initialize(string python, string projectRoot, string voicePath = "Models/Piper/ru_RU-irina-medium.onnx")
    {
        if (worker != null) return;
        pythonPath = python; projectPath = projectRoot;
        VoicePath = voicePath;
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = Volume * OfficeGameSettings.Voice;
        PitchSemitones = pitchSemitones;
        if (raspFilter == null) raspFilter = gameObject.AddComponent<AudioDistortionFilter>();
        Rasp = rasp;
        // Conventional telephone speech bandwidth; applies live to queued and playing speech.
        if (telephoneHighPass == null) telephoneHighPass = gameObject.AddComponent<AudioHighPassFilter>();
        if (telephoneLowPass == null) telephoneLowPass = gameObject.AddComponent<AudioLowPassFilter>();
        telephoneHighPass.cutoffFrequency = 300f;
        telephoneHighPass.highpassResonanceQ = 1f;
        telephoneLowPass.cutoffFrequency = 3400f;
        telephoneLowPass.lowpassResonanceQ = 1f;
        TelephoneEnabled = telephoneEnabled;
        bool hasListener = false;
        foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude))
            if (listener.isActiveAndEnabled) { hasListener = true; break; }
        if (!hasListener) ownedListener = gameObject.AddComponent<AudioListener>();
        string model = Path.IsPathRooted(voicePath) ? voicePath : Path.Combine(projectRoot, voicePath);
        if (!File.Exists(model)) { SetStatus("Нет голоса Piper: скачайте Models/Piper (Tools/Piper/README.md)"); return; }
        if (!File.Exists(python)) { SetStatus("Не найден Python для Piper"); return; }
        int activeGeneration = ++generation;
        try
        {
            worker = new Process { StartInfo = new ProcessStartInfo
            {
                FileName = python,
                Arguments = "-u \"" + Path.Combine(projectRoot, "Tools/Piper/worker.py") + "\" --model \"" + model + "\"",
                WorkingDirectory = projectRoot,
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
            }, EnableRaisingEvents = true };
            worker.StartInfo.EnvironmentVariables["PYTHONUTF8"] = "1";
            worker.OutputDataReceived += (_, evt) =>
            {
                if (!string.IsNullOrEmpty(evt.Data)) pending.Enqueue(() =>
                { if (activeGeneration == generation) Receive(evt.Data); });
            };
            worker.ErrorDataReceived += (_, evt) =>
            {
                if (!string.IsNullOrEmpty(evt.Data)) pending.Enqueue(() =>
                {
                    if (activeGeneration != generation) return;
                    diagnostics += "\n" + evt.Data;
                    if (diagnostics.Length > 1500) diagnostics = diagnostics.Substring(diagnostics.Length - 1500);
                });
            };
            worker.Exited += (_, __) => pending.Enqueue(() =>
            {
                if (activeGeneration != generation) return;
                Ready = false; Cancel(); SetStatus("Piper завершён: " + diagnostics);
            });
            TimingEvent?.Invoke("voice_worker_start", "");
            worker.Start(); worker.BeginOutputReadLine(); worker.BeginErrorReadLine();
            SetStatus("Загружаю голос " + Path.GetFileNameWithoutExtension(model) + "…");
        }
        catch (Exception error) { Shutdown(); SetStatus("Ошибка Piper: " + error.Message); }
    }

    public void SelectVoice(string voicePath)
    {
        Shutdown();
        diagnostics = "";
        Initialize(pythonPath, projectPath, voicePath);
    }

    public void Speak(string text)
    {
        BeginStream();
        AppendText(text);
        EndStream();
    }

    public void PlayPrepared(AudioClip clip, string text)
    {
        if (source == null || clip == null) throw new InvalidOperationException("Нет подготовленного приветствия");
        Cancel();
        // Resources owns the original; only our playback copy is destroyed by Cancel/Update.
        current = Instantiate(clip);
        LastText = text; LastDuration = clip.length;
        source.clip = current;
        source.Play();
        TimingEvent?.Invoke("prepared_audio_start",text);
        SetStatus("Персонаж говорит…");
    }

    public void BeginStream(bool preserveCurrentPlayback=false)
    {
        if(preserveCurrentPlayback && Playing)
        {
            // Queue the real answer behind the short prepared thinking phrase.
            ++requestId;streamOpen=requestActive=false;textBuffer.Clear();phrases.Clear();
            while(clips.Count>0)Destroy(clips.Dequeue());
        }
        else Cancel();
        streamOpen = true;
        LastText = ""; LastDuration = 0; LastRms = 0;
    }

    public void AppendText(string fragment)
    {
        if (!streamOpen || string.IsNullOrEmpty(fragment)) return;
        LastText += fragment;
        textBuffer.Append(fragment);
        QueuePhrases(false);
    }

    public void EndStream()
    {
        if (!streamOpen) return;
        streamOpen = false;
        QueuePhrases(true);
    }

    void QueuePhrases(bool flush)
    {
        string phrase;
        while ((phrase = textBuffer.Take(flush)) != null)
            if (!string.IsNullOrWhiteSpace(phrase)) phrases.Enqueue(phrase.Trim());
        SendNextPhrase();
    }

    void SendNextPhrase()
    {
        if (!Ready || requestActive || phrases.Count == 0) return;
        try
        {
            string phrase = phrases.Dequeue();
            // Compensate playback pitch in the duration predicted by Piper.
            float lengthScale = Mathf.Pow(2f, pitchSemitones / 12f) / Mathf.Clamp(SpeechRate, .65f, 1.3f);
            byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Request
            {
                id = requestId, text = phrase, length_scale = lengthScale,
                pause_seconds = Mathf.Clamp(PauseSeconds, 0f, .5f) * Mathf.Pow(2f, pitchSemitones / 12f)
            }) + "\n");
            worker.StandardInput.BaseStream.Write(bytes, 0, bytes.Length);
            worker.StandardInput.BaseStream.Flush();
            TimingEvent?.Invoke("phrase_requested",phrase);
            requestActive = true;
            if (!Playing) SetStatus("Синтез речи…");
        }
        catch (Exception error) { Cancel(); SetStatus("Ошибка озвучки: " + error.Message); }
    }

    void Receive(string json)
    {
        try
        {
            var reply = JsonUtility.FromJson<Reply>(json);
            if (reply == null) return;
            if (reply.kind == "ready") { TimingEvent?.Invoke("voice_ready", "");Ready = true; SetStatus("Голос: " + reply.text); SendNextPhrase(); return; }
            if (reply.kind == "fatal") { Ready = false; Cancel(); SetStatus("Ошибка Piper: " + reply.text); return; }
            if (reply.id != requestId) return;
            if (reply.kind == "audio")
            {
                byte[] pcm = Convert.FromBase64String(reply.audio);
                if (reply.channels < 1 || reply.channels > 2 || reply.sample_rate < 8000 || pcm.Length % (2 * reply.channels) != 0)
                    throw new InvalidDataException("Некорректный PCM от Piper");
                var samples = new float[pcm.Length / 2];
                double energy = 0;
                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i] = (short)(pcm[i * 2] | pcm[i * 2 + 1] << 8) / 32768f;
                    energy += samples[i] * samples[i];
                }
                if (samples.Length == 0) return;
                var clip = AudioClip.Create("Нина Петровна — Piper", samples.Length / reply.channels, reply.channels, reply.sample_rate, false);
                clip.SetData(samples, 0); clips.Enqueue(clip);
                TimingEvent?.Invoke("pcm_ready",clip.length.ToString(System.Globalization.CultureInfo.InvariantCulture));
                LastDuration += clip.length;
                LastRms = Mathf.Max(LastRms, (float)Math.Sqrt(energy / samples.Length));
            }
            else if (reply.kind == "done")
            {
                requestActive = false;
                SendNextPhrase();
                if (!Playing && clips.Count == 0 && !Synthesizing)
                    SetStatus(streamOpen ? "Жду продолжения ответа…" : "Озвучка завершена");
            }
            else if (reply.kind == "error") { Cancel(); SetStatus("Ошибка озвучки: " + reply.text); }
        }
        catch (Exception error) { Cancel(); SetStatus("Ошибка звука: " + error.Message); }
    }

    public void Cancel()
    {
        ++requestId; streamOpen = requestActive = false; PlaybackLevel=0;
        textBuffer.Clear(); phrases.Clear();
        if (source != null) { source.Stop(); source.clip = null; }
        if (current != null) { Destroy(current); current = null; }
        while (clips.Count > 0) Destroy(clips.Dequeue());
        if (Ready) SetStatus("Озвучка остановлена");
    }

    void Update()
    {
        float level=0;
        if(source!=null && source.isPlaying && source.clip!=null && source.clip.samples>256)
        {
            source.clip.GetData(mouthSamples,Mathf.Min(source.timeSamples,source.clip.samples-256));
            double energy=0;foreach(float sample in mouthSamples)energy+=sample*sample;
            level=Mathf.Clamp01((float)Math.Sqrt(energy/mouthSamples.Length)*8);
        }
        PlaybackLevel=Mathf.Lerp(PlaybackLevel,level,1-Mathf.Exp(-Time.unscaledDeltaTime*(level>PlaybackLevel?32:22)));
        while (pending.TryDequeue(out var action)) action();
        if (source != null)
        {
            float t = Time.unscaledTime;
            float wobble = Mathf.Sin(t * 2f * Mathf.PI * 4.1f) * .65f + Mathf.Sin(t * 2f * Mathf.PI * 2.7f) * .35f;
            source.pitch = Mathf.Pow(2f, (pitchSemitones + wobble * Mathf.Clamp01(Tremble) * .35f) / 12f);
            float envelope = .5f + .3f * Mathf.Sin(t * 2.3f) + .2f * Mathf.Sin(t * 4.7f);
            source.volume = volume * OfficeGameSettings.Voice * (1f - Mathf.Clamp01(VolumeVariation) * .25f * envelope);
        }
        if (source == null || source.isPlaying) return;
        if (current != null)
        {
            TimingEvent?.Invoke("audio_end", "");
            Destroy(current); current = null;
            if (clips.Count == 0 && !Synthesizing)
                SetStatus(streamOpen ? "Жду продолжения ответа…" : "Озвучка завершена");
        }
        if (clips.Count > 0)
        {
            current = clips.Dequeue(); source.clip = current; source.Play();
            TimingEvent?.Invoke("audio_start",current.length.ToString(System.Globalization.CultureInfo.InvariantCulture));
            PlaybackStarted?.Invoke();
            SetStatus("Нина Петровна говорит…");
        }
    }

    void Shutdown()
    {
        ++generation; Ready = false; Cancel();
        if (worker != null)
        {
            try { if (!worker.HasExited) worker.Kill(); }
            catch (InvalidOperationException) { }
            finally { worker.Dispose(); worker = null; }
        }
    }

    void OnDisable()
    {
        Shutdown();
        if (raspFilter != null) { raspFilter.enabled = false; Destroy(raspFilter); raspFilter = null; }
        if (telephoneHighPass != null) { telephoneHighPass.enabled = false; Destroy(telephoneHighPass); telephoneHighPass = null; }
        if (telephoneLowPass != null) { telephoneLowPass.enabled = false; Destroy(telephoneLowPass); telephoneLowPass = null; }
        if (source != null) { source.enabled = false; Destroy(source); source = null; }
        if (ownedListener != null) { ownedListener.enabled = false; Destroy(ownedListener); ownedListener = null; }
    }
}
