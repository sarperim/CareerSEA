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
            var existingUser = await _dbContext.Users.FirstOrDefaultAsync(a => a.Id == userId);
            if(existingUser == null)
            {
                return new BaseResponse
                {
                    Status = false,
                    Message = "Null"
                };
            }
            var experience = new Experience
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = response.Title,
                Description = response.Description,
                Skills = response.Skills
            };
            await _dbContext.Experiences.AddAsync(experience);

            var saved = new ExperienceResponse
            {
                Title = response.Title,
                Description = response.Description,
                Skills = response.Skills
            };

            AIResponse aiResult = null;
            try
            {
                // Prepare the payload matching the Python "UserInput" structure
                var aiRequest = new AIRequest
                {
                    jobs = new List<AIJobDto>
                    {
                        new AIJobDto
                        {
                            title = response.Title,
                            description = response.Description,
                            skills = response.Skills
                        }
                    }
                };

                // Serialize to JSON
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(aiRequest),
                    Encoding.UTF8,
                    "application/json"
                );

                // Send POST request to Python API
                // Ensure this URL matches where your Python script is running
                var httpResponse = await _httpClient.PostAsync("/predict", jsonContent);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseString = await httpResponse.Content.ReadAsStringAsync();

                    // 1. Deserialize into your DTO (AIResponse)
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    aiResult = JsonSerializer.Deserialize<AIResponse>(responseString, options);

                    if (aiResult != null)
                    {
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
                }
            }
            catch (Exception ex)
            {
                // Log the error, but maybe don't fail the whole request if the DB save succeeded
                Console.WriteLine($"AI Service Error: {ex.Message}");
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

            AIResponse aiResult = null;
            try
            {
                // 1. Serialize the Llama output directly (it's already in the correct AIRequest format)
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(llamaOutput),
                    Encoding.UTF8,
                    "application/json"
                );

                // 2. Send POST request to Python API
                var httpResponse = await _httpClient.PostAsync("/predict", jsonContent);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseString = await httpResponse.Content.ReadAsStringAsync();

                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    aiResult = JsonSerializer.Deserialize<AIResponse>(responseString, options);

                    if (aiResult != null)
                    {
                        // 3. Map the data: Copy from AIResponse -> PredictionResult
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

                        // 4. Create the Database Entity
                        var predictionEntry = new Prediction
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            Result = dbResult
                        };

                        // 5. Save to DB
                        await _dbContext.Predictions.AddAsync(predictionEntry);
                        await _dbContext.SaveChangesAsync();
                    }
                }
                else
                {
                    return new BaseResponse
                    {
                        Status = false,
                        Message = $"Python API failed with status code: {httpResponse.StatusCode}"
                    };
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
    }
}
