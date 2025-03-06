namespace Güvenli_Belge_Anonimleştirme_Sistemi.Model
{
    public class Message
    {
        public int Id { get; set; }
        public int ArticleId { get; set; } // Mesajın hangi makale ile ilgili olduğu
        public Makale Article { get; set; }
        public string SenderEmail { get; set; } // Mesajı gönderenin e-posta adresi
        public string Content { get; set; } // Mesaj içeriği
        public DateTime SentAt { get; set; } // Mesajın gönderilme tarihi
    }
}