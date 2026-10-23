using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public static class CheckVoiceCharacter
{
    public static async Task<string> Check()
    {
        var chat = UnityEngine.Object.FindAnyObjectByType<GemmaTestChat>();
        var voice = chat.GetComponent<GemmaSpeech>();
        var root = chat.GetComponent<UIDocument>().rootVisualElement;
        string[] names = { "Rate", "Pause", "Tremble", "Variation", "Rasp" };
        float[] values = { 82f, 230f, 35f, 40f, 20f };
        var saved = new float[names.Length];
        float pitch = voice.PitchSemitones;
        try
        {
            for (int i = 0; i < names.Length; i++)
            {
                var slider = root.Q<Slider>("voice" + names[i]);
                saved[i] = slider.value; slider.value = values[i];
            }
            if (Mathf.Abs(voice.SpeechRate - .82f) > .001f || Mathf.Abs(voice.PauseSeconds - .23f) > .001f
                || Mathf.Abs(voice.Tremble - .35f) > .001f || Mathf.Abs(voice.VolumeVariation - .4f) > .001f)
                throw new Exception("UI settings not applied");
            var rasp = voice.GetComponent<AudioDistortionFilter>();
            if (!rasp.enabled || Mathf.Abs(rasp.distortionLevel - .07f) > .001f) throw new Exception("Rasp not applied");
            voice.PitchSemitones = -2f;
            for (int i = 0; i < 150 && !voice.Ready; i++) await Task.Delay(100);
            voice.Speak("Ой, сейчас... Очки найду. Вы меня хорошо слышите?");
            for (int i = 0; i < 200 && !voice.Playing; i++) await Task.Delay(100);
            if (!voice.Playing || voice.LastRms <= 0) throw new Exception("No speech playback");
            var source = voice.GetComponent<AudioSource>();
            float minPitch = float.MaxValue, maxPitch = 0, minVolume = float.MaxValue, maxVolume = 0;
            for (int i = 0; i < 20; i++)
            {
                await Task.Delay(50);
                minPitch = Mathf.Min(minPitch, source.pitch); maxPitch = Mathf.Max(maxPitch, source.pitch);
                minVolume = Mathf.Min(minVolume, source.volume); maxVolume = Mathf.Max(maxVolume, source.volume);
            }
            if (maxPitch - minPitch < .001f || maxVolume - minVolume < .00001f) throw new Exception("No modulation");
            root.Q<Slider>("voiceRasp").value = 0;
            if (rasp.enabled) throw new Exception("Rasp bypass failed");
            voice.Cancel();
            return "PASS: five UI controls, Piper speech, pitch/volume modulation, rasp bypass, cancel; pitch range="
                + minPitch + ".." + maxPitch + "; volume=" + minVolume + ".." + maxVolume;
        }
        finally
        {
            for (int i = 0; i < names.Length; i++) root.Q<Slider>("voice" + names[i]).value = saved[i];
            voice.PitchSemitones = pitch;
        }
    }
}
