using Discord.WebSocket;

namespace uwu_mew_mew.Bases;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public abstract class CommandAttribute : Attribute
{
    public abstract bool HideInHelp { get; }

    public abstract bool Matches(SocketUserMessage message, out int argPos);
}