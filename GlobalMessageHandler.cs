using Discord.WebSocket;
using uwu_mew_mew.Bases;
using uwu_mew_mew.Handlers;

namespace uwu_mew_mew;

public class GlobalMessageHandler
{
    private readonly DiscordSocketClient _client;
    private List<IMessageHandler> _handlers = new()
    {
        new CommandMessageHandler(),
        new ReactionHandler(),
        new AgiHandler()
    };

    public GlobalMessageHandler(DiscordSocketClient client)
    {
        _client = client;
    }

    public async Task InstallAsync()
    {
        foreach (var handler in _handlers) await handler.Init(_client);

        _client.MessageReceived += HandleMessage;
    }

    private async Task HandleMessage(SocketMessage messageParam)
    {
        Task.Run(() => HandleMessageAsync(messageParam));
    }

    private void HandleMessageAsync(SocketMessage messageParam)
    {
        var message = messageParam as SocketUserMessage;

        if (message == null) return;

        foreach (var messageHandler in _handlers) messageHandler.HandleMessageAsync(message, _client);
    }
}