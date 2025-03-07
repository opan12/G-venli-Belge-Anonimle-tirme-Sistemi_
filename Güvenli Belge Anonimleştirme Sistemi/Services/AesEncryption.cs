using System;
using System.Security.Cryptography;
using System.Text;

public static class AesEncryption
{
    public static byte[] Key { get; set; }

    public static string Encrypt(string data)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Key;
            aes.GenerateIV();

            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            using (var ms = new MemoryStream())
            {
                ms.Write(aes.IV, 0, aes.IV.Length); // IV'yi dosyanın başına ekle
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    var dataBytes = Encoding.UTF8.GetBytes(data);
                    cs.Write(dataBytes, 0, dataBytes.Length);
                    cs.FlushFinalBlock();
                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    public static string Decrypt(string encryptedData)
    {
        var fullCipher = Convert.FromBase64String(encryptedData);

        using (Aes aes = Aes.Create())
        {
            aes.Key = Key;
            var iv = new byte[16];
            Array.Copy(fullCipher, 0, iv, 0, iv.Length); // IV'yi dosyanın başından al
            aes.IV = iv;

            using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            using (var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (var sr = new StreamReader(cs))
            {
                return sr.ReadToEnd();
            }
        }
    }
}