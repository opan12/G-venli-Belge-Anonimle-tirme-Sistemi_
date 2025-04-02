using Microsoft.AspNetCore.Mvc;
using Güvenli_Belge_Anonimleştirme_Sistemi.Data;
using Güvenli_Belge_Anonimleştirme_Sistemi.Model;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using iTextSharp.text.pdf;
using iTextSharp.text;
using Güvenli_Belge_Anonimleştirme_Sistemi.Services;

namespace Güvenli_Belge_Anonimleştirme_Sistemi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class YorumController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public readonly IMakaleLogService _makaleLogService;

        public YorumController(ApplicationDbContext context, IMakaleLogService makaleLogService)
        {
            _context = context;
            _makaleLogService = makaleLogService;
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
            var reviewerAssignedArticle = await _context.reviews
        .FirstOrDefaultAsync(ra => ra.MakaleId == model.MakaleId && ra.ReviewerId == model.ReviewerId);

            if (reviewerAssignedArticle == null)
            {
                return BadRequest("Bu makale üzerinde yorum yapma yetkiniz yok.");
            }
            // Yeni yorumu veritabanına ekle
            var yeniYorum = new Yorum
            {
                MakaleId = model.MakaleId,
                ReviewerId = model.ReviewerId,
                Comments = model.Comments,
                ReviewDate = DateTime.Now
            };

            _context.reviews.Add(yeniYorum);

            // Makale durumunu güncelle
            makale.Status = "Yorum eklendi";
            _context.Articles.Update(makale);

            await _context.SaveChangesAsync();

            // PDF Yolu
            string pdfPath = makale.AnonymizedContent;
            if (string.IsNullOrEmpty(pdfPath) || !System.IO.File.Exists(pdfPath))
            {
                return NotFound("Anonimleştirilmiş PDF dosyası bulunamadı.");
            }

            string updatedPdfPath = Path.Combine(Path.GetDirectoryName(pdfPath), $"Updated_{Path.GetFileName(pdfPath)}");

            try
            {
                using (var existingPdfStream = new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var newPdfStream = new FileStream(updatedPdfPath, FileMode.Create, FileAccess.Write))
                using (var reader = new PdfReader(existingPdfStream))
                using (var stamper = new PdfStamper(reader, newPdfStream))
                {
                    int lastPage = reader.NumberOfPages;
                    stamper.InsertPage(lastPage + 1, reader.GetPageSize(lastPage)); // Yeni sayfa ekle

                    PdfContentByte canvas = stamper.GetOverContent(lastPage + 1);
                    BaseFont baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);

                    canvas.BeginText();
                    canvas.SetFontAndSize(baseFont, 12);
                    canvas.SetTextMatrix(50, 750); // Yeni sayfanın en üstüne yerleştirme

                    // Yeni eklenen yorumu yeni sayfaya ekle
                    canvas.ShowText("--- Yeni Yorum ---");
                    canvas.ShowTextAligned(PdfContentByte.ALIGN_LEFT, $"Yorum: {yeniYorum.Comments}", 50, 730, 0);
                    canvas.ShowTextAligned(PdfContentByte.ALIGN_LEFT, $"Tarih: {yeniYorum.ReviewDate:yyyy-MM-dd HH:mm}", 50, 710, 0);

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


            /* if (model == null || string.IsNullOrWhiteSpace(model.Comments))
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
             }*/
        }
        [HttpGet]
        public async Task<IActionResult> GetLogs()
        {
            var logs = await _context.Logs
                .OrderByDescending(l => l.ActionDate)
                .Select(l => new
                {
                    l.Id,
                    l.TrackingNumber,
                    l.Action,
                    ActionDate = l.ActionDate.ToString("yyyy-MM-ddTHH:mm:ss"), // ISO-8601 formatı
                    l.PerformedBy
                })
                .ToListAsync();

            return Ok(logs);
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
                var trackingNumber = article.TrackingNumber;
                await _makaleLogService.LogMakaleAction(trackingNumber, "hakem değiştirildi", "Yönetici", DateTime.Now);

                //  await _makaleLogService.LogMakaleAction(trackingNumber, "hakem değiştirildi", "Yönetici", DateTime.Now);

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
[HttpPost("AddCommentToPdf")]
public async Task<IActionResult> AddCommentToPdfAsync(int makaleId)
{
    // Veritabanından makaleyi al
    var makale = await _context.Articles.FirstOrDefaultAsync(m => m.Id == makaleId);

    if (makale == null)
    {
        return NotFound("Makale bulunamadı.");
    }

    // Yorum verisini veritabanından al (Son yorumu alıyoruz)
    var yorum = await _context.reviews
                                .Where(y => y.MakaleId == makaleId)
                                .OrderByDescending(y => y.ReviewDate) // Son inceleme ilk alınacak
                                .FirstOrDefaultAsync(); // Sadece son yorumu al

    if (yorum == null)
    {
        return NotFound("Makale için yorum bulunamadı.");
    }

    // PDF yolu
    string pdfPath = makale.DecryptedContent;
    if (string.IsNullOrEmpty(pdfPath) || !System.IO.File.Exists(pdfPath))
    {
        return NotFound("PDF dosyası bulunamadı.");
    }

    string updatedPdfPath = Path.Combine(Path.GetDirectoryName(pdfPath), $"Updated_{Path.GetFileName(pdfPath)}");

    try
    {
        // PDF dosyasını oku ve güncelle
        using (var existingPdfStream = new FileStream(pdfPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var newPdfStream = new FileStream(updatedPdfPath, FileMode.Create, FileAccess.Write))
        using (var reader = new PdfReader(existingPdfStream))
        using (var stamper = new PdfStamper(reader, newPdfStream))
        {
            int lastPage = reader.NumberOfPages;
            stamper.InsertPage(lastPage + 1, reader.GetPageSize(lastPage)); // Yeni sayfa ekle

            PdfContentByte canvas = stamper.GetOverContent(lastPage + 1);
            BaseFont baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);

            canvas.BeginText();
            canvas.SetFontAndSize(baseFont, 12);
            canvas.SetTextMatrix(50, 750); // Yeni sayfanın en üstüne yerleştirme

            // Yeni yorumu ekle
            canvas.ShowText("--- Yeni Yorum ---");
            canvas.ShowTextAligned(PdfContentByte.ALIGN_LEFT, $"Yorum: {yorum.Comments}", 50, 730, 0);
            canvas.ShowTextAligned(PdfContentByte.ALIGN_LEFT, $"Tarih: {yorum.ReviewDate:yyyy-MM-dd HH:mm}", 50, 710, 0);

            canvas.EndText();
            stamper.Close();
        }

        // Yeni PDF dosyasını belirli bir dizine kaydet
        string newSavePath = Path.Combine("wwwroot", "uploads", "updated", Path.GetFileName(updatedPdfPath));
        if (!Directory.Exists(Path.GetDirectoryName(newSavePath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(newSavePath));
        }

        System.IO.File.Move(updatedPdfPath, newSavePath, true);

        // Makale durumunu ve yeni PDF yolunu güncelle
        makale.Status = "Teslim Edildi";
        makale.DecryptedContent = newSavePath;
        _context.Articles.Update(makale);

        await _context.SaveChangesAsync();
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
                // Geçersiz kimlik kontrolü
                if (model.ArticleId <= 0 || model.ReviewerId <= 0)
                {
                    return BadRequest("Geçersiz makale veya hakem kimliği.");
                }

                // Makale ve hakemi veritabanından bul
                var article = await _context.Articles.FindAsync(model.ArticleId);
                var reviewer = await _context.Reviewers.FindAsync(model.ReviewerId);

                // Eğer makale veya hakem bulunamazsa hata dön
                if (article == null || reviewer == null)
                {
                    return NotFound("Makale veya hakem bulunamadı.");
                }

                // Yeni yorum oluştur
                var yorum = new Yorum
                {
                    MakaleId = model.ArticleId,
                    ReviewerId = model.ReviewerId,
                    ReviewDate = DateTime.Now,
                    Comments = "",
                };

                // Yorum veritabanına ekle
                _context.reviews.Add(yorum);
                await _context.SaveChangesAsync();

                // Makalenin tracking numarasını al
                var trackingNumber = article.TrackingNumber; // Makale nesnesinden TrackingNumber alın

                // Loglama işlemi
                Console.WriteLine($"[INFO] {yorum.ReviewerId} hakem olarak atandı!"); // Log Ekle
                await _makaleLogService.LogMakaleAction(trackingNumber, "hakem eklendi", "Yönetici", DateTime.Now);

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