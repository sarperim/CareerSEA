using System.Text.Json.Serialization;

namespace CareerSEA.Web;

// 1. The Service to hold the data
public class PredictionState
{
    public AnalysisData? Result { get; set; }
}

// 2. The Models (moved here so both pages can use them)
public class AnalysisData
{
    [JsonPropertyName("best_job")]
    public string BestJob { get; set; }

    [JsonPropertyName("match_score")]
    public float MatchScore { get; set; }

    [JsonPropertyName("recommendations")]
    public List<JobRecommendation> Recommendations { get; set; } = new();
}

public class JobRecommendation
{
    [JsonPropertyName("label")]
    public string Label { get; set; }

    [JsonPropertyName("score")]
    public float Score { get; set; }
}