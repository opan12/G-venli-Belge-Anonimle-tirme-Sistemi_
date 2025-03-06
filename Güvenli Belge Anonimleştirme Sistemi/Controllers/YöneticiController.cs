using Güvenli_Belge_Anonimleştirme_Sistemi.Data;
using Güvenli_Belge_Anonimleştirme_Sistemi.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
                    Title = a.Title,
                    AuthorEmail = a.AuthorEmail,
                    TrackingNumber = a.TrackingNumber,
                    Status = a.Status,
                    ArticleDate = a.ArticleDate
                })
                .ToListAsync();

            // Return the list of articles
            return Ok(articles);
        }
    }
}
