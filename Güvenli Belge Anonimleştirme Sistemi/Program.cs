using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Güvenli_Belge_Anonimleþtirme_Sistemi.Model;
using Güvenli_Belge_Anonimleþtirme_Sistemi.Data;
using Güvenli_Belge_Anonimleþtirme_Sistemi.Services;

var builder = WebApplication.CreateBuilder(args);

// CORS Politikasý Tanýmlama
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// CORS ayarlarý, port dinamikliði için geliþmiþ yapý
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy =>
        {
            // Burada dinamik olarak localhost portlarýný kabul edebilirsiniz
            policy.SetIsOriginAllowed(origin =>
            {
                return origin.StartsWith("http://localhost:");  // localhost ile baþlayan tüm portlara izin verir
            })
            .AllowAnyHeader()
            .AllowAnyMethod();
        });
});

// Add services to the container.
builder.Services.AddControllers();

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddScoped<IMakaleLogService, MakaleLogService>();
// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Swagger yapýlandýrma ayarlarý
});

var app = builder.Build();

// Middleware and HTTP request pipeline
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    if (app.Environment.IsProduction())
    {
        app.UseHsts();
    }
}

app.UseHttpsRedirection();

// CORS Middleware'ini Ekle (Authentication ve Authorization'dan Önce)
app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
