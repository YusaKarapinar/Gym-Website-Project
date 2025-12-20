using System.Text;
using System.Collections.Generic;
using System.IO;
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

// Load .env for local runs (before building configuration)
static void LoadDotEnvIfPresent()
{
    try
    {
        var current = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.Combine(current, ".env"),
            Path.Combine(current, "..", ".env"),
            Path.Combine(current, "..", "..", ".env")
        };

        string? envPath = candidates.FirstOrDefault(File.Exists);
        if (envPath == null) return;

        foreach (var rawLine in File.ReadAllLines(envPath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.StartsWith("#")) continue;
            var idx = line.IndexOf('=');
            if (idx <= 0) continue;
            var key = line.Substring(0, idx).Trim();
            var value = line.Substring(idx + 1).Trim().Trim('"');
            if (!string.IsNullOrWhiteSpace(key))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
    catch { /* best-effort for local dev */ }
}

LoadDotEnvIfPresent();

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
    options.Password.RequiredLength = 3;
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
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetConnectionString("Redis");
    return ConnectionMultiplexer.Connect(configuration ?? "localhost:6379");
});
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IGeminiService, GeminiService>();

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
        Log.Information("Seed data başlıyor...");
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        string[] roles = { Roles.Admin, Roles.Trainer, Roles.Member };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new AppRole { Name = role });
                Log.Information("Role oluşturuldu: {Role}", role);
            }
        }

        string adminEmail = "ogrencinumarasi@sakarya.edu.tr";
        string adminPassword = "sau";
        
        Log.Information("Admin kontrolü başlıyor...");
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin == null)
        {
            Log.Information("Admin bulunamadı, oluşturuluyor...");
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
            else
            {
                Log.Error("Admin oluşturulamadı: {Email}, Hatalar: {Errors}", adminEmail, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            Log.Information("Admin zaten mevcut: {Email}", adminEmail);
            // Ensure admin has the Admin role
            var rolesOfAdmin = await userManager.GetRolesAsync(existingAdmin);
            if (!rolesOfAdmin.Contains(Roles.Admin))
            {
                await userManager.AddToRoleAsync(existingAdmin, Roles.Admin);
                Log.Information("Admin kullanıcısına Admin rolü atandı: {Email}", adminEmail);
            }

            // Reset password to required value if needed
            var tokenForReset = await userManager.GeneratePasswordResetTokenAsync(existingAdmin);
            var resetResult = await userManager.ResetPasswordAsync(existingAdmin, tokenForReset, adminPassword);
            if (resetResult.Succeeded)
            {
                Log.Information("Admin parolası güncellendi.");
            }
            else
            {
                Log.Warning("Admin parolası güncellenemedi: {Errors}", string.Join(", ", resetResult.Errors.Select(e => e.Description)));
            }
        }

        var trainers = new[]
        {
            new { UserName = "ahmet.trainer", Email = "ahmet@fittrack.com", Password = "Trainer123" },
            new { UserName = "ayse.trainer", Email = "ayse@fittrack.com", Password = "Trainer123" },
            new { UserName = "Trainer", Email = "trainer@fittrack.com", Password = "Trainer123" }
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
                else
                {
                    Log.Error("Trainer oluşturulamadı: {Email}, Hatalar: {Errors}", trainer.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
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
                else
                {
                    Log.Error("Member oluşturulamadı: {Email}, Hatalar: {Errors}", member.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        // Default gym + services
        if (!await dbContext.Gyms.AnyAsync())
        {
            var gyms = new List<Gym>
            {
                new Gym
                {
                    Name = "Downtown Fitness Center",
                    Address = "123 Main St, City",
                    PhoneNumber = "+90 555 111 22 33",
                    IsActive = true,
                    Services = new List<Service>
                    {
                        new Service
                        {
                            Name = "Personal Training",
                            Description = "One-on-one tailored sessions",
                            ServiceType = ServiceTypes.PersonalTraining,
                            Price = 750,
                            Duration = TimeSpan.FromMinutes(60),
                            IsActive = true
                        },
                        new Service
                        {
                            Name = "Yoga Class",
                            Description = "Vinyasa flow for all levels",
                            ServiceType = ServiceTypes.Yoga,
                            Price = 350,
                            Duration = TimeSpan.FromMinutes(50),
                            IsActive = true
                        }
                    }
                },
                new Gym
                {
                    Name = "Seaside Gym",
                    Address = "456 Coastal Rd, City",
                    PhoneNumber = "+90 555 444 55 66",
                    IsActive = true,
                    Services = new List<Service>
                    {
                        new Service
                        {
                            Name = "Crossfit",
                            Description = "High intensity functional training",
                            ServiceType = ServiceTypes.Crossfit,
                            Price = 650,
                            Duration = TimeSpan.FromMinutes(55),
                            IsActive = true
                        },
                        new Service
                        {
                            Name = "Swimming Lesson",
                            Description = "Technique and endurance coaching",
                            ServiceType = ServiceTypes.Swimming,
                            Price = 500,
                            Duration = TimeSpan.FromMinutes(45),
                            IsActive = true
                        }
                    }
                }
            };

            dbContext.Gyms.AddRange(gyms);
            await dbContext.SaveChangesAsync();
            Log.Information("Default gym ve hizmetler eklendi.");
        }

        // Ensure the new Trainer has a Gym assigned
        var newTrainer = await userManager.FindByEmailAsync("trainer@fittrack.com");
        if (newTrainer != null && newTrainer.GymId == null)
        {
            var firstGym = await dbContext.Gyms.FirstOrDefaultAsync();
            if (firstGym != null)
            {
                newTrainer.GymId = firstGym.GymId;
                await userManager.UpdateAsync(newTrainer);
                Log.Information("Trainer kullanıcısına Gym atandı: {GymName}", firstGym.Name);
            }
        }

        // Default appointment
        if (!await dbContext.Appointments.AnyAsync())
        {
            var member = await userManager.FindByEmailAsync("mehmet@test.com");
            var trainer = await userManager.FindByEmailAsync("ahmet@fittrack.com");
            var service = await dbContext.Services.FirstOrDefaultAsync();
            var gym = await dbContext.Gyms.FirstOrDefaultAsync();

            if (member != null && trainer != null && service != null && gym != null)
            {
                var appointment = new Appointment
                {
                    Date = DateTime.UtcNow.Date.AddDays(1),
                    Time = new TimeSpan(10, 0, 0),
                    UserId = member.Id,
                    TrainerId = trainer.Id,
                    ServiceId = service.ServiceId,
                    GymId = gym.GymId,
                    Status = AppointmentStatus.Pending,
                    Price = service.Price,
                    CreatedAt = DateTime.UtcNow
                };

                await dbContext.Appointments.AddAsync(appointment);
                await dbContext.SaveChangesAsync();
                Log.Information("Default randevu oluşturuldu ({Date} {Time})", appointment.Date.ToShortDateString(), appointment.Time);
            }
            else
            {
                Log.Warning("Default randevu oluşturulamadı: kullanıcı/egitmen/hizmet/salon eksik.");
            }
        }
        
        Log.Information("Seed data tamamlandı.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Seed data sırasında hata oluştu.");
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