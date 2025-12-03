using Microsoft.AspNetCore.Authentication.Cookies;

namespace Project.web.Extensions
{
    public static class ServiceExtensions
    {
        public static class Roles
        {
            public const string Admin = "Admin";
            public const string Trainer = "Trainer";
            public const string Member = "Member";
        }

        /// <summary>
        /// HTTP Client servislerini yapılandırır (API iletişimi için)
        /// </summary>
        public static IServiceCollection AddHttpClientServices(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            services.AddHttpClient("ProjectApi", client =>
            {
                var baseUrl = configuration["ApiSettings:ProjectApiBaseUrl"];
                client.BaseAddress = new Uri(baseUrl ?? "http://localhost:5002");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            return services;
        }

        public static IServiceCollection AddCookieAuthenticationConfiguration(this IServiceCollection services)
        {
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Users/Login";
                    options.AccessDeniedPath = "/Home/AccessDenied"; // Erişim reddedildiğinde
                    options.ExpireTimeSpan = TimeSpan.FromDays(3); // 3 gün
                    options.SlidingExpiration = true; // Kullanıcı aktif olduğunda süre yenilenir
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                });

            return services;
        }

        public static IServiceCollection AddSessionConfiguration(this IServiceCollection services)
        {
            services.AddDistributedMemoryCache();

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            return services;
        }
    }
}
