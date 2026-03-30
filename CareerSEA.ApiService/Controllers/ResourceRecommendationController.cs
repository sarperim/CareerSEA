using CareerSEA.Contracts.Requests;
using CareerSEA.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CareerSEA.ApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // This ensures only logged-in users with a valid JWT can access this endpoint
    public class ResourceRecommendationController : ControllerBase
    {
        private readonly IResourceRecommendationService _resourceService;

        // The service is automatically injected here because we registered it in Program.cs
        public ResourceRecommendationController(IResourceRecommendationService resourceService)
        {
            _resourceService = resourceService;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateRecommendations([FromBody] ResourceRecommendationRequest request)
        {
            // Basic validation
            if (request == null || string.IsNullOrWhiteSpace(request.BestJob))
            {
                return BadRequest(new { message = "The BestJob field is required." });
            }

            try
            {
                // Call the service layer
                var recommendations = await _resourceService.GenerateResourceRecommendationsAsync(
                    request.BestJob,
                    request.MissingSkills ?? new List<string>(),
                    request.UserSkills ?? new List<string>(),
                    request.MaxSkills > 0 ? request.MaxSkills : 5,
                    request.PerSkill > 0 ? request.PerSkill : 4
                );

                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                // Return a 500 Internal Server Error if something goes wrong (e.g., Brave Search API is down)
                return StatusCode(500, new
                {
                    message = "An error occurred while fetching resource recommendations.",
                    details = ex.Message
                });
            }
        }
    }

    
}