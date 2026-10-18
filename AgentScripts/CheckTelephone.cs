using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public static class CheckTelephone
{
    public static async Task<string> Check()
    {
        var chat = UnityEngine.Object.FindAnyObjectByType<GemmaTestChat>();
        var voice = chat.GetComponent<GemmaSpeech>();
        var toggle = (Toggle)typeof(GemmaTestChat).GetField("telephoneEffect", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(chat);
        var high = voice.GetComponent<AudioHighPassFilter>();
        var low = voice.GetComponent<AudioLowPassFilter>();
        bool original = toggle.value;
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var strength = (Slider)typeof(GemmaTestChat).GetField("telephoneStrength", flags).GetValue(chat);
        var pitch = (Slider)typeof(GemmaTestChat).GetField("voicePitch", flags).GetValue(chat);
        float originalStrength = strength.value, originalPitch = pitch.value;
        try
        {
            strength.value = 50f;
            toggle.value = false;
            if (high.enabled || low.enabled || voice.TelephoneEnabled) throw new Exception("Bypass failed");
            toggle.value = true;
            if (!high.enabled || !low.enabled || Mathf.Abs(high.cutoffFrequency - 300f) > 1f || Mathf.Abs(low.cutoffFrequency - 3400f) > 1f)
                throw new Exception("Telephone filters failed: " + high.enabled + "/" + low.enabled + " " + high.cutoffFrequency + "/" + low.cutoffFrequency);
            strength.value = 100f;
            if (Mathf.Abs(high.cutoffFrequency - 650f) > 1f || Mathf.Abs(low.cutoffFrequency - 2200f) > 1f)
                throw new Exception("Maximum strength failed");
            strength.value = 0f;
            if (high.enabled || low.enabled) throw new Exception("Zero strength bypass failed");
            strength.value = 50f;
            pitch.value = -12f;
            if (Mathf.Abs(voice.GetComponent<AudioSource>().pitch - .5f) > .001f) throw new Exception("Pitch octave failed");
            pitch.value = -3f;
            for (int i = 0; i < 150 && !voice.Ready; i++) await Task.Delay(100);
            if (!voice.Ready) throw new Exception("Piper not ready");
            voice.Speak("Алло? Это Нина Петровна. Вы хорошо меня слышите?");
            for (int i = 0; i < 200 && !voice.Playing; i++) await Task.Delay(100);
            if (!voice.Playing || voice.LastRms <= 0) throw new Exception("No speech playback");
            return "PASS: UI strength 0/50/100%; pitch -12 semitones=0.5; speech playing at -3 semitones; RMS=" + voice.LastRms
                + "; filter counts=" + voice.GetComponents<AudioHighPassFilter>().Length + "/" + voice.GetComponents<AudioLowPassFilter>().Length
                + "; volume=" + voice.Volume;
        }
        finally { strength.value = originalStrength; pitch.value = originalPitch; toggle.value = original; }
    }
}
