using CareerSEA.Contracts.Responses;
using CareerSEA.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CareerSEA.ApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobPostController : ControllerBase
    {
        private readonly IJobPostService _jobPostService;

        public JobPostController(IJobPostService adzunaService)
        {
            _jobPostService= adzunaService;
        }

        [HttpGet]
        public async Task<IActionResult> SearchJobs([FromQuery] string query, [FromQuery] string country = "gb")
        {
            if (string.IsNullOrEmpty(query)) return BadRequest("Query is required");

            try
            {
                // Controller delegates work to the Service
                var jobs = await _jobPostService.SearchJobsAsync(query, country);
                return Ok(jobs);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

    }
}
