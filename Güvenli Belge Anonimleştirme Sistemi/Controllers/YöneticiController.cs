using Güvenli_Belge_Anonimleştirme_Sistemi.Data;
using Güvenli_Belge_Anonimleştirme_Sistemi.Model;
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

        public YöneticiController(ApplicationDbContext context)
        {
            _context = context;
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
                    ArticleDate = a.ArticleDate
                })
                .ToListAsync();

            // Return the list of articles
            return Ok(articles);
        }
        public class ReviewerViewModel
        {
            public string Alan { get; set; } // Sadece Alan adı
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
    }
}