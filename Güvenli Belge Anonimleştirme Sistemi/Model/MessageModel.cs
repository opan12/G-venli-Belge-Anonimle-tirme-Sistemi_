namespace Güvenli_Belge_Anonimleştirme_Sistemi.Model
{
    public class MessageModel
    {
        public int ArticleId { get; set; }
        public string SenderEmail { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; } // Burayı ekleyebilirsiniz
    }
}