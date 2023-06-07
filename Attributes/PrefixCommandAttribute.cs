using Discord.Commands;
using Discord.WebSocket;
using CommandAttribute = uwu_mew_mew.Bases.CommandAttribute;

namespace uwu_mew_mew.Attributes;

public class PrefixCommandAttribute : CommandAttribute
{
    private const char Prefix = '%';

    private readonly string _match;

    public PrefixCommandAttribute(string name, bool hideInHelp = false)
    {
        _match = Prefix + name;
        HideInHelp = hideInHelp;
    }

    public override bool HideInHelp { get; }

    public override bool Matches(SocketUserMessage message, out int argPos)
    {
        argPos = 0;
        return message.HasStringPrefix(_match, ref argPos);
    }

    public override string ToString()
    {
        return _match;
    }
}