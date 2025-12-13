using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Project.API.Services
{
    /// <summary>
    /// Service for generating personalized fitness recommendations using Google Gemini AI API
    /// </summary>
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiService> _logger;
        private readonly string _apiKey;

        public GeminiService(IConfiguration configuration, ILogger<GeminiService> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _apiKey = configuration["GeminiAI:ApiKey"] ?? "";
            _httpClient = httpClientFactory.CreateClient();
            
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("Gemini API key is not configured.");
            }
        }

        public async Task<string> GenerateFitnessRecommendationAsync(string height, string weight, string bodyType, string goal)
        {
            try
            {
                var prompt = $@"Sen bir fitness ve beslenme uzmanısın. Aşağıdaki kullanıcı bilgilerine göre kişiselleştirilmiş bir fitness planı hazırla:

Boy: {height} cm
Kilo: {weight} kg
Vücut Tipi: {bodyType}
Hedef: {goal}

Lütfen şunları içeren kapsamlı bir plan hazırla:

## Egzersiz Programı
1. Haftalık egzersiz planı (Hangi günler hangi egzersizler)
2. Her egzersizin set ve tekrar sayıları
3. Hangi kas gruplarına odaklanılmalı
4. Önerilen egzersiz süreleri

## Beslenme Planı
1. Günlük kalori ihtiyacı tahmini
2. Makro besin dağılımı (protein, karbonhidrat, yağ)
3. Örnek öğün önerileri
4. Su tüketimi önerisi

## Beklenen Sonuçlar
1. 3 ay sonraki muhtemel değişimler
2. 6 ay sonraki muhtemel değişimler
3. Motivasyon önerileri

Cevabını Türkçe ver, detaylı ve gerçekçi ol. Markdown formatında düzenli bir şekilde sun.";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = prompt }
                            }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={_apiKey}";
                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);
                    
                    if (responseObj.TryGetProperty("candidates", out var candidates) &&
                        candidates.GetArrayLength() > 0)
                    {
                        var candidate = candidates[0];
                        if (candidate.TryGetProperty("content", out var contentObj) &&
                            contentObj.TryGetProperty("parts", out var parts) &&
                            parts.GetArrayLength() > 0)
                        {
                            var part = parts[0];
                            if (part.TryGetProperty("text", out var text))
                            {
                                return text.GetString() ?? "Öneri oluşturulamadı.";
                            }
                        }
                    }
                    return "Gemini API'den cevap alınamadı.";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Gemini API Error: {errorContent}");
                    
                    if (errorContent.Contains("quota") || errorContent.Contains("Quota"))
                    {
                        return "Gemini API'nin günlük limitine ulaşıldı. Lütfen birkaç saat sonra tekrar deneyin.";
                    }
                    
                    return "API hatası: İçerik oluşturulamadı.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating fitness recommendation with Gemini AI");
                throw;
            }
        }
    }
}
