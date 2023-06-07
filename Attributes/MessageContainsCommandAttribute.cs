using Discord.WebSocket;
using uwu_mew_mew.Bases;

namespace uwu_mew_mew.Attributes;

public class MessageContainsCommandAttribute : CommandAttribute
{
    private readonly string _match;

    public MessageContainsCommandAttribute(string match, bool hideInHelp = false)
    {
        _match = match;
        HideInHelp = hideInHelp;
    }

    public override bool HideInHelp { get; }

    public override bool Matches(SocketUserMessage message, out int argPos)
    {
        argPos = 0;
        return message.Content.Contains(_match);
    }
}