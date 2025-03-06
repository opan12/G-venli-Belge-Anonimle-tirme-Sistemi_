using Güvenli_Belge_Anonimleştirme_Sistemi.Data;
using Microsoft.EntityFrameworkCore;

namespace Güvenli_Belge_Anonimleştirme_Sistemi.Services
{
    public class ArticleService
    {
        private readonly ApplicationDbContext _context;

        //    public async Task<byte[]> DownloadArticleAsync(string trackingNumber, string privateKey)
        //    {
        //        var article = await _context.Articles
        //            .FirstOrDefaultAsync(a => a.TrackingNumber == trackingNumber);

        //        if (article == null)
        //        {
        //            throw new FileNotFoundException("Makale bulunamadı.");
        //        }

        //        // RSA ile şifrelenmiş AES anahtarını çöz
        //        var decryptedAesKey = RsaEncryption.Decrypt(article.EncryptedAesKey, privateKey);
        //        AesEncryption.Key = Convert.FromBase64String(decryptedAesKey);

        //        // Şifrelenmiş içeriği dosyadan oku
        //        var encryptedContent = await System.IO.File.ReadAllTextAsync(article.ContentPath);

        //        // İçeriği AES ile çöz
        //        var decryptedContent = AesEncryption.Decrypt(encryptedContent);
        //        return Convert.FromBase64String(decryptedContent);
        //    }

        //public async Task<byte[]> DownloadArticleAsync(string trackingNumber, string privateKey)
        //{
        //    var article = await _context.Articles
        //        .FirstOrDefaultAsync(a => a.TrackingNumber == trackingNumber);

        //    if (article == null)
        //    {
        //        throw new FileNotFoundException("Makale bulunamadı.");
        //    }

        //    // RSA ile şifrelenmiş AES anahtarını çöz
        //    var decryptedAesKey = RsaEncryption.Decrypt(article.EncryptedAesKey, privateKey);
        //    AesEncryption.Key = Convert.FromBase64String(decryptedAesKey);

        //    // Şifrelenmiş içeriği dosyadan oku
        //    var encryptedContent = await System.IO.File.ReadAllTextAsync(article.ContentPath);

        //    // İçeriği AES ile çöz
        //    var decryptedContent = AesEncryption.Decrypt(encryptedContent);
        //    return Convert.FromBase64String(decryptedContent);
        //}
    }
}