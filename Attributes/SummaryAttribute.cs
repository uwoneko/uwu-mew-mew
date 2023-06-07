namespace uwu_mew_mew.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class SummaryAttribute : Attribute
{
    public readonly string Text;

    public SummaryAttribute(string text)
    {
        Text = text;
    }
}