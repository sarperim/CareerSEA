using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CareerSEA.Data.Entities
{
    public class Prediction
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; } //FK
        [Column(TypeName = "jsonb")]
        public PredictionResult Result { get; set; }

        public User User { get; set; }
    }

    public class PredictionResult
    {
        [JsonPropertyName("best_job")]
        public string BestJob { get; set; }

        [JsonPropertyName("match_score")]
        public double MatchScore { get; set; }

        [JsonPropertyName("recommendations")]
        public List<JobRecommendation> Recommendations { get; set; } = new();
    }

    public class JobRecommendation
    {
        [JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonPropertyName("score")]
        public double Score { get; set; }
    }
}
