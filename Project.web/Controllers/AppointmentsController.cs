using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.web.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Project.web.Controllers
{
    [Authorize] // Tüm giriş yapmış kullanıcılar erişebilir
    public class AppointmentsController : Controller
    {

        private readonly ILogger<AppointmentsController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public AppointmentsController(ILogger<AppointmentsController> logger, IHttpClientFactory httpClientFactory)
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> Approve(int id)
        {
            var httpClient = GetAuthenticatedHttpClient();
            var response = await httpClient.PostAsJsonAsync("api/Appointments/approve", id);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Failed to approve appointment. {response.StatusCode}: {errorContent}";
            }
            else
            {
                TempData["Success"] = "Appointment approved.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> Reject(int id)
        {
            var httpClient = GetAuthenticatedHttpClient();
            var response = await httpClient.PostAsJsonAsync("api/Appointments/reject", id);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                TempData["Error"] = $"Failed to reject appointment. {response.StatusCode}: {errorContent}";
            }
            else
            {
                TempData["Success"] = "Appointment rejected.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Appointments - Herkes randevularını görebilir
        public async Task<IActionResult> Index()
        {
            var httpClient = GetAuthenticatedHttpClient();

            var response = await httpClient.PostAsync("api/Appointments/get", null);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Failed to retrieve appointments. {response.StatusCode}: {errorContent}");
                return View(new List<AppointmentViewModel>());
            }
            var appointments = await response.Content.ReadFromJsonAsync<IEnumerable<AppointmentViewModel>>()
                              ?? new List<AppointmentViewModel>();

            // Üye giriş yaptıysa sadece kendi randevularını göster
            if (User.IsInRole("Member"))
            {
                var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                appointments = appointments.Where(a => a.UserId == currentUserId);
            }

            return View(appointments);
        }

        // GET: Appointments/Create - Sadece Admin ve Trainer
        [Authorize(Roles = "Admin,Trainer")]
        public async Task<IActionResult> Create()
        {
            var httpClient = GetAuthenticatedHttpClient();
            
            // Load users for dropdown (only Members)
            var usersResponse = await httpClient.GetAsync("api/Users");
            if (usersResponse.IsSuccessStatusCode)
            {
                var users = await usersResponse.Content.ReadFromJsonAsync<IEnumerable<UserViewModel>>();
                ViewBag.Users = users?.Where(u => u.Role == "Member");
            }
            
            // Load trainers for dropdown
            var trainersResponse = await httpClient.GetAsync("api/Users");
            if (trainersResponse.IsSuccessStatusCode)
            {
                var trainers = await trainersResponse.Content.ReadFromJsonAsync<IEnumerable<UserViewModel>>();
                if (User.IsInRole("Trainer"))
                {
                    var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                    ViewBag.Trainers = trainers?.Where(u => u.Id == currentUserId);
                }
                else
                {
                    ViewBag.Trainers = trainers?.Where(u => u.Role == "Trainer");
                }
            }
            
            // Admin: Load gyms for dropdown
            if (User.IsInRole("Admin"))
            {
                var gymsResponse = await httpClient.GetAsync("api/Gyms");
                if (gymsResponse.IsSuccessStatusCode)
                {
                    var gyms = await gymsResponse.Content.ReadFromJsonAsync<IEnumerable<GymViewModel>>();
                    ViewBag.Gyms = gyms;
                }
            }
            
            // Trainer: Load services from own gym
            if (User.IsInRole("Trainer"))
            {
                var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var allUsersResponse = await httpClient.GetAsync("api/Users");
                
                if (allUsersResponse.IsSuccessStatusCode)
                {
                    var allUsers = await allUsersResponse.Content.ReadFromJsonAsync<IEnumerable<UserViewModel>>();
                    var currentUser = allUsers?.FirstOrDefault(u => u.Id == currentUserId);
                    
                    if (currentUser?.GymId != null)
                    {
                        ViewBag.TrainerGymId = currentUser.GymId;
                        
                        var servicesResponse = await httpClient.GetAsync($"api/Services/bygym/{currentUser.GymId}");
                        if (servicesResponse.IsSuccessStatusCode)
                        {
                            var services = await servicesResponse.Content.ReadFromJsonAsync<IEnumerable<ServiceViewModel>>();
                            ViewBag.Services = services;
                        }
                    }
                }
            }
            
            return View();
        }

        // POST: Appointments/Create - Sadece Admin ve Trainer
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Trainer")]
        public async Task<IActionResult> Create(AppointmentViewModel model)
        {
            var httpClient = GetAuthenticatedHttpClient();

            // Trainer ise kendi GymId'sini ata
            if (User.IsInRole("Trainer"))
            {
                var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                model.TrainerId = currentUserId; // Trainer kendisi için randevu oluşturuyor

                var userResponse = await httpClient.GetAsync($"api/Users");
                if (userResponse.IsSuccessStatusCode)
                {
                    var users = await userResponse.Content.ReadFromJsonAsync<IEnumerable<UserViewModel>>();
                    var currentUser = users?.FirstOrDefault(u => u.Id == currentUserId);
                    if (currentUser?.GymId != null)
                    {
                        model.GymId = currentUser.GymId.Value;
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Trainer'ın kayıtlı bir salonu yok.");
                        return View(model);
                    }
                }
            }

            var response = await httpClient.PostAsJsonAsync("api/Appointments/create", model);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Failed to create appointment. {response.StatusCode}: {errorContent}");
                
                // Reload dropdowns
                var usersResponse = await httpClient.GetAsync("api/Users");
                if (usersResponse.IsSuccessStatusCode)
                {
                    var users = await usersResponse.Content.ReadFromJsonAsync<IEnumerable<UserViewModel>>();
                    ViewBag.Users = users?.Where(u => u.Role == "Member");
                }
                
                var trainersResponse = await httpClient.GetAsync("api/Users");
                if (trainersResponse.IsSuccessStatusCode)
                {
                    var trainers = await trainersResponse.Content.ReadFromJsonAsync<IEnumerable<UserViewModel>>();
                    if (User.IsInRole("Trainer"))
                    {
                        var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                        ViewBag.Trainers = trainers?.Where(u => u.Id == currentUserId);
                    }
                    else
                    {
                        ViewBag.Trainers = trainers?.Where(u => u.Role == "Trainer");
                    }
                }
                
                if (User.IsInRole("Admin"))
                {
                    var gymsResponse = await httpClient.GetAsync("api/Gyms");
                    if (gymsResponse.IsSuccessStatusCode)
                    {
                        var gyms = await gymsResponse.Content.ReadFromJsonAsync<IEnumerable<GymViewModel>>();
                        ViewBag.Gyms = gyms;
                    }
                }
                
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Appointments/Delete/5 - Sadece Admin ve Trainer
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Trainer")]
        public async Task<IActionResult> Delete(int id)
        {
            var httpClient = GetAuthenticatedHttpClient();
            var response = await httpClient.PostAsJsonAsync("api/Appointments/delete", id);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Failed to delete appointment. {response.StatusCode}: {errorContent}");
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
