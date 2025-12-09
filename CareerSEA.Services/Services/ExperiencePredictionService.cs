using CareerSEA.Contracts.Requests;
using CareerSEA.Contracts.Responses;
using CareerSEA.Data;
using CareerSEA.Data.Entities;
using CareerSEA.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace CareerSEA.Services.Services
{
    public class ExperiencePredictionService : IExperiencePredictionService
    {
        public CareerSEADbContext _dbContext;
        public ExperiencePredictionService(CareerSEADbContext dbContext)
        {
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
            return new BaseResponse 
            {
                Status = true, 
                Message = "Success",
                Data =  saved
            };


        }
    }
}
