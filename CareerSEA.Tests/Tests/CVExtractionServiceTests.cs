using CareerSEA.Contracts.Responses;
using CareerSEA.Services.Services;
using System.Net;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace CareerSEA.Tests.Services;

public class CvExtractionServiceTests
{
    private readonly ITestOutputHelper _output;

    public CvExtractionServiceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(DisplayName = "CV extraction succeeds when Python service returns valid extracted experiences")]
    public async Task ExtractAsync_ShouldReturnSuccess_WhenPythonServiceReturnsValidCvData()
    {
        var json = """
        {
          "experiences": [
            {
              "title": "Backend Developer",
              "description": "Built APIs using .NET",
              "skills": ["C#", "SQL", "ASP.NET", "SQL"]
            }
          ]
        }
        """;

        var httpClient = new HttpClient(new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            json))
        {
            BaseAddress = new Uri("http://localhost")
        };

        var service = new CvExtractionService(httpClient);

        using var pdfStream = new MemoryStream(Encoding.UTF8.GetBytes("fake pdf content"));

        var result = await service.ExtractAsync(
            pdfStream,
            "cv.pdf",
            "application/pdf",
            CancellationToken.None);

        Assert.True(result.Status);
        Assert.Equal("CV extracted successfully. Review the experiences before creating a prediction.", result.Message);

        var data = Assert.IsType<ExtractedCvResponse>(result.Data);
        Assert.Single(data.Experiences);

        var experience = data.Experiences[0];

        Assert.Equal("Backend Developer", experience.Title);
        Assert.Equal("Built APIs using .NET", experience.Description);

        Assert.Equal(3, experience.Skills.Count);
        Assert.Contains("C#", experience.Skills);
        Assert.Contains("SQL", experience.Skills);
        Assert.Contains("ASP.NET", experience.Skills);

        _output.WriteLine("Passed: CV extraction succeeds when Python service returns valid extracted experiences");
        _output.WriteLine("Arrange: Mocked Python CV extraction service returns Backend Developer experience.");
        _output.WriteLine("Act: Extracted experience data from a mocked PDF upload.");
        _output.WriteLine($"Assert: Extracted title = {experience.Title}");
        _output.WriteLine($"Assert: Extracted description = {experience.Description}");
        _output.WriteLine($"Assert: Extracted skills = {string.Join(", ", experience.Skills)}");
    }

    [Fact(DisplayName = "CV extraction fails when Python extraction service is not reachable")]
    public async Task ExtractAsync_ShouldReturnFailure_WhenPythonServiceIsNotReachable()
    {
        var httpClient = new HttpClient(new ThrowingHttpMessageHandler())
        {
            BaseAddress = new Uri("http://localhost")
        };

        var service = new CvExtractionService(httpClient);

        using var pdfStream = new MemoryStream(Encoding.UTF8.GetBytes("fake pdf content"));

        var result = await service.ExtractAsync(
            pdfStream,
            "cv.pdf",
            "application/pdf",
            CancellationToken.None);

        Assert.False(result.Status);
        Assert.StartsWith("Extraction service error:", result.Message);

        _output.WriteLine("Passed: CV extraction fails when Python extraction service is not reachable");
        _output.WriteLine("Arrange: Mocked Python CV extraction service throws an HTTP exception.");
        _output.WriteLine("Act: Tried to extract CV data from a mocked PDF upload.");
        _output.WriteLine($"Assert: Failure message returned = {result.Message}");
    }

    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseBody;

        public FakeHttpMessageHandler(HttpStatusCode statusCode, string responseBody)
        {
            _statusCode = statusCode;
            _responseBody = responseBody;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }

    private class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Python CV extraction service is not reachable.");
        }
    }
}