using CareerSEA.Contracts.DTOs;
using CareerSEA.Services.Interfaces;
using System.Text.Json;
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
            var bestJobTitle = bestJob.Trim();

            var normalizedUserSkills = userSkills
                .SelectMany(skill => (skill ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(skill => skill.Trim())
                .Where(skill => !string.IsNullOrWhiteSpace(skill))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var bestMatch = await ResolveOccupationAsync(bestJobTitle);
            if (bestMatch is null)
            {
                // No O*NET occupation matches this title (e.g. niche / non-US ESCO roles
                // like "astrologer"). Degrade gracefully instead of failing the whole
                // analysis with a 500 the UI surfaces as a red error.
                return new SkillGapEnvelopeDTO
                {
                    OnetOccupationTitle = bestJobTitle,
                    OnetOccupationCode = string.Empty,
                    MatchType = "no_match",
                    UserSkills = normalizedUserSkills,
                    TechnologyGap = new TechnologyGapDTO()
                };
            }

            var onetCode = _onetService.ExtractOnetCode(bestMatch.Value);
            var onetTitle = GetOccupationTitle(bestMatch.Value);

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
                MatchType = IsEquivalentOccupationTitle(bestJobTitle, onetTitle) ? "title_match" : "closest_title_match",
                UserSkills = normalizedUserSkills,
                TechnologyGap = new TechnologyGapDTO
                {
                    TargetSkills = targetTechnologies,
                    MatchedSkills = matched,
                    MissingSkills = missing
                }
            };
        }

        private async Task<JsonElement?> ResolveOccupationAsync(string bestJobTitle)
        {
            foreach (var keyword in BuildSearchCandidates(bestJobTitle))
            {
                var careers = await _onetService.SearchOccupationsAsync(keyword, 20);
                if (careers.Any())
                    return SelectClosestOccupation(bestJobTitle, careers);
            }

            return null;
        }

        // Broadens the search when the full title has no O*NET hit by dropping leading
        // qualifier words, so "advanced nurse practitioner" can fall back to "practitioner".
        // SelectClosestOccupation still scores results against the original title.
        private static IEnumerable<string> BuildSearchCandidates(string bestJobTitle)
        {
            yield return bestJobTitle;

            var tokens = NormalizeOccupationTitle(bestJobTitle)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (var skip = 1; skip < tokens.Length; skip++)
                yield return string.Join(' ', tokens.Skip(skip));
        }

        private static JsonElement SelectClosestOccupation(string bestJobTitle, List<JsonElement> careers)
        {
            return careers
                .Select((career, index) => new
                {
                    Career = career,
                    Rank = index,
                    Score = ScoreOccupationTitle(bestJobTitle, GetOccupationTitle(career))
                })
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.Rank)
                .First()
                .Career;
        }

        private static string GetOccupationTitle(JsonElement item) =>
            item.TryGetProperty("title", out var title) ? title.GetString() ?? "Unknown" : "Unknown";

        private static bool IsEquivalentOccupationTitle(string first, string second) =>
            NormalizeOccupationTitle(first) == NormalizeOccupationTitle(second)
            || NormalizeOccupationTitleForMatching(first) == NormalizeOccupationTitleForMatching(second);

        private static double ScoreOccupationTitle(string bestJobTitle, string onetTitle)
        {
            var target = NormalizeOccupationTitle(bestJobTitle);
            var candidate = NormalizeOccupationTitle(onetTitle);

            if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(candidate))
                return 0;

            if (target == candidate)
                return 100;

            var targetForMatching = NormalizeOccupationTitleForMatching(bestJobTitle);
            var candidateForMatching = NormalizeOccupationTitleForMatching(onetTitle);

            if (targetForMatching == candidateForMatching)
                return 95;

            var targetTokens = targetForMatching.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var candidateTokens = candidateForMatching.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (targetTokens.Count == 0 || candidateTokens.Count == 0)
                return 0;

            var shared = targetTokens.Intersect(candidateTokens, StringComparer.OrdinalIgnoreCase).Count();
            var union = targetTokens.Union(candidateTokens, StringComparer.OrdinalIgnoreCase).Count();
            var targetCoverage = (double)shared / targetTokens.Count;
            var candidateCoverage = (double)shared / candidateTokens.Count;
            var jaccard = union == 0 ? 0 : (double)shared / union;

            var score = (targetCoverage * 70) + (candidateCoverage * 20) + (jaccard * 10);

            if (candidateForMatching.Contains(targetForMatching, StringComparison.OrdinalIgnoreCase)
                || targetForMatching.Contains(candidateForMatching, StringComparison.OrdinalIgnoreCase))
            {
                score += 10;
            }

            return score;
        }

        private static string NormalizeOccupationTitle(string title)
        {
            title = (title ?? string.Empty).Trim().ToLowerInvariant();
            title = Regex.Replace(title, @"[^\p{L}\p{N}]+", " ");
            return Regex.Replace(title, @"\s+", " ").Trim();
        }

        private static string NormalizeOccupationTitleForMatching(string title)
        {
            var normalized = NormalizeOccupationTitle(title);
            var tokens = normalized
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(SingularizeToken);

            return string.Join(' ', tokens);
        }

        private static string SingularizeToken(string token)
        {
            if (token.Length > 3 && token.EndsWith("ies", StringComparison.OrdinalIgnoreCase))
                return token[..^3] + "y";

            if (token.Length > 3
                && token.EndsWith("s", StringComparison.OrdinalIgnoreCase)
                && !token.EndsWith("ss", StringComparison.OrdinalIgnoreCase))
            {
                return token[..^1];
            }

            return token;
        }
    }
}
