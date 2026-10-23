using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public static class CheckCharacterChat
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    public static async Task<string> WaitReady()
    {
        var chat = UnityEngine.Object.FindAnyObjectByType<GemmaTestChat>();
        for (int i = 0; i < 500; i++)
        {
            var speech = chat.GetComponent<GemmaSpeech>();
            bool ready = (bool)typeof(GemmaTestChat).GetField("ready", Flags).GetValue(chat);
            if (ready && speech != null && speech.Ready)
            {
                var root = chat.GetComponent<UIDocument>().rootVisualElement;
                if (root.Q<PopupField<string>>("characterChoice") == null || root.Q<TextField>("messageInput") == null)
                    throw new Exception("Missing chat controls");
                if (root.Q<Slider>("voiceRate") != null) throw new Exception("Tuning controls leaked into compact scene");
                if (!speech.VoicePath.EndsWith("femalecommoner.onnx") || Mathf.Abs(speech.PitchSemitones + 1.5f) > .001f)
                    throw new Exception("Wrong character voice profile");
                return "PASS: automatic Gemma + Piper load; character selector; compact UI; Nina voice profile";
            }
            await Task.Delay(100);
        }
        throw new Exception("Automatic loading timeout");
    }

    public static async Task<string> SendAndCheckSpeech()
    {
        var chat = UnityEngine.Object.FindAnyObjectByType<GemmaTestChat>();
        var root = chat.GetComponent<UIDocument>().rootVisualElement;
        root.Q<TextField>("messageInput").value = "Алло, здравствуйте! Как вас зовут?";
        typeof(GemmaTestChat).GetMethod("SendMessage", Flags).Invoke(chat, null);
        var speech = chat.GetComponent<GemmaSpeech>();
        bool streamed = false;
        for (int i = 0; i < 550; i++)
        {
            bool busy = (bool)typeof(GemmaTestChat).GetField("busy", Flags).GetValue(chat);
            if (speech.Playing && busy) streamed = true;
            if (speech.Playing && speech.LastRms > 0)
            {
                var answer = (Label)typeof(GemmaTestChat).GetField("answer", Flags).GetValue(chat);
                return "PASS: character speech playing; Gemma still generating=" + busy + "; streamed=" + streamed
                    + "; RMS=" + speech.LastRms + "; answer=" + answer.text;
            }
            await Task.Delay(100);
        }
        throw new Exception("No spoken Gemma response");
    }

    public static async Task<string> CheckCharacterReset()
    {
        var chat = UnityEngine.Object.FindAnyObjectByType<GemmaTestChat>();
        for (int i = 0; i < 500 && (bool)typeof(GemmaTestChat).GetField("busy", Flags).GetValue(chat); i++)
            await Task.Delay(100);
        var workerField = typeof(GemmaTestChat).GetField("worker", Flags);
        var worker = (System.Diagnostics.Process)workerField.GetValue(chat);
        int pid = worker.Id;
        var selected = typeof(GemmaTestChat).GetField("selectedCharacter", Flags).GetValue(chat);
        typeof(GemmaTestChat).GetMethod("SelectCharacter", Flags).Invoke(chat, new[] { selected });
        for (int i = 0; i < 100 && (bool)typeof(GemmaTestChat).GetField("busy", Flags).GetValue(chat); i++)
            await Task.Delay(100);
        if ((bool)typeof(GemmaTestChat).GetField("busy", Flags).GetValue(chat)) throw new Exception("Character reset timeout");
        if (((System.Diagnostics.Process)workerField.GetValue(chat)).Id != pid) throw new Exception("Model unnecessarily reloaded");
        var root = chat.GetComponent<UIDocument>().rootVisualElement;
        if (root.Q<ScrollView>("conversation").contentContainer.childCount != 0) throw new Exception("Old history retained");
        if (chat.GetComponent<GemmaSpeech>().Playing) throw new Exception("Old speech retained");
        return "PASS: character role reset without reloading Gemma; history and old speech cleared";
    }
}
