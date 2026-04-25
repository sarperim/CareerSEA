using CareerSEA.Contracts.Requests;
using CareerSEA.Contracts.Responses;
using CareerSEA.Data;
using CareerSEA.Data.Entities;
using CareerSEA.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
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

        public CareerSEADbContext _dbContext;
        public HttpClient _httpClient;
        public ExperiencePredictionService(CareerSEADbContext dbContext, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
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
            if (userExists)
            {
                var experiences = responses.Select(response => new Experience
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Title = response.Title.Trim(),
                    Description = response.Description.Trim(),
                    Skills = response.Skills.Trim()
                }).ToList();
                await _dbContext.Experiences.AddRangeAsync(experiences);
            }

            return await RunPredictionAsync(aiRequest, userId, saveToDb: userExists);
        }

        private async Task<BaseResponse> RunPredictionAsync(AIRequest aiRequest, Guid userId, bool saveToDb = true)
        {
            AIResponse? aiResult = null;
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
                    return new BaseResponse
                    {
                        Status = false,
                        Message = $"Python API failed with status code: {httpResponse.StatusCode}. {errorDetails}"
                    };
                }

                var responseString = await httpResponse.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var pythonResult = JsonSerializer.Deserialize<PythonPredictResponse>(responseString, options);

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

                aiResult = new AIResponse
                {
                    best_job = orderedPredictions[0].Label,
                    match_score = orderedPredictions[0].Score,
                    recommendations = orderedPredictions.Select(prediction => new AIRecommendation
                    {
                        label = prediction.Label,
                        score = prediction.Score
                    }).ToList()
                };

                if (saveToDb)
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

                    await _dbContext.Predictions.AddAsync(predictionEntry);
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AI Service Error: {ex.Message}");
                return new BaseResponse
                {
                    Status = false,
                    Message = "An error occurred while communicating with the prediction service."
                };
            }

            return new BaseResponse
            {
                Status = true,
                Message = "Success",
                Data = aiResult
            };
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

            bool userExists = await _dbContext.Users.AnyAsync(u => u.Id == userId);
            return await RunPredictionAsync(llamaOutput, userId, saveToDb: userExists);
        }
    }
}
