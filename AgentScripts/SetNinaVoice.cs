using System;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public static class SetNinaVoice
{
    public static async Task<string> Apply()
    {
        var chat = UnityEngine.Object.FindAnyObjectByType<GemmaTestChat>();
        if (chat == null) throw new Exception("Chat scene is not running");
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var speech = chat.GetComponent<GemmaSpeech>();
        speech.Cancel();
        var paths = (List<string>)typeof(GemmaTestChat).GetField("voicePaths", flags).GetValue(chat);
        var choice = (PopupField<string>)typeof(GemmaTestChat).GetField("voiceChoice", flags).GetValue(chat);
        int index = paths.FindIndex(p => p.EndsWith("/femalecommoner.onnx", StringComparison.OrdinalIgnoreCase));
        if (index < 0) throw new Exception("femalecommoner missing");
        choice.value = choice.choices[index];
        ((Toggle)typeof(GemmaTestChat).GetField("telephoneEffect", flags).GetValue(chat)).value = true;
        ((Slider)typeof(GemmaTestChat).GetField("telephoneStrength", flags).GetValue(chat)).value = 55f;
        ((Slider)typeof(GemmaTestChat).GetField("voicePitch", flags).GetValue(chat)).value = -1.5f;
        var root = chat.GetComponent<UIDocument>().rootVisualElement;
        root.Q<Slider>("voiceRate").value = 88f;
        root.Q<Slider>("voicePause").value = 180f;
        root.Q<Slider>("voiceTremble").value = 18f;
        root.Q<Slider>("voiceVariation").value = 22f;
        root.Q<Slider>("voiceRasp").value = 10f;
        PlayerPrefs.Save();
        for (int i = 0; i < 200 && !speech.Ready; i++) await Task.Delay(100);
        if (!speech.Ready) throw new Exception("Piper not ready");
        speech.Speak("Алло? Ой, милок... Это Нина Петровна. Сейчас, очки найду. Вы меня хорошо слышите?");
        for (int i = 0; i < 200 && !speech.Playing; i++) await Task.Delay(100);
        if (!speech.Playing) throw new Exception("Playback failed");
        return "Applied and saved: femalecommoner; telephone=" + speech.TelephoneStrength
            + "; pitch=" + speech.PitchSemitones + "; rate=" + speech.SpeechRate
            + "; pause=" + speech.PauseSeconds + "; tremble=" + speech.Tremble
            + "; variation=" + speech.VolumeVariation + "; rasp=" + speech.Rasp
            + "; playing=" + speech.Playing + "; volume preserved=" + speech.Volume;
    }
}
