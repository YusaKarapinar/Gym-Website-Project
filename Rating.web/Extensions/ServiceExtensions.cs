using Microsoft.AspNetCore.Authentication.Cookies;

namespace Rating.web.Extensions
{
    public static class ServiceExtensions
    {
        /// <summary>
        /// HTTP Client servislerini yapılandırır (API iletişimi için)
        /// </summary>
        public static IServiceCollection AddHttpClientServices(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            services.AddHttpClient("RatingApi", client =>
            {
                var baseUrl = configuration["ApiSettings:RatingApiBaseUrl"];
                client.BaseAddress = new Uri(baseUrl ?? "http://localhost:5002");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            return services;
        }

        /// <summary>
        /// Cookie Authentication servislerini yapılandırır (Oturum kalıcılığı)
        /// </summary>
        public static IServiceCollection AddAuthenticationServices(this IServiceCollection services)
        {
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Users/Login";
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(1);
                    options.SlidingExpiration = true;
                });

            return services;
        }

        /// <summary>
        /// Session servislerini yapılandırır (Geçici veri saklama)
        /// </summary>
        public static IServiceCollection AddSessionServices(this IServiceCollection services)
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
