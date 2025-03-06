namespace Güvenli_Belge_Anonimleştirme_Sistemi.Model
{
    public class Makale
    {

        public int Id { get; set; }
        public string Title { get; set; }
        public string ContentPath { get; set; }
        public string AuthorEmail { get; set; }
        public string EncryptedAesKey { get; set; } // RSA ile şifrelenmiş AES anahtarı
        public string ContentHash { get; set; } // İçeriğin SHA-256 hash'i
        public string TrackingNumber { get; set; }
        public string Status { get; set; }
       public DateTime ArticleDate { get; set; }
    }
}
