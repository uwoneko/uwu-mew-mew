using System.Text;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using uwu_mew_mew.Attributes;
using uwu_mew_mew.Bases;

namespace uwu_mew_mew.Modules;

public class StableDiffusionModule : CommandModule
{
    private static readonly HttpClient HttpClient = new();
    private const string url = "http://127.0.0.1:7860/sdapi/v1/txt2img";
    
    [PrefixCommand("image")]
    [PrefixCommand("img", hideInHelp: true)]
    public async Task LegacyGenerateAsync(SocketCommandContext context, string message)
    {
        var generatingMessage = await context.Message.ReplyAsync("Generating (owo %image is legacy... Use /image instead uwu!)...", allowedMentions: AllowedMentions.None);
        var (image, _) = await GenerateImage(message, defaultCfgScale, defaultSamplingSteps);
        if (!image.Any())
        {
            await context.Message.ReplyAsync("qwq i didnt get an image from api :cry:");
            return;
        }
        using var memoryStream = new MemoryStream(image);

        await context.Channel.SendFileAsync(memoryStream, "image.png", "Hewe is youw image mastew! uwu~",
            messageReference: new MessageReference(context.Message.Id));

        await generatingMessage.DeleteAsync();
    }


    [SlashCommand("image", "Generate an image",
        """{"Name":"prompt","Description":"Prompt for the image","Type":3,"IsRequired":true,"IsAutocomplete":false}""",
        """{"Name":"cfg_scale","Description":"Classifier Free Guidance scale, how much the model should respect your prompt.","Type":10}""",
        """{"Name":"sampling_steps","Description":"Number of sampling steps. The more the better and longer. Max is 100.","Type":4}""")]
    public async Task GenerateAsync(SocketSlashCommand command,
        IReadOnlyCollection<SocketSlashCommandDataOption> args, DiscordSocketClient client)
    {
        var cfgScale = args.Any(a => a.Name == "cfg_scale")
            ? (double)args.First(a => a.Name == "cfg_scale").Value
            : defaultCfgScale;
        var samplingSteps = args.Any(a => a.Name == "sampling_steps")
            ? (int)(long)args.First(a => a.Name == "sampling_steps").Value
            : defaultSamplingSteps;

        if (samplingSteps > 100)
        {
            await command.FollowupAsync("owo thats too much steps...");
            return;
        }

        await command.FollowupAsync("Generating...", allowedMentions: AllowedMentions.None);

        var (image, _) = await GenerateImage((string)args.First(a => a.Name == "prompt"), cfgScale, samplingSteps);
        if (!image.Any())
        {
            await command.FollowupAsync("qwq i didnt get an image from api :cry:");
            return;
        }
        using var memoryStream = new MemoryStream(image);
        
        await command.FollowupWithFileAsync(memoryStream, "image.png", MentionUtils.MentionUser(command.User.Id)+", hewe is youw image mastew! uwu~");
    }
    
    public static async Task<(byte[] image, long seed)> GenerateImage(string prompt, double cfgScale, int steps, long seed = -1)
    {
        var payload = new
        {
            prompt,
            steps,
            cfg_scale = cfgScale,
            sampler = "DPM++ SDE Karras",
            seed
        };

        var jsonPayload = JsonConvert.SerializeObject(payload);
        var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        var response = await HttpClient.PostAsync(url, httpContent);

        if (!response.IsSuccessStatusCode) return (Array.Empty<byte>(), -1);
        
        var result = JObject.Parse(await response.Content.ReadAsStringAsync());
        var image = Convert.FromBase64String(((JArray)result["images"]).First().ToString());

        return (image, (long)JObject.Parse(result["info"].ToString())["seed"]);
    }

    public static readonly double defaultCfgScale = 5.5;
    public static readonly int defaultSamplingSteps = 60;
}