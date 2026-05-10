using CareerSEA.Contracts.DTOs;
using CareerSEA.Services.Interfaces;
using System.Text.RegularExpressions;

namespace CareerSEA.Services.Services
{
    public class SkillGapService : ISkillGapService
    {
        private readonly IOnetService _onetService;

        public SkillGapService(IOnetService onetService)
        {
            _onetService = onetService;
        }

        public async Task<SkillGapEnvelopeDTO> GenerateSkillGapAsync(string bestJob, List<string> userSkills)
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
                foreach (var sectionName in new[] { "example", "example_more" })
                {
                    if (category.TryGetProperty(sectionName, out var examples) && examples.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var example in examples.EnumerateArray())
                        {
                            string skillName = string.Empty;

                            if (example.TryGetProperty("title", out var titleProp))
                                skillName = titleProp.GetString() ?? string.Empty;
                            else if (example.TryGetProperty("name", out var nameProp))
                                skillName = nameProp.GetString() ?? string.Empty;

                            if (!string.IsNullOrWhiteSpace(skillName) && !targetTechnologies.Contains(skillName))
                                targetTechnologies.Add(skillName);
                        }
                    }
                }
            }

            static string NormalizeSkill(string s)
            {
                s = (s ?? "").Trim().ToLowerInvariant();
                s = Regex.Replace(s, @"[^\p{L}\p{N}]+", " ");
                s = Regex.Replace(s, @"\s+", " ").Trim();
                return s;
            }

            static List<string> TokenizeSkill(string s) =>
                NormalizeSkill(s).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

            static bool SkillMatch(string userSkill, string targetSkill)
            {
                var user = NormalizeSkill(userSkill);
                var target = NormalizeSkill(targetSkill);

                if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(target))
                    return false;

                if (user == target) return true;

                var userTokens = TokenizeSkill(userSkill);
                var targetTokens = TokenizeSkill(targetSkill);

                if (userTokens.Count == 0 || targetTokens.Count == 0) return false;

                var userSet = new HashSet<string>(userTokens, StringComparer.OrdinalIgnoreCase);
                var targetSet = new HashSet<string>(targetTokens, StringComparer.OrdinalIgnoreCase);

                if (userSet.Count == 1)
                    return targetSet.Contains(userSet.First());

                return userSet.All(t => targetSet.Contains(t));
            }

            var normalizedUserSkills = userSkills
                .SelectMany(skill => (skill ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(skill => skill.Trim())
                .Where(skill => !string.IsNullOrWhiteSpace(skill))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var matched = targetTechnologies
                .Where(target => normalizedUserSkills.Any(userSkill => SkillMatch(userSkill, target)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var missing = targetTechnologies
                .Where(target => !normalizedUserSkills.Any(userSkill => SkillMatch(userSkill, target)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToList();

            return new SkillGapEnvelopeDTO
            {
                OnetOccupationTitle = onetTitle,
                OnetOccupationCode = onetCode,
                UserSkills = normalizedUserSkills,
                TechnologyGap = new TechnologyGapDTO
                {
                    TargetSkills = targetTechnologies,
                    MatchedSkills = matched,
                    MissingSkills = missing
                }
            };
        }
    }
}
