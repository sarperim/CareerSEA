using CareerSEA.Contracts.Requests;
using CareerSEA.Contracts.Responses;
using CareerSEA.Services.Interfaces;
using CareerSEA.Services.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace CareerSEA.ApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CvToInput : ControllerBase
    {
        public readonly ILlamaInputService _llamaInputService;

        public CvToInput(ILlamaInputService llamaInputService)
        {
            _llamaInputService = llamaInputService;
        }

        [HttpPost("Parse")]
        [RequestSizeLimit(long.MaxValue)] 
        [RequestTimeout(180000)]
        public async Task<ActionResult<BaseResponse>> Parse(ParseRequest request)
        {
            var data = await _llamaInputService.ExtractCareerDataAsync(request.cvText);
            return Ok(data);
        }
    }
}
