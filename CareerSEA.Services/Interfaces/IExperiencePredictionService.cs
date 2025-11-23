using CareerSEA.Contracts.Requests;
using CareerSEA.Contracts.Responses;
using CareerSEA.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareerSEA.Services.Interfaces
{
    public interface IExperiencePredictionService
    {
        Task<List<Experience>> GetForms();
        Task<BaseResponse> SaveForm(ExperienceRequest response,Guid userId);

    }
}
