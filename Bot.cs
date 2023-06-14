using System.Diagnostics;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using uwu_mew_mew;

namespace uwu_mew_mew;

public class Bot
{
    private static readonly string Token = Environment.GetEnvironmentVariable("discord-token")!;
    
    public async Task RunAsync()
    {
        var config = new DiscordSocketConfig
        { GatewayIntents =   GatewayIntents.AllUnprivileged 
                           | GatewayIntents.MessageContent
                           | GatewayIntents.GuildMembers
        };
        var _discord = new DiscordSocketClient(config);
        var _messageHandler = new GlobalMessageHandler(_discord);

        await _discord.LoginAsync(TokenType.Bot, Token);
        await _discord.StartAsync();

        await _discord.SetStatusAsync(UserStatus.Online);

        await _messageHandler.InstallAsync();
        
        Console.WriteLine("Initialized.");

        await Task.Delay(-1);
    }
}