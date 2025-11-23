using CareerSEA.Contracts.Requests;
using CareerSEA.Contracts.Responses;
using CareerSEA.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CareerSEA.ApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExperiencePredictionController : ControllerBase
    {
        private readonly IExperiencePredictionService _eps;
        public ExperiencePredictionController(IExperiencePredictionService experiencePredictionService) 
        {
            this._eps = experiencePredictionService;
        }

        [Authorize]
        [HttpPost("SaveForm")]
        public async Task<IActionResult> SaveForm(ExperienceRequest form)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                     return Unauthorized(); ;
            var response = await _eps.SaveForm(form,Guid.Parse(userIdClaim));

            return Ok(response);
        }
        [Authorize]
        [HttpGet("GetForms")]
        public async Task<ActionResult<BaseResponse>> GetForms()
        {
            var forms = await _eps.GetForms();
            return Ok(new BaseResponse { Status = true, Data = forms });
        }
    }
}

