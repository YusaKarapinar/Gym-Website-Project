using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.web.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Project.web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public IActionResult Index()
    {
        return View();
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Dashboard()
    {
        var httpClient = _httpClientFactory.CreateClient("ProjectApi");
        var token = User.FindFirst("Token")?.Value;
        if (!string.IsNullOrEmpty(token))
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var model = new DashboardViewModel();

        try
        {
            // Get Gyms
            var gymsResponse = await httpClient.GetAsync("api/Gyms");
            if (gymsResponse.IsSuccessStatusCode)
            {
                model.Gyms = await gymsResponse.Content.ReadFromJsonAsync<List<GymViewModel>>() ?? new List<GymViewModel>();
            }

            // Get Services
            var servicesResponse = await httpClient.GetAsync("api/Services");
            if (servicesResponse.IsSuccessStatusCode)
            {
                model.Services = await servicesResponse.Content.ReadFromJsonAsync<List<ServiceViewModel>>() ?? new List<ServiceViewModel>();
            }

            // Get Users
            var usersResponse = await httpClient.GetAsync("api/Users");
            if (usersResponse.IsSuccessStatusCode)
            {
                model.Users = await usersResponse.Content.ReadFromJsonAsync<List<UserViewModel>>() ?? new List<UserViewModel>();
            }

            // Get Appointments
            var appointmentsResponse = await httpClient.PostAsync("api/Appointments/get", null);
            if (appointmentsResponse.IsSuccessStatusCode)
            {
                model.Appointments = await appointmentsResponse.Content.ReadFromJsonAsync<List<AppointmentViewModel>>() ?? new List<AppointmentViewModel>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard data");
        }

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
