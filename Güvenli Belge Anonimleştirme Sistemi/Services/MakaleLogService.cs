using Güvenli_Belge_Anonimleştirme_Sistemi.Data;
using Güvenli_Belge_Anonimleştirme_Sistemi.Model;

namespace Güvenli_Belge_Anonimleştirme_Sistemi.Services
{
    public class MakaleLogService : IMakaleLogService
    {
        private readonly ApplicationDbContext _context;

        public MakaleLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogMakaleAction(string trackingNumber, string action, string performedBy, DateTime actionDate)
        {
            var log = new Log
            {
                TrackingNumber = trackingNumber,
                Action = action,
                ActionDate = actionDate,
                PerformedBy = performedBy
            };

            _context.Logs.Add(log);
            await _context.SaveChangesAsync();
        }
    }

}
