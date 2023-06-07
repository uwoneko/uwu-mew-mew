using Discord;
using Discord.WebSocket;

namespace uwu_mew_mew.Misc;

public static class Reply
{
    public static void WithError(SocketUserMessage message, string prompt)
    {
        WithErrorAsync(message, prompt);
    }

    private static async Task WithErrorAsync(SocketUserMessage message, string prompt)
    {
        var typing = message.Channel.EnterTypingState();
        var response = await OpenAi.AskChatAsync("You are not an assistant. Reply as a catgirl. Show actions and uwuify every response. Use emojis and keep the text readable. Keep the responses as short as possible.", prompt);
        typing.Dispose();
        
        var embed = new EmbedBuilder()
            .WithColor(Color.Red)
            .WithTitle("Error")
            .WithDescription(response)
            .WithCurrentTimestamp();

        await message.ReplyAsync(embed: embed.Build());
    }
    
    public static void WithInfo(SocketUserMessage message, string prompt)
    {
        WithInfoAsync(message, prompt);
    }

    private static async Task WithInfoAsync(SocketUserMessage message, string prompt)
    {
        var typing = message.Channel.EnterTypingState();
        var response = await OpenAi.AskChatAsync("You are not an assistant. Reply as a catgirl. Show actions and uwuify every response. Use emojis and keep the text readable. Keep the responses as short as possible.", prompt);
        typing.Dispose();
        
        var embed = new EmbedBuilder()
            .WithColor(new Color(255, 255, 255))
            .WithDescription(response)
            .WithCurrentTimestamp();

        await message.ReplyAsync(embed: embed.Build());
    }
    
    public static void WithEmbed(SocketUserMessage message, string title, string text)
    {
        WithEmbedAsync(message, title, text).Wait();
    }

    private static async Task WithEmbedAsync(SocketUserMessage message, string title, string text)
    {
        var embed = new EmbedBuilder()
            .WithColor(new Color(255, 255, 255))
            .WithTitle(title)
            .WithDescription(text)
            .WithCurrentTimestamp();

        await message.ReplyAsync(embed: embed.Build());
    }
}