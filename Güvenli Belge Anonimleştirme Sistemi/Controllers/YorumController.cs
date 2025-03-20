using Microsoft.AspNetCore.Mvc;
using Güvenli_Belge_Anonimleştirme_Sistemi.Data;
using Güvenli_Belge_Anonimleştirme_Sistemi.Model;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using iTextSharp.text.pdf;
using iTextSharp.text;

namespace Güvenli_Belge_Anonimleştirme_Sistemi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class YorumController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public YorumController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("makaleler/{reviewerId}")]
        public async Task<IActionResult> GetReviewerMakaleler(int reviewerId)
        {
            var makaleler = await _context.reviews
                .Where(y => y.ReviewerId == reviewerId)
                .Select(y => y.Makale) // Sadece makaleleri seç
                .Distinct() // Aynı makale tekrar eklenmesin
                .ToListAsync();

            if (!makaleler.Any())
            {
                return NotFound("Bu hakeme ait makale bulunamadı.");
            }

            return Ok(makaleler);
        }
        [HttpGet("hakem/{reviewerId}")]
        public async Task<IActionResult> GetYorumlarByReviewer(int reviewerId)
        {
            var yorumlar = await (from y in _context.reviews
                                  join m in _context.Articles on y.MakaleId equals m.Id
                                  where y.ReviewerId == reviewerId
                                  select new
                                  {
                                      y.Id,
                                      y.MakaleId,
                                  //  y.Makale.TrackingNumber,
                                      y.Comments,
                                      y.ReviewDate
                                  })
                                  .ToListAsync();

            if (!yorumlar.Any())
            {
                return NotFound(new { message = "Bu hakeme ait yorum bulunamadı." });
            }

            return Ok(yorumlar);
        }

        public class YorumViewModel1
        {
            public int MakaleId { get; set; }  // Hangi makale için yorum yapılıyor
            public int ReviewerId { get; set; } // Yorumu yapan hakem
            public string Comments { get; set; } // Yorum içeriği
        }
        [HttpPost("yorum-ekle")]
        public async Task<IActionResult> UpdateReview([FromBody] YorumViewModel1 model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Comments))
            {
                return BadRequest("Yorum bilgileri eksik.");
            }

            var makale = await _context.Articles.FindAsync(model.MakaleId);
            if (makale == null)
            {
                return NotFound("Makale bulunamadı.");
            }

            // Yeni yorumu ekle
            var yeniYorum = new Yorum
            {
                MakaleId = model.MakaleId,
                ReviewerId = model.ReviewerId,
                Comments = model.Comments,
                ReviewDate = DateTime.UtcNow
            };

            _context.reviews.Add(yeniYorum);
            await _context.SaveChangesAsync();

            // PDF Yolu
            string pdfPath = makale.ContentPath;
            string updatedPdfPath = Path.Combine(Path.GetDirectoryName(pdfPath), $"Updated_{Path.GetFileName(pdfPath)}");

            try
            {
                using (var existingPdfStream = new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var newPdfStream = new FileStream(updatedPdfPath, FileMode.Create, FileAccess.Write))
                using (var reader = new PdfReader(existingPdfStream))
                using (var stamper = new PdfStamper(reader, newPdfStream))
                {
                    PdfContentByte canvas = stamper.GetOverContent(reader.NumberOfPages);
                    BaseFont baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                    canvas.BeginText();
                    canvas.SetFontAndSize(baseFont, 12);
                    canvas.SetTextMatrix(50, 100);

                    // Yeni eklenen yorumu PDF'ye ekle
                    canvas.ShowText("--- Yeni Yorum ---");
                    canvas.ShowTextAligned(PdfContentByte.ALIGN_LEFT, $"Yorum: {yeniYorum.Comments}", 50, 80, 0);
                    canvas.ShowTextAligned(PdfContentByte.ALIGN_LEFT, $"Tarih: {yeniYorum.ReviewDate:yyyy-MM-dd HH:mm}", 50, 60, 0);

                    canvas.EndText();
                    stamper.Close();
                }

                string newSavePath = Path.Combine("wwwroot", "uploads", "updated", Path.GetFileName(updatedPdfPath));
                if (!Directory.Exists(Path.GetDirectoryName(newSavePath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(newSavePath));
                }

                System.IO.File.Move(updatedPdfPath, newSavePath, true);

                var fileBytes = await System.IO.File.ReadAllBytesAsync(newSavePath);
                return File(fileBytes, "application/pdf", Path.GetFileName(newSavePath));
            }
            catch (IOException ioEx)
            {
                return StatusCode(500, $"PDF güncellenirken dosya hatası oluştu: {ioEx.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"PDF güncellenirken hata oluştu: {ex.Message}");
            }
        }

        [HttpPost("change-reviewer")]
        public async Task<IActionResult> ChangeReviewer([FromBody] YorumViewModel model)
        {
            try
            {
                if (model.ArticleId <= 0 || model.ReviewerId <= 0)
                {
                    return BadRequest("Geçersiz makale veya hakem kimliği.");
                }

                var article = await _context.Articles.FindAsync(model.ArticleId);
                var newReviewer = await _context.Reviewers.FindAsync(model.ReviewerId);

                if (article == null || newReviewer == null)
                {
                    return NotFound("Makale veya hakem bulunamadı.");
                }

                // Mevcut atamayı kontrol et
                var existingReview = await _context.reviews
                    .FirstOrDefaultAsync(r => r.MakaleId == model.ArticleId);

                if (existingReview != null)
                {
                    // Eski hakem bilgisi
                    int oldReviewerId = existingReview.ReviewerId;
                    existingReview.ReviewerId = model.ReviewerId;
                    existingReview.ReviewDate = DateTime.Now;

                    await _context.SaveChangesAsync();

                    Console.WriteLine($"[INFO] Hakem değiştirildi: {oldReviewerId} → {model.ReviewerId}");

                    return Ok(new
                    {
                        message = "Hakem başarıyla değiştirildi.",
                        OldReviewerId = oldReviewerId,
                        NewReviewerId = model.ReviewerId,
                        ChangeDate = existingReview.ReviewDate
                    });
                }

                // Eğer makaleye daha önce hakem atanmamışsa yeni atama yap
                var newReview = new Yorum
                {
                    MakaleId = model.ArticleId,
                    ReviewerId = model.ReviewerId,
                    ReviewDate = DateTime.Now,
                    Comments = "",
                };

                _context.reviews.Add(newReview);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[INFO] Yeni hakem atandı: {model.ReviewerId}");

                return Ok(new
                {
                    message = "Yeni hakem başarıyla atandı.",
                    ReviewerId = model.ReviewerId,
                    AssignmentDate = newReview.ReviewDate
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                return StatusCode(500, $"Sunucu hatası: {ex.Message}");
            }
        }

        [HttpGet("reviewers/{field}")]
        public IActionResult GetReviewersByField(string field)
        {
            try
            {
                var reviewers = _context.Reviewers
                    .Where(r => r.Alan == field) 
                    .Select(r => new
                    {
                        Id = r.Id,
                        Alan = r.Alan
                    })
                    .ToList();

                return Ok(reviewers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Hakemler alınırken hata oluştu: " + ex.Message);
            }
        }


        public class YorumViewModel
        {
            public int ArticleId { get; set; } // İncelenen makalenin kimliği
            public int ReviewerId { get; set; } // Hakemin kimliği
            public DateTime AssignmentDate { get; set; } // Atama tarihi
        }
        [HttpPost("assign")]
        public async Task<IActionResult> AssignReviewer([FromBody] YorumViewModel model)
        {
            try
            {
                if (model.ArticleId <= 0 || model.ReviewerId <= 0)
                {
                    return BadRequest("Geçersiz makale veya hakem kimliği.");
                }

                var article = await _context.Articles.FindAsync(model.ArticleId);
                var reviewer = await _context.Reviewers.FindAsync(model.ReviewerId);

                if (article == null || reviewer == null)
                {
                    return NotFound("Makale veya hakem bulunamadı.");
                }

                var yorum = new Yorum
                {
                    MakaleId = model.ArticleId,
                    ReviewerId = model.ReviewerId,
                    ReviewDate = DateTime.Now,
                    Comments = "",
                };

                _context.reviews.Add(yorum);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[INFO] {yorum.ReviewerId} hakem olarak atandı!"); // Log Ekle

                return Ok(new { message = "Hakem başarıyla atandı.", AssignmentDate = yorum.ReviewDate });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                return StatusCode(500, $"Sunucu hatası: {ex.Message}");
            }
        }
    }
}