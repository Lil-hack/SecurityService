using System;
using System.Threading.Tasks;
using UnityEngine;

public static class CheckCartoonVoice
{
    public static async Task<string> Preview()
    {
        var chat = UnityEngine.Object.FindAnyObjectByType<GemmaTestChat>();
        var voice = chat.GetComponent<GemmaSpeech>();
        for (int i = 0; i < 200 && !voice.Ready; i++) await Task.Delay(100);
        if (!voice.Ready) throw new Exception("Piper not ready");
        if (Mathf.Abs(voice.PitchSemitones - 3.5f) > .001f || Mathf.Abs(voice.SpeechRate - 1.1f) > .001f
            || Mathf.Abs(voice.TelephoneStrength - .25f) > .001f) throw new Exception("Cartoon profile not applied");
        voice.Speak("Алло, милок! Ой, куда же мои очки подевались? Кошка опять на них уселась!");
        for (int i = 0; i < 200 && !voice.Playing; i++) await Task.Delay(100);
        if (!voice.Playing || voice.LastRms <= 0) throw new Exception("No speech playback");
        var source = voice.GetComponent<AudioSource>();
        if (source.pitch <= 1.2f || source.pitch >= 1.25f) throw new Exception("Pitch not raised: " + source.pitch);
        return "PASS: cartoon profile loaded; pitch=+3.5 semitones; rate=110%; telephone=25%; speech playing; pitch factor="
            + source.pitch + "; RMS=" + voice.LastRms + "; volume preserved=" + voice.Volume;
    }
}
