using System.Reflection;
using System.Text;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using uwu_mew_mew.Attributes;
using uwu_mew_mew.Bases;
using uwu_mew_mew.Modules;
using Color = Discord.Color;
using CommandAttribute = uwu_mew_mew.Bases.CommandAttribute;
using SummaryAttribute = uwu_mew_mew.Attributes.SummaryAttribute;

namespace uwu_mew_mew.Handlers;

public class CommandMessageHandler : IMessageHandler
{
    private static readonly List<Type> ModuleTypes = new()
    {
        typeof(AiCommandModule),
        typeof(AiModule),
        typeof(StableDiffusionModule),
        typeof(DebugModule),
        typeof(DumpModule),
        typeof(OptOutModule),
        typeof(StatusModule)
    };

    private static readonly Dictionary<Type, CommandModule> Modules = new();

    private static readonly List<(CommandAttribute attribute, MethodInfo methodInfo)> Commands = new();
    
    private static readonly List<(SlashCommandAttribute attribute, MethodInfo methodInfo)> SlashCommands = new();

    private DiscordSocketClient _client;
    
    public async Task Init(DiscordSocketClient client)
    {
        _client = client;
        
        foreach (var type in ModuleTypes)
        {
            var methodInfos = type.GetMethods();
            var commands = methodInfos
                .Where(m => m.GetCustomAttributes(typeof(CommandAttribute), true).Any());
            foreach (var command in commands)
            {
                var attributes =
                    command.GetCustomAttributes(typeof(CommandAttribute), true)
                        .Cast<CommandAttribute>();

                foreach (var commandAttribute in attributes) Commands.Add((commandAttribute, command));
            }

            Modules[type] = (CommandModule)Activator.CreateInstance(type) ??
                             throw new Exception($"Could not instantiate module {type.Name}");
            await Modules[type].Init(client);
        }
        
        client.Ready += () => ClientOnReady(client);
        client.SlashCommandExecuted += HandleSlashCommand;
    }

    private async Task HandleSlashCommand(SocketSlashCommand command)
    {
        if(command.ChannelId == 1050422061803245600)
            return;
        
        await command.DeferAsync();

        foreach (var (commandAttribute, methodInfo) in SlashCommands.DistinctBy(c => c.attribute.Name))
        {
            if (command.Data.Name != commandAttribute.Name) continue;
            
            switch (methodInfo.GetParameters().Length)
            {
                case 0:
                    methodInfo.Invoke(Modules[methodInfo.DeclaringType!], null);
                    break;
                case 1:
                    methodInfo.Invoke(Modules[methodInfo.DeclaringType!],
                        new object[] { command });
                    break;
                case 2:
                    methodInfo.Invoke(Modules[methodInfo.DeclaringType!],
                        new object[] { command, command.Data.Options });
                    break;
                case 3:
                    methodInfo.Invoke(Modules[methodInfo.DeclaringType!],
                        new object[] { command, command.Data.Options, _client });
                    break;
            }
        }
    }

    private async Task ClientOnReady(DiscordSocketClient client)
    {
        var oldCommands = await _client.GetGlobalApplicationCommandsAsync();
        if(Environment.GetEnvironmentVariable("slash-reset") is not null)
            foreach (var command in oldCommands)
            {
                Console.WriteLine($"Deleted command \"{command.Name}\"");
                command.DeleteAsync();
            }

        foreach (var type in ModuleTypes)
        {
            var methodInfos = type.GetMethods();
            var commands = methodInfos
                .Where(m => m.GetCustomAttributes(typeof(SlashCommandAttribute), true).Any());
            
            foreach (var command in commands)
            {
                var attributes =
                    command.GetCustomAttributes(typeof(SlashCommandAttribute), true)
                        .Cast<SlashCommandAttribute>();

                var commandAttribute = attributes.First();
                
                SlashCommands.Add((commandAttribute, command));
                
                var options = commandAttribute.Options.Select(s => JsonConvert.DeserializeObject<SlashCommandOptionBuilder>(s)!).ToList();
                
                if(Environment.GetEnvironmentVariable("slash-reset") is null)
                    if(oldCommands.Any(c => c.Name == commandAttribute.Name && c.Description == commandAttribute.Description && c.Options.Count == options.Count))
                        continue;

                var slashCommand = new SlashCommandBuilder
                {
                    Name = commandAttribute.Name,
                    Description = commandAttribute.Description,
                    Options = options
                };

                await client.CreateGlobalApplicationCommandAsync(slashCommand.Build());
                Console.WriteLine($"Started creating command \"{commandAttribute.Name}\"");
            }
        }
    }

    public async Task HandleMessageAsync(SocketUserMessage message, DiscordSocketClient client)
    {
        if(message.Channel.Id == 1050422061803245600)
            return;
        
        bool anyMatch = false;
        foreach (var (commandAttribute, methodInfo) in Commands)
        {
            if (!commandAttribute.Matches(message, out var argPos)) continue;

            if (await CheckIfBaka(message)) return;

            anyMatch = true;
            
            ExecuteMethod(methodInfo, message, client, argPos);
        }

        if(await HandleHelp(message, client))
            anyMatch = true;

        if (!anyMatch)
        {
            var argPos = 0;
            if (message.Channel is SocketDMChannel && !message.Author.IsBot)
            {
                if (await CheckIfBaka(message)) return;
                
                foreach (var (commandAttribute, methodInfo) in Commands.Where(c =>
                             c.methodInfo.GetCustomAttributes(typeof(PingCommandAttribute), true).Any()))
                {
                    if (commandAttribute is not PingCommandAttribute)
                        continue;

                    ExecuteMethod(methodInfo, message, client, argPos);
                }
            }
            
            if (message.HasCharPrefix('%', ref argPos))
            {
                if (await CheckIfBaka(message)) return;
                
                foreach (var (commandAttribute, methodInfo) in Commands.Where(c =>
                             c.methodInfo.GetCustomAttributes(typeof(NoMatchCommandAttribute), true).Any()))
                {
                    if (commandAttribute is not NoMatchCommandAttribute)
                        continue;
                    
                    ExecuteMethod(methodInfo, message, client, argPos);
                }
            }
        }
    }

    private async Task<bool> CheckIfBaka(SocketUserMessage message)
    {
        if (message.Author.Id == 1082069574901563453)
            return true;
        
        if ((await (await ((ITextChannel)await _client.Rest.GetChannelAsync(1120330028048207974)).GetMessageAsync(1120436897727131809)).GetReactionUsersAsync(Emoji.Parse(":thumbsup:"), Int32.MaxValue).FirstAsync()).Any(u => u.Id == message.Author.Id))
        {
            await message.ReplyAsync("i refuse to do anything for you. you want to ban me.");
            return true;
        }

        return false;
    }

    private static void ExecuteMethod(MethodInfo methodInfo, SocketUserMessage message, DiscordSocketClient client,
        int argPos)
    {
        var context = new SocketCommandContext(client, message);

        switch (methodInfo.GetParameters().Length)
        {
            case 0:
                methodInfo.Invoke(Modules[methodInfo.DeclaringType!], null);
                break;
            case 1:
                methodInfo.Invoke(Modules[methodInfo.DeclaringType!],
                    new object[] { context });
                break;
            case 2:
                methodInfo.Invoke(Modules[methodInfo.DeclaringType!],
                    new object[] { context, message.Content.Substring(argPos) });
                break;
        }
    }

    private async Task<bool> HandleHelp(SocketUserMessage message, DiscordSocketClient client)
    {
        if (!message.Content.StartsWith("%help")) return false;

        var helpMessage = GenerateHelpMessage();

        var embed = new EmbedBuilder()
            .WithAuthor(client.CurrentUser.Username)
            .WithTitle("Help:")
            .WithDescription(helpMessage)
            .WithColor(Color.Blue)
            .WithCurrentTimestamp();

        await message.ReplyAsync(embed: embed.Build());

        return true;
    }

    public static string GenerateHelpMessage()
    {
        var helpMessage = new StringBuilder("Available commands: \n```");

        foreach (var command in Commands)
        {
            if (command.attribute.HideInHelp)
                continue;

            var summaryAttributes = command.methodInfo
                .GetCustomAttributes(typeof(SummaryAttribute), true)
                .Cast<SummaryAttribute>().ToArray();
            var parameterAttributes = command.methodInfo
                .GetCustomAttributes(typeof(ParametersAttribute), true)
                .Cast<ParametersAttribute>().ToArray();

            if (!summaryAttributes.Any())
                continue;

            helpMessage.Append(command.attribute);
            if (parameterAttributes.Any())
            {
                helpMessage.Append(' ');
                helpMessage.Append(parameterAttributes.First().Text);
            }

            helpMessage.Append(' ');
            helpMessage.Append('-');
            helpMessage.Append(' ');
            helpMessage.Append(summaryAttributes.First().Text);
            helpMessage.Append('\n');
        }

        helpMessage.Append('`');
        helpMessage.Append('`');
        helpMessage.Append('`');
        helpMessage.Append('\n');
        helpMessage.Append("[Long Privacy Policy](https://storage.googleapis.com/uwu-mew-mew/uwuprivacy.txt)\n");
        helpMessage.Append("[Very short Privacy Policy](https://storage.googleapis.com/uwu-mew-mew/uwuprivacy_short.txt)");
        return helpMessage.ToString();
    }
}