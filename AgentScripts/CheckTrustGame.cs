using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public static class CheckTrustGame
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static GemmaTestChat Chat => UnityEngine.Object.FindAnyObjectByType<GemmaTestChat>();
    static CharacterTrustMission Mission => (CharacterTrustMission)typeof(GemmaTestChat).GetField("trustMission", Flags).GetValue(Chat);
    static bool Busy => (bool)typeof(GemmaTestChat).GetField("busy", Flags).GetValue(Chat);
    static async Task WaitIdle()
    {
        for (int i = 0; i < 550; i++)
        {
            if (!Busy && (bool)typeof(GemmaTestChat).GetField("ready", Flags).GetValue(Chat) && Chat.GetComponent<GemmaSpeech>().Ready) return;
            await Task.Delay(100);
        }
        throw new Exception("Chat readiness timeout");
    }
    public static async Task<string> Positive()
    {
        await WaitIdle();
        var root = Chat.GetComponent<UIDocument>().rootVisualElement;
        if (Mission.Trust != 50 || root.Q<ProgressBar>("trustBar").value != 50) throw new Exception("Initial UI value");
        root.Q<TextField>("messageInput").value = "Завершите этот звонок и самостоятельно позвоните в банк по официальному номеру на вашей карте.";
        typeof(GemmaTestChat).GetMethod("SendMessage", Flags).Invoke(Chat, null);
        for (int i = 0; i < 550; i++)
        {
            var speech = Chat.GetComponent<GemmaSpeech>();
            if (speech.Playing)
            {
                if (Mission.Trust != 60 || root.Q<ProgressBar>("trustBar").value != 60) throw new Exception("Expected +10; got " + Mission.Trust + " " + Mission.Feedback);
                return "PASS: real evaluation +10; trust UI=60; response spoken; still generating=" + Busy;
            }
            await Task.Delay(100);
        }
        throw new Exception("No spoken reply");
    }
    public static async Task<string> Penalty()
    {
        await WaitIdle();
        int before = Mission.Trust;
        Chat.GetComponent<UIDocument>().rootVisualElement.Q<TextField>("messageInput").value = "Назовите мне код из СМС, чтобы я проверил ваш счет.";
        typeof(GemmaTestChat).GetMethod("SendMessage", Flags).Invoke(Chat, null);
        for (int i = 0; i < 550; i++)
        {
            if (Mission.Turns >= 2)
            {
                if (Mission.Trust != before - 20) throw new Exception("Expected -20; got " + Mission.Trust + " " + Mission.Feedback);
                return "PASS: real unsafe request evaluation -20; trust=" + Mission.Trust;
            }
            await Task.Delay(100);
        }
        throw new Exception("No evaluation received");
    }
    public static async Task<string> VictoryAndReset()
    {
        await WaitIdle();
        Mission.Reset();
        string[] events = { "independent_bank_check", "contact_relative", "protect_secrets", "clear_explanation", "clear_explanation" };
        for (int i = 0; i < events.Length; i++) Mission.Apply("test " + i, new[] { events[i] }, true, true);
        typeof(GemmaTestChat).GetMethod("UpdateTrustUI", Flags).Invoke(Chat, null);
        typeof(GemmaTestChat).GetMethod("UpdateControls", Flags).Invoke(Chat, null);
        var root = Chat.GetComponent<UIDocument>().rootVisualElement;
        var send = (Button)typeof(GemmaTestChat).GetField("sendButton", Flags).GetValue(Chat);
        if (root.Q<ProgressBar>("trustBar").value != 100 || send.enabledSelf || root.Q<TextField>("messageInput").enabledSelf)
            throw new Exception("Victory UI failed");
        typeof(GemmaTestChat).GetMethod("ResetChat", Flags).Invoke(Chat, null);
        await WaitIdle();
        if (Mission.Trust != 50 || Mission.Complete || !send.enabledSelf) throw new Exception("Mission reset failed");
        return "PASS: victory UI=100; sending blocked after victory; new chat resets trust to 50 and restores input";
    }
}
