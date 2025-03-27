namespace Güvenli_Belge_Anonimleştirme_Sistemi.Model
{
    public class Message
    {
        public int Id { get; set; }

        public string SenderEmail { get; set; }

        public string ReceiverEmail { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
    }

}