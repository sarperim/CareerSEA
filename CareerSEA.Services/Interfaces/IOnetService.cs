using System.Text.Json;

namespace CareerSEA.Services.Interfaces
{
    public interface IOnetService
    {
        Task<List<JsonElement>> SearchOccupationsAsync(string keyword, int end = 10);
        Task<List<JsonElement>> GetOccupationTechnologyAsync(string onetCode, int start = 1, int end = 10);
        string ExtractOnetCode(JsonElement item);
    }
}
