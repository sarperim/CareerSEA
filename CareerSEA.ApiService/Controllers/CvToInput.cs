using CareerSEA.Contracts.Requests;
using CareerSEA.Contracts.Responses;
using CareerSEA.Data.Entities;
using CareerSEA.Services.Interfaces;
using CareerSEA.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CareerSEA.ApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CvToInput : ControllerBase
    {
        public readonly ILlamaInputService _llamaInputService;
        public readonly IExperiencePredictionService _experiencePredictionService;

        public CvToInput(ILlamaInputService llamaInputService, IExperiencePredictionService experiencePredictionService)
        {
            _llamaInputService = llamaInputService;
            _experiencePredictionService = experiencePredictionService;
        }

        [Authorize]
        [HttpPost("Parse")]
        [RequestSizeLimit(long.MaxValue)] 
        [RequestTimeout(180000)]
        public async Task<ActionResult<BaseResponse>> Parse(ParseRequest request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized(); ;
            var llamaData = await _llamaInputService.ExtractCareerDataAsync(request.cvText);
            var predictionResponse = await _experiencePredictionService.PredictFromLlamaOutput(llamaData, Guid.Parse(userIdClaim));
            return Ok(predictionResponse);
        }
    }
}
