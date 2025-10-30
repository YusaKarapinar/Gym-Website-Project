using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Rating.API.Controllers;
using Rating.API.Models; // AppUser, AppRole ve PostsContext'in tanımlı olduğu namespace'ler
using Rating.API.Services;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// ***************************************************************
// EN ÜST DÜZEY DEYİMLER (TOP-LEVEL STATEMENTS) KULLANILMIŞTIR.
// Program sınıfı ve Main metodu artık gerekli değildir.
// ***************************************************************

var builder = WebApplication.CreateBuilder(args);

// 1. SERILOG YAPILANDIRMASI
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(LogEventLevel.Information)
    .Enrich.FromLogContext() // Daha zengin loglama için
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day,
                  outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// 2. VERITABANI VE IDENTITY SERVISLERI
builder.Services.AddDbContext<PostsContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
Console.WriteLine($"Database connected. {builder.Configuration.GetConnectionString("DefaultConnection")}");

builder.Services.AddIdentity<AppUser, AppRole>()
    .AddEntityFrameworkStores<PostsContext>()
    .AddDefaultTokenProviders(); // Şifre sıfırlama vb. için

// Password gereksinimleri (Orijinal koddaki gibi gevşek tutulmuştur)
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
});

// 3. JWT AUTHENTICATION
var tokenSecret = builder.Configuration["AppSettings:Token"];
if (string.IsNullOrEmpty(tokenSecret))
{
    // Ciddi bir güvenlik uyarısı veya uygulama durdurma
    Log.Fatal("AppSettings:Token configuration is missing or empty.");
    // Uygulamanın başlatılmasını durdurmak için throw kullanılır.
    throw new InvalidOperationException("JWT Secret Token is missing in configuration.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false; // Geliştirme için olabilir, Production'da true olmalı
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidateAudience = false, // Projenize göre true/false ayarlanabilir.
        ValidateIssuer = false,   // Projenize göre true/false ayarlanabilir.
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.ASCII.GetBytes(tokenSecret))
    };
});

// 4. DIĞER SERVISLER
builder.Services.AddControllers();
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetConnectionString("Redis");
    return ConnectionMultiplexer.Connect(configuration ?? "localhost:6379");
});
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

var app = builder.Build();

// 5. ROL OLUŞTURMA (Seedleme)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        // Veritabanı Migration'larının uygulanması (isteğe bağlı ama önerilir)
        var dbContext = scope.ServiceProvider.GetRequiredService<PostsContext>();
        await dbContext.Database.MigrateAsync();

        string[] roles = { "User", "Admin" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new AppRole { Name = role });
        }

        // Örnek bir Admin kullanıcısı da oluşturulabilir (isteğe bağlı)
        if (await userManager.FindByEmailAsync("admin@example.com") == null)
        {
            var adminUser = new AppUser { UserName = "admin", Email = "admin@example.com" };
            var result = await userManager.CreateAsync(adminUser, "SecureP@ss123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

    }
    catch (Exception ex)
    {
        Log.Error(ex, "Uygulama başlatılırken bir hata oluştu (Veritabanı/Identity Seedleme).");
        // Uygulamanın çökmesi isteniyorsa throw edilebilir.
    }
}

// 6. MIDDLEWARE PIPELINE
// if (app.Environment.IsDevelopment()) { app.UseDeveloperExceptionPage(); } // Geliştirme ortamı için

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// 7. UYGULAMAYI BAŞLATMA
try
{
    Log.Information("Web Host başlatılıyor.");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host beklenmedik bir şekilde sonlandı.");
}
finally
{
    // Serilog'un tüm tamponlanmış logları diske yazmasını sağlar.
    Log.CloseAndFlush(); 
}

// EOF