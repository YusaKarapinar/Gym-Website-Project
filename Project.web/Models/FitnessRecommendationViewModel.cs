using System.ComponentModel.DataAnnotations;

namespace Project.web.Models
{
    public class FitnessRecommendationViewModel
    {
        [Required(ErrorMessage = "Boy bilgisi gereklidir")]
        [Display(Name = "Boy (cm)")]
        public string Height { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kilo bilgisi gereklidir")]
        [Display(Name = "Kilo (kg)")]
        public string Weight { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vücut tipi gereklidir")]
        [Display(Name = "Vücut Tipi")]
        public string BodyType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hedef gereklidir")]
        [Display(Name = "Hedef")]
        public string Goal { get; set; } = string.Empty;

        public string? Recommendation { get; set; }
    }
}
