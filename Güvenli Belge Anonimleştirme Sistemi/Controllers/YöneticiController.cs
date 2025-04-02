using Güvenli_Belge_Anonimleştirme_Sistemi.Data;
using Güvenli_Belge_Anonimleştirme_Sistemi.Model;
using Güvenli_Belge_Anonimleştirme_Sistemi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Güvenli_Belge_Anonimleştirme_Sistemi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class YöneticiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public readonly IMakaleLogService _makaleLogService;


        public YöneticiController(ApplicationDbContext context, IMakaleLogService makaleLogService)
        {
            _context = context;
            _makaleLogService = makaleLogService;
        }
        public class UserRegisterModel
        {
            public string UserName { get; set; }
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterModel model)
        {


            var user = new User
            {
                Id = 1,
                UserName = "YONETİCİ",
                Email = "YONETİCİ",
            };
            _context.users.Add(user);

            _context.SaveChanges();

            return Ok(new { Message = "Kullanıcı başarıyla oluşturuldu!", UserId = user.Id });
        }
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] MessageModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Content))
            {
                return BadRequest("Mesaj içeriği boş olamaz.");
            }

            var message = new Message
            {
                SenderEmail = model.SenderEmail,
                ReceiverEmail = "YONETİCİ",  // Yöneticiye mesaj gönderiyoruz
                Content = model.Content,
                SentAt = DateTime.UtcNow
            };

            _context.messages.Add(message);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Mesaj gönderildi.", SentAt = message.SentAt });
        }
        [HttpGet]
        public async Task<IActionResult> GetAdminMessages()
        {
            // Sadece yöneticinin mesajlarını getir
            var adminMessages = await _context.messages
                .Where(m => m.ReceiverEmail == "YONETİCİ") // Yöneticiye özel
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            return Ok(adminMessages);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllArticles()
        {
            // Retrieve all articles from the database
            var articles = await _context.Articles
                .Select(a => new ArticleViewModel
                {
                    Id = a.Id,
                    AuthorEmail = a.AuthorEmail,
                    TrackingNumber = a.TrackingNumber,
                    Status = a.Status,
                    Alan =a.Alan,
                    ArticleDate = a.ArticleDate
                })
                .ToListAsync();

            // Return the list of articles
            return Ok(articles);
        }
        [HttpGet("yorum-eklenenler")]
        public async Task<IActionResult> GetReviewedArticles()
        {
            var reviewedArticles = await _context.Articles
                .Where(m => m.Status == "Yorum eklendi")
                .ToListAsync();

            if (reviewedArticles == null || !reviewedArticles.Any())
            {
                return NotFound("Yorum eklenmiş makale bulunamadı.");
            }

            return Ok(reviewedArticles);
        }
        public class ReviewerViewModel
        {
            public string Alan { get; set; } // Sadece Alan adı
        }

        [HttpGet("getAll")]
        public async Task<ActionResult<IEnumerable<Reviewer>>> GetReviewers()
        {
            var reviewers = await _context.Reviewers.ToListAsync();
            return Ok(reviewers);
        }
        [HttpGet("log")]
        public async Task<ActionResult<IEnumerable<Log>>> GetLogs()
        {
            var logs = await _context.Logs.OrderByDescending(l => l.ActionDate).ToListAsync();
            return Ok(logs);
        }
        [HttpPost("add")]
        public async Task<IActionResult> AddReviewer([FromBody] ReviewerViewModel reviewerViewModel)
        {
            if (reviewerViewModel == null || string.IsNullOrEmpty(reviewerViewModel.Alan))
            {
                return BadRequest("Hakem bilgileri geçersiz.");
            }

            // ReviewerViewModel'den Reviewer modeline dönüştürme
            var reviewer = new Reviewer
            {
                Alan = reviewerViewModel.Alan
            };

            // Hakemi veritabanına ekle
            _context.Reviewers.Add(reviewer);
            await _context.SaveChangesAsync();
            // await _makaleLogService.LogMakaleAction(trackingNumber, "hakem eklendi", "Yönetici", DateTime.Now);
            await _makaleLogService.LogMakaleAction("", "hakem eklendi", "Yönetici", DateTime.Now);

            // Başarılı ekleme sonrası 201 Created dönüyoruz.
            return CreatedAtAction(nameof(AddReviewer), new { alan = reviewer.Alan }, reviewer);
        }
     

        [HttpGet("{id}")]
        public async Task<IActionResult> GetReviewerById(int id)
        {
            var reviewer = await _context.Reviewers.FindAsync(id);
            if (reviewer == null)
            {
                return NotFound("Hakem bulunamadı.");
            }

            return Ok(reviewer);
        }
    
}} public class DecryptRequest

    {
        public string TrackingNumber { get; set; }
    }