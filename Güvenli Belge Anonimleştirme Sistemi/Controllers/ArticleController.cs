using Microsoft.AspNetCore.Mvc;
using Güvenli_Belge_Anonimleştirme_Sistemi.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Threading.Tasks;
using Güvenli_Belge_Anonimleştirme_Sistemi.Data;
using System;

namespace Güvenli_Belge_Anonimleştirme_Sistemi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArticleController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ArticleController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadArticle([FromForm] ArticleUploadModel model)
        {
            if (model.PdfFile == null || model.PdfFile.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            // uploads klasörünün var olup olmadığını kontrol et, yoksa oluştur
            var uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadDirectory))
            {
                Directory.CreateDirectory(uploadDirectory); // uploads klasörünü oluştur
            }

            var fileExtension = Path.GetExtension(model.PdfFile.FileName);
            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadDirectory, fileName);

            // Dosyayı kaydedin
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await model.PdfFile.CopyToAsync(fileStream);
            }

            // NLP ile alan belirleme
            var articleArea = DetermineArticleArea(filePath); // Bu metod NLP ile alanı belirleyecek

            // Makale bilgilerini veritabanına kaydedin
            var article = new Makale
            {
                AuthorEmail = model.AuthorEmail,
                ContentPath = filePath, // Dosyanın tam yolunu saklıyoruz
                TrackingNumber = Guid.NewGuid().ToString(),
                Status = "Uploaded",
                Content = "",
                AnonymizedContent = "",
                ArticleDate = DateTime.Now,
                Alan = articleArea // Alanı kaydedin
            };

            _context.Articles.Add(article);
            await _context.SaveChangesAsync();

            return Ok(new { TrackingNumber = article.TrackingNumber });
        }

        [HttpGet("status/{trackingNumber}")]
        public async Task<IActionResult> GetArticleStatus(string trackingNumber)
        {
            var article = await _context.Articles
                .FirstOrDefaultAsync(a => a.TrackingNumber == trackingNumber);

            if (article == null)
            {
                return NotFound("Makale bulunamadı.");
            }

            return Ok(new { Status = article.Status });
        }

        [HttpGet("reviews/{articleId}")]
        public async Task<IActionResult> GetReviews(int articleId)
        {
            var reviews = await _context.reviews
                .Where(r => r.ArticleId == articleId)
                .Select(r => new
                {
                    ReviewerId = r.ReviewerId,
                    Comments = r.Comments
                })
                .ToListAsync();

            if (!reviews.Any())
            {
                return NotFound("Bu makale için henüz yorum bulunmamaktadır.");
            }

            return Ok(reviews);
        }

        [HttpPut("revise/{trackingNumber}")]
        public async Task<IActionResult> ReviseArticle(string trackingNumber, [FromForm] ArticleUploadModel model)
        {
            // Find the article by tracking number
            var article = await _context.Articles
                .FirstOrDefaultAsync(a => a.TrackingNumber == trackingNumber);

            if (article == null)
            {
                return NotFound("Makale bulunamadı.");
            }

            if (model.PdfFile == null || model.PdfFile.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            // Read the new file content into a byte array
            byte[] fileContent;
            using (var memoryStream = new MemoryStream())
            {
                await model.PdfFile.CopyToAsync(memoryStream);
                fileContent = memoryStream.ToArray();
            }

            // Convert the byte array to a Base64 string
            var base64Content = Convert.ToBase64String(fileContent);

            // Update the article details
            article.ContentPath = base64Content; // Store the new file content as a Base64 string
            article.Status = "Revized"; // Update the status
            article.ArticleDate = DateTime.Now; // Update the revision date

            _context.Articles.Update(article);
            await _context.SaveChangesAsync();

            return Ok(new { TrackingNumber = article.TrackingNumber });
        }
        private string DetermineArticleArea(string filePath)
        {
            // Dosya içeriğini oku
            string fileContent;
            using (var reader = new StreamReader(filePath))
            {
                fileContent = reader.ReadToEnd();
            }

            // Alan belirlemek için anahtar kelimeleri tanımlayın
            var keywords = new Dictionary<string, List<string>>
    {
        { "Biology", new List<string> { "cell", "organism", "ecosystem", "genetics" } },
        { "Physics", new List<string> { "force", "energy", "mass", "quantum" } },
        { "Medicine", new List<string> { "health", "treatment", "disease", "symptom" } },
        { "Artificial Intelligence", new List<string> { "machine learning", "neural network", "algorithm", "data" } },
        { "Chemistry", new List<string> { "reaction", "molecule", "compound", "element" } },
        { "Mathematics", new List<string> { "calculus", "algebra", "geometry", "theorem" } },
        { "Astronomy", new List<string> { "planet", "star", "galaxy", "universe" } }
    };

            // İçerikte anahtar kelimeleri kontrol et ve alanı belirle
            foreach (var keyword in keywords)
            {
                foreach (var term in keyword.Value)
                {
                    if (fileContent.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return keyword.Key; // Anahtar kelime bulunduğunda alanı döndür
                    }
                }
            }

            return "Bilinmeyen Alan"; // Hiçbir anahtar kelime bulunamazsa
        }
    }
}