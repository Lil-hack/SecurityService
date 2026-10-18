using System;
using System.IO;
using UnityEngine;

/// <summary>Project-local saved voice settings for Nina; output volume stays a user preference.</summary>
[Serializable]
public sealed class NinaVoiceProfile
{
    public string voicePath;
    public string openingAudio = "Characters/NinaOpeningGrandmother";
    public bool telephone = true;
    public float telephoneStrength = 55, pitchSemitones = -1.5f;
    public float ratePercent = 88, pauseMilliseconds = 180;
    public float tremblePercent = 18, variationPercent = 22, raspPercent = 10;
    public static string ProfilePath(string root) => Path.Combine(root, "Tools/Gemma/Prompts/nina_voice.json");

    public static void Load(string root)
    {
        string path = ProfilePath(root);
        if (!File.Exists(path)) return;
        var profile = JsonUtility.FromJson<NinaVoiceProfile>(File.ReadAllText(path));
        if (profile == null || string.IsNullOrWhiteSpace(profile.voicePath)) throw new InvalidDataException("Пустой профиль голоса Нины");
        PlayerPrefs.SetString("GemmaChat.PiperVoice", profile.voicePath);
        PlayerPrefs.SetInt("GemmaChat.TelephoneEffect", profile.telephone ? 1 : 0);
        PlayerPrefs.SetFloat("GemmaChat.TelephoneStrength", Mathf.Clamp(profile.telephoneStrength, 0, 100) / 100f);
        PlayerPrefs.SetFloat("GemmaChat.PitchSemitones", Mathf.Clamp(profile.pitchSemitones, -12, 7));
        PlayerPrefs.SetFloat("GemmaChat.Character.Rate", Mathf.Clamp(profile.ratePercent, 65, 130));
        PlayerPrefs.SetFloat("GemmaChat.Character.Pause", Mathf.Clamp(profile.pauseMilliseconds, 0, 500));
        PlayerPrefs.SetFloat("GemmaChat.Character.Tremble", Mathf.Clamp(profile.tremblePercent, 0, 100));
        PlayerPrefs.SetFloat("GemmaChat.Character.Variation", Mathf.Clamp(profile.variationPercent, 0, 100));
        PlayerPrefs.SetFloat("GemmaChat.Character.Rasp", Mathf.Clamp(profile.raspPercent, 0, 100));
    }

    public void Save(string root)
    {
        string path = ProfilePath(root);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path + ".tmp", JsonUtility.ToJson(this, true));
        if (File.Exists(path)) File.Replace(path + ".tmp", path, null);
        else File.Move(path + ".tmp", path);
    }
}
