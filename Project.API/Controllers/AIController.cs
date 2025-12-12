using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.API.Services;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Project.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AIController : ControllerBase
    {
        private readonly IGeminiService _geminiService;
        private readonly ILogger<AIController> _logger;

        public AIController(IGeminiService geminiService, ILogger<AIController> logger)
        {
            _geminiService = geminiService;
            _logger = logger;
        }

        /// <summary>
        /// Kullanıcının boy, kilo, vücut tipi ve hedefine göre AI destekli fitness önerisi oluşturur
        /// </summary>
        [HttpPost("fitness-recommendation")]
        [Authorize]
        public async Task<IActionResult> GetFitnessRecommendation([FromBody] FitnessRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var recommendation = await _geminiService.GenerateFitnessRecommendationAsync(
                    request.Height, 
                    request.Weight, 
                    request.BodyType, 
                    request.Goal
                );
                
                return Ok(new { success = true, recommendation });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fitness önerisi oluşturulurken hata oluştu");
                return StatusCode(500, new { success = false, message = "Fitness önerisi oluşturulurken bir hata oluştu." });
            }
        }
    }

    public class FitnessRequest
    {
        [Required(ErrorMessage = "Boy bilgisi gereklidir")]
        public string Height { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kilo bilgisi gereklidir")]
        public string Weight { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vücut tipi gereklidir")]
        public string BodyType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hedef gereklidir")]
        public string Goal { get; set; } = string.Empty;
    }
}
