using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.web.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Project.web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ServicesController : Controller
    {
        private readonly ILogger<ServicesController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public ServicesController(ILogger<ServicesController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient GetAuthenticatedHttpClient()
        {
            var httpClient = _httpClientFactory.CreateClient("ProjectApi");
            
            // Token'ı claims'den al
            var token = User.FindFirst("Token")?.Value;
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Bearer", token);
            }
            
            return httpClient;
        }

        // GET: Services
        public async Task<IActionResult> Index()
        {
            var httpClient = GetAuthenticatedHttpClient();

            var response = await httpClient.GetAsync("api/Services");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Failed to retrieve services. {response.StatusCode}: {errorContent}");
                return View();
            }
            var services = await response.Content.ReadFromJsonAsync<IEnumerable<ServiceViewModel>>();
            return View(services);
        }

        // GET: Services/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var httpClient = GetAuthenticatedHttpClient();

            var response = await httpClient.GetAsync($"api/Services/{id}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Failed to retrieve service details. {response.StatusCode}: {errorContent}");
                return View();
            }

            var service = await response.Content.ReadFromJsonAsync<ServiceViewModel>();
            return View(service);
        }

        // GET: Services/Create
        public async Task<IActionResult> Create()
        {
            var httpClient = GetAuthenticatedHttpClient();
            
            // Gym listesini çek
            var gymsResponse = await httpClient.GetAsync("api/Gyms");
            if (gymsResponse.IsSuccessStatusCode)
            {
                var gyms = await gymsResponse.Content.ReadFromJsonAsync<IEnumerable<GymViewModel>>();
                ViewBag.Gyms = gyms;
            }
            
            return View();
        }

        // POST: Services/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceViewModel model)
        {
            var httpClient = GetAuthenticatedHttpClient();

            var response = await httpClient.PostAsJsonAsync($"api/Services", model);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Failed to create service. {response.StatusCode}: {errorContent}");
                
                // Gym listesini tekrar yükle
                var gymsResponse = await httpClient.GetAsync("api/Gyms");
                if (gymsResponse.IsSuccessStatusCode)
                {
                    var gyms = await gymsResponse.Content.ReadFromJsonAsync<IEnumerable<GymViewModel>>();
                    ViewBag.Gyms = gyms;
                }
                
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Services/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var httpClient = GetAuthenticatedHttpClient();

            var response = await httpClient.GetAsync($"api/Services/{id}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Failed to retrieve service details. {response.StatusCode}: {errorContent}");
                return View();
            }

            var service = await response.Content.ReadFromJsonAsync<ServiceViewModel>();
            
            // Gym listesini çek
            var gymsResponse = await httpClient.GetAsync("api/Gyms");
            if (gymsResponse.IsSuccessStatusCode)
            {
                var gyms = await gymsResponse.Content.ReadFromJsonAsync<IEnumerable<GymViewModel>>();
                ViewBag.Gyms = gyms;
            }
            
            return View(service);
        }

        // POST: Services/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ServiceViewModel model)
        {
            var httpClient = GetAuthenticatedHttpClient();
            var response = await httpClient.PutAsJsonAsync($"api/Services/{id}", model);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Failed to update service. {response.StatusCode}: {errorContent}");
                
                // Gym listesini tekrar yükle
                var gymsResponse = await httpClient.GetAsync("api/Gyms");
                if (gymsResponse.IsSuccessStatusCode)
                {
                    var gyms = await gymsResponse.Content.ReadFromJsonAsync<IEnumerable<GymViewModel>>();
                    ViewBag.Gyms = gyms;
                }
                
                return View(model);
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Services/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var httpClient = GetAuthenticatedHttpClient();
            var response = await httpClient.GetAsync($"api/Services/{id}");
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Failed to retrieve service details. {response.StatusCode}: {errorContent}");
                return View();
            }
            var service = await response.Content.ReadFromJsonAsync<ServiceViewModel>();
            return View(service);
        }

        // POST: Services/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var httpClient = GetAuthenticatedHttpClient();
            var response = await httpClient.DeleteAsync($"api/Services/{id}");
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Failed to delete service. {response.StatusCode}: {errorContent}");
                return View();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Services/GetByGym/5
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetByGym(int gymId)
        {
            var httpClient = GetAuthenticatedHttpClient();
            var response = await httpClient.GetAsync($"api/Services/bygym/{gymId}");
            
            if (!response.IsSuccessStatusCode)
            {
                return Json(new List<ServiceViewModel>());
            }
            
            var services = await response.Content.ReadFromJsonAsync<IEnumerable<ServiceViewModel>>();
            return Json(services);
        }
    }
}
