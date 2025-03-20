using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Güvenli_Belge_Anonimleştirme_Sistemi.Data;
using Güvenli_Belge_Anonimleştirme_Sistemi.Model;
using System;
using System.Linq;
using System.Threading.Tasks;

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

            // Get messages for an article
            [HttpGet("getMessages/{articleId}")]
            public async Task<IActionResult> GetMessages(int articleId)
            {
                var messages = await _context.messages
                    .Where(m => m.ArticleId == articleId)
                    .ToListAsync();

                return Ok(messages);
            }

            // Send a new message
            [HttpPost("sendMessage")]
            public async Task<IActionResult> SendMessage([FromBody] Message message)
            {
                if (message == null)
                {
                    return BadRequest("Message content cannot be null.");
                }

                _context.messages.Add(message);
                await _context.SaveChangesAsync();

                return Ok(message);
            }
        }

    }
