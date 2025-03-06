using System.Security.Cryptography;
using System.Text;

public static class EncryptionHelper
{
    private static readonly byte[] Key = Encoding.UTF8.GetBytes("Your32CharLongSecureKeyHere!!"); // 32 byte key
    private static readonly byte[] IV = Encoding.UTF8.GetBytes("Your16CharIVHere!"); // 16 byte IV

    public static string Encrypt(byte[] data)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Key;
            aes.IV = IV;
            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            {
                byte[] encryptedData = encryptor.TransformFinalBlock(data, 0, data.Length);
                return Convert.ToBase64String(encryptedData); // Base64 string olarak döndürülür
            }
        }
    }

    public static byte[] Decrypt(string encryptedBase64Data)
    {
        byte[] encryptedData = Convert.FromBase64String(encryptedBase64Data);
        using (Aes aes = Aes.Create())
        {
            aes.Key = Key;
            aes.IV = IV;
            using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            {
                return decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
            }
        }
    }
}
