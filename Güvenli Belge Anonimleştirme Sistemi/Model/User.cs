using Microsoft.AspNetCore.Identity;

namespace Güvenli_Belge_Anonimleştirme_Sistemi.Model
{
    public class User : IdentityUser
    {

        public int Id { get; set; }
        public string Email { get; set; }


    }
}
