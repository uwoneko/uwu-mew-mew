using System.Diagnostics;
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

    [PrefixCommand("break")]
    public async Task BreakAsync(SocketCommandContext context)
    {
        if(context.User.Id == 687600977830084696)
             Debugger.Break();
    }

    [PrefixCommand("react")]
    public async Task ReactAsync(SocketCommandContext context, string message)
    {
        var targetMessage = await context.Client.GetGuild(ulong.Parse(message.Split('/')[4]))
            .GetTextChannel(ulong.Parse(message.Split('/')[5]))
            .GetMessageAsync(ulong.Parse(message.Split('/')[6]));
        await targetMessage.AddReactionAsync(Emoji.Parse(message.Trim().Split(' ')[1]));
    }

    [PrefixCommand("functiontest")]
    public async Task FunctionAsync(SocketCommandContext context, string message)
    {
        var function = JObject.Parse("""
        {
            "name": "generate_image",
            "description": "Generates an image and returns it to the user",
            "parameters": {
                "type": "object",
                "properties": {
                    "prompt": {
                        "type": "string",
                        "description": "Prompt to generate an image from. Example: 'cat, cute, fluffy'"
                    },
                    "cfg": {
                        "type": "number",
                        "description": "Classifier Free Guidance scale, how much the model should respect your prompt. Optional."
                    },
                    "sampling_steps": {
                        "type": "number",
                        "description": "Number of sampling steps. The more the better, but longer. Max is 100. Optional."
                    }
                },
                "required": ["prompt"]
            }
        }
        """);

        var result = OpenAi.StreamChatCompletionAsyncWithFunctions(new List<OpenAi.ChatMessage>
        {
            new("user", "Draw me a picture of a cat.")
        }, functions: new[]{function});
        var functionCall = false;
        var functionArguments = "";
        await foreach (var valueTuple in result)
        {
            if (valueTuple.functionCall != null && !functionCall)
            {
                await context.Message.ReplyAsync($"ayo a function \"{JObject.Parse(valueTuple.functionCall)["name"]}\" up ahead");
                functionCall = true;
                continue;
            }

            if (valueTuple.functionCall != null && functionCall)
            {
                functionArguments += JObject.Parse(valueTuple.functionCall)["arguments"];
                await context.Message.ReplyAsync(functionArguments);
            }
        }
    }
}