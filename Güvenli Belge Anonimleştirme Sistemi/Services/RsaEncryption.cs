using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;

public static class RsaEncryption
{
    public static string Encrypt(string data, string publicKey)
    {
        publicKey = publicKey.Replace("\r\n", "\n").Trim(); // Fazladan boşlukları temizle

        using (var rsa = RSA.Create())
        {
            rsa.ImportFromPem(publicKey.ToCharArray()); // String yerine char[] kullan
            var encryptedData = rsa.Encrypt(Encoding.UTF8.GetBytes(data), RSAEncryptionPadding.OaepSHA256);
            return Convert.ToBase64String(encryptedData);
        }
    }

    public static string Decrypt(string encryptedData, string privateKey)
    {
        privateKey = privateKey.Replace("\r\n", "\n").Trim(); // Fazladan boşlukları temizle

        using (var rsa = RSA.Create())
        {
            rsa.ImportFromPem(privateKey.ToCharArray()); // String yerine char[] kullan
            var decryptedData = rsa.Decrypt(Convert.FromBase64String(encryptedData), RSAEncryptionPadding.OaepSHA256);
            return Encoding.UTF8.GetString(decryptedData);
        }
    }
}
