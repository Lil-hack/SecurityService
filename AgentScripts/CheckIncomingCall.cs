using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public static class CheckIncomingCall
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static GemmaTestChat Chat => UnityEngine.Object.FindAnyObjectByType<GemmaTestChat>();
    static VisualElement Root => Chat.GetComponent<UIDocument>().rootVisualElement;
    static PhoneCallState State => (PhoneCallState)typeof(GemmaTestChat).GetField("callState", Flags).GetValue(Chat);
    static void Click(string name)
    {
        var button = Root.Q<Button>(name);
        typeof(Clickable).GetMethod("SimulateSingleClick", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Invoke(button.clickable, new object[] { null, 0 });
    }
    public static async Task<string> Intro()
    {
        if (State != PhoneCallState.Incoming) throw new Exception("Expected incoming screen");
        if (Root.Q<ScrollView>("conversation").contentContainer.childCount != 0 || Chat.GetComponent<GemmaSpeech>().Playing)
            throw new Exception("Speech/history before acceptance");
        Click("declineCall");
        if (State != PhoneCallState.Ended || (bool)typeof(GemmaTestChat).GetField("ready", Flags).GetValue(Chat))
            throw new Exception("Decline failed");
        Click("answerCall"); // New incoming call, starting resource load.
        if (State != PhoneCallState.Incoming) throw new Exception("New call failed");
        Click("answerCall"); // Answer immediately, before resources necessarily finish loading.
        if (State != PhoneCallState.Speaking || !Chat.GetComponent<GemmaSpeech>().Playing)
            throw new Exception("Prepared greeting did not start immediately");
        typeof(GemmaTestChat).GetMethod("AnswerIncomingCall", Flags).Invoke(Chat, null); // Double-answer guard.
        for (int i = 0; i < 550; i++)
        {
            var speech = Chat.GetComponent<GemmaSpeech>();
            if (speech.Playing)
            {
                var history = Root.Q<ScrollView>("conversation");
                var mission = (CharacterTrustMission)typeof(GemmaTestChat).GetField("trustMission", Flags).GetValue(Chat);
                if (history.contentContainer.childCount != 1 || mission.Trust != 50 || mission.Turns != 0)
                    throw new Exception("Opening counted as player action or duplicated");
                if (Root.Q<TextField>("messageInput").enabledSelf || State != PhoneCallState.Speaking)
                    throw new Exception("Player input enabled before opening finishes");
                return "PASS: incoming; decline; restart; immediate prepared greeting while loading; one character reply; trust=50; turns=0; text=" + speech.LastText;
            }
            await Task.Delay(100);
        }
        throw new Exception("No spoken opening line");
    }
    public static async Task<string> FinishAndReset()
    {
        for (int i = 0; i < 550 && State != PhoneCallState.Active; i++) await Task.Delay(100);
        if (State != PhoneCallState.Active || !Root.Q<TextField>("messageInput").enabledSelf)
            throw new Exception("Player turn not unlocked after opening speech");
        if (Chat.GetComponent<GemmaSpeech>().HasPendingSpeech) throw new Exception("Input unlocked too early");
        typeof(GemmaTestChat).GetMethod("ResetChat", Flags).Invoke(Chat, null);
        if (State != PhoneCallState.Incoming || Chat.GetComponent<GemmaSpeech>().Playing
            || Root.Q<ScrollView>("conversation").contentContainer.childCount != 0)
            throw new Exception("New call state/reset failed");
        return "PASS: input unlocks after opening speech; New returns to incoming and clears history/audio";
    }
}
