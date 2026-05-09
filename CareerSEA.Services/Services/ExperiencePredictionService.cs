using CareerSEA.Contracts.Requests;
using CareerSEA.Contracts.Responses;
using CareerSEA.Data;
using CareerSEA.Data.Entities;
using CareerSEA.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CareerSEA.Services.Services
{
    public class ExperiencePredictionService : IExperiencePredictionService
    {
        private sealed class PythonPredictRequest
        {
            [JsonPropertyName("text")]
            public string Text { get; set; } = string.Empty;

            [JsonPropertyName("top_k")]
            public int TopK { get; set; } = 5;

            [JsonPropertyName("add_bge_prefix")]
            public bool AddBgePrefix { get; set; } = true;
        }

        private sealed class PythonPredictResponse
        {
            public string Input { get; set; } = string.Empty;
            public int Top_K { get; set; }
            public List<PythonPredictionItem> Predictions { get; set; } = new();
        }

        private sealed class PythonPredictionItem
        {
            public string Label { get; set; } = string.Empty;
            public float Score { get; set; }
            public int Rank { get; set; }
        }

        private readonly CareerSEADbContext _dbContext;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ExperiencePredictionService>? _logger;

        public ExperiencePredictionService(
            CareerSEADbContext dbContext,
            HttpClient httpClient,
            ILogger<ExperiencePredictionService>? logger = null)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
            _logger = logger;
        }
        public async Task<BaseResponse> GetForms(Guid userId)
        {
            var existingUser = await _dbContext.Experiences.FirstOrDefaultAsync(a => a.UserId == userId);
            if (existingUser == null)
            {
                return new BaseResponse
                {
                    Status = false,
                    Message = "Error"
                };
            }
            var relatedData = await _dbContext.Experiences
                              .Where(o => o.UserId == userId)
                              .ToListAsync();
            return new BaseResponse
            {
                Status = true,
                Message = "Success",
                Data = relatedData
            };
        }

        public async Task<BaseResponse> SaveForm(ExperienceRequest response,Guid userId)
        {
            return await SaveForms(new List<ExperienceRequest> { response }, userId);
        }

        public async Task<BaseResponse> SaveForms(List<ExperienceRequest> responses, Guid userId)
        {
            if (responses == null || !responses.Any())
            {
                return new BaseResponse
                {
                    Status = false,
                    Message = "At least one complete experience is required."
                };
            }

            if (responses.Any(response => response == null
                || string.IsNullOrWhiteSpace(response.Title)
                || string.IsNullOrWhiteSpace(response.Description)
                || string.IsNullOrWhiteSpace(response.Skills)))
            {
                return new BaseResponse
                {
                    Status = false,
                    Message = "All experience entries must include a title, description, and skills."
                };
            }

            var aiRequest = new AIRequest
            {
                jobs = responses.Select(response => new AIJobDto
                {
                    title = response.Title.Trim(),
                    description = response.Description.Trim(),
                    skills = response.Skills.Trim()
                }).ToList()
            };

            bool userExists = await _dbContext.Users.AnyAsync(u => u.Id == userId);

            // Experiences are constructed but NOT added to the change tracker yet;
            // they're persisted atomically with the prediction only if the AI call succeeds.
            var experiencesToSave = userExists
                ? responses.Select(response => new Experience
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Title = response.Title.Trim(),
                    Description = response.Description.Trim(),
                    Skills = response.Skills.Trim()
                }).ToList()
                : null;

            return await RunPredictionAsync(aiRequest, userId, savePrediction: userExists, experiencesToSave);
        }

        private async Task<BaseResponse> RunPredictionAsync(
            AIRequest aiRequest,
            Guid userId,
            bool savePrediction,
            List<Experience>? experiencesToSave)
        {
            try
            {
                var predictionText = BuildPredictionText(aiRequest);
                if (string.IsNullOrWhiteSpace(predictionText))
                {
                    return new BaseResponse
                    {
                        Status = false,
                        Message = "Prediction input was empty after formatting the submitted jobs."
                    };
                }

                var pythonRequest = new PythonPredictRequest
                {
                    Text = predictionText
                };

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(pythonRequest),
                    Encoding.UTF8,
                    "application/json"
                );

                var httpResponse = await _httpClient.PostAsync("/predict", jsonContent);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    var errorDetails = await httpResponse.Content.ReadAsStringAsync();
                    _logger?.LogWarning(
                        "Prediction API returned non-success status {StatusCode} for user {UserId}: {Details}",
                        httpResponse.StatusCode, userId, errorDetails);
                    return new BaseResponse
                    {
                        Status = false,
                        Message = $"Prediction service failed with status code {(int)httpResponse.StatusCode}."
                    };
                }

                var responseString = await httpResponse.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                PythonPredictResponse? pythonResult;
                try
                {
                    pythonResult = JsonSerializer.Deserialize<PythonPredictResponse>(responseString, options);
                }
                catch (JsonException ex)
                {
                    _logger?.LogError(ex, "Prediction API returned malformed JSON for user {UserId}.", userId);
                    return new BaseResponse
                    {
                        Status = false,
                        Message = "Prediction service returned a malformed response."
                    };
                }

                if (pythonResult?.Predictions == null || !pythonResult.Predictions.Any())
                {
                    return new BaseResponse
                    {
                        Status = false,
                        Message = "Prediction service returned no ranked job matches."
                    };
                }

                var orderedPredictions = pythonResult.Predictions
                    .OrderBy(prediction => prediction.Rank)
                    .ToList();

                var aiResult = new AIResponse
                {
                    best_job = orderedPredictions[0].Label,
                    match_score = orderedPredictions[0].Score,
                    recommendations = orderedPredictions.Select(prediction => new AIRecommendation
                    {
                        label = prediction.Label,
                        score = prediction.Score
                    }).ToList()
                };

                if (savePrediction)
                {
                    var dbResult = new PredictionResult
                    {
                        BestJob = aiResult.best_job,
                        MatchScore = aiResult.match_score,
                        Recommendations = aiResult.recommendations?.Select(r => new JobRecommendation
                        {
                            Label = r.label,
                            Score = r.score
                        }).ToList() ?? new List<JobRecommendation>()
                    };

                    var predictionEntry = new Prediction
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Result = dbResult
                    };

                    if (experiencesToSave != null && experiencesToSave.Count > 0)
                    {
                        await _dbContext.Experiences.AddRangeAsync(experiencesToSave);
                    }
                    await _dbContext.Predictions.AddAsync(predictionEntry);
                    await _dbContext.SaveChangesAsync();
                }

                return new BaseResponse
                {
                    Status = true,
                    Message = "Success",
                    Data = aiResult
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Prediction service call failed for user {UserId}.", userId);
                return new BaseResponse
                {
                    Status = false,
                    Message = "An error occurred while communicating with the prediction service."
                };
            }
        }

        private static string BuildPredictionText(AIRequest aiRequest)
        {
            if (aiRequest.jobs == null || !aiRequest.jobs.Any())
            {
                return string.Empty;
            }

            return string.Join(
                "\n\n",
                aiRequest.jobs
                    .Where(job => job != null)
                    .Select(job =>
                    {
                        var parts = new List<string>();

                        if (!string.IsNullOrWhiteSpace(job.title))
                        {
                            parts.Add($"Title: {job.title.Trim()}");
                        }

                        if (!string.IsNullOrWhiteSpace(job.description))
                        {
                            parts.Add($"Description: {job.description.Trim()}");
                        }

                        if (!string.IsNullOrWhiteSpace(job.skills))
                        {
                            parts.Add($"Skills: {job.skills.Trim()}");
                        }

                        return string.Join(". ", parts);
                    })
                    .Where(text => !string.IsNullOrWhiteSpace(text))
            );
        }

        public async Task<BaseResponse> PredictFromLlamaOutput(AIRequest llamaOutput, Guid userId)
        {
            if (llamaOutput == null || llamaOutput.jobs == null || !llamaOutput.jobs.Any())
            {
                return new BaseResponse
                {
                    Status = false,
                    Message = "No valid jobs found in the Qwen output to send for prediction."
                };
            }

            // Llama-driven flow only persists the prediction itself; experiences
            // were already saved during the original CV ingestion step.
            bool userExists = await _dbContext.Users.AnyAsync(u => u.Id == userId);
            return await RunPredictionAsync(llamaOutput, userId, savePrediction: userExists, experiencesToSave: null);
        }
    }
}
