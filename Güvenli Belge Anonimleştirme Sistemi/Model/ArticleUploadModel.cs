using Microsoft.AspNetCore.Http;

namespace Güvenli_Belge_Anonimleştirme_Sistemi.Model
{
    public class ArticleUploadModel
    {
        public string AuthorEmail { get; set; }
        public IFormFile PdfFile { get; set; }
    }
}