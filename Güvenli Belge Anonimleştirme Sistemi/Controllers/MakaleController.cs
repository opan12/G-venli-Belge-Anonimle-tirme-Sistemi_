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
using System.Diagnostics;

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

        private byte[] CreateAnonymizedPdf(string originalPdfPath)
        {
            using (var reader = new PdfReader(originalPdfPath))
            {
                var outputStream = new MemoryStream();
                using (var stamper = new PdfStamper(reader, outputStream))
                {
                    int totalPages = reader.NumberOfPages;

                    for (int pageNum = 1; pageNum <= totalPages; pageNum++)
                    {
                        // Sayfadaki mevcut metni çıkar
                        var pageContent = ExtractTextFromPdfPage(reader, pageNum);

                        // Python betiğini çalıştırarak anonimleştirilmiş metni al
                        var anonymizedContent = RunPythonScript(pageContent);

                        // Sayfanın içeriğini anonimleştir
                        var contentByte = stamper.GetOverContent(pageNum);
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

        private string RunPythonScript(string inputText)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = @"C:\Users\Casper\AppData\Local\Programs\Python\Python311\python.exe", // Python'un tam yolu
                Arguments =  @"C:\Users\Casper\source\repos\Güvenli Belge Anonimleştirme Sistemi\Güvenli Belge Anonimleştirme Sistemi\anonymize.py",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                using (var reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    process.WaitForExit();
                    return result;
                }
            }
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

            // Anonimleştirilmiş dosyanın kaydedileceği klasör
            string directoryPath = System.IO.Path.Combine("wwwroot", "uploads", "anonimized");

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Yeni dosya adı
            string anonymizedFileName = $"anonimized_{trackingNumber}.pdf";
            string anonymizedFilePath = System.IO.Path.Combine(directoryPath, anonymizedFileName);
            // Python scriptini çalıştır ve anonimleştirilmiş dosyayı üret
            var result = await RunPythonScript(filePath, anonymizedFilePath);

            if (!result)
            {
                return BadRequest("Anonimleştirme işlemi başarısız oldu.");
            }

            // Dosya yolunu AES ile şifrele
            await _context.SaveChangesAsync();

            return File(await System.IO.File.ReadAllBytesAsync(anonymizedFilePath), "application/pdf", anonymizedFileName);
        }

        // Python scriptini çalıştırma fonksiyonu
        private async Task<bool> RunPythonScript(string inputFilePath, string outputFilePath)
        {
            try
            {
                string pythonScript = @"C:\Users\Casper\source\repos\Güvenli Belge Anonimleştirme Sistemi\Güvenli Belge Anonimleştirme Sistemi\anonymize.py";
                string pythonExe = @"C:\Users\Casper\AppData\Local\Programs\Python\Python311\python.exe";

                var startInfo = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = $"\"{pythonScript}\" \"{inputFilePath}\" \"{outputFilePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (!string.IsNullOrEmpty(error))
                    {
                        Console.WriteLine("Python Hatası: " + error);
                        return false;
                    }

                    Console.WriteLine("Python Çıktısı: " + output);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Hata: " + ex.Message);
                return false;
            }
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
