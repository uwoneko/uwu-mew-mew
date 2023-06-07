using Discord.Commands;
using Discord.WebSocket;
using CommandAttribute = uwu_mew_mew.Bases.CommandAttribute;

namespace uwu_mew_mew.Attributes;

public class PingCommandAttribute : CommandAttribute
{
    public PingCommandAttribute(bool hideInHelp = false)
    {
        HideInHelp = hideInHelp;
    }

    public override bool HideInHelp { get; }

    public override bool Matches(SocketUserMessage message, out int argPos)
    {
        argPos = 0;
        return message.HasStringPrefix("<@1109341287372554250>", ref argPos) 
               || message.Content.Contains("<@1109341287372554250>");
    }

    public override string ToString()
    {
        return "@uwu mew mew~";
    }
}