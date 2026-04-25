using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CareerSEA.Contracts.Responses
{
    public class ExtractedCvResponse
    {
        [JsonPropertyName("experiences")]
        public List<ExtractedExperienceDto> Experiences { get; set; } = new();
    }

    public class ExtractedExperienceDto
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("skills")]
        public List<string> Skills { get; set; } = new();
    }
}
