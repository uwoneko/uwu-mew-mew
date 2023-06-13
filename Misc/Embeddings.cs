using MathNet.Numerics;
using uwu_mew_mew_bot.Misc;
using uwu_mew_mew.Misc;
using static MathNet.Numerics.Distance;

namespace uwu_mew_mew_2.Misc;

public static class Embeddings
{
    public static bool Similar(string first, string second, float threshold = 0.15f)
    {
        var taskFirst = Get(first);
        var taskSecond = Get(second);
        taskFirst.Wait();
        taskSecond.Wait();
        return Distance(taskFirst.Result, taskSecond.Result) < threshold;
    }
    
    public static float Distance(string first, string second)
    {
        var taskFirst = Get(first);
        var taskSecond = Get(second);
        taskFirst.Wait();
        taskSecond.Wait();
        return Distance(taskFirst.Result, taskSecond.Result);
    }
    
    public static float Distance(float[] first, float[] second)
    {
        return Cosine(first, second);
    }
    
    public static float DistanceTo(this float[] first, float[] second) => Distance(first, second);

    public static async Task<float[]> Get(string text)
    {
        if (Cache.TryGetValue(text, out var cached))
        {
            return cached;
        }

        var embedding = await OpenAi.Api.Embeddings.GetEmbeddingsAsync(text);
        Cache[text] = embedding;
        Task.Run(() => File.WriteAllBytes("data/embeddings_cache.bin", BinarySerializerConvert.Serialize(Cache)));
        return embedding;
    }

    private static readonly Dictionary<string, float[]> Cache;

    static Embeddings()
    {
        try
        {
            Cache = (Dictionary<string, float[]>)BinarySerializerConvert.Deserialize(File.ReadAllBytes("data/embeddings_cache.bin"));
        }
        catch
        {
            Cache = new Dictionary<string, float[]>();
        }
    }
}