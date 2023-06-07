using Discord.WebSocket;

namespace uwu_mew_mew.Bases;

public interface IMessageHandler
{
    public Task Init(DiscordSocketClient client);

    public Task HandleMessageAsync(SocketUserMessage message, DiscordSocketClient client);
}