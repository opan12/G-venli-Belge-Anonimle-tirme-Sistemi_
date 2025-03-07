using Microsoft.AspNetCore.Mvc;
using Güvenli_Belge_Anonimleştirme_Sistemi.Data;
using Güvenli_Belge_Anonimleştirme_Sistemi.Model;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Güvenli_Belge_Anonimleştirme_Sistemi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class YorumController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public YorumController(ApplicationDbContext context)
        {
            _context = context;
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
            // Modelin doğrulanması
            if (model.ArticleId <= 0 || model.ReviewerId <= 0)
            {
                return BadRequest("Geçersiz makale veya hakem kimliği.");
            }

            // Makaleyi ve hakemi bul
            var article = await _context.Articles.FindAsync(model.ArticleId);
            var reviewer = await _context.Reviewers.FindAsync(model.ReviewerId);

            if (article == null || reviewer == null)
            {
                return NotFound("Makale veya hakem bulunamadı.");
            }

            // Yeni bir yorum oluştur ve atamayı kaydet
            var yorum = new Yorum
            {
                ArticleId = model.ArticleId,
                ReviewerId = model.ReviewerId,
                ReviewDate = DateTime.Now, // Atama tarihi
                Comments = "",
            };

            _context.reviews.Add(yorum);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Hakem başarıyla atandı.", AssignmentDate = yorum.ReviewDate });
        }

    }
}
