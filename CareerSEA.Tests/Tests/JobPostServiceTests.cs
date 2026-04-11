using CareerSEA.Services.Services;
using CareerSEA.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace CareerSEA.Tests.Services;

public class JobPostServiceTests
{
    private readonly ITestOutputHelper _output;

    public JobPostServiceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values!)
            .Build();

    [Fact(DisplayName = "Job search throws an error when Adzuna configuration is missing")]
    public async Task SearchJobsAsync_ShouldThrow_WhenConfigIsMissing()
    {
        _output.WriteLine("Arrange: Creating configuration without Adzuna AppId and AppKey.");

        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Adzuna:AppId"] = null,
            ["Adzuna:AppKey"] = null
        });

        var http = StubHttpMessageHandler.CreateClient(_ => StubHttpMessageHandler.Json("{}"));
        var service = new JobPostService(http, config);

        _output.WriteLine("Act + Assert: SearchJobsAsync should throw InvalidOperationException.");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SearchJobsAsync("developer", "gb"));
    }

    [Fact(DisplayName = "Job search maps Adzuna API results into job post objects")]
    public async Task SearchJobsAsync_ShouldMapResults_WhenApiSucceeds()
    {
        _output.WriteLine("Arrange: Creating valid configuration and fake Adzuna response with two jobs.");

        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Adzuna:AppId"] = "id",
            ["Adzuna:AppKey"] = "key"
        });

        var json = """
        {
          "results": [
            {
              "title": "Backend Developer",
              "redirect_url": "https://adzuna/job1",
              "company": { "display_name": "Acme" },
              "location": { "display_name": "London" }
            },
            {
              "title": "Frontend Developer",
              "redirect_url": "https://adzuna/job2",
              "company": null,
              "location": null
            }
          ]
        }
        """;

        var http = StubHttpMessageHandler.CreateClient(_ => StubHttpMessageHandler.Json(json));
        var service = new JobPostService(http, config);

        _output.WriteLine("Act: Searching jobs for 'developer' in 'gb'.");

        var results = (await service.SearchJobsAsync("developer", "gb")).ToList();

        _output.WriteLine($"Assert: Expected 2 mapped results. Actual count = {results.Count}");

        Assert.Equal(2, results.Count);
        Assert.Equal("Backend Developer", results[0].Title);
        Assert.Equal("Acme", results[0].Company);
        Assert.Equal("London", results[0].Location);
        Assert.Equal("https://adzuna/job1", results[0].Link);

        Assert.Equal("Unknown", results[1].Company);
        Assert.Equal("Remote", results[1].Location);
    }

    [Fact(DisplayName = "Job search throws HttpRequestException when Adzuna API returns an error")]
    public async Task SearchJobsAsync_ShouldThrowHttpRequestException_WhenApiFails()
    {
        _output.WriteLine("Arrange: Creating valid configuration and fake failed API response.");

        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Adzuna:AppId"] = "id",
            ["Adzuna:AppKey"] = "key"
        });

        var http = StubHttpMessageHandler.CreateClient(_ =>
            StubHttpMessageHandler.Text("{\"error\":\"bad request\"}", System.Net.HttpStatusCode.BadRequest));

        var service = new JobPostService(http, config);

        _output.WriteLine("Act + Assert: SearchJobsAsync should throw HttpRequestException.");

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.SearchJobsAsync("developer", "gb"));

        _output.WriteLine($"Actual exception message = {ex.Message}");

        Assert.Contains("Adzuna API Error", ex.Message);
    }
}