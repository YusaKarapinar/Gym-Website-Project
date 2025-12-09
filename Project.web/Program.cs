using Project.web.Extensions;

var builder = WebApplication.CreateBuilder(args);

// --- SERVİS KAYITLARI ---
builder.Services.AddControllersWithViews();

// HTTP Client, Authentication ve Session servislerini ekle
builder.Services.AddHttpClientServices(builder.Configuration);
builder.Services.AddCookieAuthenticationConfiguration();
builder.Services.AddSessionConfiguration();


var app = builder.Build();

// --- MIDDLEWARE DÜZENİ ---

// Geliştirme ortamı kontrolü
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Middleware sırası önemlidir:
app.UseAuthentication();
app.UseSession(); 
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Make the implicit Program class public so test projects can access it
public partial class Program { }
