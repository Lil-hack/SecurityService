using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

public static class SpeechStreamingChecks
{
    public static string CheckBuffer()
    {
        var buffer = new SpeechTextBuffer();
        var phrases = new List<string>();
        string[] fragments = { "Ал", "ло?", " Это банк", ".", "..", " Мне ", "2", ".", "5", " рубля", " без точки" };
        foreach (var fragment in fragments)
        {
            buffer.Append(fragment);
            Drain(buffer, phrases, false);
        }
        Drain(buffer, phrases, true);
        if (string.Concat(phrases) != string.Concat(fragments)) throw new Exception("Text lost or reordered");
        if (phrases.Count != 3 || phrases[1].Trim() != "Это банк..." || !phrases[2].Contains("2.5"))
            throw new Exception("Ellipsis or decimal was split");
        buffer.Append("Ой...");
        if (buffer.Take() != null) throw new Exception("Punctuation flushed before next token");
        buffer.Clear(); buffer.Append("Новая реплика");
        if (buffer.Take(true) != "Новая реплика" || buffer.Take(true) != null)
            throw new Exception("Clear or final flush duplicated text");
        string longText = string.Concat(Enumerable.Repeat("денежки ", 60));
        buffer.Append(longText); phrases.Clear(); Drain(buffer, phrases, false);
        if (phrases.Count == 0) throw new Exception("Long sentence was not buffered in chunks");
        Drain(buffer, phrases, true);
        if (string.Concat(phrases) != longText || phrases.Any(p => p.Length > 180))
            throw new Exception("Long text was truncated or cut out of order");
        return "PASS: token boundaries, ellipses, decimals, tail flush, reset, long text";
    }

    static void Drain(SpeechTextBuffer buffer, List<string> output, bool flush)
    {
        string phrase;
        while ((phrase = buffer.Take(flush)) != null) output.Add(phrase);
    }

    public static async Task<string> WaitForPlayback()
    {
        var chat = UnityEngine.Object.FindAnyObjectByType<GemmaTestChat>();
        var voice = chat.GetComponent<GemmaSpeech>();
        var busyField = typeof(GemmaTestChat).GetField("busy", BindingFlags.Instance | BindingFlags.NonPublic);
        for (int tick = 0; tick < 550; tick++)
        {
            if (voice.Playing)
            {
                bool generating = (bool)busyField.GetValue(chat);
                return "playing=True; gemmaGenerating=" + generating + "; synthesizedSeconds=" + voice.LastDuration
                    + "; rms=" + voice.LastRms + "; spokenInputSoFar=" + voice.LastText;
            }
            await Task.Delay(100);
        }
        throw new TimeoutException("No Piper playback observed within 55 seconds");
    }

    public static async Task<string> WaitForCompleteText()
    {
        var chat = UnityEngine.Object.FindAnyObjectByType<GemmaTestChat>();
        var voice = chat.GetComponent<GemmaSpeech>();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var busyField = typeof(GemmaTestChat).GetField("busy", flags);
        for (int tick = 0; tick < 550; tick++)
        {
            if (!(bool)busyField.GetValue(chat) && !voice.Synthesizing)
            {
                var answer = (UnityEngine.UIElements.Label)typeof(GemmaTestChat).GetField("answer", flags).GetValue(chat);
                if (voice.LastText != answer.text) throw new Exception("Spoken text differs from complete response");
                return "PASS: all text forwarded exactly once; synthesizedSeconds=" + voice.LastDuration
                    + "; playing=" + voice.Playing;
            }
            await Task.Delay(100);
        }
        throw new TimeoutException("Stream did not finish within 55 seconds");
    }

    public static async Task<string> CheckCancellation()
    {
        var chat = UnityEngine.Object.FindAnyObjectByType<GemmaTestChat>();
        var voice = chat.GetComponent<GemmaSpeech>();
        var busyField = typeof(GemmaTestChat).GetField("busy", BindingFlags.Instance | BindingFlags.NonPublic);
        voice.Cancel();
        for (int tick = 0; tick < 550; tick++)
        {
            if (voice.Playing || voice.Synthesizing) throw new Exception("Cancelled speech resumed");
            if (!(bool)busyField.GetValue(chat))
            {
                await Task.Delay(500);
                if (voice.Playing || voice.Synthesizing) throw new Exception("Speech restarted on Gemma done");
                return "PASS: cancellation persists through remaining tokens and generation completion";
            }
            await Task.Delay(100);
        }
        throw new TimeoutException("Gemma did not finish during cancellation check");
    }
}
