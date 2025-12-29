using CareerSEA.Contracts.DTOs;
using CareerSEA.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace CareerSEA.Services.Services
{
    public class JobPostService : IJobPostService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public JobPostService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<IEnumerable<JobListingDto>> SearchJobsAsync(string query, string country)
        {
            var appId = _configuration["Adzuna:AppId"];
            var appKey = _configuration["Adzuna:AppKey"];

            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(appKey))
                throw new InvalidOperationException("Adzuna configuration is missing.");

            // 1. Build Query Params
            var queryParams = new Dictionary<string, string>
    {
        { "app_id", appId },
        { "app_key", appKey },
        { "what", query },
        { "results_per_page", "5" }
    };

            var queryString = await new FormUrlEncodedContent(queryParams).ReadAsStringAsync();
            var url = $"https://api.adzuna.com/v1/api/jobs/{country}/search/1?{queryString}";

            // 2. Send Request
            var response = await _httpClient.GetAsync(url);

            // 3. NUCLEAR FIX: Read as Bytes to bypass invalid Charset Header
            var responseBytes = await response.Content.ReadAsByteArrayAsync();
            var jsonString = System.Text.Encoding.UTF8.GetString(responseBytes);

            if (!response.IsSuccessStatusCode)
            {
                // Now you can see the real error without crashing
                throw new HttpRequestException($"Adzuna API Error: {response.StatusCode} - {jsonString}");
            }

            // 4. Deserialize
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var adzunaData = System.Text.Json.JsonSerializer.Deserialize<AdzunaResponse>(jsonString, options);

            return adzunaData?.Results.Select(j => new JobListingDto
            {
                Title = j.Title,
                Company = j.Company?.Name ?? "Unknown",
                Location = j.Location?.Name ?? "Remote",
                Link = j.Url
            }) ?? Enumerable.Empty<JobListingDto>();
        }

        // Internal classes for JSON Deserialization (Keep these private or internal to avoid pollution)
        private class AdzunaResponse { public List<AdzunaJob> Results { get; set; } = new(); }
        private class AdzunaJob
        {
            public string Title { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("redirect_url")] public string Url { get; set; }
            public AdzunaCompany Company { get; set; }
            public AdzunaLocation Location { get; set; }
        }
        private class AdzunaCompany { [System.Text.Json.Serialization.JsonPropertyName("display_name")] public string Name { get; set; } }
        private class AdzunaLocation { [System.Text.Json.Serialization.JsonPropertyName("display_name")] public string Name { get; set; } }
    }
}
