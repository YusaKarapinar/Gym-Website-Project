using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.web.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Project.web.Controllers
{
    [Authorize]
    public class AIController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AIController> _logger;

        public AIController(IHttpClientFactory httpClientFactory, ILogger<AIController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult FitnessRecommendation()
        {
            return View(new FitnessRecommendationViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> FitnessRecommendation(FitnessRecommendationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var httpClient = _httpClientFactory.CreateClient("ProjectApi");
                var token = User.FindFirst("Token")?.Value;

                if (string.IsNullOrEmpty(token))
                {
                    TempData["Error"] = "Oturum süreniz dolmuş. Lütfen tekrar giriş yapın.";
                    return RedirectToAction("Login", "Users");
                }

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var requestData = new
                {
                    height = model.Height,
                    weight = model.Weight,
                    bodyType = model.BodyType,
                    goal = model.Goal
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestData),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await httpClient.PostAsync("/api/AI/fitness-recommendation", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadFromJsonAsync<JsonElement>();
                    
                    _logger.LogInformation($"API Response: {responseData}");
                    
                    if (responseData.TryGetProperty("recommendation", out var recommendation))
                    {
                        model.Recommendation = recommendation.GetString();
                        _logger.LogInformation($"Recommendation: {model.Recommendation}");
                    }
                    else
                    {
                        _logger.LogWarning("Response'da recommendation alanı bulunamadı");
                        TempData["Error"] = "Öneri alınamadı.";
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"AI API Error: {errorContent}");
                    TempData["Error"] = "Fitness önerisi oluşturulurken bir hata oluştu. Lütfen tekrar deneyin.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fitness önerisi oluşturulurken hata oluştu");
                TempData["Error"] = "Bir hata oluştu. Lütfen tekrar deneyin.";
            }

            return View(model);
        }
    }
}
