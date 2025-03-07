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

        // PDF dosyasını veritabanından alıp anonimleştirme işlemi
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

            var filePath = makale.ContentPath; // Veritabanından alınan PDF dosyasının yolu
            if (!System.IO.File.Exists(filePath))
            {
                return BadRequest("PDF dosyası mevcut değil.");
            }

            // Anonimleştirilmiş PDF'yi oluştur
            var anonymizedPdfBytes = await Task.Run(() => CreateAnonymizedPdf(filePath));

            // Dosyayı indirmek için HTTP yanıtı döndür
            return File(anonymizedPdfBytes, "application/pdf", "anonimleştirilmis_makale.pdf");
        }

        // PDF içeriğini anonimleştir ve yeni bir PDF dosyası oluştur (formatı koruyarak)
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
