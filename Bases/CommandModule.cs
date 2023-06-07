using Discord.WebSocket;

namespace uwu_mew_mew.Bases;

public abstract class CommandModule
{
    public virtual async Task Init(DiscordSocketClient client)
    {
    }
}