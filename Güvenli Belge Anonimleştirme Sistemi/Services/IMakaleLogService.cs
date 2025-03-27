namespace Güvenli_Belge_Anonimleştirme_Sistemi.Services
{
    public interface IMakaleLogService
    {
        Task LogMakaleAction(string trackingNumber, string action, string performedBy, DateTime actionDate);
    }

}
