using CareerSEA.Contracts.Requests;
using CareerSEA.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CareerSEA.ApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Protect it with JWT
    public class SkillGapController : ControllerBase
    {
        private readonly ISkillGapService _skillGapService;

        public SkillGapController(ISkillGapService skillGapService)
        {
            _skillGapService = skillGapService;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeSkillGap([FromBody] SkillGapRequest request)
        {
            // Basic validation
            if (request == null || string.IsNullOrWhiteSpace(request.BestJob))
            {
                return BadRequest(new { message = "BestJob is required to calculate the skill gap." });
            }

            try
            {
                var skillGap = await _skillGapService.GenerateSkillGapAsync(request.BestJob, request.UserSkills ?? new List<string>());
                return Ok(skillGap);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Failed to communicate with O*NET.", details = ex.Message });
            }
        }
    }

    // DTO for the Request
  
}
