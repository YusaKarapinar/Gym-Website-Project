using System.Threading.Tasks;

namespace Project.API.Services
{
    public interface IGeminiService
    {
        Task<string> GenerateFitnessRecommendationAsync(string height, string weight, string bodyType, string goal);
    }
}
