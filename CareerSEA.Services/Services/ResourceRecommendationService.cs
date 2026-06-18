using CareerSEA.Contracts.DTOs;
using CareerSEA.Services.Interfaces;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CareerSEA.Services.Services
{
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

        private async Task<List<(string Url, string Title, string Description)>> SearchBraveAsync(string query, int count = 20)
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

            var list = new List<(string, string, string)>();
            foreach (var item in results.EnumerateArray())
            {
                list.Add((
                    item.TryGetProperty("url", out var urlEl) ? urlEl.GetString() ?? string.Empty : string.Empty,
                    item.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? string.Empty : string.Empty,
                    item.TryGetProperty("description", out var descEl) ? descEl.GetString() ?? string.Empty : string.Empty
                ));
            }
            return list;
        }

        public async Task<List<ResourceGroupDTO>> GenerateResourceRecommendationsAsync(
            string bestJob, List<string> missingSkills, List<string> userSkills, int maxSkills = 5, int perSkill = 4)
        {
            var grouped = new List<ResourceGroupDTO>();
            var bestJobTitle = bestJob.Trim();
            var targetSkills = missingSkills
                .Select(skill => skill.Trim())
                .Where(skill => !string.IsNullOrWhiteSpace(skill))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(maxSkills)
                .ToList();
            var source = "missing_skill";

            if (!targetSkills.Any())
            {
                targetSkills = new List<string> { bestJobTitle };
                source = "job_title";
            }

            foreach (var skill in targetSkills)
            {
                var resources = new List<ResourceItemDTO>();
                try
                {
                    var query = $"\"{skill}\" \"{bestJobTitle}\" (site:youtube.com OR site:udemy.com OR site:coursera.org OR site:freecodecamp.org) course tutorial";
                    var rawResults = await SearchBraveAsync(query, 20);

                    var seen = new HashSet<string>();
                    foreach (var (itemUrl, itemTitle, itemDescription) in rawResults)
                    {
                        var provider = DetectProvider(itemUrl);
                        if (provider == "Unknown" || string.IsNullOrWhiteSpace(itemUrl) || !seen.Add(itemUrl)) continue;

                        resources.Add(new ResourceItemDTO
                        {
                            Title = itemTitle,
                            Url = itemUrl,
                            Snippet = itemDescription,
                            Provider = provider,
                            Skill = skill,
                            QueryUsed = query,
                            Score = 1.0
                        });

                        if (resources.Count >= perSkill) break;
                    }
                }
                catch (Exception ex)
                {
                    resources.Add(new ResourceItemDTO
                    {
                        Title = "Search failed",
                        Url = string.Empty,
                        Snippet = ex.Message,
                        Provider = "System",
                        Skill = skill,
                        Score = 0.0
                    });
                }

                grouped.Add(new ResourceGroupDTO { Skill = skill, Source = source, Resources = resources });
            }

            return grouped;
        }
    }
}
