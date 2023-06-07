namespace uwu_mew_mew.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class ParametersAttribute : Attribute
{
    public readonly string Text;

    public ParametersAttribute(string text)
    {
        Text = text;
    }
}