using CareerSEA.Contracts.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareerSEA.Services.Interfaces
{
    public interface IJobPostService
    {
        Task<IEnumerable<JobListingDto>> SearchJobsAsync(string query, string country);
    }
}
