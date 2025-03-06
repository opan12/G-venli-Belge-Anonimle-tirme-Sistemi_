using Microsoft.AspNetCore.Mvc;
using Güvenli_Belge_Anonimleştirme_Sistemi.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Threading.Tasks;
using Güvenli_Belge_Anonimleştirme_Sistemi.Data;

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


            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.PdfFile.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await model.PdfFile.CopyToAsync(fileStream);
            }

            var article = new Makale
            {
                Title = model.Title,
                AuthorEmail = model.AuthorEmail,
                ContentPath = filePath,
                TrackingNumber = Guid.NewGuid().ToString(),
                Status = "Uploaded",
                ArticleDate= DateTime.Now,
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
            var article = await _context.Articles
                .FirstOrDefaultAsync(a => a.TrackingNumber == trackingNumber);

            if (article == null)
            {
                return NotFound("Makale bulunamadı.");
            }

            // Eski dosyayı sil
            if (System.IO.File.Exists(article.ContentPath))
            {
                System.IO.File.Delete(article.ContentPath);
            }

            // Yeni dosyayı yükle
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.PdfFile.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await model.PdfFile.CopyToAsync(fileStream);
            }

            // Makaleyi güncelle
            article.Title = model.Title;
            article.ContentPath = filePath;
            article.Status = "Revized"; // Durumu güncelle
            article.ArticleDate = DateTime.Now;

            _context.Articles.Update(article);
            await _context.SaveChangesAsync();

            return Ok(new { TrackingNumber = article.TrackingNumber });
        }
    }
}