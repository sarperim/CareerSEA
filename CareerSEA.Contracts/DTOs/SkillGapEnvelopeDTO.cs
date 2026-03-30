using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CareerSEA.Contracts.DTOs
{
    public class SkillGapEnvelopeDTO
    {
        [JsonPropertyName("onet_occupation_title")]
        public string OnetOccupationTitle { get; set; } = string.Empty;

        [JsonPropertyName("onet_occupation_code")]
        public string OnetOccupationCode { get; set; } = string.Empty;

        [JsonPropertyName("match_type")]
        public string MatchType { get; set; } = string.Empty;

        [JsonPropertyName("user_skills")]
        public List<string> UserSkills { get; set; } = new();

        [JsonPropertyName("technology_gap")]
        public TechnologyGapDTO TechnologyGap { get; set; } = new();
    }

    public class TechnologyGapDTO
    {
        [JsonPropertyName("target_skills")]
        public List<string> TargetSkills { get; set; } = new();

        [JsonPropertyName("matched_skills")]
        public List<string> MatchedSkills { get; set; } = new();

        [JsonPropertyName("missing_skills")]
        public List<string> MissingSkills { get; set; } = new();
    }
}
