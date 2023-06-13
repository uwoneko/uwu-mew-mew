using Discord.WebSocket;
using uwu_mew_mew.Bases;

namespace uwu_mew_mew.Attributes;

public class NoMatchCommandAttribute : CommandAttribute
{
    public NoMatchCommandAttribute(bool hideInHelp = false)
    {
        HideInHelp = hideInHelp;
    }

    public override bool HideInHelp { get; }

    public override bool Matches(SocketUserMessage message, out int argPos)
    {
        argPos = 0;
        return false;
    }

    public override string ToString()
    {
        return "Any command";
    }
}