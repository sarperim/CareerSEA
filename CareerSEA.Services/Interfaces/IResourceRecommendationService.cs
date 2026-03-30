using System.Text.Json;

namespace CareerSEA.Services.Interfaces
{
    public interface IOnetService
    {
        Task<List<JsonElement>> SearchOccupationsAsync(string keyword, int end = 10);
        Task<List<JsonElement>> GetOccupationTechnologyAsync(string onetCode, int start = 1, int end = 10);
        string ExtractOnetCode(JsonElement item);
    }

    public interface ISkillGapService
    {
        Task<Dictionary<string, object>> GenerateSkillGapAsync(string bestJob, List<string> userSkills);
    }

    public interface IResourceRecommendationService
    {
        Task<List<Dictionary<string, object>>> GenerateResourceRecommendationsAsync(
                string bestJob,
                List<string> missingSkills,
                List<string> userSkills,
                int maxSkills = 5,
                int perSkill = 4);
    }
}
