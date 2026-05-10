using CareerSEA.Services.Interfaces;
using System.Text.Json;

namespace CareerSEA.Services.Services
{
    public class OnetService : IOnetService
    {
        private readonly HttpClient _httpClient;

        public OnetService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private async Task<JsonDocument> GetAsync(string path, Dictionary<string, string>? query = null)
        {
            var url = path.TrimStart('/');
            if (query is { Count: > 0 })
            {
                url += "?" + string.Join("&", query.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
            }

            using var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        }

        public async Task<List<JsonElement>> SearchOccupationsAsync(string keyword, int end = 10)
        {
            using var doc = await GetAsync("/mnm/search", new() { ["keyword"] = keyword, ["end"] = end.ToString() });
            if (!doc.RootElement.TryGetProperty("career", out var careers) || careers.ValueKind != JsonValueKind.Array)
                return new();
            return careers.EnumerateArray().Select(x => x.Clone()).ToList();
        }

        public async Task<List<JsonElement>> GetOccupationTechnologyAsync(string onetCode, int start = 1, int end = 10)
        {
            using var doc = await GetAsync($"/online/occupations/{onetCode}/details/technology_skills", new() { ["start"] = start.ToString(), ["end"] = end.ToString() });

            if (doc.RootElement.TryGetProperty("technology_skills", out var techSkills))
            {
                if (techSkills.TryGetProperty("category", out var categories) && categories.ValueKind == JsonValueKind.Array)
                {
                    return categories.EnumerateArray().Select(x => x.Clone()).ToList();
                }
            }

            if (doc.RootElement.TryGetProperty("category", out var directCategories) && directCategories.ValueKind == JsonValueKind.Array)
            {
                return directCategories.EnumerateArray().Select(x => x.Clone()).ToList();
            }

            return new();
        }

        public string ExtractOnetCode(JsonElement item) =>
            item.TryGetProperty("code", out var code) ? code.GetString() ?? string.Empty : string.Empty;
    }
}
