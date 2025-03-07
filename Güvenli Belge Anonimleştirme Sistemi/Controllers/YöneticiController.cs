using Güvenli_Belge_Anonimleştirme_Sistemi.Data;
using Güvenli_Belge_Anonimleştirme_Sistemi.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Güvenli_Belge_Anonimleştirme_Sistemi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class YöneticiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public YöneticiController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet("all")]
        public async Task<IActionResult> GetAllArticles()
        {
            // Retrieve all articles from the database
            var articles = await _context.Articles
                .Select(a => new ArticleViewModel
                {
                    Id = a.Id,
                    AuthorEmail = a.AuthorEmail,
                    TrackingNumber = a.TrackingNumber,
                    Status = a.Status,
                    ArticleDate = a.ArticleDate
                })
                .ToListAsync();

            // Return the list of articles
            return Ok(articles);
        }
    
    [HttpPost("add")]
    public async Task<IActionResult> AddReviewer([FromBody] Reviewer reviewer)
    {
        if (reviewer == null || string.IsNullOrEmpty(reviewer.Alan))
        {
            return BadRequest("Hakem bilgileri geçersiz.");
        }

        // Hakemi veritabanına ekle
        _context.Reviewers.Add(reviewer);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetReviewerById), new { id = reviewer.Id }, reviewer);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetReviewerById(int id)
    {
        var reviewer = await _context.Reviewers.FindAsync(id);
        if (reviewer == null)
        {
            return NotFound("Hakem bulunamadı.");
        }

        return Ok(reviewer);
    }
       

// Şifreleme için gerekli metot
public byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
    {
        if (plainText == null || plainText.Length <= 0)
            throw new ArgumentNullException("plainText");
        if (Key == null || Key.Length <= 0)
            throw new ArgumentNullException("Key");
        if (IV == null || IV.Length <= 0)
            throw new ArgumentNullException("IV");

        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Key;
            aesAlg.IV = IV;

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }
                    return msEncrypt.ToArray();
                }
            }
        }
    }
        private string AnonymizeText(string inputText)
        {
            // E-posta adreslerini anonimleştir
            var emailPattern = @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}";
            inputText = Regex.Replace(inputText, emailPattern, "[ANONYMIZED_EMAIL]");

            // Yazar isimlerini anonimleştir
            var personPattern = @"\b[A-Z][a-z]* [A-Z][a-z]*\b"; // Yazar isimleri
            inputText = Regex.Replace(inputText, personPattern, "[ANONYMIZED_PERSON]");

            // Kurum adlarını anonimleştir
            var organizationPattern = @"\b(?:Google|Microsoft|Tech Corp)\b"; // Örnek kurum adları
            inputText = Regex.Replace(inputText, organizationPattern, "[ANONYMIZED_ORG]");

            return inputText;
        }

        // Makaleyi şifrele ve hakeme ilet
        public async Task SendArticleToReviewer(int reviewerId, string articleContent)
    {
        // 1. Makale içeriğini anonimleştir
        string anonymizedContent = AnonymizeText(articleContent);

        // 2. Şifrelemek için anahtar ve IV oluştur
        using (Aes aes = Aes.Create())
        {
            aes.GenerateKey();
            aes.GenerateIV();

            byte[] encryptedContent = EncryptStringToBytes_Aes(anonymizedContent, aes.Key, aes.IV);

            // 3. Şifrelenmiş makale içeriğini gönder
            // Burada hakemin e-posta adresi veya başka bir iletişim kanalı ile gönderim yapabilirsiniz
            // Örnek: await SendEmail(reviewerId, encryptedContent, aes.Key, aes.IV);
        }
    }

}
}
