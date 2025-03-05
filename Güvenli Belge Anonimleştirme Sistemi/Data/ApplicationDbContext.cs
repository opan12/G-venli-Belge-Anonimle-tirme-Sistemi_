using Güvenli_Belge_Anonimleştirme_Sistemi.Model;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Güvenli_Belge_Anonimleştirme_Sistemi.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {



        public DbSet<User> users { get; set; }

        public DbSet<Article> articles { get; set; }
        public DbSet<Review> reviews { get; set; }



        //  public DbSet<Odenek> Odeneks { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
                : base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>();

         
        }



    }


}


