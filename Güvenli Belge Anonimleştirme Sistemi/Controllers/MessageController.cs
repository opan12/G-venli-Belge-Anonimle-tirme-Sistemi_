using Microsoft.AspNetCore.Mvc;
using Güvenli_Belge_Anonimleştirme_Sistemi.Model;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Güvenli_Belge_Anonimleştirme_Sistemi.Data;

namespace Güvenli_Belge_Anonimleştirme_Sistemi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MessageController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] MessageModel model)
        {

            var message = new Message
            {
                ArticleId = model.ArticleId,
                SenderEmail = model.SenderEmail,
                Content = model.Content,
            };

            _context.messages.Add(message);
            await _context.SaveChangesAsync();

            return Ok(new { MessageId = message.Id });
        }
    }
}