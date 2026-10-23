using System.Text;

/// <summary>Buffers token fragments until a phrase can be spoken without cutting a word.</summary>
public sealed class SpeechTextBuffer
{
    readonly StringBuilder text = new StringBuilder();
    public void Append(string fragment) { if (!string.IsNullOrEmpty(fragment)) text.Append(fragment); }
    public void Clear() => text.Clear();

    public string Take(bool flush = false)
    {
        int end = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char character = text[i];
            if (character == '\n') { end = i + 1; break; }
            if (character != '.' && character != '!' && character != '?' && character != '…') continue;
            // Wait for the next token after punctuation. This preserves ellipses,
            // punctuation groups, closing quotes, and decimal numbers such as 2.5.
            int next = i + 1;
            while (next < text.Length && ".!?…\"»)”".IndexOf(text[next]) >= 0) next++;
            if (next < text.Length && char.IsWhiteSpace(text[next])) { end = next; break; }
            if (next == text.Length && flush) { end = next; break; }
            i = next - 1;
        }
        // A long sentence must not postpone speech until the entire response ends.
        if (end == 0 && text.Length >= 180)
            for (int i = 179; i >= 80; i--)
                if (char.IsWhiteSpace(text[i])) { end = i + 1; break; }
        if (end == 0 && flush) end = text.Length;
        if (end == 0) return null;
        string phrase = text.ToString(0, end);
        text.Remove(0, end);
        return phrase;
    }
}
