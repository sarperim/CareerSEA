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
        public ExperiencePredictionService(CareerSEADbContext dbContext, IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
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
            await _dbContext.SaveChangesAsync();

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
                var httpResponse = await _httpClient.PostAsync("http://localhost:8001/predict", jsonContent);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseString = await httpResponse.Content.ReadAsStringAsync();

                    // Configure deserializer to be case-insensitive (Python uses snake_case, C# usually PascalCase)
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    aiResult = JsonSerializer.Deserialize<AIResponse>(responseString, options);
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
    }
}
