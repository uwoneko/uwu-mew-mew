using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using MathNet.Numerics.Random;
using Newtonsoft.Json;

namespace uwu_mew_mew.Misc;

public static class EncryptedJsonConvert
{
    private const int IvLength = 16;
    private static readonly byte[] Key;

    static EncryptedJsonConvert()
    {
        var stopwatch = Stopwatch.StartNew();
        
        var guid1 = new Guid(Environment.GetEnvironmentVariable("key1")!);
        var guid2 = new Guid(Environment.GetEnvironmentVariable("key2")!);
        var guid3 = new Guid(Environment.GetEnvironmentVariable("key3")!);
        var key1 = KeyDerivationGuid(guid1, guid2);
        var key2 = KeyDerivationGuid(guid2, guid3);
        var key3 = KeyDerivationGuid(guid1, guid3);
        var argon = new Argon2d(key1);
        argon.Salt = guid1.ToByteArray();
        argon.DegreeOfParallelism = 16;
        argon.MemorySize = 128000;
        argon.Iterations = 10;
        var argon2 = new Argon2d(key2);
        argon2.Salt = guid3.ToByteArray();
        argon2.DegreeOfParallelism = 16;
        argon2.MemorySize = 128000;
        argon2.Iterations = 10;
        var argon3 = new Argon2d(key3);
        argon3.Salt = guid2.ToByteArray();
        argon3.DegreeOfParallelism = 16;
        argon3.MemorySize = 128000;
        argon3.Iterations = 10;
        
        var keyArgon1 = KeyDerivation(argon.GetBytes(32), argon2.GetBytes(32));
        var keyArgon2 = KeyDerivation(argon2.GetBytes(32), argon3.GetBytes(32));
        var keyArgon3 = KeyDerivation(keyArgon1, keyArgon2);
        
        Console.WriteLine($"Computed key in {stopwatch.Elapsed}");
        
        Key = keyArgon3;
        
        GC.Collect();
    }

    private static byte[] KeyDerivationGuid(Guid guid1, Guid guid2)
    {
        var random = new Random(guid1.GetHashCode());
        var key = guid2.ToByteArray()
            .OrderBy(b => random.Next() ^ b).Select(b => (byte)((byte)new Random(b * 9598365 ^ 73592).Next() ^ b))
            .OrderBy(b => random.Next() ^ b * 255).ToArray();
        return key;
    }

    private static byte[] KeyDerivation(byte[] first, byte[] second)
    {
        var random = new Random(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(second)).Sum(b => b));
        var key = first.OrderBy(b => new Random(b^255>>2).Next())
            .Select(b => (byte)((byte)(b * 890637) ^ 4735624))
            .Select(b => Encoding.UTF8.GetBytes(Convert.ToBase64String(Encoding.UTF8.GetBytes(b.ToString()))).OrderBy(b => b ^ 2897562347865^99999999).First())
            .Select(b => random.NextBytes(b).OrderBy(b => random.NextFullRangeInt32() ^ b).Last()).ToArray();
        return key;
    }

    public static byte[] Serialize(object obj)
    {
        var json = JsonConvert.SerializeObject(obj);

        var jsonBytes = Encoding.UTF8.GetBytes(json);
        byte[] encryptedBytes;

        using (var aes = Aes.Create())
        {
            if (aes == null)
                throw new ApplicationException("Failed to create AES cipher.");

            aes.Key = Key;

            using (var buffer = new MemoryStream())
            {
                buffer.Write(aes.IV, 0, IvLength);

                using (var cryptoStream = new CryptoStream(buffer, aes.CreateEncryptor(), CryptoStreamMode.Write))
                using (var writer = new BinaryWriter(cryptoStream))
                {
                    writer.Write(jsonBytes);
                }

                encryptedBytes = buffer.ToArray();
            }
        }

        return encryptedBytes;
    }

    public static T? Deserialize<T>(byte[] bytes)
    {
        byte[] decryptedBytes;
        var iv = new byte[IvLength];
        Array.Copy(bytes, iv, IvLength);

        using (var aes = Aes.Create())
        {
            if (aes == null)
                throw new ApplicationException("Failed to create AES cipher.");

            aes.Key = Key;
            aes.IV = iv;

            using (var buffer = new MemoryStream())
            using (var cryptoStream = new CryptoStream(new MemoryStream(bytes, IvLength, bytes.Length - IvLength),
                       aes.CreateDecryptor(), CryptoStreamMode.Read))
            {
                int read;
                var chunk = new byte[1024];

                while ((read = cryptoStream.Read(chunk, 0, chunk.Length)) > 0) buffer.Write(chunk, 0, read);

                decryptedBytes = buffer.ToArray();
            }
        }

        var json = Encoding.UTF8.GetString(decryptedBytes);

        return JsonConvert.DeserializeObject<T>(json);
    }
}