using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public static class CheckPhoneChat
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static GemmaTestChat Chat => UnityEngine.Object.FindAnyObjectByType<GemmaTestChat>();
    static VisualElement Root => Chat.GetComponent<UIDocument>().rootVisualElement;
    static void Click(Button button)
    {
        var method = typeof(Clickable).GetMethod("SimulateSingleClick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method == null) throw new Exception("No Clickable simulation method");
        method.Invoke(button.clickable, new object[] { null, 0 });
    }
    static async Task Ready()
    {
        for (int i = 0; i < 400; i++)
        {
            bool ready = (bool)typeof(GemmaTestChat).GetField("ready", Flags).GetValue(Chat);
            bool busy = (bool)typeof(GemmaTestChat).GetField("busy", Flags).GetValue(Chat);
            if (ready && !busy && Chat.GetComponent<GemmaSpeech>().Ready) return;
            await Task.Delay(100);
        }
        throw new Exception("Chat readiness timeout");
    }
    public static async Task<string> Controls()
    {
        await Ready();
        var root = Root;
        var window = root.Q("phoneWindow");
        if (window == null || window.worldBound.height > root.worldBound.height + 2) throw new Exception("Window does not fit");
        if (root.Q<Image>(className: "phone-avatar").image == null) throw new Exception("Portrait missing");
        var toggle = (Toggle)typeof(GemmaTestChat).GetField("speechEnabled", Flags).GetValue(Chat);
        var sound = root.Q<Button>(className: "phone-sound");
        bool initial = toggle.value;
        Click(sound);
        if (toggle.value == initial) throw new Exception("Sound button not connected");
        Click(sound);
        if (toggle.value != initial) throw new Exception("Sound not restored");
        Click(root.Q<Button>(className: "phone-hangup"));
        if ((bool)typeof(GemmaTestChat).GetField("ready", Flags).GetValue(Chat) || Chat.GetComponent<GemmaSpeech>().Playing)
            throw new Exception("Hangup did not stop chat/speech");
        var retry = (Button)typeof(GemmaTestChat).GetField("retryButton", Flags).GetValue(Chat);
        Click(retry);
        await Ready();
        Click(root.Q<Button>(className: "phone-new"));
        await Ready();
        var mission = (CharacterTrustMission)typeof(GemmaTestChat).GetField("trustMission", Flags).GetValue(Chat);
        if (mission.Trust != 50 || root.Q<ScrollView>("conversation").contentContainer.childCount != 0) throw new Exception("New conversation did not reset");
        return "PASS: portrait; window fits; sound off/on; hangup stops workers; reconnect; new conversation resets trust/history";
    }
}
