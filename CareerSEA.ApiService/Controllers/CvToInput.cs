using CareerSEA.Contracts.Responses;
using CareerSEA.Services.Interfaces;
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
        private readonly ICvExtractionService _cvExtractionService;

        public CvToInput(ICvExtractionService cvExtractionService)
        {
            _cvExtractionService = cvExtractionService;
        }

        [Authorize]
        [HttpPost("Parse")]
        [RequestSizeLimit(20 * 1024 * 1024)]
        [RequestTimeout(300000)]
        public async Task<ActionResult<BaseResponse>> Parse(IFormFile file, CancellationToken cancellationToken)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return Unauthorized();

            if (file == null || file.Length == 0)
                return BadRequest(new BaseResponse { Status = false, Message = "No file uploaded." });

            await using var stream = file.OpenReadStream();
            var response = await _cvExtractionService.ExtractAsync(
                stream,
                file.FileName ?? "cv.pdf",
                file.ContentType,
                cancellationToken);

            return Ok(response);
        }
    }
}
