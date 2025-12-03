using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Project.API.Constants;
using Project.API.Data;
using Project.API.Models;
using Project.API.Services;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

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
builder.Services.AddDbContext<GymContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
Console.WriteLine($"Database connected. {builder.Configuration.GetConnectionString("DefaultConnection")}");

builder.Services.AddIdentity<AppUser, AppRole>()
    .AddEntityFrameworkStores<GymContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
});

// 3. JWT AUTHENTICATION
var tokenSecret = builder.Configuration["AppSettings:Token"];
if (string.IsNullOrEmpty(tokenSecret))
{
    Log.Fatal("AppSettings:Token configuration is missing or empty.");
    throw new InvalidOperationException("JWT Secret Token is missing in configuration.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidateAudience = false,
        ValidateIssuer = false,
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
    var dbContext = scope.ServiceProvider.GetRequiredService<GymContext>();
    
    try
    {
        Log.Information("Database migration başlanıyor...");
        await dbContext.Database.MigrateAsync();
        Log.Information("Database migration tamamlandı.");
    }
    catch (Exception ex)
    {
       Log.Error(ex, "Veritabanı şeması oluşturulurken bir hata meydana geldi.");
    }

    try
    {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        string[] roles = { Roles.Admin, Roles.Trainer, Roles.Member };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new AppRole { Name = role });
        }

        string adminEmail = "ogrencinumarasi@sakarya.edu.tr";
        string adminPassword = "sau";
        
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var adminUser = new AppUser 
            { 
                UserName = "admin", 
                Email = adminEmail 
            };
            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, Roles.Admin);
                Log.Information("Admin kullanıcısı oluşturuldu: {Email}", adminEmail);
            }
        }

        var trainers = new[]
        {
            new { UserName = "ahmet.trainer", Email = "ahmet@fittrack.com", Password = "Trainer123" },
            new { UserName = "ayse.trainer", Email = "ayse@fittrack.com", Password = "Trainer123" }
        };

        foreach (var trainer in trainers)
        {
            if (await userManager.FindByEmailAsync(trainer.Email) == null)
            {
                var trainerUser = new AppUser
                {
                    UserName = trainer.UserName,
                    Email = trainer.Email
                };
                var result = await userManager.CreateAsync(trainerUser, trainer.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(trainerUser, Roles.Trainer);
                    Log.Information("Trainer kullanıcısı oluşturuldu: {Email}", trainer.Email);
                }
            }
        }

        var members = new[]
        {
            new { UserName = "mehmet.member", Email = "mehmet@test.com", Password = "Member123" },
            new { UserName = "zeynep.member", Email = "zeynep@test.com", Password = "Member123" },
            new { UserName = "ali.member", Email = "ali@test.com", Password = "Member123" }
        };

        foreach (var member in members)
        {
            if (await userManager.FindByEmailAsync(member.Email) == null)
            {
                var memberUser = new AppUser
                {
                    UserName = member.UserName,
                    Email = member.Email
                };
                var result = await userManager.CreateAsync(memberUser, member.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(memberUser, Roles.Member);
                    Log.Information("Member kullanıcısı oluşturuldu: {Email}", member.Email);
                }
            }
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Identity seedleme sırasında bir hata oluştu.");
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
    Log.CloseAndFlush(); 
}