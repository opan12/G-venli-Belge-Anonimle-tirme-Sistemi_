﻿namespace Güvenli_Belge_Anonimleştirme_Sistemi.Model
{
    public class Makale
    {
        public int Id { get; set; }
        public string ContentPath { get; set; }
        public string AuthorEmail { get; set; }
        public string TrackingNumber { get; set; }
        public string Status { get; set; }
        public DateTime ArticleDate { get; set; }
        public string Content { get; set; }
        public string AnonymizedContent { get; set; }
        public string? DeanonymizedContent { get; set; } // Eski anonim hali

        public string? DecryptedContent { get; set; } // 📌 Yeni Alan: Deşifre Edilmiş İçerik

        public string Alan { get; set; }
    }
}