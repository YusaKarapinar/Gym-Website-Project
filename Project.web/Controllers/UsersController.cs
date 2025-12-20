using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Project.web.Models;

namespace Project.web.Controllers
{
    public class UsersController : Controller
    {
        private readonly ILogger<UsersController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public UsersController(ILogger<UsersController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient GetAuthenticatedHttpClient()
        {
            var httpClient = _httpClientFactory.CreateClient("ProjectApi");
            
            var token = User.FindFirst("Token")?.Value;
            if (!string.IsNullOrEmpty(token))
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
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var httpClient = _httpClientFactory.CreateClient("ProjectApi");

            var loginResponse = await httpClient.PostAsJsonAsync("api/Users/login", model);
            if (!loginResponse.IsSuccessStatusCode)
            {
                // Show server-provided error details when available for clearer feedback
                var serverError = await loginResponse.Content.ReadAsStringAsync();
                var message = string.IsNullOrWhiteSpace(serverError)
                    ? $"Invalid login attempt. {loginResponse.StatusCode}"
                    : $"Invalid login attempt. {loginResponse.StatusCode}: {serverError}";
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            var result = await loginResponse.Content.ReadFromJsonAsync<Dictionary<string, object>>();
            if (result == null)
            {
                ModelState.AddModelError(string.Empty, "No response received from server.");
                return View(model);
            }

            string? token = null;
            if (result.ContainsKey("Token"))
            {
                token = result["Token"]?.ToString();
            }
            else if (result.ContainsKey("token"))
            {
                token = result["token"]?.ToString();
            }

            if (string.IsNullOrEmpty(token))
            {
                ModelState.AddModelError(string.Empty, "No token received from server.");
                return View(model);
            }
            
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, model.Username),
                new Claim("Token", token)
            };
            
            foreach (var claim in jwtToken.Claims)
            {
                _logger.LogInformation($"JWT Claim: Type={claim.Type}, Value={claim.Value}");
                
                // Role mapping - handle various formats
                if (claim.Type == "role" || claim.Type.EndsWith("/role") || claim.Type == ClaimTypes.Role)
                {
                    claims.Add(new Claim(ClaimTypes.Role, claim.Value));
                    _logger.LogInformation($"Added Role Claim: {claim.Value}");
                }
                // NameIdentifier mapping
                else if (claim.Type == "nameid" || claim.Type.EndsWith("/nameidentifier") || claim.Type == ClaimTypes.NameIdentifier)
                {
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, claim.Value));
                }
                // Email mapping
                else if (claim.Type == "email" || claim.Type.EndsWith("/emailaddress") || claim.Type == ClaimTypes.Email)
                {
                    claims.Add(new Claim(ClaimTypes.Email, claim.Value));
                }
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            
            var cookieExpiration = model.RememberMe 
                ? DateTimeOffset.UtcNow.AddDays(30) 
                : DateTimeOffset.UtcNow.AddHours(2);
                
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = cookieExpiration
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient("ProjectApi");
                
                var gymsResponse = await httpClient.GetAsync("api/Gyms");
                if (gymsResponse.IsSuccessStatusCode)
                {
                    var gyms = await gymsResponse.Content.ReadFromJsonAsync<IEnumerable<GymViewModel>>();
                    ViewBag.Gyms = gyms;
                    _logger.LogInformation($"Loaded {gyms?.Count() ?? 0} gyms successfully");
                }
                else
                {
                    _logger.LogWarning($"Failed to load gyms: {gymsResponse.StatusCode}");
                    ViewBag.Gyms = new List<GymViewModel>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading gyms for registration");
                ViewBag.Gyms = new List<GymViewModel>();
            }
            
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                try
                {
                    var httpClient = _httpClientFactory.CreateClient("ProjectApi");
                    
                    var gymsResponse = await httpClient.GetAsync("api/Gyms");
                    if (gymsResponse.IsSuccessStatusCode)
                    {
                        var gyms = await gymsResponse.Content.ReadFromJsonAsync<IEnumerable<GymViewModel>>();
                        ViewBag.Gyms = gyms;
                    }
                    else
                    {
                        ViewBag.Gyms = new List<GymViewModel>();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reloading gyms");
                    ViewBag.Gyms = new List<GymViewModel>();
                }
                
                return View(model);
            }

            try
            {
                var httpClient2 = _httpClientFactory.CreateClient("ProjectApi");

                var registerResponse = await httpClient2.PostAsJsonAsync("api/Users/register", model);
                if (!registerResponse.IsSuccessStatusCode)
                {
                    var errorContent = await registerResponse.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, $"Registration failed. {registerResponse.StatusCode}: {errorContent}");
                    
                    // Reload gyms on error
                    try
                    {
                        var gymsResponse = await httpClient2.GetAsync("api/Gyms");
                        if (gymsResponse.IsSuccessStatusCode)
                        {
                            var gyms = await gymsResponse.Content.ReadFromJsonAsync<IEnumerable<GymViewModel>>();
                            ViewBag.Gyms = gyms;
                        }
                        else
                        {
                            ViewBag.Gyms = new List<GymViewModel>();
                        }
                    }
                    catch
                    {
                        ViewBag.Gyms = new List<GymViewModel>();
                    }
                    
                    return View(model);
                }

                return RedirectToAction("Login", "Users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                ModelState.AddModelError(string.Empty, "Registration failed. Please try again.");
                
                try
                {
                    var httpClient3 = _httpClientFactory.CreateClient("ProjectApi");
                    var gymsResponse = await httpClient3.GetAsync("api/Gyms");
                    if (gymsResponse.IsSuccessStatusCode)
                    {
                        var gyms = await gymsResponse.Content.ReadFromJsonAsync<IEnumerable<GymViewModel>>();
                        ViewBag.Gyms = gyms;
                    }
                    else
                    {
                        ViewBag.Gyms = new List<GymViewModel>();
                    }
                }
                catch
                {
                    ViewBag.Gyms = new List<GymViewModel>();
                }
                
                return View(model);
            }
        }
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            return RedirectToAction("Login", "Users");
        }
    }
}