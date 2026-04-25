using CareerSEA.Contracts.Requests;
using CareerSEA.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

namespace CareerSEA.ApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CvToInput : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CvToInput(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
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

            var isPdf = string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
                        || (file.FileName ?? "").EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
            if (!isPdf)
                return StatusCode(StatusCodes.Status415UnsupportedMediaType,
                    new BaseResponse { Status = false, Message = "Only PDF uploads are supported." });

            ExtractedCvResponse? extracted;
            try
            {
                using var client = _httpClientFactory.CreateClient("aiservice");
                using var form = new MultipartFormDataContent();
                using var stream = file.OpenReadStream();
                using var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                form.Add(fileContent, "file", file.FileName ?? "cv.pdf");

                using var pyResponse = await client.PostAsync("/extract-cv", form, cancellationToken);
                if (!pyResponse.IsSuccessStatusCode)
                {
                    var detail = await pyResponse.Content.ReadAsStringAsync(cancellationToken);
                    return StatusCode((int)pyResponse.StatusCode,
                        new BaseResponse { Status = false, Message = $"CV extraction failed: {detail}" });
                }

                extracted = await pyResponse.Content.ReadFromJsonAsync<ExtractedCvResponse>(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway,
                    new BaseResponse { Status = false, Message = $"Extraction service error: {ex.Message}" });
            }

            if (extracted?.Experiences == null || extracted.Experiences.Count == 0)
                return Ok(new BaseResponse { Status = false, Message = "No work experiences extracted from CV." });

            var reviewExperiences = extracted.Experiences
                .Select(experience => new ExtractedExperienceDto
                {
                    Title = (experience.Title ?? string.Empty).Trim(),
                    Description = (experience.Description ?? string.Empty).Trim(),
                    Skills = (experience.Skills ?? new List<string>())
                        .Select(skill => (skill ?? string.Empty).Trim())
                        .Where(skill => !string.IsNullOrWhiteSpace(skill))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList()
                })
                .Where(experience =>
                    !string.IsNullOrWhiteSpace(experience.Title)
                    || !string.IsNullOrWhiteSpace(experience.Description)
                    || experience.Skills.Count > 0)
                .ToList();

            if (reviewExperiences.Count == 0)
                return Ok(new BaseResponse { Status = false, Message = "No complete work experiences extracted from CV." });

            var response = new ExtractedCvResponse
            {
                Experiences = reviewExperiences
            };

            return Ok(new BaseResponse
            {
                Status = true,
                Message = "CV extracted successfully. Review the experiences before creating a prediction.",
                Data = response
            });
        }
    }
}
