using System.Buffers.Text;
using System.Collections;
using System.Diagnostics;
using System.Management;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Unicode;
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
    
    [PrefixCommand("image")]
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
    
    [SlashCommand("uwuimg2img", "Generates image using input image as starting point.",
        """{"Name":"image","Description":"Image to start from","Type":11,"IsRequired":true,"IsAutocomplete":false}""",
        """{"Name":"prompt","Description":"Prompt for the image","Type":3,"IsRequired":true,"IsAutocomplete":false}""",
        """{"Name":"cfg_scale","Description":"Classifier Free Guidance scale, how much the model should respect your prompt.","Type":10}""",
        """{"Name":"sampling_steps","Description":"Number of sampling steps. The more the better, but longer. Max is 100.","Type":4}""")]
    public async Task ImageToImageAsync(SocketSlashCommand command,
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

        var generatingFollowupMessage = await command.FollowupAsync("Generating...", allowedMentions: AllowedMentions.None);

        var stopwatch = Stopwatch.StartNew();
        
        void UpdateProgress(float progress, float eta, int step)
        {
            if(progress < 0.025)
            {
                generatingFollowupMessage.ModifyAsync(m => m.Content = "Finishing...");
                return;
            }

            generatingFollowupMessage.ModifyAsync(m => m.Content = $"Generating...\n{step.ToString()}/{samplingSteps.ToString()} [{((int)Math.Round(progress * 100)).ToString()}%, {((int)Math.Round(eta)).ToString()} s remaining]");
        }

        var initImage = await HttpClient.GetByteArrayAsync(((IAttachment)args.First(a => a.Name == "image").Value).Url);

        var (image, _) = await Image2Image(initImage,(string)args.First(a => a.Name == "prompt"), cfgScale, samplingSteps, updateProgress: UpdateProgress);
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


    [SlashCommand("image", "Generate an image",
        """{"Name":"prompt","Description":"Prompt for the image","Type":3,"IsRequired":true,"IsAutocomplete":false}""",
        """{"Name":"cfg_scale","Description":"Classifier Free Guidance scale, how much the model should respect your prompt.","Type":10}""",
        """{"Name":"sampling_steps","Description":"Number of sampling steps. The more the better, but longer. Max is 100.","Type":4}""",
        """{"Name":"model","Description":"Model to use.","Type":3,"Choices":[{"Name": "Ghostmix V2", "Value": "Ghostmix"}, {"Name": "Holysh*t v1.3", "Value": "Holyshit"}]}""")]
    public async Task GenerateAsync(SocketSlashCommand command,
        IReadOnlyCollection<SocketSlashCommandDataOption> args, DiscordSocketClient client)
    {
        var cfgScale = args.Any(a => a.Name == "cfg_scale")
            ? (double)args.First(a => a.Name == "cfg_scale").Value
            : DefaultCfgScale;
        var samplingSteps = args.Any(a => a.Name == "sampling_steps")
            ? (int)(long)args.First(a => a.Name == "sampling_steps").Value
            : DefaultSamplingSteps;
        var model = args.Any(a => a.Name == "model")
            ? Enum.Parse<Model>((string)args.First(a => a.Name == "model").Value)
            : Model.Holyshit;

        if (samplingSteps > 100)
        {
            await command.FollowupAsync("owo thats too much steps...");
            return;
        }

        var generatingFollowupMessage = await command.FollowupAsync("Generating...", allowedMentions: AllowedMentions.None);

        var stopwatch = Stopwatch.StartNew();
        
        void UpdateProgress(float progress, float eta, int step)
        {
            if(progress < 0.025)
            {
                generatingFollowupMessage.ModifyAsync(m => m.Content = "Finishing...");
                return;
            }

            generatingFollowupMessage.ModifyAsync(m => m.Content = $"Generating...\n{step.ToString()}/{samplingSteps.ToString()} [{((int)Math.Round(progress * 100)).ToString()}%, {((int)Math.Round(eta)).ToString()} s remaining]");
        }

        var (image, _) = await GenerateImage((string)args.First(a => a.Name == "prompt"), cfgScale, samplingSteps, updateProgress: UpdateProgress, model: model);
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
    
    private static readonly HttpClient HttpClient;
    private const string txt2imgHolyshitUrl = "http://127.0.0.1:7860/sdapi/v1/txt2img";
    private const string img2imgHolyshitUrl = "http://127.0.0.1:7860/sdapi/v1/img2img";
    private const string txt2imgGhostmixUrl = "http://127.0.0.1:7865/sdapi/v1/txt2img";
    private const string img2imgGhostmixUrl = "http://127.0.0.1:7865/sdapi/v1/img2img";

    private static string? currentGeneration = null;

    static StableDiffusionModule()
    {
        HttpClient = new HttpClient();
        HttpClient.Timeout = TimeSpan.FromMinutes(10);
    }

    public enum Model
    {
        Holyshit,
        Ghostmix
    }

    public static async Task<(byte[] image, long seed)> GenerateImage(string prompt, double cfgScale, int steps, long seed = -1, Action<float, float, int>? updateProgress = null, Model model = Model.Holyshit)
    {
        try
        {
            var payload = new
            {
                prompt,
                negative_prompt = "nsfw, naked, lewd",
                steps,
                cfg_scale = cfgScale,
                sampler_name = "DPM++ 2S a Karras",
                seed
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("sd-credentials")));
            var url = model switch
            {
                Model.Holyshit => txt2imgHolyshitUrl,
                Model.Ghostmix => txt2imgGhostmixUrl,
                _ => throw new ArgumentOutOfRangeException(nameof(model), model, null)
            };
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = httpContent;
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
            var responseTask = HttpClient.SendAsync(request);
            var generationId = Guid.NewGuid().ToString();
            currentGeneration ??= generationId;

            while (!responseTask.IsCompleted)
            {
                await Task.Delay(5000);
                
                if(currentGeneration != null && currentGeneration != generationId)
                    continue;
                
                currentGeneration ??= generationId;

                var progressUrl = model switch
                {
                    Model.Holyshit => "http://127.0.0.1:7860/sdapi/v1/progress",
                    Model.Ghostmix => "http://127.0.0.1:7865/sdapi/v1/progress",
                    _ => throw new ArgumentOutOfRangeException(nameof(model), model, null)
                };
                var progressRequest = new HttpRequestMessage(HttpMethod.Get, progressUrl);
                progressRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);

                try
                {
                    if (updateProgress is not null)
                    {
                        var progressResponse = await HttpClient.SendAsync(progressRequest);
                        var progress = JObject.Parse(await progressResponse.Content.ReadAsStringAsync());
                
                        updateProgress.Invoke(progress["progress"].Value<float>(), progress["eta_relative"].Value<float>(), progress["state"]["sampling_step"].Value<int>());
                    }
                }
                catch
                {
                    //idc
                }
            }

            currentGeneration = null;

            if (!responseTask.Result.IsSuccessStatusCode) return (Array.Empty<byte>(), -1);
        
            var result = JObject.Parse(await responseTask.Result.Content.ReadAsStringAsync());
            var image = Convert.FromBase64String(((JArray)result["images"]).First().ToString());

            return (image, (long)JObject.Parse(result["info"].ToString())["seed"]);
        }
        catch
        {
            return (Array.Empty<byte>(), -1);
        }
    }
    
    public static async Task<(byte[] image, long seed)> Image2Image(byte[] initialImage, string prompt, double cfgScale, int steps, long seed = -1, Action<float, float, int>? updateProgress = null, Model model = Model.Holyshit)
    {
        try
        {
            var payload = new
            {
                init_images = new[]
                {
                    Convert.ToBase64String(initialImage)
                },
                prompt,
                negative_prompt = "nsfw, naked, lewd",
                steps,
                cfg_scale = cfgScale,
                sampler_name = "DPM++ 2S a Karras",
                seed
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("sd-credentials")));
            var url = model switch
            {
                Model.Holyshit => img2imgHolyshitUrl,
                Model.Ghostmix => img2imgGhostmixUrl,
                _ => throw new ArgumentOutOfRangeException(nameof(model), model, null)
            };
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = httpContent;
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
            var responseTask = HttpClient.SendAsync(request);
            var generationId = Guid.NewGuid().ToString();
            currentGeneration ??= generationId;

            while (!responseTask.IsCompleted)
            {
                await Task.Delay(5000);
                
                if(currentGeneration != null && currentGeneration != generationId)
                    continue;
                
                currentGeneration ??= generationId;
                
                
                var progressUrl = model switch
                {
                    Model.Holyshit => "http://127.0.0.1:7860/sdapi/v1/progress",
                    Model.Ghostmix => "http://127.0.0.1:7865/sdapi/v1/progress",
                    _ => throw new ArgumentOutOfRangeException(nameof(model), model, null)
                };
                var progressRequest = new HttpRequestMessage(HttpMethod.Get, progressUrl);
                progressRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);

                try
                {
                    if (updateProgress is not null)
                    {
                        var progressResponse = await HttpClient.SendAsync(progressRequest);
                        
                        var progress = JObject.Parse(await progressResponse.Content.ReadAsStringAsync());
                
                        updateProgress.Invoke(progress["progress"].Value<float>(), progress["eta_relative"].Value<float>(), progress["state"]["sampling_step"].Value<int>());
                    }
                }
                catch
                {
                    //idc
                }
            }

            currentGeneration = null;

            if (!responseTask.Result.IsSuccessStatusCode) return (Array.Empty<byte>(), -1);
        
            var result = JObject.Parse(await responseTask.Result.Content.ReadAsStringAsync());
            var image = Convert.FromBase64String(((JArray)result["images"]).First().ToString());

            return (image, (long)JObject.Parse(result["info"].ToString())["seed"]);
        }
        catch
        {
            return (Array.Empty<byte>(), -1);
        }
    }
    
    public static bool IsSpecificPythonRunning(string fullPathToPythonInterpreter)
    {
        var query = "SELECT ExecutablePath FROM Win32_Process WHERE Name = 'python.exe' OR Name = 'python3.exe'";
        using (var searcher = new ManagementObjectSearcher(query))
        {
            foreach (var process in searcher.Get())
            {
                string path = process["ExecutablePath"]?.ToString();

                if (string.Equals(path, fullPathToPythonInterpreter, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }
    
    public enum ApiStatus
    {
        Up,
        DownButRunning,
        Down
    }

    public static async Task<(bool isUp, ApiStatus status)> CheckApiStatus()
    {
        if (IsSpecificPythonRunning(@"D:\stable-diffusion-webui\venv\Scripts\python.exe"))
        {
            var payload = new
            {
                prompt = "girl",
                steps = 1,
                seed = 0,
                width = 32,
                height = 32
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("sd-credentials")));
            var request = new HttpRequestMessage(HttpMethod.Post, txt2imgHolyshitUrl);
            request.Content = httpContent;
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
            try
            {
                var response = await HttpClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return (true, ApiStatus.Up);
                }
                return (false, ApiStatus.DownButRunning);
            }
            catch (Exception e)
            {
                return (false, ApiStatus.DownButRunning);
            }
        }

        return (false, ApiStatus.Down);
    }

    public static async Task<bool> CheckExposedApiStatus()
    {
        var payload = new
        {
            prompt = "girl",
            steps = 1,
            seed = 0,
            width = 32,
            height = 32
        };

        var jsonPayload = JsonConvert.SerializeObject(payload);
        var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("sd-credentials")));
        var request = new HttpRequestMessage(HttpMethod.Post, "https://yuspautomatic1111.loca.lt/sdapi/v1/txt2img");
        request.Content = httpContent;
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
        
        try
        {
            var response = await HttpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public const double DefaultCfgScale = 5.5;
    public const int DefaultSamplingSteps = 60;
}