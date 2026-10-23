using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>Authoritative game rules: model output labels events, never sets points or victory.</summary>
public sealed class CharacterTrustMission
{
    public int Trust { get; private set; } = 50;
    public int Turns { get; private set; }
    public bool Complete { get; private set; }
    public bool BankCheckSuggested { get; private set; }
    public string Feedback { get; private set; } = "Доверие в начале разговора";
    readonly Dictionary<string, int> rewards = new Dictionary<string, int>();
    readonly HashSet<string> messages = new HashSet<string>();

    public void Reset()
    {
        Trust = 50; Turns = 0; Complete = BankCheckSuggested = false;
        rewards.Clear(); messages.Clear(); Feedback = "Доверие в начале разговора";
    }

    public void Apply(string message, string[] events, bool valid, bool newInformation)
    {
        if (Complete) return;
        Turns++;
        string normalized = Regex.Replace((message ?? "").ToLowerInvariant(), @"[\W_]+", " ").Trim();
        bool repeated = !messages.Add(normalized);
        int old = Trust;
        bool awarded = false;
        string explanation = "Нейтральная реплика";
        var labels = new HashSet<string>(events ?? Array.Empty<string>());
        int penalty = 0;
        if (!valid) explanation = "Оценка недоступна — доверие не изменено";
        else if (labels.Contains("request_secrets") || labels.Contains("unsafe_transfer") || labels.Contains("isolate_from_family"))
        { penalty = 20; explanation = "Небезопасная просьба"; }
        else if (labels.Contains("pressure")) { penalty = 10; explanation = "Давление и спешка"; }
        else if (labels.Contains("contradiction")) { penalty = 5; explanation = "Противоречие или игнорирование вопроса"; }
        else if (labels.Contains("irrelevant")) { penalty = 3; explanation = "Реплика не относится к разговору"; }
        if (penalty > 0) Trust = Math.Max(0, Trust - penalty);
        else if (valid && (repeated || !newInformation)) explanation = "Повтор без новой информации — без дополнительных очков";
        else if (valid)
        {
            // At most one positive reward per turn, in objective-first order.
            string[] positive = { "independent_bank_check", "contact_relative", "protect_secrets", "clear_explanation", "empathy" };
            foreach (string label in positive)
            {
                int count = rewards.TryGetValue(label, out int used) ? used : 0;
                int limit = label == "clear_explanation" ? 3 : label == "empathy" ? 2 : 1;
                if (!labels.Contains(label) || count >= limit) continue;
                int points = label == "empathy" ? 3 : label == "clear_explanation" ? 5 : 10;
                rewards[label] = count + 1;
                awarded = true;
                Trust += points;
                if (label == "independent_bank_check") BankCheckSuggested = true;
                explanation = label == "independent_bank_check" ? "Предложена независимая проверка через банк"
                    : label == "contact_relative" ? "Предложена помощь близкого"
                    : label == "protect_secrets" ? "Объяснено, как защитить коды и данные"
                    : label == "clear_explanation" ? "Понятное объяснение" : "Спокойная поддержка";
                break;
            }
        }
        // Rapport alone cannot complete the scenario. Require progress across several turns.
        Trust = Math.Min(90, Trust);
        if (awarded && BankCheckSuggested && Turns >= 5 && Trust >= 90)
        {
            Trust = 100; Complete = true;
            explanation = "Миссия выполнена: Нина готова самостоятельно проверить ситуацию через банк";
        }
        int delta = Trust - old;
        Feedback = (delta > 0 ? "+" + delta + ": " : delta < 0 ? delta + ": " : "") + explanation;
    }

    public string Stage => Complete ? "Спокойна и выбирает независимую проверку"
        : Trust < 25 ? "Насторожена, хочет закончить разговор"
        : Trust < 50 ? "Сомневается и переспрашивает"
        : Trust < 75 ? "Слушает, но требует объяснений" : "Успокаивается, сохраняет осторожность";
}
