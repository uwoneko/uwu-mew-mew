using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI_API;
using OpenAI_API.Chat;
using StringContent = System.Net.Http.StringContent;

namespace uwu_mew_mew.Misc;

public static class OpenAi
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public record ChatMessage(string role, string content, string name = "", JObject? function_call = null);

    private static readonly HttpClient httpClient = new();

    private static readonly string apiUrl = Environment.GetEnvironmentVariable("openai-endpoint")!;
    
    static OpenAi()
    {
        Api = new OpenAIAPI(Environment.GetEnvironmentVariable("openai"));
        Api.ApiUrlFormat = apiUrl+"/{0}/{1}";
    }

    public static OpenAIAPI Api { get; }

    public static async Task<string> AskCatgirlAsync(string prompt)
    {
        var answer = (await Api.Chat.CreateChatCompletionAsync(new OpenAI_API.Chat.ChatMessage[]
        {
            new(ChatMessageRole.System,
                "Reply as a catgirl. Show actions and uwuify every response. Use emojis and keep the text readable."),
            new(ChatMessageRole.User, prompt)
        })).ToString();
        return answer;
    }

    public static async Task<string> AskChatAsync(string system, string prompt, string model = "gpt-3.5-turbo")
    {
        var answer = await CreateChatCompletionAsync(new List<ChatMessage>()
        {
            new("system", system),
            new("user", prompt)
        }, model);
        return answer;
    }
    
    public static async Task<string> CreateEdit(string input, string instruction, string model = "text-davinci-edit-001", int n = 1, double temperature = 1, double topP = 1)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable("openai"));

        var requestBody = new { model, input, instruction, n, temperature, top_p = topP };

        var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, apiUrl+"/v1/edits")
        {
            Content = content
        };

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseBody = JObject.Parse(await response.Content.ReadAsStringAsync());
        return responseBody["choices"][0]["text"].ToString();
    }
    
    public static async Task<(string content, string finishReason, string functionCall)> CreateChatCompletionAsyncWithFunctions(List<ChatMessage> chatMessages, string model = "gpt-3.5-turbo-0613", IReadOnlyList<JObject>? functions = null)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable("openai"));

        dynamic requestBody = new ExpandoObject();
        requestBody.model = model;
        requestBody.messages = chatMessages.Select(m => new {m.role, m.content});
        
        if(functions is not null)
        {
            requestBody.functions = functions;
            requestBody.function_call = "auto";
        }

        var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, apiUrl+"/v1/chat/completions")
        {
            Content = content
        };

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseBody = JObject.Parse(await response.Content.ReadAsStringAsync());
        return (responseBody["choices"][0]["message"]["content"].ToString(), 
            responseBody["choices"][0]["finish_reason"].ToString(),
            responseBody["choices"][0]["message"]["function_call"]?.ToString());
    }
        
    public static async Task<int> GetTokenCount(List<ChatMessage> chatMessages, string model = "gpt-3.5-turbo-0613")
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable("chimera"));

        dynamic requestBody = new ExpandoObject();
        requestBody.model = model;
        requestBody.messages = chatMessages.Select(m => new {m.role, m.content});

        var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "https://chimeragpt.adventblocks.cc/v1/chat/tokenizer")
        {
            Content = content
        };

        var response = await httpClient.SendAsync(request);
        
        if(response.StatusCode == HttpStatusCode.BadGateway)
            return -1;
        
        response.EnsureSuccessStatusCode();

        var responseBody = JObject.Parse(await response.Content.ReadAsStringAsync());
        return responseBody["openai"]["count"].Value<int>();
    }
    
    public static async Task<string> CreateChatCompletionAsync(List<ChatMessage> chatMessages, string model = "gpt-3.5-turbo-0613")
    {
        var key = Environment.GetEnvironmentVariable("openai");
        var url = apiUrl+"/v1/chat/completions";
        if (!await CheckOpenAiStatus())
        {
            key = Environment.GetEnvironmentVariable("chimera");
            url = "https://chimeragpt.adventblocks.cc/v1/chat/completions/";
        }
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);

        dynamic requestBody = new ExpandoObject();
        requestBody.model = model;
        requestBody.messages = chatMessages.Select(m => new {m.role, m.content});

        var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseBody = JObject.Parse(await response.Content.ReadAsStringAsync());
        return responseBody["choices"][0]["message"]["content"].ToString();
    }
    
    public static async IAsyncEnumerable<string> StreamChatCompletionAsync(List<ChatMessage> chatMessages, string model = "gpt-3.5-turbo-0613")
    {
        var key = Environment.GetEnvironmentVariable("openai");
        var url = apiUrl+"/v1/chat/completions";
        if (!await CheckOpenAiStatus())
        {
            key = Environment.GetEnvironmentVariable("chimera");
            url = "https://chimeragpt.adventblocks.cc/v1/chat/completions/";
        }
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);

        dynamic requestBody = new ExpandoObject();
        requestBody.model = model;
        requestBody.messages = chatMessages.Select(m => new {m.role, m.content});
        requestBody.stream = true;

        var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };

        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync());
        
        while (await streamReader.ReadLineAsync() is { } line)
        {
            if (!line.ToLower().StartsWith("data:")) continue;
            
            var dataString = line[5..].Trim();
            
            if(dataString == "[DONE]")
                continue;

            var data = JObject.Parse(dataString);
            if(data["choices"][0]["finish_reason"].ToString() != "")
            {
                yield break;
            }

            yield return data["choices"][0]["delta"]["content"].ToString();
        }
    }
    
    public static async IAsyncEnumerable<(string content, string finishReason, string? functionCall)> StreamChatCompletionAsyncWithFunctions(List<ChatMessage> chatMessages, string model = "gpt-3.5-turbo-0613", IReadOnlyList<JObject>? functions = null)
    {
        var key = Environment.GetEnvironmentVariable("openai");
        var url = apiUrl+"/v1/chat/completions";
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);

        dynamic requestBody = new ExpandoObject();
        requestBody.model = model;
        requestBody.messages = new List<ExpandoObject>();
        foreach (var chatMessage in chatMessages)
        {
            dynamic message = new ExpandoObject();
            message.role = chatMessage.role;
            message.content = chatMessage.content;
            if (chatMessage.function_call != null)
                message.function_call = chatMessage.function_call;
            if (chatMessage.name != string.Empty)
                message.name = chatMessage.name;
            requestBody.messages.Add(message);
            
        }
        requestBody.stream = true;
        
        if(functions is not null)
        {
            requestBody.functions = functions;
            requestBody.function_call = "auto";
        }

        var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        var gotAnyResponse = false;

        while(!gotAnyResponse)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            
            if(response.StatusCode == HttpStatusCode.BadGateway)
                continue;
            
            response.EnsureSuccessStatusCode();

            using var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync());

            while (await streamReader.ReadLineAsync() is { } line)
            {
                if (!line.ToLower().StartsWith("data:")) continue;
                
                var dataString = line[5..].Trim();
                
                if(dataString == "[DONE]")
                    continue;

                var data = JObject.Parse(dataString);
                if(data["choices"][0]["finish_reason"].ToString() != "")
                {
                    yield return ("", 
                        data["choices"][0]["finish_reason"].ToString(),
                        data["choices"][0]["delta"]["function_call"]?.ToString());
                    continue;
                }

                yield return (data["choices"][0]["delta"]["content"].ToString(), 
                    data["choices"][0]["finish_reason"].ToString(),
                    data["choices"][0]["delta"]["function_call"]?.ToString());
                gotAnyResponse = true;
            }

            await Task.Delay(3000);
        }
    }

    public static async Task<bool> CheckOpenAiStatus()
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable("openai"));

        dynamic requestBody = new ExpandoObject();
        requestBody.model = "gpt-3.5-turbo";
        requestBody.max_tokens = 1;
        requestBody.messages = new object[]
        {
            new{role="user", content="Hi"}
        };

        var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, apiUrl+"/v1/chat/completions")
        {
            Content = content
        };

        try
        {
            var response = await httpClient.SendAsync(request);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<bool> CheckChimeraStatus()
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable("chimera"));

        dynamic requestBody = new ExpandoObject();
        requestBody.model = "gpt-3.5-turbo";
        requestBody.max_tokens = 1;
        requestBody.messages = new object[]
        {
            new{role="user", content="Hi"}
        };

        var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "https://chimeragpt.adventblocks.cc/v1/chat/completions")
        {
            Content = content
        };

        try
        {
            var response = await httpClient.SendAsync(request);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}