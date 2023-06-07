using System.Reflection;
using System.Text;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using uwu_mew_mew.Attributes;
using uwu_mew_mew.Bases;
using uwu_mew_mew.Modules;
using CommandAttribute = uwu_mew_mew.Bases.CommandAttribute;
using SummaryAttribute = uwu_mew_mew.Attributes.SummaryAttribute;

namespace uwu_mew_mew.Handlers;

public class CommandMessageHandler : IMessageHandler
{
    private static readonly List<Type> ModuleTypes = new()
    {
        typeof(RealtimeCommandModule),
        typeof(AiModule)
    };

    private static readonly Dictionary<Type, CommandModule> Modules = new();

    private static readonly List<(CommandAttribute attribute, MethodInfo methodInfo)> Commands = new();

    public async Task Init(DiscordSocketClient client)
    {
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
    }

    public async Task HandleMessageAsync(SocketUserMessage message, DiscordSocketClient client)
    {
        foreach (var (commandAttribute, methodInfo) in Commands)
        {
            if (!commandAttribute.Matches(message, out var argPos)) continue;

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

        await HandleHelp(message, client);
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
        return helpMessage.ToString();
    }
}