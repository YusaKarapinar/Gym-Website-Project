using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.web.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

namespace Project.web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class GymsController : Controller
    {
        private readonly ILogger<GymsController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public GymsController(ILogger<GymsController> logger, IHttpClientFactory httpClientFactory)
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

        public async Task<IActionResult> Index()
        {
            var httpClient = GetAuthenticatedHttpClient();

            var response = await httpClient.GetAsync("api/Gyms");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Failed to retrieve gyms. {response.StatusCode}: {errorContent}");
                return View();
            }
            var gyms = await response.Content.ReadFromJsonAsync<IEnumerable<GymViewModel>>();
            return View(gyms);
        }

        public async Task<IActionResult> Details(int id)
        {
            var httpClient = GetAuthenticatedHttpClient();

            var response = await httpClient.GetAsync($"api/Gyms/{id}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Failed to retrieve gym details. {response.StatusCode}: {errorContent}");
                return View();
            }

            var gym = await response.Content.ReadFromJsonAsync<GymViewModel>();
            return View(gym);
        }

        

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }


        // POST: Gyms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GymViewModel model)
        {
             var httpClient = GetAuthenticatedHttpClient();

            var response = await httpClient.PostAsJsonAsync($"api/Gyms", model);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Failed to create gym. {response.StatusCode}: {errorContent}");
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Gyms/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var httpClient = GetAuthenticatedHttpClient();

            var response = await httpClient.GetAsync($"api/Gyms/{id}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Failed to retrieve gym details. {response.StatusCode}: {errorContent}");
                return View();
            }

            var gym = await response.Content.ReadFromJsonAsync<GymViewModel>();
            return View(gym);
        }

        // POST: Gyms/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, GymViewModel model)
        {
            var httpClient = GetAuthenticatedHttpClient();
            var response = await httpClient.PutAsJsonAsync($"api/Gyms/{id}", model);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Failed to update gym. {response.StatusCode}: {errorContent}");
                return View(model);
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Gyms/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var httpClient = GetAuthenticatedHttpClient();
            var response = await httpClient.GetAsync($"api/Gyms/{id}");
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Failed to retrieve gym details. {response.StatusCode}: {errorContent}");
                return View();
            }
            var gym = await response.Content.ReadFromJsonAsync<GymViewModel>();
            return View(gym);
        }

        // POST: Gyms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var httpClient = GetAuthenticatedHttpClient();
            var response = await httpClient.DeleteAsync($"api/Gyms/{id}");
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Failed to delete gym. {response.StatusCode}: {errorContent}");
                return View();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
