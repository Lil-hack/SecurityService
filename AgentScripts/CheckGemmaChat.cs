using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public static class CheckGemmaChat
{
    public static string RuntimeState()
    {
        return "paused=" + UnityEditor.EditorApplication.isPaused
            + "; background=" + Application.runInBackground + "; time=" + Time.time;
    }
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static MonoBehaviour Chat()
    {
        var type = Type.GetType("GemmaTestChat, Assembly-CSharp", true);
        return (MonoBehaviour)UnityEngine.Object.FindAnyObjectByType(type);
    }
    public static string Load()
    {
        var chat = Chat();
        chat.GetType().GetMethod("LoadModel", Flags).Invoke(chat, null);
        return Snapshot();
    }
    public static string Select(string path)
    {
        var chat = Chat();
        chat.GetType().GetMethod("StopModel", Flags).Invoke(chat, null);
        var input = (TextField)chat.GetType().GetField("modelInput", Flags).GetValue(chat);
        input.value = path;
        return Snapshot();
    }
    public static string Send(string text)
    {
        var chat = Chat();
        var input = (TextField)chat.GetType().GetField("messageInput", Flags).GetValue(chat);
        input.value = text;
        chat.GetType().GetMethod("SendMessage", Flags).Invoke(chat, null);
        return Snapshot();
    }
    public static string Snapshot()
    {
        var chat = Chat();
        var type = chat.GetType();
        var status = (Label)type.GetField("status", Flags).GetValue(chat);
        var answer = (Label)type.GetField("answer", Flags).GetValue(chat);
        return JsonUtility.ToJson(new State
        {
            ready = (bool)type.GetField("ready", Flags).GetValue(chat),
            busy = (bool)type.GetField("busy", Flags).GetValue(chat),
            status = status.text,
            answer = answer == null ? "" : answer.text
        });
    }
    public static string Reset()
    {
        var chat = Chat();
        chat.GetType().GetMethod("ResetChat", Flags).Invoke(chat, null);
        return Snapshot();
    }
    public static string VoiceSnapshot()
    {
        var voice = Chat().GetComponent<GemmaSpeech>();
        if (voice == null) return "Speech component is unavailable outside Play Mode.";
        var mute = typeof(UnityEditor.EditorUtility).GetProperty("audioMasterMute",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        return JsonUtility.ToJson(new VoiceState
        {
            ready = voice.Ready, synthesizing = voice.Synthesizing,
            playing = voice.Playing, seconds = voice.LastDuration, rms = voice.LastRms,
            status = voice.Status, text = voice.LastText, voicePath = voice.VoicePath,
            sourceCount = Chat().GetComponents<AudioSource>().Length, volume = voice.Volume,
            listener = UnityEngine.Object.FindAnyObjectByType<AudioListener>() != null,
            listenerVolume = AudioListener.volume,
            editorMuted = mute != null && (bool)mute.GetValue(null)
        });
    }
    public static string StopVoice()
    {
        Chat().GetComponent<GemmaSpeech>()?.Cancel();
        return VoiceSnapshot();
    }
    public static string UnmuteEditor()
    {
        var mute = typeof(UnityEditor.EditorUtility).GetProperty("audioMasterMute",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (mute != null && mute.CanWrite) mute.SetValue(null, false);
        return VoiceSnapshot();
    }
    public static string RepeatVoice()
    {
        var chat = Chat();
        var answer = (Label)chat.GetType().GetField("answer", Flags).GetValue(chat);
        if (answer != null) chat.GetComponent<GemmaSpeech>()?.Speak(answer.text);
        return VoiceSnapshot();
    }
    public static string SetVolume(float percent)
    {
        var chat = Chat();
        var slider = (Slider)chat.GetType().GetField("speechVolume", Flags).GetValue(chat);
        slider.value = percent;
        var speech = chat.GetComponent<GemmaSpeech>();
        var source = chat.GetComponent<AudioSource>();
        return "slider=" + slider.value + "; voice=" + speech.Volume
            + "; source=" + source.volume + "; playing=" + source.isPlaying;
    }
    public static string SpeakVoice(string text)
    {
        Chat().GetComponent<GemmaSpeech>().Speak(text);
        return VoiceSnapshot();
    }
    public static string SelectVoice(string filename)
    {
        var chat = Chat();
        var paths = (List<string>)chat.GetType().GetField("voicePaths", Flags).GetValue(chat);
        var choice = (PopupField<string>)chat.GetType().GetField("voiceChoice", Flags).GetValue(chat);
        int index = paths.FindIndex(path => path.EndsWith("/" + filename, StringComparison.OrdinalIgnoreCase));
        if (index < 0) throw new ArgumentException("Voice not found: " + filename);
        choice.value = choice.choices[index];
        return "available=" + paths.Count + "; selected=" + choice.value;
    }
    public static string PreviewVoice()
    {
        var chat = Chat();
        chat.GetType().GetMethod("PreviewSelectedVoice", Flags).Invoke(chat, null);
        return VoiceSnapshot();
    }
    [Serializable] sealed class VoiceState
    {
        public bool ready, synthesizing, playing, listener, editorMuted;
        public float seconds, rms, listenerVolume;
        public float volume;
        public int sourceCount;
        public string status, text, voicePath;
    }
    [Serializable] sealed class State { public bool ready, busy; public string status, answer; }
}
