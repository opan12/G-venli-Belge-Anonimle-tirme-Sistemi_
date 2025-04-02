using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using G�venli_Belge_Anonimle�tirme_Sistemi.Model;
using G�venli_Belge_Anonimle�tirme_Sistemi.Data;
using G�venli_Belge_Anonimle�tirme_Sistemi.Services;

var builder = WebApplication.CreateBuilder(args);

// CORS Politikas� Tan�mlama
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// CORS ayarlar�, port dinamikli�i i�in geli�mi� yap�
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy =>
        {
            // Burada dinamik olarak localhost portlar�n� kabul edebilirsiniz
            policy.SetIsOriginAllowed(origin =>
            {
                return origin.StartsWith("http://localhost:");  // localhost ile ba�layan t�m portlara izin verir
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
    // Swagger yap�land�rma ayarlar�
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

// CORS Middleware'ini Ekle (Authentication ve Authorization'dan �nce)
app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
