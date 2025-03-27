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
using Güvenli_Belge_Anonimleştirme_Sistemi.Model;
using System.IO; // Çakışmayı önlemek için
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;


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
                Arguments = @"C:\Users\Casper\source\repos\Güvenli Belge Anonimleştirme Sistemi\Güvenli Belge Anonimleştirme Sistemi\anonymize.py",
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
        public async Task<IActionResult> AnonimizeMakale(string trackingNumber, [FromBody] AnonymizationOptions options)
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

            var inputFilePath = makale.ContentPath;
            var outputFilePath = System.IO.Path.Combine(
     System.IO.Path.GetDirectoryName(inputFilePath),
     $"anon_{System.IO.Path.GetFileName(inputFilePath)}"
 );


            if (!System.IO.File.Exists(inputFilePath))
            {
                return BadRequest("PDF dosyası mevcut değil.");
            }

            // Python scriptini çalıştır
            bool success = await RunPythonScript(inputFilePath, outputFilePath, options);
            if (!success)
            {
                return StatusCode(500, "Python scripti çalıştırılamadı.");
            }

            // Base64 yerine dosya yolunu kaydet
            makale.AnonymizedContent = outputFilePath;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Anonimleştirme tamamlandı ve veritabanına kaydedildi.", path = outputFilePath });
        }
        [HttpGet("yorum-eklenenler-indir")]
        public async Task<IActionResult> DownloadReviewedArticle(int makaleıd)
        {
            var reviewedArticle = await _context.Articles
                .Where(m => m.Id == makaleıd)
                .Select(m => m.AnonymizedContent) // Sadece dosya yolunu çekiyoruz
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(reviewedArticle))
            {
                return NotFound("Yorum eklenmiş makale bulunamadı.");
            }

            var filePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", reviewedArticle);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Dosya bulunamadı.");
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "application/pdf", System.IO.Path.GetFileName(filePath));
        }


        /*   [HttpPost("anonimize/{trackingNumber}")]
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

               var inputFilePath = makale.ContentPath;
               var outputFilePath = System.IO.Path.Combine(
                   System.IO.Path.GetDirectoryName(inputFilePath),
                   $"anon_{System.IO.Path.GetFileName(inputFilePath)}"
               );

               if (!System.IO.File.Exists(inputFilePath))
               {
                   return BadRequest("PDF dosyası mevcut değil.");
               }

               // 🛠 Python scriptini çalıştır
               bool success = await RunPythonScript(inputFilePath, outputFilePath);
               if (!success)
               {
                   return StatusCode(500, "Python scripti çalıştırılamadı.");
               }

               // 📂 Anonimleştirilmiş PDF'yi Base64 formatına çevir
               string base64Pdf = Convert.ToBase64String(await System.IO.File.ReadAllBytesAsync(outputFilePath));

               // 📥 Veritabanına kaydet
               makale.AnonymizedContent = base64Pdf;
               await _context.SaveChangesAsync();

               // 🗑 Geçici dosyayı temizle
               System.IO.File.Delete(outputFilePath);

               return Ok(new { message = "Anonimleştirme tamamlandı ve veritabanına kaydedildi." });
           }*/
        [HttpGet("download-anon/{trackingNumber}")]
        public async Task<IActionResult> DownloadAnonymizedPdf(string trackingNumber)
        {
            var makale = await _context.Articles.FirstOrDefaultAsync(m => m.TrackingNumber == trackingNumber);
            if (makale == null)
            {
                return NotFound("Makale bulunamadı.");
            }

            if (string.IsNullOrEmpty(makale.AnonymizedContent) || !System.IO.File.Exists(makale.AnonymizedContent))
            {
                return BadRequest("Anonimleştirilmiş makale bulunamadı veya dosya mevcut değil.");
            }

            // 📌 Python dosyasını çalıştır ve çıktıyı al (Dosya yolunu gönderiyoruz)
            string decryptedText = RunPythonScript1("decrypt_script.py", makale.AnonymizedContent);

            if (string.IsNullOrEmpty(decryptedText))
            {
                return BadRequest("Şifrelenmiş veri çözülemedi.");
            }

            var filePath = makale.AnonymizedContent;
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var fileName = $"anon_{trackingNumber}.pdf";

            return File(fileBytes, "application/pdf", fileName);
        }

        private string RunPythonScript1(string scriptName, string filePath)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "python",  // Python çalıştırıcısını belirt
                    Arguments = $"\"{scriptName}\" \"{filePath}\"",  // Argüman olarak script adı ve dosya yolu ekle
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null)
                        throw new Exception("Python süreci başlatılamadı.");

                    using (var outputReader = process.StandardOutput)
                    using (var errorReader = process.StandardError)
                    {
                        string output = outputReader.ReadToEnd();
                        string error = errorReader.ReadToEnd();

                        process.WaitForExit();  // Python betiğinin tamamlanmasını bekle

                        if (!string.IsNullOrEmpty(error))
                            throw new Exception($"Python Hatası: {error}");

                        return output.Trim();  // Çıktıyı temizleyerek döndür
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
                return $"Hata: {ex.Message}";
            }
        }



        // 🔄 Python scriptini çalıştırma fonksiyonu
        /*     private async Task<bool> RunPythonScript(string inputFilePath, string outputFilePath)
             {
                 try
                 {
                     string pythonScript = @"C:\Users\medih\Source\Repos\G-venli-Belge-Anonimle-tirme-Sistemi_\Güvenli Belge Anonimleştirme Sistemi\anonymize.py";
                     string pythonExe = @"C:\Users\medih\AppData\Local\Programs\Python\Python311\python.exe";
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
        */
        private async Task<bool> RunPythonScript(string inputFilePath, string outputFilePath, AnonymizationOptions options)
        {
            try
            {
                //string pythonScript = @"C:\Users\Casper\source\repos\Güvenli Belge Anonimleştirme Sistemi\Güvenli Belge Anonimleştirme Sistemi\anonymize.py";
                // string pythonExe = @"C:\Users\Casper\AppData\Local\Programs\Python\Python311\python.exe";
                string pythonScript = @"C:\Users\medih\Source\Repos\G-venli-Belge-Anonimle-tirme-Sistemi_\Güvenli Belge Anonimleştirme Sistemi\anonymize.py";
                string pythonExe = @"C:\Users\medih\AppData\Local\Programs\Python\Python311\python.exe";

                string maskAuthors = options.MaskAuthors ? "names" : "";
                string maskEmails = options.MaskEmails ? "emails" : "";
                string maskOrganizations = options.MaskOrganizations ? "organizations" : "";
                string maskPhoneNumbers = options.MaskPhoneNumbers ? "phoneNumbers" : "";
                string maskLocations = options.MaskLocations ? "locations" : "";

                // Parametreleri birleştir
                string optionsString = string.Join(",", new[] { maskAuthors, maskEmails, maskOrganizations, maskPhoneNumbers, maskLocations }
                                                     .Where(arg => !string.IsNullOrEmpty(arg)));

                // Python scriptini çalıştır
                var startInfo = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = $"\"{pythonScript}\" \"{inputFilePath}\" \"{outputFilePath}\" {optionsString}",
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

        [HttpGet("yorum-eklenenler-user")]
        public async Task<IActionResult> GetuserArticles(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("E-posta adresi boş olamaz.");
            }

            try
            {
                var reviewedArticles = await _context.Articles
                    .Where(m => m.Status == "Yorum eklendi" && m.AuthorEmail.ToLower() == email.ToLower())
                    .ToListAsync();

                if (reviewedArticles == null || !reviewedArticles.Any())
                {
                    return NotFound($"E-posta adresiyle eşleşen makale bulunamadı: {email}");
                }

                return Ok(reviewedArticles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Sunucu hatası: {ex.Message} \n {ex.StackTrace}");
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

            // wwwroot içindeki yolunu istemciye göndermek için relative path oluştur
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