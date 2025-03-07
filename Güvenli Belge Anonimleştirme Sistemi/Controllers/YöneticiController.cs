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

        [HttpPost("add")]
        public async Task<IActionResult> AddReviewer([FromBody] Reviewer reviewer)
        {
            if (reviewer == null || string.IsNullOrEmpty(reviewer.Alan))
            {
                return BadRequest("Hakem bilgileri geçersiz.");
            }

            // Hakemi veritabanına ekle
            _context.Reviewers.Add(reviewer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReviewerById), new { id = reviewer.Id }, reviewer);
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