using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace uwu_mew_mew_bot.Misc;

public static class EncryptedJsonConvert
{
    private const int IvLength = 16;
    private static readonly byte[] Key;

    static EncryptedJsonConvert()
    {
        var random = new Random(new Guid("8173e4a5-d008-47b9-a00e-c09b50d64aff").GetHashCode());
        var key = new Guid("0ec8423e-aca5-4008-8b1d-0a1c10091464").ToByteArray()
            .OrderBy(b => random.Next() ^ b).Select(b => (byte)((byte)new Random(b*9598365^73592).Next()^b))
            .OrderBy(b => random.Next() ^ b*255);
        
        Key = key.Take(32).ToArray();
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