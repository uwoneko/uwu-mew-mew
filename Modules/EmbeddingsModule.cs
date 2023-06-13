using System.Globalization;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using uwu_mew_mew_2.Misc;
using uwu_mew_mew.Attributes;
using uwu_mew_mew.Bases;

namespace uwu_mew_mew.Modules;

public class EmbeddingsModule : CommandModule
{
    [PrefixCommand("embeddingdistance")]
    public async Task DistanceAsync(SocketCommandContext context, string message)
    {
        var json = JArray.Parse(message);
        await context.Message.ReplyAsync(Embeddings.Distance(json[0].ToString(), json[1].ToString()).ToString(CultureInfo.InvariantCulture));
    }
}