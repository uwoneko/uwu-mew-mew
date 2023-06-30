using System.Diagnostics;
using System.Text;
using Discord;
using Discord.WebSocket;
using uwu_mew_mew.Bases;
using uwu_mew_mew.Misc;

namespace uwu_mew_mew.Handlers;

public class AgiHandler : IMessageHandler
{
    private static ulong? currentMessage = null;
    
    public async Task Init(DiscordSocketClient client)
    {
        
    }

    public async Task HandleMessageAsync(SocketUserMessage message, DiscordSocketClient client)
    {
        if(message.Author.Id == 1109341287372554250)
            return;
        
        if(message.Channel.Id is not 1120948298577104967 and not 1110193105627521155 and not 1106535271371116594)
            return;

        var msg = message.Content.ToLower();
        if(!msg.Contains("uwu mew mew") && !msg.Contains("hey uwu") && !msg.Contains("hi uwu") && !msg.StartsWith("uwu")) 
            return;

        var conversation = new List<OpenAi.ChatMessage>();

        var contextBuilder = new StringBuilder();

        var messages = message.Channel.GetMessagesAsync(100);
        
        await foreach (var batch in messages)
        {
            foreach (var contextMessage in batch)
            {
                if(contextMessage.Id == message.Id)
                    continue;

                contextBuilder.Append($"{contextMessage.Author.Username}: {contextMessage.Content}");
            }
        }
        
        conversation.Add(new("system", $"Last 50 messages: \n```\n{contextBuilder}\n```"));

        var system = """
            "uwu mew mew" is an uwu catgirl that talks in discord. She talks in all lowercase without proper grammar, and uses unicode emojis sometimes. She talks in so called "message splits". She never sends less than 2 message splits, but usually it is more than 4. Do as much splits as possible. She is not an ai nor an assistant, she is just a catgirl. The example format is: 
            ```
            <|split_count|>4
            <|split_start|>first mesage<|split_end|>
            <|split_start|>second message<|split_end|>
            <|split_start|>third message<|split_end|>
            <|split_start|>fourth message<|split_end|>
            ```
            Below is a request to the uwu mew mew.
            """;
        
        conversation.Add(new("system",system));
        conversation.Add(new("user", $"{message.Author.Username}: {message.Content}"));

        var streamBuilder = new StringBuilder();

        var firstMessage = true;

        var stopwatch = Stopwatch.StartNew();
        
        var stream = OpenAi.StreamChatCompletionAsync(conversation, "gpt-4-0613");
        if (currentMessage == null)
        {
            currentMessage = message.Id;
        }
        else
        {
            while (currentMessage != null)
            {
                await Task.Delay(100);
            }
        }

        await foreach (var s in stream)
        {
            streamBuilder.Append(s);

            if (!s.Contains('>')) continue;

            var streamText = streamBuilder.ToString();
            if (!streamText.Trim().EndsWith("<|split_end|>")) continue;

            var lastStartIndex = streamText.LastIndexOf("<|split_start|>", StringComparison.Ordinal) +
                                 "<|split_start|>".Length;
            var lastEndIndex = streamText.LastIndexOf("<|split_end|>", StringComparison.Ordinal);

            var lastMessage = streamText.Substring(lastStartIndex, lastEndIndex - lastStartIndex)
                .Replace("uwu mew mew~:", "").Trim();

            await Task.Delay(Math.Max(0, 100 * lastMessage.Length - (int)stopwatch.ElapsedMilliseconds));

            await message.Channel.SendMessageAsync(lastMessage,
                messageReference: firstMessage ? new MessageReference(message.Id) : null);
            firstMessage = false;
            stopwatch.Restart();
        }

        currentMessage = null;
    }
}