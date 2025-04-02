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
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Güvenli_Belge_Anonimleştirme_Sistemi.Services;
using iTextSharp.text;



namespace Güvenli_Belge_Anonimleştirme_Sistemi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MakaleController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public readonly IMakaleLogService _makaleLogService;


        public MakaleController(ApplicationDbContext context, IMakaleLogService makaleLogService)
        {
            _context = context;
            _makaleLogService = makaleLogService;


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

            // Dosya yolunu uygun şekilde oluşturuyoruz (Uygun dizin ile)
            var directoryPath = System.IO.Path.GetDirectoryName(inputFilePath);
            var outputFilePath = System.IO.Path.Combine(directoryPath, $"anon_{System.IO.Path.GetFileName(inputFilePath)}");
            var blurFilePath = System.IO.Path.Combine(directoryPath, $"blur_{System.IO.Path.GetFileName(inputFilePath)}");

            if (!System.IO.File.Exists(inputFilePath))
            {
                return BadRequest("PDF dosyası mevcut değil.");
            }

            // Python scriptini çalıştır
            bool success = await RunPythonScript(inputFilePath, outputFilePath, blurFilePath, options);
            if (!success)
            {
                return StatusCode(500, "Python scripti çalıştırılamadı.");
            }

            // Anonimleştirilmiş ve bulanıklaştırılmış dosya yollarını kaydediyoruz
            makale.AnonymizedContent = outputFilePath;
            makale.DeanonymizedContent = blurFilePath;
            await _context.SaveChangesAsync();
            await _makaleLogService.LogMakaleAction(trackingNumber, "Makale anonimleştirildi", "Yönetici", DateTime.Now);

            return Ok(new { message = "Anonimleştirme tamamlandı ve veritabanına kaydedildi.", path = outputFilePath });
        }

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

        [HttpGet("download-blur/{trackingNumber}")]
        public async Task<IActionResult> DownloadBlurPdf(string trackingNumber)
        {
            var makale = await _context.Articles.FirstOrDefaultAsync(m => m.TrackingNumber == trackingNumber);
            if (makale == null)
            {
                return NotFound("Makale bulunamadı.");
            }

            // Bulanıklaştırılmış dosya yolunun doğru olup olmadığını kontrol et
            if (string.IsNullOrEmpty(makale.DeanonymizedContent) || !System.IO.File.Exists(makale.DeanonymizedContent))
            {
                return BadRequest("Bulanıklaştırılmış makale bulunamadı veya dosya mevcut değil.");
            }

            var filePath = makale.DeanonymizedContent;
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var fileName = $"blur_{trackingNumber}.pdf";

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
        [HttpGet("decrypt-anon/{trackingNumber}")]
        public async Task<IActionResult> DecryptAnonymizedPdf(string trackingNumber)
        {
            var makale = await _context.Articles.FirstOrDefaultAsync(m => m.TrackingNumber == trackingNumber);
            if (makale == null)
                return NotFound("Makale bulunamadı.");

            if (string.IsNullOrEmpty(makale.AnonymizedContent) || !System.IO.File.Exists(makale.AnonymizedContent))
                return BadRequest("Anonimleştirilmiş makale bulunamadı veya dosya mevcut değil.");

            // 📌 Python dosyasını çalıştır
            string result = RunPythonScript1("decrypt_script.py", makale.AnonymizedContent);

            // 📌 Python scripti çıktı olarak dosya yolunu döndürüyor olacak
            string outputPdfPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "decrypted_output.pdf");

            if (!System.IO.File.Exists(outputPdfPath))
                return BadRequest("Deşifrelenmiş PDF oluşturulamadı.");

            // 📌 PDF dosyasını 'DecryptedFiles' klasörüne kopyala
            var decryptedFileName = $"decrypted_{trackingNumber}.pdf";
            var decryptedFilePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "DecryptedFiles", decryptedFileName);

            if (!Directory.Exists(System.IO.Path.GetDirectoryName(decryptedFilePath)))
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(decryptedFilePath));

            System.IO.File.Copy(outputPdfPath, decryptedFilePath, true);

            // 📌 Veritabanına PDF yolunu kaydet
            makale.DecryptedContent = decryptedFilePath;
            _context.Articles.Update(makale);
            await _context.SaveChangesAsync();
            await _makaleLogService.LogMakaleAction(trackingNumber, "Makale deşifrelendi", "Yönetici", DateTime.Now);

            // 📌 PDF dosyasını indirilebilir hale getir
            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(decryptedFilePath);
            return File(fileBytes, "application/pdf", decryptedFileName);
        }

        [HttpGet("yorum-eklenenler-indir")]
        public async Task<IActionResult> DownloadReviewedArticle(int makaleId)
        {
            var reviewedArticle = await _context.Articles
                .Where(m => m.Id == makaleId)
                .Select(m => new { m.AnonymizedContent, m.TrackingNumber }) // TrackingNumber ve AnonymizedContent çekiliyor
                .FirstOrDefaultAsync();

            if (reviewedArticle == null || string.IsNullOrEmpty(reviewedArticle.AnonymizedContent))
            {
                return NotFound("Yorum eklenmiş makale bulunamadı.");
            }

            var filePath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", reviewedArticle.AnonymizedContent);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Dosya bulunamadı.");
            }

            // Log kaydı ekle (TrackingNumber artık mevcut)
            await _makaleLogService.LogMakaleAction(reviewedArticle.TrackingNumber, "yorumlanmış Makale indirildi", "Yönetici", DateTime.Now);

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "application/pdf", System.IO.Path.GetFileName(filePath));
        }
        
  
        private async Task<bool> RunPythonScript(string inputFilePath, string outputFilePath, string blurFilePath, AnonymizationOptions options)
        {
            try
            {
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
                    Arguments = $"\"{pythonScript}\" \"{inputFilePath}\" \"{outputFilePath}\" \"{blurFilePath}\" {optionsString}",
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


        [HttpGet("DownloadUpdatedPdf/{makaleId}")]
        public async Task<IActionResult> DownloadUpdatedPdf(int makaleId)
        {
            // Veritabanından makaleyi al
            var makale = await _context.Articles.FirstOrDefaultAsync(m => m.Id == makaleId);
            if (makale == null)
            {
                return NotFound("Makale bulunamadı.");
            }

            // Makale durumu kontrol et
            if (makale.Status != "Teslim Edildi")
            {
                return BadRequest("Makale henüz teslim edilmedi.");
            }

            // Güncellenmiş PDF dosya yolu
            string pdfPath = makale.DecryptedContent;
            if (string.IsNullOrEmpty(pdfPath) || !System.IO.File.Exists(pdfPath))
            {
                return NotFound("Güncellenmiş PDF dosyası bulunamadı.");
            }

            try
            {
                var fileBytes = await System.IO.File.ReadAllBytesAsync(pdfPath);
                return File(fileBytes, "application/pdf", System.IO.Path.GetFileName(pdfPath));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"PDF indirme sırasında hata oluştu: {ex.Message}");
            }
        }


 
    }
}