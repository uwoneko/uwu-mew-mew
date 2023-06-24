using System.Collections;
using System.Text;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Google.Cloud.Storage.V1;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing.Processors.Dithering;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using uwu_mew_mew.Attributes;
using uwu_mew_mew.Bases;
using Color = SixLabors.ImageSharp.Color;

namespace uwu_mew_mew.Modules;

public class StableDiffusionModule : CommandModule
{
    private static readonly HttpClient HttpClient = new();
    private const string url = "http://127.0.0.1:7860/sdapi/v1/txt2img";
    
    [PrefixCommand("image")]
    [PrefixCommand("img", hideInHelp: true)]
    public async Task LegacyGenerateAsync(SocketCommandContext context, string message)
    {
        var text = "Generating (owo %image is legacy... Use /image instead uwu!)...";
        if(context.User.Id == 1099387852808257656)
            text = "What is it? Oh yeah sure~ uwu. Wait a lil sweetie~";
        var generatingMessage = await context.Message.ReplyAsync(text, allowedMentions: AllowedMentions.None);
        var (image, _) = await GenerateImage(message, DefaultCfgScale, DefaultSamplingSteps);
        if (!image.Any())
        {
            await context.Message.ReplyAsync("qwq i didnt get an image from api :cry:");
            return;
        }
        using var memoryStream = new MemoryStream(image);

        var doneText = "Hewe is youw image mastew! uwu~";
        if(context.User.Id == 1099387852808257656)
            doneText = "Here ya go~ uwu";
        await context.Channel.SendFileAsync(memoryStream, "image.png", doneText,
            messageReference: new MessageReference(context.Message.Id));

        await generatingMessage.DeleteAsync();
    }


    [SlashCommand("image", "Generate an image",
        """{"Name":"prompt","Description":"Prompt for the image","Type":3,"IsRequired":true,"IsAutocomplete":false}""",
        """{"Name":"cfg_scale","Description":"Classifier Free Guidance scale, how much the model should respect your prompt.","Type":10}""",
        """{"Name":"sampling_steps","Description":"Number of sampling steps. The more the better, but longer. Max is 100.","Type":4}""")]
    public async Task GenerateAsync(SocketSlashCommand command,
        IReadOnlyCollection<SocketSlashCommandDataOption> args, DiscordSocketClient client)
    {
        var cfgScale = args.Any(a => a.Name == "cfg_scale")
            ? (double)args.First(a => a.Name == "cfg_scale").Value
            : DefaultCfgScale;
        var samplingSteps = args.Any(a => a.Name == "sampling_steps")
            ? (int)(long)args.First(a => a.Name == "sampling_steps").Value
            : DefaultSamplingSteps;

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
        using var uploadStream = new MemoryStream(image);

        var storageClient = await StorageClient.CreateAsync();
        var obj = storageClient.UploadObject("uwu-mew-mew", $"{Guid.NewGuid()}.png", "image/png", uploadStream);
        
        using var decodeStream = new MemoryStream(image);

        var decodedImage = await PngDecoder.Instance.DecodeAsync<Rgba32>(new DecoderOptions(), decodeStream);
        var heat = new Dictionary<Rgba32, int>();
        decodedImage.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                Span<Rgba32> pixelRow = accessor.GetRowSpan(y);

                for (var x = 0; x < pixelRow.Length; x++)
                {
                    if(!heat.ContainsKey(pixelRow[x]))
                        heat.Add(pixelRow[x], 1);
                    else
                        heat[pixelRow[x]] += 1;
                }
            }
        });

        Rgba32 bestColor = new Rgba32();
        var maxSum = int.MinValue;
        foreach (var h in heat.OrderByDescending(h => h.Value).Take(heat.Count/1000))
        {
            var sum = h.Key.R + h.Key.G + h.Key.B;
            if (sum <= maxSum) continue;
            
            maxSum = sum;
            bestColor = h.Key;
        }
        
        var embed = new EmbedBuilder()
            .WithAuthor(command.User.Username, command.User.GetAvatarUrl())
            .WithTitle("Done uwu!")
            .WithDescription($"[Download]({obj.MediaLink})")
            .WithImageUrl(obj.MediaLink)
            .WithColor(bestColor.R, bestColor.G, bestColor.B);
        await command.FollowupAsync(command.User.Mention, embed: embed.Build());
    }
    
    public static async Task<(byte[] image, long seed)> GenerateImage(string prompt, double cfgScale, int steps, long seed = -1)
    {
        try
        {
            var payload = new
            {
                prompt,
                negative_prompt = "nsfw, naked, lewd",
                steps,
                cfg_scale = cfgScale,
                sampler_name = "DPM++ SDE Karras",
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
        catch
        {
            return (Array.Empty<byte>(), -1);
        }
    }

    public const double DefaultCfgScale = 5.5;
    public const int DefaultSamplingSteps = 60;
}