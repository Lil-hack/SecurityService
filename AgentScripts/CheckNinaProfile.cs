using System;
using System.IO;
using System.Reflection;
using UnityEngine;

public static class CheckNinaProfile
{
    public static string Check()
    {
        var chat = UnityEngine.Object.FindAnyObjectByType<GemmaTestChat>();
        var voice = chat.GetComponent<GemmaSpeech>();
        if (!voice.VoicePath.EndsWith("femalecommoner.onnx") || !voice.TelephoneEnabled
            || Mathf.Abs(voice.TelephoneStrength - .55f) > .001f
            || Mathf.Abs(voice.PitchSemitones + 1.5f) > .001f
            || Mathf.Abs(voice.SpeechRate - .88f) > .001f
            || Mathf.Abs(voice.PauseSeconds - .18f) > .001f
            || Mathf.Abs(voice.Tremble - .18f) > .001f
            || Mathf.Abs(voice.VolumeVariation - .22f) > .001f
            || Mathf.Abs(voice.Rasp - .1f) > .001f) throw new Exception("Saved profile not applied");
        typeof(GemmaTestChat).GetMethod("SaveNinaVoice", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(chat, null);
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var saved = JsonUtility.FromJson<NinaVoiceProfile>(File.ReadAllText(NinaVoiceProfile.ProfilePath(root)));
        if (saved.voicePath != voice.VoicePath || saved.ratePercent != 88f) throw new Exception("Save failed");
        return "PASS: Nina profile loaded at startup and saved through UI; all nine voice settings match";
    }
}
