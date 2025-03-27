namespace Güvenli_Belge_Anonimleştirme_Sistemi.Model
{
    public class Log
    {
        public int Id { get; set; }
        public string TrackingNumber { get; set; } // Makaleye bağlamak için
        public string Action { get; set; } // Yapılan işlem (örn: "Hakeme atandı")
        public DateTime ActionDate { get; set; } // İşlemin gerçekleştiği tarih
        public string PerformedBy { get; set; } // İşlemi yapan kişi (Editör, Hakem vb.)
    }

}
