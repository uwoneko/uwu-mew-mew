using MathNet.Numerics;
using uwu_mew_mew.Misc;
using static MathNet.Numerics.Distance;

namespace uwu_mew_mew.Misc;

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
            Cache[text] = (cached.embedding, cached.importance + 1);
            return cached.embedding;
        }

        var embedding = await OpenAi.Api.Embeddings.GetEmbeddingsAsync(text);
        Cache[text] = (embedding, 0);
        SaveAndCleanUp();
        return embedding;
    }

    private static readonly Dictionary<string, (float[]? embedding, int importance)> Cache;

    private static async Task SaveAndCleanUp()
    {
        try
        {
            if (Cache.Count % 2000 == 0)
            {
                foreach (var item in 
                         Cache.Where(item => item.Value.importance <= Cache.Count / 2000))
                {
                    Cache.Remove(item.Key);
                }
            }
            if (Cache.Count % 50 == 0)
            {
                await File.WriteAllBytesAsync("data/embeddings_cache.bin", BinarySerializerConvert.Serialize(Cache));
            }
        }
        catch
        {
            // idc about ur errors
        }
    }

    static Embeddings()
    {
        try
        {
            Cache = (Dictionary<string, (float[] embedding, int importance)>)BinarySerializerConvert.Deserialize(File.ReadAllBytes("data/embeddings_cache.bin"));
        }
        catch
        {
            Cache = new Dictionary<string, (float[] embedding, int importance)>();
        }
    }
}