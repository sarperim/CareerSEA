using CareerSEA.Contracts.Responses;
using CareerSEA.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CareerSEA.Services.Services
{
    public class CvExtractionService : ICvExtractionService
    {
        private readonly HttpClient _httpClient;

        public CvExtractionService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<BaseResponse> ExtractAsync(
            Stream pdfStream,
            string fileName,
            string? contentType,
            CancellationToken cancellationToken)
        {
            var isPdf = string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase)
                        || (fileName ?? "").EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
            if (!isPdf)
            {
                return new BaseResponse
                {
                    Status = false,
                    Message = "Only PDF uploads are supported."
                };
            }

            ExtractedCvResponse? extracted;
            try
            {
                using var form = new MultipartFormDataContent();
                using var fileContent = new StreamContent(pdfStream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                form.Add(fileContent, "file", fileName ?? "cv.pdf");

                using var pyResponse = await _httpClient.PostAsync("/extract-cv", form, cancellationToken);
                if (!pyResponse.IsSuccessStatusCode)
                {
                    var detail = await pyResponse.Content.ReadAsStringAsync(cancellationToken);
                    return new BaseResponse
                    {
                        Status = false,
                        Message = $"CV extraction failed: {detail}"
                    };
                }

                extracted = await pyResponse.Content.ReadFromJsonAsync<ExtractedCvResponse>(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                return new BaseResponse
                {
                    Status = false,
                    Message = $"Extraction service error: {ex.Message}"
                };
            }

            if (extracted?.Experiences == null || extracted.Experiences.Count == 0)
            {
                return new BaseResponse
                {
                    Status = false,
                    Message = "No work experiences extracted from CV."
                };
            }

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
            {
                return new BaseResponse
                {
                    Status = false,
                    Message = "No complete work experiences extracted from CV."
                };
            }

            var response = new ExtractedCvResponse
            {
                Experiences = reviewExperiences
            };

            return new BaseResponse
            {
                Status = true,
                Message = "CV extracted successfully. Review the experiences before creating a prediction.",
                Data = response
            };
        }
    }
}
