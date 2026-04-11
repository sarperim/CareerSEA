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
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CareerSEA.Services.Services
{
    public class ExperiencePredictionService : IExperiencePredictionService
    {
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
            var existingUser = await _dbContext.Users.FirstOrDefaultAsync(a => a.Id == userId);
            if(existingUser == null)
            {
                return new BaseResponse
                {
                    Status = false,
                    Message = "Null"
                };
            }

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

            var experiences = responses.Select(response => new Experience
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = response.Title.Trim(),
                Description = response.Description.Trim(),
                Skills = response.Skills.Trim()
            }).ToList();
            await _dbContext.Experiences.AddRangeAsync(experiences);

            var aiRequest = new AIRequest
            {
                jobs = experiences.Select(experience => new AIJobDto
                {
                    title = experience.Title,
                    description = experience.Description,
                    skills = experience.Skills
                }).ToList()
            };

            return await RunPredictionAsync(aiRequest, userId);
        }

        private async Task<BaseResponse> RunPredictionAsync(AIRequest aiRequest, Guid userId)
        {
            AIResponse? aiResult = null;
            try
            {
                // Serialize to JSON
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(aiRequest),
                    Encoding.UTF8,
                    "application/json"
                );

                // Send POST request to Python API
                // Ensure this URL matches where your Python script is running
                var httpResponse = await _httpClient.PostAsync("/predict", jsonContent);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    return new BaseResponse
                    {
                        Status = false,
                        Message = $"Python API failed with status code: {httpResponse.StatusCode}"
                    };
                }

                var responseString = await httpResponse.Content.ReadAsStringAsync();

                // 1. Deserialize into your DTO (AIResponse)
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                aiResult = JsonSerializer.Deserialize<AIResponse>(responseString, options);

                if (aiResult == null)
                {
                    return new BaseResponse
                    {
                        Status = false,
                        Message = "Prediction service returned an empty response."
                    };
                }

                // 2. MAP the data: Copy from AIResponse -> PredictionResult
                var dbResult = new PredictionResult
                {
                    // Left side = Database Class | Right side = AI Response Class
                    BestJob = aiResult.best_job,
                    MatchScore = aiResult.match_score, // float converts to double automatically

                    // Map the list of recommendations
                    Recommendations = aiResult.recommendations?.Select(r => new JobRecommendation
                    {
                        Label = r.label,
                        Score = r.score
                    }).ToList() ?? new List<JobRecommendation>()
                };

                // 3. Create the Database Entity
                var predictionEntry = new Prediction
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Result = dbResult // Now we are assigning the correct type!
                };

                // 4. Save to DB
                await _dbContext.Predictions.AddAsync(predictionEntry);
                await _dbContext.SaveChangesAsync();
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

        public async Task<BaseResponse> PredictFromLlamaOutput(AIRequest llamaOutput, Guid userId)
        {
            var existingUser = await _dbContext.Users.FirstOrDefaultAsync(a => a.Id == userId);
            if (existingUser == null)
            {
                return new BaseResponse
                {
                    Status = false,
                    Message = "User not found"
                };
            }

            if (llamaOutput == null || llamaOutput.jobs == null || !llamaOutput.jobs.Any())
            {
                return new BaseResponse
                {
                    Status = false,
                    Message = "No valid jobs found in the Llama output to send for prediction."
                };
            }

            return await RunPredictionAsync(llamaOutput, userId);
        }
    }
}
