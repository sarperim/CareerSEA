using CareerSEA.Contracts.DTOs;

namespace CareerSEA.Services.Interfaces
{
    public interface IResourceRecommendationService
    {
        Task<List<ResourceGroupDTO>> GenerateResourceRecommendationsAsync(
            string bestJob,
            List<string> missingSkills,
            List<string> userSkills,
            int maxSkills = 5,
            int perSkill = 4);
    }
}
