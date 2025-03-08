using Microsoft.AspNetCore.Mvc;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Güvenli_Belge_Anonimleştirme_Sistemi.Data;
using iTextSharp.text.pdf.parser;
using System.IO; // Çakışmayı önlemek için
using System.Security.Cryptography;
using System.Text;

namespace Güvenli_Belge_Anonimleştirme_Sistemi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MakaleController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MakaleController(ApplicationDbContext context)
        {
            _context = context;
        }


    [HttpPost("anonimize/{trackingNumber}")]
        public async Task<IActionResult> AnonimizeMakale(string trackingNumber)
        {
            var makale = await _context.Articles.FirstOrDefaultAsync(m => m.TrackingNumber == trackingNumber);
            if (makale == null)
            {
                return NotFound("Makale bulunamadı.");
            }

            if (string.IsNullOrEmpty(makale.ContentPath))
            {
                return BadRequest("Makale içerik yolu bulunamadı.");
            }

            var filePath = makale.ContentPath;

            if (!System.IO.File.Exists(filePath))
            {
                return BadRequest("PDF dosyası mevcut değil.");
            }

            // Anonimleştirilmiş dosyanın kaydedileceği klasörü belirle
            string directoryPath = System.IO.Path.Combine("wwwroot", "uploads", "anonimized");

            // Eğer klasör yoksa oluştur
            if (!System.IO.Directory.Exists(directoryPath))
            {
                System.IO.Directory.CreateDirectory(directoryPath);
            }

            // Yeni dosyanın adını oluştur
            string anonymizedFileName = $"anonimized_{trackingNumber}.pdf";
            string anonymizedFilePath = System.IO.Path.Combine(directoryPath, anonymizedFileName);

            // Anonimleştirilmiş PDF'yi oluştur
            byte[] anonymizedPdfBytes = await Task.Run(() => CreateAnonymizedPdf(filePath));

            // Dosyayı sunucuda kaydet
            await System.IO.File.WriteAllBytesAsync(anonymizedFilePath, anonymizedPdfBytes);

            // Dosya yolunu AES ile şifrele
            var aesHelper = new AesEncryptionHelper("1234567890123456"); // 16 byte'lık bir anahtar kullan
            makale.AnonymizedContent = aesHelper.Encrypt(anonymizedFilePath);
            await _context.SaveChangesAsync();

            return File(anonymizedPdfBytes, "application/pdf", anonymizedFileName);
        }

        [HttpGet("get-anonymized-pdf/{trackingNumber}")]
        public IActionResult GetAnonymizedPdf(string trackingNumber)
        {
            var makale = _context.Articles.FirstOrDefault(m => m.TrackingNumber == trackingNumber);
            if (makale == null || string.IsNullOrEmpty(makale.AnonymizedContent))
            {
                return NotFound("Anonimleştirilmiş makale bulunamadı.");
            }

            var filePath = makale.AnonymizedContent;
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Dosya mevcut değil.");
            }

            // `wwwroot` içindeki yolunu istemciye göndermek için relative path oluştur
            var relativePath = filePath.Replace("wwwroot", "").Replace("\\", "/");

            // Tarayıcıdan erişilebilecek URL'yi döndür
            var fileUrl = $"{Request.Scheme}://{Request.Host}{relativePath}";

            return Ok(new { fileUrl });
        }

        [HttpGet("download/{trackingNumber}")]
        public IActionResult DownloadFile(string trackingNumber)
        {
            var makale = _context.Articles.FirstOrDefault(m => m.TrackingNumber == trackingNumber);
            if (makale == null || string.IsNullOrEmpty(makale.AnonymizedContent))
            {
                return NotFound("Makale bulunamadı veya henüz yüklenmedi.");
            }

            // AES ile şifrelenmiş dosya yolunu çöz
            var aesHelper = new AesEncryptionHelper("1234567890123456"); // Aynı anahtarı kullan
            string filePath = aesHelper.Decrypt(makale.AnonymizedContent);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Dosya bulunamadı.");
            }

            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "application/pdf", System.IO.Path.GetFileName(filePath));
        }

        private byte[] CreateAnonymizedPdf(string originalPdfPath)
        {
            using (var reader = new PdfReader(originalPdfPath))
            {
                // Yeni PDF dosyasını oluşturmak için bellek akışı kullanıyoruz
                var outputStream = new MemoryStream();
                using (var stamper = new PdfStamper(reader, outputStream))
                {
                    // Sayfa sayfa anonimleştir
                    int totalPages = reader.NumberOfPages;

                    for (int pageNum = 1; pageNum <= totalPages; pageNum++)
                    {
                        // Sayfadaki mevcut metni çıkar
                        var pageContent = ExtractTextFromPdfPage(reader, pageNum);

                        // Anonimleştirilmiş metni oluştur
                        var anonymizedContent = AnonymizeText(pageContent);

                        // Sayfanın içeriğini anonimleştir
                        var contentByte = stamper.GetOverContent(pageNum);

                        // Burada metni anonimleştirilmiş haliyle ekleyebiliriz
                        contentByte.BeginText();
                        contentByte.SetFontAndSize(BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED), 12);
                        contentByte.ShowTextAligned(PdfContentByte.ALIGN_LEFT, anonymizedContent, 36, 800, 0);
                        contentByte.EndText();
                    }

                    stamper.Close();
                }

                return outputStream.ToArray();
            }
        }

        // PDF sayfasından metin çıkarma
        private string ExtractTextFromPdfPage(PdfReader reader, int pageNum)
        {
            return PdfTextExtractor.GetTextFromPage(reader, pageNum);
        }

        // Metni anonimleştirme (e-posta, yazar isimleri vb.)
        private string AnonymizeText(string inputText)
        {
            // E-posta adreslerini anonimleştir
            var emailPattern = @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}";
            inputText = Regex.Replace(inputText, emailPattern, "[ANONYMIZED_EMAIL]");

            // Yazar isimlerini anonimleştir
            var personPattern = @"\b[A-Z][a-z]* [A-Z][a-z]*\b"; // Yazar isimleri
            inputText = Regex.Replace(inputText, personPattern, "[ANONYMIZED_PERSON]");

            // Kurum adlarını anonimleştir
            var organizationPattern = @"\b(?:Google|Microsoft|Tech Corp)\b";
            inputText = Regex.Replace(inputText, organizationPattern, "[ANONYMIZED_ORG]");

            return inputText;
        }
    }
}
