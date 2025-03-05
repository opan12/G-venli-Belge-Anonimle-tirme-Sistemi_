namespace Güvenli_Belge_Anonimleştirme_Sistemi.Model
{
    public class Review
    {
        public int Id { get; set; } // Her incelemenin benzersiz kimliği
        public int ArticleId { get; set; } // İncelenen makalenin kimliği
        public Article Article { get; set; } // Makale ilişkisi

        public int ReviewerId { get; set; } // Hakemin kimliği
        public  User User { get; set; }//den

        public string Comments { get; set; } // Hakemin yorumu
     
    }

}
