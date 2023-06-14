using System.Runtime.Serialization.Formatters.Binary;

#pragma warning disable SYSLIB0011

namespace uwu_mew_mew.Misc;

public static class BinarySerializerConvert
{
    public static byte[] Serialize(object obj)
    {
        using var memory = new MemoryStream();

        var formatter = new BinaryFormatter();
        formatter.Serialize(memory, obj);
        return memory.ToArray();
    }

    public static object Deserialize(byte[] data)
    {
        using var memory = new MemoryStream(data);

        var formatter = new BinaryFormatter();
        return formatter.Deserialize(memory);
    }
}