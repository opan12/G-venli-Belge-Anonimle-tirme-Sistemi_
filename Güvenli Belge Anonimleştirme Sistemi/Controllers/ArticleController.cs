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
            var articleArea = DetermineArticleAreas(filePath); // Bu metod NLP ile alanı belirleyecek

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
                Alan = string.Join(", ", articleArea) // Liste elemanlarını virgülle ayırarak bir dize oluştur
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
        private List<string> DetermineArticleAreas(string filePath)
        {
            // Read the content of the file
            string fileContent;
            using (var reader = new StreamReader(filePath))
            {
                fileContent = reader.ReadToEnd();
            }

            // Define keywords for each area
            var keywords = new Dictionary<string, List<string>>
    {
        { "Artificial Intelligence", new List<string> { "deep learning", "natural language processing", "computer vision", "generative AI" } },
        { "Human-Computer Interaction", new List<string> { "brain-computer interfaces", "user experience design", "augmented reality", "virtual reality" } },
        { "Big Data and Data Analytics", new List<string> { "data mining", "data visualization", "Hadoop", "Spark", "time series analysis" } },
        { "Cybersecurity", new List<string> { "encryption algorithms", "secure software development", "network security", "authentication systems", "digital forensics" } },
        { "Networking and Distributed Systems", new List<string> { "5G", "cloud computing", "blockchain", "P2P systems", "decentralized systems" } }
    };

            // List to hold found areas
            var foundAreas = new List<string>();

            // Check the content for keywords and determine the areas
            foreach (var keyword in keywords)
            {
                foreach (var term in keyword.Value)
                {
                    if (fileContent.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (!foundAreas.Contains(keyword.Key))
                        {
                            foundAreas.Add(keyword.Key); // Add the area if a keyword is found
                        }
                        break; // Break out of the term loop to avoid duplicates for the same area
                    }
                }
            }

            return foundAreas.Count > 0 ? foundAreas : new List<string> { "Unknown Area" }; // Return found areas or "Unknown Area"
        }

    }
}