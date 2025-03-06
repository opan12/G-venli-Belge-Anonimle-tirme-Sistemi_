namespace Güvenli_Belge_Anonimleştirme_Sistemi.Model
{
    public class Yorum
    {
        public int Id { get; set; } // Her incelemenin benzersiz kimliği
        public int ArticleId { get; set; } // İncelenen makalenin kimliği
        public Makale Makale { get; set; } // Makale ilişkisi

        public int ReviewerId { get; set; } // Hakemin kimliği
        public  Reviewer Reviewer { get; set; }//den

        public string Comments { get; set; } // Hakemin yorumu
        public DateTime ReviewDate { get; set;}
     
    }

}
