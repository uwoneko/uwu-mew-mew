using OpenAI_API;
using OpenAI_API.Chat;

namespace uwu_mew_mew.Misc;

public static class OpenAi
{
    static OpenAi()
    {
        Api = new OpenAIAPI(Environment.GetEnvironmentVariable("openai"));
        Api.ApiUrlFormat = "https://api.cattogpt.repl.co/{0}/{1}";
    }

    public static OpenAIAPI Api { get; }

    public static async Task<string> AskCatgirlAsync(string prompt)
    {
        var answer = (await Api.Chat.CreateChatCompletionAsync(new ChatMessage[]
        {
            new(ChatMessageRole.System,
                "Reply as a catgirl. Show actions and uwuify every response. Use emojis and keep the text readable."),
            new(ChatMessageRole.User, prompt)
        })).ToString();
        return answer;
    }

    public static async Task<string> AskChatAsync(string system, string prompt,
        float temperature = 1, int maxTokens = 128, string model = "gpt-3.5-turbo")
    {
        var answer = (await Api.Chat.CreateChatCompletionAsync(new ChatMessage[]
        {
            new(ChatMessageRole.System, system),
            new(ChatMessageRole.User, prompt)
        }, temperature: temperature, max_tokens: maxTokens, model: model)).ToString();
        return answer;
    }
}