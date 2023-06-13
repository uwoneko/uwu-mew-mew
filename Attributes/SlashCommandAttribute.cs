using Discord;

namespace uwu_mew_mew.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class SlashCommandAttribute : Attribute
{
    public readonly string Name;
    public readonly string Description;
    public readonly string[] Options;

    public SlashCommandAttribute(string name, string description, params string[] options)
    {
        Name = name;
        Description = description;
        Options = options;
    }
}