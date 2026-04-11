using CareerSEA.Services.Services;
using CareerSEA.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace CareerSEA.Tests.Services;

public class OnetServiceTests
{
    private readonly ITestOutputHelper _output;

    public OnetServiceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(DisplayName = "O*NET occupation search parses the returned career array")]
    public async Task SearchOccupationsAsync_ShouldParseCareerArray()
    {
        _output.WriteLine("Arrange: Mocking O*NET occupation search response with two careers.");

        var json = """
        {
          "career": [
            { "code": "15-1254.00", "title": "Web Developers" },
            { "code": "15-1255.00", "title": "Web and Digital Interface Designers" }
          ]
        }
        """;

        var http = StubHttpMessageHandler.CreateClient(_ => StubHttpMessageHandler.Json(json), "https://services.onetcenter.org/");
        var service = new OnetService(http);

        _output.WriteLine("Act: Searching occupations for 'web developer'.");

        var results = await service.SearchOccupationsAsync("web developer", 3);

        _output.WriteLine($"Assert: Expected 2 results. First code = {service.ExtractOnetCode(results[0])}");

        Assert.Equal(2, results.Count);
        Assert.Equal("15-1254.00", service.ExtractOnetCode(results[0]));
    }

    [Fact(DisplayName = "O*NET technology lookup parses nested technology_skills category")]
    public async Task GetOccupationTechnologyAsync_ShouldParseNestedTechnologySkillsCategory()
    {
        _output.WriteLine("Arrange: Mocking nested technology_skills.category response.");

        var json = """
        {
          "technology_skills": {
            "category": [
              {
                "example": [{ "title": "JavaScript" }],
                "example_more": [{ "title": "Vue.js" }]
              }
            ]
          }
        }
        """;

        var http = StubHttpMessageHandler.CreateClient(_ => StubHttpMessageHandler.Json(json), "https://services.onetcenter.org/");
        var service = new OnetService(http);

        _output.WriteLine("Act: Fetching technology skills for occupation code 15-1254.00.");

        var results = await service.GetOccupationTechnologyAsync("15-1254.00");

        _output.WriteLine($"Assert: Expected 1 category block. Actual count = {results.Count}");

        Assert.Single(results);
    }

    [Fact(DisplayName = "O*NET technology lookup falls back to direct category when needed")]
    public async Task GetOccupationTechnologyAsync_ShouldParseDirectCategoryFallback()
    {
        _output.WriteLine("Arrange: Mocking direct category response without technology_skills wrapper.");

        var json = """
        {
          "category": [
            {
              "example": [{ "title": "Python" }]
            }
          ]
        }
        """;

        var http = StubHttpMessageHandler.CreateClient(_ => StubHttpMessageHandler.Json(json), "https://services.onetcenter.org/");
        var service = new OnetService(http);

        _output.WriteLine("Act: Fetching technology skills for occupation code 15-2051.00.");

        var results = await service.GetOccupationTechnologyAsync("15-2051.00");

        _output.WriteLine($"Assert: Expected 1 category block. Actual count = {results.Count}");

        Assert.Single(results);
    }

    [Fact(DisplayName = "O*NET technology lookup returns empty when category is missing")]
    public async Task GetOccupationTechnologyAsync_ShouldReturnEmpty_WhenCategoryMissing()
    {
        _output.WriteLine("Arrange: Mocking response without category data.");

        var json = """{ "foo": "bar" }""";

        var http = StubHttpMessageHandler.CreateClient(_ => StubHttpMessageHandler.Json(json), "https://services.onetcenter.org/");
        var service = new OnetService(http);

        _output.WriteLine("Act: Fetching technology skills for occupation code 15-2051.00.");

        var results = await service.GetOccupationTechnologyAsync("15-2051.00");

        _output.WriteLine($"Assert: Expected empty result. Actual count = {results.Count}");

        Assert.Empty(results);
    }
}