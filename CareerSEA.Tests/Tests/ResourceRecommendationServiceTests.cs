using CareerSEA.Services.Services;
using CareerSEA.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace CareerSEA.Tests.Services;

public class ResourceRecommendationServiceTests
{
    private readonly ITestOutputHelper _output;

    public ResourceRecommendationServiceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory(DisplayName = "Provider detection identifies the correct learning platform from URL")]
    [InlineData("https://www.youtube.com/watch?v=1", "YouTube")]
    [InlineData("https://youtu.be/1", "YouTube")]
    [InlineData("https://www.udemy.com/course/test", "Udemy")]
    [InlineData("https://www.coursera.org/learn/test", "Coursera")]
    [InlineData("https://www.freecodecamp.org/learn/test", "freeCodeCamp")]
    [InlineData("https://example.com/test", "Unknown")]
    public void DetectProvider_ShouldReturnExpectedValue(string url, string expected)
    {
        _output.WriteLine($"Arrange/Act: Detecting provider for URL = {url}");

        var result = ResourceRecommendationService.DetectProvider(url);

        _output.WriteLine($"Assert: Expected provider = {expected}, Actual provider = {result}");

        Assert.Equal(expected, result);
    }

    [Fact(DisplayName = "Resource recommendation uses missing skills and filters duplicates and unknown providers")]
    public async Task GenerateResourceRecommendationsAsync_ShouldUseMissingSkills_AndFilterDuplicatesAndUnknown()
    {
        _output.WriteLine("Arrange: Mocking Brave search response with duplicate and unknown-provider resources.");

        var braveJson = """
        {
          "web": {
            "results": [
              {
                "url": "https://www.youtube.com/watch?v=1",
                "title": "Vue.js Tutorial",
                "description": "Learn Vue"
              },
              {
                "url": "https://www.youtube.com/watch?v=1",
                "title": "Duplicate Vue.js Tutorial",
                "description": "Duplicate"
              },
              {
                "url": "https://www.udemy.com/course/vue",
                "title": "Vue Course",
                "description": "Udemy course"
              },
              {
                "url": "https://example.com/random",
                "title": "Random",
                "description": "Unknown provider"
              }
            ]
          }
        }
        """;

        var http = StubHttpMessageHandler.CreateClient(_ => StubHttpMessageHandler.Json(braveJson), "https://api.search.brave.com/");
        var service = new ResourceRecommendationService(http);

        _output.WriteLine("Act: Generating recommendations for missing skill 'vue.js'.");

        var result = await service.GenerateResourceRecommendationsAsync(
            "web developer",
            new List<string> { "vue.js" },
            new List<string> { "html" },
            maxSkills: 5,
            perSkill: 4);

        var resources = (List<Dictionary<string, object>>)result[0]["resources"];

        _output.WriteLine($"Assert: Expected 2 filtered resources. Actual count = {resources.Count}");

        Assert.Single(result);
        Assert.Equal("vue.js", result[0]["skill"]);
        Assert.Equal("missing_skill", result[0]["source"]);
        Assert.Equal(2, resources.Count);
        Assert.Contains(resources, r => r["provider"].ToString() == "YouTube");
        Assert.Contains(resources, r => r["provider"].ToString() == "Udemy");
    }

    [Fact(DisplayName = "Resource recommendation falls back to user skills when missing skills are empty")]
    public async Task GenerateResourceRecommendationsAsync_ShouldFallbackToUserSkills_WhenMissingSkillsEmpty()
    {
        _output.WriteLine("Arrange: Mocking Brave search response for fallback to user skills.");

        var braveJson = """
        {
          "web": {
            "results": [
              {
                "url": "https://www.youtube.com/watch?v=1",
                "title": "HTML Tutorial",
                "description": "Learn HTML"
              }
            ]
          }
        }
        """;

        var http = StubHttpMessageHandler.CreateClient(_ => StubHttpMessageHandler.Json(braveJson), "https://api.search.brave.com/");
        var service = new ResourceRecommendationService(http);

        _output.WriteLine("Act: Generating recommendations with no missing skills but with user skill 'html'.");

        var result = await service.GenerateResourceRecommendationsAsync(
            "web developer",
            new List<string>(),
            new List<string> { "html" });

        _output.WriteLine($"Assert: Source should be 'user_skill'. Actual source = {result[0]["source"]}");

        Assert.Single(result);
        Assert.Equal("user_skill", result[0]["source"]);
        Assert.Equal("html", result[0]["skill"]);
    }

    [Fact(DisplayName = "Resource recommendation falls back to job title when no skills exist")]
    public async Task GenerateResourceRecommendationsAsync_ShouldFallbackToJobTitle_WhenNoSkillsExist()
    {
        _output.WriteLine("Arrange: Mocking Brave search response for fallback to job title.");

        var braveJson = """
        {
          "web": {
            "results": [
              {
                "url": "https://www.coursera.org/learn/webdev",
                "title": "Web Dev Course",
                "description": "Course"
              }
            ]
          }
        }
        """;

        var http = StubHttpMessageHandler.CreateClient(_ => StubHttpMessageHandler.Json(braveJson), "https://api.search.brave.com/");
        var service = new ResourceRecommendationService(http);

        _output.WriteLine("Act: Generating recommendations with no missing skills and no user skills.");

        var result = await service.GenerateResourceRecommendationsAsync(
            "web developer",
            new List<string>(),
            new List<string>());

        _output.WriteLine($"Assert: Source should be 'job_title'. Actual source = {result[0]["source"]}");

        Assert.Single(result);
        Assert.Equal("job_title", result[0]["source"]);
        Assert.Equal("web developer", result[0]["skill"]);
    }

    [Fact(DisplayName = "Resource recommendation returns a search failed placeholder when HTTP call throws")]
    public async Task GenerateResourceRecommendationsAsync_ShouldReturnSearchFailed_WhenHttpThrows()
    {
        _output.WriteLine("Arrange: Creating HTTP client that throws an exception.");

        var http = StubHttpMessageHandler.CreateClient(_ => throw new HttpRequestException("boom"), "https://api.search.brave.com/");
        var service = new ResourceRecommendationService(http);

        _output.WriteLine("Act: Generating recommendations while HTTP request fails.");

        var result = await service.GenerateResourceRecommendationsAsync(
            "web developer",
            new List<string> { "react" },
            new List<string>());

        var resources = (List<Dictionary<string, object>>)result[0]["resources"];

        _output.WriteLine("Assert: Should return a single fallback resource indicating search failure.");

        Assert.Single(resources);
        Assert.Equal("Search failed", resources[0]["title"]);
        Assert.Equal("System", resources[0]["provider"]);
        Assert.Equal(0.0, resources[0]["score"]);
    }
}