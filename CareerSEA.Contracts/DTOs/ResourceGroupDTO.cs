using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CareerSEA.Contracts.DTOs
{
    public class ResourceGroupDTO
    {
        [JsonPropertyName("skill")]
        public string Skill { get; set; } = string.Empty;

        [JsonPropertyName("source")]
        public string Source { get; set; } = string.Empty;

        [JsonPropertyName("resources")]
        public List<ResourceItemDTO> Resources { get; set; } = new();
    }

    public class ResourceItemDTO
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("snippet")]
        public string Snippet { get; set; } = string.Empty;

        [JsonPropertyName("provider")]
        public string Provider { get; set; } = string.Empty;

        [JsonPropertyName("skill")]
        public string Skill { get; set; } = string.Empty;

        [JsonPropertyName("query_used")]
        public string QueryUsed { get; set; } = string.Empty;

        [JsonPropertyName("score")]
        public double Score { get; set; }
    }
}
