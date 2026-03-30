using CareerSEA.Services.Interfaces;
using System.Text.Json;

namespace CareerSEA.Services.Services
{
    // =========================================================================
    // 1. RESOURCE RECOMMENDATION SERVICE (Brave Search Integration)
    // =========================================================================
    public class ResourceRecommendationService : IResourceRecommendationService
    {
        private readonly HttpClient _httpClient;

        public ResourceRecommendationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public static string DetectProvider(string? url)
        {
            url = (url ?? string.Empty).ToLowerInvariant();
            if (url.Contains("youtube.com") || url.Contains("youtu.be")) return "YouTube";
            if (url.Contains("udemy.com")) return "Udemy";
            if (url.Contains("coursera.org")) return "Coursera";
            if (url.Contains("freecodecamp.org")) return "freeCodeCamp";
            return "Unknown";
        }

        private async Task<List<Dictionary<string, object>>> SearchBraveAsync(string query, int count = 20)
        {
            var url = $"res/v1/web/search?q={Uri.EscapeDataString(query)}&count={count}&search_lang=en&country=us";
            using var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            if (!doc.RootElement.TryGetProperty("web", out var web) ||
                !web.TryGetProperty("results", out var results) ||
                results.ValueKind != JsonValueKind.Array)
            {
                return new();
            }

            var list = new List<Dictionary<string, object>>();
            foreach (var item in results.EnumerateArray())
            {
                list.Add(new Dictionary<string, object>
                {
                    ["url"] = item.TryGetProperty("url", out var urlEl) ? urlEl.GetString() ?? string.Empty : string.Empty,
                    ["title"] = item.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? string.Empty : string.Empty,
                    ["description"] = item.TryGetProperty("description", out var descEl) ? descEl.GetString() ?? string.Empty : string.Empty,
                });
            }
            return list;
        }

        public async Task<List<Dictionary<string, object>>> GenerateResourceRecommendationsAsync(
            string bestJob, List<string> missingSkills, List<string> userSkills, int maxSkills = 5, int perSkill = 4)
        {
            var grouped = new List<Dictionary<string, object>>();
            var targetSkills = missingSkills.Take(maxSkills).ToList();
            var source = "missing_skill";

            if (!targetSkills.Any())
            {
                targetSkills = userSkills.Select(s => s.Trim().ToLowerInvariant()).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().Take(maxSkills).ToList();
                source = "user_skill";
            }

            if (!targetSkills.Any())
            {
                targetSkills = new List<string> { bestJob.Trim() };
                source = "job_title";
            }

            foreach (var skill in targetSkills)
            {
                List<Dictionary<string, object>> resources = new();
                try
                {
                    var query = $"\"{skill}\" \"{bestJob}\" (site:youtube.com OR site:udemy.com OR site:coursera.org OR site:freecodecamp.org) course tutorial";
                    var rawResults = await SearchBraveAsync(query, 20);

                    var seen = new HashSet<string>();
                    foreach (var item in rawResults)
                    {
                        var url = item.GetValueOrDefault("url")?.ToString() ?? string.Empty;
                        var provider = DetectProvider(url);
                        if (provider == "Unknown" || string.IsNullOrWhiteSpace(url) || !seen.Add(url)) continue;

                        resources.Add(new Dictionary<string, object>
                        {
                            ["title"] = item.GetValueOrDefault("title")?.ToString() ?? string.Empty,
                            ["url"] = url,
                            ["snippet"] = item.GetValueOrDefault("description")?.ToString() ?? string.Empty,
                            ["provider"] = provider,
                            ["skill"] = skill,
                            ["query_used"] = query,
                            ["score"] = 1.0
                        });

                        if (resources.Count >= perSkill) break;
                    }
                }
                catch (Exception ex)
                {
                    resources.Add(new Dictionary<string, object>
                    {
                        ["title"] = "Search failed",
                        ["url"] = string.Empty,
                        ["snippet"] = ex.Message,
                        ["provider"] = "System",
                        ["skill"] = skill,
                        ["score"] = 0.0
                    });
                }

                grouped.Add(new Dictionary<string, object> { ["skill"] = skill, ["source"] = source, ["resources"] = resources });
            }
            return grouped;
        }
    }

    // =========================================================================
    // 2. ONET SERVICE HELPER (O*NET API Integration)
    // =========================================================================
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

            // FIX: O*NET wraps the categories inside a "technology_skills" object
            if (doc.RootElement.TryGetProperty("technology_skills", out var techSkills))
            {
                if (techSkills.TryGetProperty("category", out var categories) && categories.ValueKind == JsonValueKind.Array)
                {
                    return categories.EnumerateArray().Select(x => x.Clone()).ToList();
                }
            }

            // Fallback: If O*NET changes their structure or returns it directly
            if (doc.RootElement.TryGetProperty("category", out var directCategories) && directCategories.ValueKind == JsonValueKind.Array)
            {
                return directCategories.EnumerateArray().Select(x => x.Clone()).ToList();
            }

            return new();
        }

        public string ExtractOnetCode(JsonElement item) =>
            item.TryGetProperty("code", out var code) ? code.GetString() ?? string.Empty : string.Empty;
    }

    // =========================================================================
    // 3. SKILL GAP SERVICE HELPER (Business Logic)
    // =========================================================================
    public class SkillGapService : ISkillGapService
    {
        private readonly IOnetService _onetService;

        public SkillGapService(IOnetService onetService)
        {
            _onetService = onetService;
        }

        public async Task<Dictionary<string, object>> GenerateSkillGapAsync(string bestJob, List<string> userSkills)
        {
            var careers = await _onetService.SearchOccupationsAsync(bestJob, 3);
            if (!careers.Any()) throw new Exception($"O*NET occupation not found for: {bestJob}");

            var bestMatch = careers.First();
            var onetCode = _onetService.ExtractOnetCode(bestMatch);
            var onetTitle = bestMatch.TryGetProperty("title", out var title) ? title.GetString() ?? "Unknown" : "Unknown";

            var techCategories = await _onetService.GetOccupationTechnologyAsync(onetCode, 1, 20);
            var targetTechnologies = new List<string>();

            foreach (var category in techCategories)
            {
                // FIX 1: O*NET uses both "example" and "example_more" arrays
                foreach (var sectionName in new[] { "example", "example_more" })
                {
                    if (category.TryGetProperty(sectionName, out var examples) && examples.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var example in examples.EnumerateArray())
                        {
                            if (example.TryGetProperty("name", out var name))
                            {
                                var skillName = name.GetString() ?? string.Empty;

                                // FIX 3: Prevent duplicate skills from being added
                                if (!string.IsNullOrWhiteSpace(skillName) && !targetTechnologies.Contains(skillName))
                                {
                                    targetTechnologies.Add(skillName);
                                }
                            }
                        }
                    }
                }
            }

            // FIX 2: Find the O*NET target technologies that contain the user's skills
            var matched = targetTechnologies
                .Where(target => userSkills.Any(userSkill =>
                    target.Contains(userSkill, StringComparison.OrdinalIgnoreCase) ||
                    userSkill.Contains(target, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var missing = targetTechnologies
                .Where(t => !matched.Contains(t))
                .Take(10)
                .ToList();

            return new Dictionary<string, object>
            {
                ["onet_occupation_title"] = onetTitle,
                ["onet_occupation_code"] = onetCode,
                ["user_skills"] = userSkills,
                ["technology_gap"] = new Dictionary<string, object>
                {
                    ["target_skills"] = targetTechnologies,
                    ["matched_skills"] = matched,
                    ["missing_skills"] = missing
                }
            };
        }
    }
}
