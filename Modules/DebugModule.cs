using System.Globalization;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI_API.Chat;
using uwu_mew_mew.Attributes;
using uwu_mew_mew.Bases;
using uwu_mew_mew.Misc;

 
namespace uwu_mew_mew.Modules;

public class DebugModule : CommandModule
{
    [PrefixCommand("embeddingdistance")]
    public async Task DistanceAsync(SocketCommandContext context, string message)
    {
        var json = JArray.Parse(message);
        await context.Message.ReplyAsync(Embeddings.Distance(json[0].ToString(), json[1].ToString()).ToString(CultureInfo.InvariantCulture));
    }

    [PrefixCommand("functiontest")]
    public async Task FunctionAsync(SocketCommandContext context, string message)
    {
        var function = JObject.Parse("""
        {
            "name": "get_current_weather",
            "description": "Get the current weather in a given location",
            "parameters": {
                "type": "object",
                "properties": {
                    "location": {
                        "type": "string",
                        "description": "The city and state, e.g. San Francisco, CA"
                    },
                    "unit": {
                        "type": "string",
                        "enum": ["celsius", "fahrenheit"]
                    }
                },
                "required": ["location"]
            }
        }
        """);

        var result = await OpenAi.CreateChatCompletionAsync(new List<OpenAi.ChatMessage>()
        {
            new("user", "What is the weather like in boston?")
        }, functions: new[]{function});
        await context.Message.ReplyAsync(result.finishReason);
        await context.Message.ReplyAsync(result.functionCall);
    }
}