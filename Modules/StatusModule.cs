using System.Text;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Primitives;
using uwu_mew_mew.Attributes;
using uwu_mew_mew.Bases;
using uwu_mew_mew.Misc;
using Color = Discord.Color;

namespace uwu_mew_mew.Modules;

public class StatusModule : CommandModule
{
    [PrefixCommand("status")]
    public async Task StatusAsync(SocketCommandContext context, string message)
    {
        var msg = await context.Message.ReplyAsync("Checking status...");
        
        var sdTask = StableDiffusionModule.CheckApiStatus();
        var sdExposedTask = StableDiffusionModule.CheckExposedApiStatus();
        var openaiTask = OpenAi.CheckOpenAiStatus();
        var chimeraTask = OpenAi.CheckChimeraStatus();

        await Task.WhenAll(sdTask, sdExposedTask, openaiTask, chimeraTask);

        var stringBuilder = new StringBuilder();
        stringBuilder.Append("Image generation (localhost): ");
        stringBuilder.Append(sdTask.Result.isUp ? '✅' : '❌');
        switch (sdTask.Result.status)
        {
            case StableDiffusionModule.ApiStatus.Up:
                stringBuilder.Append(" Pass");
                break;
            case StableDiffusionModule.ApiStatus.DownButRunning:
                stringBuilder.Append(" Fail, but process is running (Update/Startup in progress)");
                break;
            case StableDiffusionModule.ApiStatus.Down:
                stringBuilder.Append(" Fail");
                break;
        }
        stringBuilder.AppendLine();
        
        stringBuilder.Append("Image generation (exposed): ");
        stringBuilder.Append(sdExposedTask.Result ? '✅' : '❌');
        stringBuilder.Append(sdExposedTask.Result ? " Pass" : " Fail");
        stringBuilder.AppendLine();
        
        stringBuilder.Append("OpenAI: ");
        stringBuilder.Append(openaiTask.Result ? '✅' : '❌');
        stringBuilder.Append(openaiTask.Result ? " Pass" : " Fail");
        stringBuilder.AppendLine();
        
        stringBuilder.Append("Chimera (token count): ");
        stringBuilder.Append(chimeraTask.Result ? '✅' : '❌');
        stringBuilder.Append(chimeraTask.Result ? " Pass" : " Fail");

        var isOk = sdTask.Result.isUp && openaiTask.Result && chimeraTask.Result;

        var embed = new EmbedBuilder()
            .WithColor(isOk ? Color.Green : Color.Gold)
            .WithTitle("uwu mew mew status")
            .WithDescription(stringBuilder.ToString())
            .WithCurrentTimestamp();

        await msg.ModifyAsync(m => m.Embed = embed.Build());
        await msg.ModifyAsync(m => m.Content = isOk ? "Uwu everything is okay!" : "Owo i am experiencing degraded state...");
    }
}