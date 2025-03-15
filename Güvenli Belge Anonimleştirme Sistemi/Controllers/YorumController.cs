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

            // Makale olup olmadığını kontrol et
            var makale = await _context.Articles.FindAsync(model.MakaleId);
            if (makale == null)
            {
                return NotFound("Makale bulunamadı.");
            }

            // Var olan yorumu getir
            var mevcutYorum = await _context.reviews
                .FirstOrDefaultAsync(y => y.MakaleId == model.MakaleId && y.ReviewerId == model.ReviewerId);

            if (mevcutYorum == null)
            {
                return NotFound("Bu makale için var olan bir yorum bulunamadı.");
            }

            // Yorumu güncelle
            mevcutYorum.Comments = model.Comments;
            mevcutYorum.ReviewDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // PDF Güncelleme İşlemi
            string pdfPath = makale.ContentPath;
            string updatedPdfPath = Path.Combine(Path.GetDirectoryName(pdfPath), $"Updated_{Path.GetFileName(pdfPath)}");

            try
            {
                using (var existingPdfStream = new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var newPdfStream = new FileStream(updatedPdfPath, FileMode.Create, FileAccess.Write))
                using (var reader = new PdfReader(existingPdfStream))
                using (var document = new Document())
                using (var writer = new PdfCopy(document, newPdfStream))
                {
                    document.Open();

                    // Mevcut sayfaları kopyala
                    for (int i = 1; i <= reader.NumberOfPages; i++)
                    {
                        writer.AddPage(writer.GetImportedPage(reader, i));
                    }

                    // Yeni sayfa ekleyerek yorumları ekle
                    document.NewPage();
                    var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
                    var commentFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);

                    document.Add(new Paragraph("\n--- Güncellenmiş Yorumlar ---\n", titleFont));

                    var yorumlar = await _context.reviews
                        .Where(y => y.MakaleId == model.MakaleId)
                        .OrderBy(y => y.ReviewDate)
                        .ToListAsync();

                    foreach (var yorum in yorumlar)
                    {
                        document.Add(new Paragraph($"Yorum: {yorum.Comments}", commentFont));
                        document.Add(new Paragraph($"Tarih: {yorum.ReviewDate:yyyy-MM-dd HH:mm}", commentFont));
                        document.Add(new Paragraph("\n------------------------------------\n", commentFont));
                    }

                    document.Close();
                    writer.Close();
                    reader.Close();
                }

                // Güncellenmiş PDF'yi orijinalin yerine koy
                if (System.IO.File.Exists(pdfPath))
                {
                    System.IO.File.Delete(pdfPath);
                }

                System.IO.File.Move(updatedPdfPath, pdfPath);

                // Güncellenmiş PDF'yi indirilebilir olarak döndür
                var fileBytes = await System.IO.File.ReadAllBytesAsync(pdfPath);
                return File(fileBytes, "application/pdf", Path.GetFileName(pdfPath));
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