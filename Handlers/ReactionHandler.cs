using Discord;
using Discord.WebSocket;
using MathNet.Numerics;
using OpenAI_API.Embedding;
using uwu_mew_mew.Bases;
using uwu_mew_mew.Misc;

namespace uwu_mew_mew.Handlers;

public class ReactionHandler : IMessageHandler
{
    public static readonly TextMatch[] ThumbupMessages =
    {
        "uwu",
        "owo",
        "mew",
        "nya",
        "femboy",
        "baka",
        "minor sex",
        "say gex",
        "thank you",
        "true",
        ("skill issue", true)
    };

    public static readonly TextMatch[] HeartMessages =
    {
        "yura",
        "yuki",
        "lordpandaspace",
        "b4",
        "anato",
        "nyx",
        "sbgpt",
        ("catgpt", true),
        "trent",
        "fussel",
        "openai",
        "nintendo",
        "apples",
        "i love kissing boys"
    };

    public static readonly TextMatch[] PleadingMessages =
    {
        "can you",
        "please",
        "can i have",
        "would you mind"
    };

    public static readonly TextMatch[] CoolMessages =
    {
        "broke",
        "cool",
        "i am the best",
        ("skill issue", true)
    };

    public static readonly TextMatch[] SkullMessages =
    {
        ("skull", true)
    };

    public static readonly TextMatch[] CryMessages =
    {
        "qwq",
        "crying",
        "i can't"
    };

    public static readonly TextMatch[] JoyMessages =
    {
        "haha",
        "lawl",
        "is chatgpt down",
        "trading algorithm"
    };

    public static List<(TextMatch[] messages, string emoji)> Reactions =
        new()
        {
            (ThumbupMessages, ":thumbsup:"),
            (HeartMessages, ":heart:"),
            (PleadingMessages, ":pleading_face:"),
            (CoolMessages, ":sunglasses:"),
            (SkullMessages, ":skull:"),
            (CryMessages, ":cry:"),
            (JoyMessages, ":joy:")
        };

    public async Task Init(DiscordSocketClient client)
    {
        if (File.Exists("data/reaction_cache.bin"))
        {
            Reactions = (List<(TextMatch[] messages, string emoji)>)
                BinarySerializerConvert.Deserialize(
                    await File.ReadAllBytesAsync("data/reaction_cache.bin"));

            return;
        }

        foreach (var reaction in Reactions)
        {
            var taskList = new Task<EmbeddingResult>[reaction.messages.Length];

            for (var i = 0; i < reaction.messages.Length; i++)
                taskList[i] = OpenAi.Api.Embeddings
                    .CreateEmbeddingAsync(reaction.messages[i].Text);

            var results = await Task.WhenAll(taskList);
            for (var i = 0; i < results.Length; i++) reaction.messages[i].Embedding = results[i];
        }

        await File.WriteAllBytesAsync("data/reaction_cache.bin",
            BinarySerializerConvert.Serialize(Reactions));
    }

    public async Task HandleMessageAsync(SocketUserMessage message, DiscordSocketClient client)
    {
        var result = await OpenAi.Api.Embeddings
            .CreateEmbeddingAsync(message.Content.ToLower().Trim());
        var embedding = result.Data.First().Embedding!;

        foreach (var emoji in Reactions)
        {
            var any = false;
            foreach (var m in emoji.messages)
            {
                if (Distance.Cosine(embedding, m.Embedding) < 0.14f) any = true;
                if (m.PlainMatch && message.Content.Contains(m.Text)) any = true;
            }

            if (message.Content.ToLower().Trim().Contains(Emoji.Parse(emoji.emoji).ToString()))
                any = true;

            try
            {
                if (any) await message.AddReactionAsync(Emoji.Parse(emoji.emoji));
            }
            catch
            {
                // return
                return;
            }
        }

        if (Random.Shared.NextDouble() * 30 < 1)
        {
            var emojis = (await OpenAi.AskChatAsync(
                    "Send 1 emoji as reaction to input message from perspective of a catgirl without anything else",
                    $"Message: \"{message.Content}\"", 1.3f, 3))
                .Replace(" ", "");

            for (var i = 0; i < emojis.Length; i += 2)
                await message.AddReactionAsync(Emoji.Parse(emojis.Substring(i, 2)));
        }
    }
}