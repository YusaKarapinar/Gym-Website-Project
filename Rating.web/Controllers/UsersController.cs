using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Rating.web.Models;

namespace Rating.web.Controllers
{
    public class UsersController : Controller
    {
        private readonly ILogger<UsersController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private const string AuthCookieName = "AuthToken";

        public UsersController(ILogger<UsersController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient GetAuthenticatedHttpClient()
        {
            var httpClient = _httpClientFactory.CreateClient("RatingApi");
            
            if (Request.Cookies.TryGetValue(AuthCookieName, out var token))
            {
                httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", token);
            }
            
            return httpClient;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // Önceki cookie'yi temizle
            Response.Cookies.Delete(AuthCookieName);
            
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var httpClient = _httpClientFactory.CreateClient("RatingApi");

            var loginResponse = await httpClient.PostAsJsonAsync("api/Users/login", model);
            if (!loginResponse.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, $"Invalid login attempt. {loginResponse.StatusCode}");
                return View(model);
            }

            var result = await loginResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            if (result == null || !result.ContainsKey("token"))
            {
                ModelState.AddModelError(string.Empty, "No token received.");
                return View(model);
            }

            var token = result["token"];
            
            // Token'ı HttpOnly cookie'ye kaydet (güvenli)
            Response.Cookies.Append(AuthCookieName, token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // HTTPS kullanımı için
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(1)
            });

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var httpClient = _httpClientFactory.CreateClient("RatingApi");

            var registerResponse = await httpClient.PostAsJsonAsync("api/Users/register", model);
            if (!registerResponse.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, "Registration failed.");
                return View(model);
            }

            return RedirectToAction("Login", "Users");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            // Cookie'yi temizle
            Response.Cookies.Delete(AuthCookieName);
            
            // Session'ı temizle
            HttpContext.Session.Clear();
            
            return RedirectToAction("Login", "Users");
        }
    }
}