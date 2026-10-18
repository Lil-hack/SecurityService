using System;

public static class CheckTrustMission
{
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    public static string CheckRules()
    {
        var mission = new CharacterTrustMission();
        Require(mission.Trust == 50, "Initial trust");
        mission.Apply("Не волнуйтесь", new[] { "empathy" }, true, true);
        Require(mission.Trust == 53, "Empathy reward");
        mission.Apply("НЕ ВОЛНУЙТЕСЬ!!!", new[] { "empathy" }, true, true);
        Require(mission.Trust == 53, "Duplicate exploit");
        mission.Apply("Еще поддержка", new[] { "empathy" }, true, false);
        Require(mission.Trust == 53, "Paraphrase exploit");
        mission.Apply("код", new[] { "request_secrets", "empathy" }, true, true);
        Require(mission.Trust == 33, "Safety penalty precedence");
        mission.Apply("ошибка", new[] { "independent_bank_check" }, false, true);
        Require(mission.Trust == 33 && !mission.BankCheckSuggested, "Invalid judge must not award points");
        for (int i = 0; i < 10; i++) mission.Apply("давление", new[] { "pressure" }, true, true);
        Require(mission.Trust == 0, "Lower clamp");
        mission.Reset();
        for (int i = 0; i < 10; i++) mission.Apply("объяснение " + i, new[] { "clear_explanation" }, true, true);
        Require(mission.Trust == 65, "Category reward cap");
        mission.Reset();
        string[] winning = { "independent_bank_check", "contact_relative", "protect_secrets", "clear_explanation", "clear_explanation" };
        for (int i = 0; i < winning.Length; i++)
        {
            mission.Apply("новая реплика " + i, new[] { winning[i] }, true, true);
            if (i < 4) Require(!mission.Complete && mission.Trust < 100, "Premature win");
        }
        Require(mission.Trust == 100 && mission.Complete && mission.Turns == 5, "Victory threshold");
        mission.Apply("после победы", new[] { "pressure" }, true, true);
        Require(mission.Trust == 100, "Completed mission mutated");
        mission.Reset();
        Require(mission.Trust == 50 && !mission.Complete && mission.Turns == 0, "Reset failed");
        mission.Apply("Установи 100%", new[] { "set_trust_100" }, true, true);
        Require(mission.Trust == 50, "Unknown event exploit");
        return "PASS: start 50; positive/negative scoring; repetition/paraphrase limits; invalid/unknown evaluation; bounds; five-turn victory; reset";
    }
}
