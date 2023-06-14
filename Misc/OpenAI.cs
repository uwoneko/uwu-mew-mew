using System.Dynamic;
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
    public record ChatMessage(string role, string content, string name = "");

    private static readonly HttpClient httpClient = new();
    
    static OpenAi()
    {
        Api = new OpenAIAPI(Environment.GetEnvironmentVariable("openai"));
        Api.ApiUrlFormat = "https://api.cattogpt.repl.co/{0}/{1}";
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

    public static async Task<string> AskChatAsync(string system, string prompt,
        float temperature = 1, int maxTokens = 32, string model = "gpt-3.5-turbo")
    {
        var answer = (await Api.Chat.CreateChatCompletionAsync(new OpenAI_API.Chat.ChatMessage[]
        {
            new(ChatMessageRole.System, system),
            new(ChatMessageRole.User, prompt)
        }, temperature: temperature, max_tokens: maxTokens, model: model)).ToString();
        return answer;
    }
    
    public static async Task<string> CreateEdit(string input, string instruction, string model = "text-davinci-edit-001", int n = 1, double temperature = 1, double topP = 1)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable("openai"));

        var requestBody = new { model, input, instruction, n, temperature, top_p = topP };

        var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.cattogpt.repl.co/v1/edits")
        {
            Content = content
        };

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseBody = JObject.Parse(await response.Content.ReadAsStringAsync());
        return responseBody["choices"][0]["text"].ToString();
    }
    
    public static async Task<(string content, string finishReason, string functionCall)> CreateChatCompletionAsync(List<ChatMessage> chatMessages, string model = "gpt-3.5-turbo-0613", IReadOnlyList<JObject>? functions = null)
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

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.cattogpt.repl.co/v1/chat/completions")
        {
            Content = content
        };

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseBody = JObject.Parse(await response.Content.ReadAsStringAsync());
        return (responseBody["choices"][0]["message"]["content"].ToString(), 
            responseBody["choices"][0]["finish_reason"].ToString(),
            responseBody["choices"][0]["function_call"]?.ToString());
    }
    
    public static async IAsyncEnumerable<string> StreamChatCompletionAsync(List<ChatMessage> chatMessages, string model = "gpt-3.5-turbo-0613", IReadOnlyList<JObject>? functions = null)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable("openai"));

        dynamic requestBody = new ExpandoObject();
        requestBody.model = model;
        requestBody.messages = chatMessages.Select(m => new {m.role, m.content});
        requestBody.stream = true;
        
        if(functions is not null)
        {
            requestBody.functions = functions;
            requestBody.function_call = "auto";
        }

        var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.cattogpt.repl.co/v1/chat/completions")
        {
            Content = content
        };

        var response = await httpClient.SendAsync(request);
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
}