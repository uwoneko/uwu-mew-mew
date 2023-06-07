namespace uwu_mew_mew_bot.Misc;

[Serializable]
public class TextMatch
{
    public TextMatch(string text, bool plainMatch = false)
    {
        Text = text;
        PlainMatch = plainMatch;
    }

    public string Text { get; }
    public float[]? Embedding { get; set; }
    public bool PlainMatch { get; set; }

    public static implicit operator TextMatch(string text)
    {
        return new TextMatch(text);
    }

    public static implicit operator TextMatch((string text, bool plainMatch) tuple)
    {
        return new TextMatch(tuple.text, tuple.plainMatch);
    }
}