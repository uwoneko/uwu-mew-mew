using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using uwu_mew_mew.Attributes;
using uwu_mew_mew.Bases;

namespace uwu_mew_mew.Modules;

public class OptOutModule : CommandModule
{
    public static readonly List<ulong> OptedOut;

    static OptOutModule()
    {
        try
        {
            OptedOut = JsonConvert.DeserializeObject<List<ulong>>(File.ReadAllText("data/optout.json"));
        }
        catch
        {
            OptedOut = new List<ulong>();
        }
    }

    [PrefixCommand("disable")]
    [PrefixCommand("optout", hideInHelp: true)]
    [Attributes.Summary("Disables bot reactions.")]
    public async Task OptOutAsync(SocketCommandContext context)
    {
        OptedOut.Add(context.User.Id);

        await context.Message.ReplyAsync("You opted out.");

        await File.WriteAllTextAsync("data/optout.json", JsonConvert.SerializeObject(OptedOut));
    }

    [PrefixCommand("enable")]
    [PrefixCommand("optin", hideInHelp: true)]
    [Attributes.Summary("Enables bot reactions.")]
    public async Task OptInAsync(SocketCommandContext context)
    {
        OptedOut.Remove(context.User.Id);

        await context.Message.ReplyAsync("You opted in.");
        
        await File.WriteAllTextAsync("data/optout.json", JsonConvert.SerializeObject(OptedOut));
    }
}