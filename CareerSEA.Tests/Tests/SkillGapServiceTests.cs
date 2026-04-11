using CareerSEA.Services.Services;
using CareerSEA.Tests.Fakes;
using Xunit;
using Xunit.Abstractions;

namespace CareerSEA.Tests.Services;

public class SkillGapServiceTests
{
    private readonly ITestOutputHelper _output;

    public SkillGapServiceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(DisplayName = "Skill gap analysis throws an exception when occupation is not found")]
    public async Task GenerateSkillGapAsync_ShouldThrow_WhenOccupationNotFound()
    {
        _output.WriteLine("Arrange: Creating fake O*NET service with no occupation search results.");

        var fakeOnet = new FakeOnetService
        {
            SearchResults = new List<System.Text.Json.JsonElement>()
        };

        var service = new SkillGapService(fakeOnet);

        _output.WriteLine("Act + Assert: Generating skill gap for unknown occupation should throw.");

        await Assert.ThrowsAsync<Exception>(() =>
            service.GenerateSkillGapAsync("unknown job", new List<string> { "c#" }));
    }

    [Fact(DisplayName = "Skill gap analysis separates matched and missing skills correctly")]
    public async Task GenerateSkillGapAsync_ShouldReturnMatchedAndMissingSkills()
    {
        _output.WriteLine("Arrange: Creating fake O*NET service with target web developer technologies.");

        var fakeOnet = new FakeOnetService
        {
            SearchResults = FakeOnetService.ParseArray("""
            [
              { "code": "15-1254.00", "title": "Web Developers" }
            ]
            """),
            TechnologyResults = FakeOnetService.ParseArray("""
            [
              {
                "example": [
                  { "title": "JavaScript" },
                  { "title": "Vue.js" },
                  { "title": "HTML" }
                ],
                "example_more": [
                  { "title": "CSS" },
                  { "title": "React" }
                ]
              }
            ]
            """)
        };

        var service = new SkillGapService(fakeOnet);

        _output.WriteLine("Act: Generating skill gap for user with JavaScript, Vue.js, and HTML.");

        var result = await service.GenerateSkillGapAsync("web developer", new List<string>
        {
            "javascript",
            "vue.js",
            "html"
        });

        var gap = (Dictionary<string, object>)result["technology_gap"];
        var matched = (List<string>)gap["matched_skills"];
        var missing = (List<string>)gap["missing_skills"];

        _output.WriteLine($"Assert: Matched count = {matched.Count}, Missing count = {missing.Count}");

        Assert.Contains("JavaScript", matched);
        Assert.Contains("Vue.js", matched);
        Assert.Contains("HTML", matched);
        Assert.Contains("CSS", missing);
        Assert.Contains("React", missing);
    }

    [Fact(DisplayName = "Skill gap analysis splits comma-separated user skills correctly")]
    public async Task GenerateSkillGapAsync_ShouldSplitCommaSeparatedSkills()
    {
        _output.WriteLine("Arrange: Creating fake O*NET service with JavaScript, Vue.js, and HTML.");

        var fakeOnet = new FakeOnetService
        {
            SearchResults = FakeOnetService.ParseArray("""
            [
              { "code": "15-1254.00", "title": "Web Developers" }
            ]
            """),
            TechnologyResults = FakeOnetService.ParseArray("""
            [
              {
                "example": [
                  { "title": "JavaScript" },
                  { "title": "Vue.js" },
                  { "title": "HTML" }
                ]
              }
            ]
            """)
        };

        var service = new SkillGapService(fakeOnet);

        _output.WriteLine("Act: Generating skill gap with one comma-separated skills string.");

        var result = await service.GenerateSkillGapAsync("web developer", new List<string>
        {
            "javascript, vue.js, html"
        });

        var gap = (Dictionary<string, object>)result["technology_gap"];
        var matched = (List<string>)gap["matched_skills"];

        _output.WriteLine($"Assert: Matched skills count = {matched.Count}");

        Assert.Contains("JavaScript", matched);
        Assert.Contains("Vue.js", matched);
        Assert.Contains("HTML", matched);
    }

    [Fact(DisplayName = "Skill gap analysis does not match unrelated skills")]
    public async Task GenerateSkillGapAsync_ShouldNotMatchUnrelatedSkills()
    {
        _output.WriteLine("Arrange: Creating fake O*NET service with web technologies only.");

        var fakeOnet = new FakeOnetService
        {
            SearchResults = FakeOnetService.ParseArray("""
            [
              { "code": "15-1254.00", "title": "Web Developers" }
            ]
            """),
            TechnologyResults = FakeOnetService.ParseArray("""
            [
              {
                "example": [
                  { "title": "JavaScript" },
                  { "title": "Vue.js" },
                  { "title": "HTML" }
                ]
              }
            ]
            """)
        };

        var service = new SkillGapService(fakeOnet);

        _output.WriteLine("Act: Generating skill gap for unrelated user skill 'python'.");

        var result = await service.GenerateSkillGapAsync("web developer", new List<string>
        {
            "python"
        });

        var gap = (Dictionary<string, object>)result["technology_gap"];
        var matched = (List<string>)gap["matched_skills"];

        _output.WriteLine($"Assert: Matched skills count should remain 0 or exclude web skills. Actual count = {matched.Count}");

        Assert.DoesNotContain("JavaScript", matched);
        Assert.DoesNotContain("Vue.js", matched);
        Assert.DoesNotContain("HTML", matched);
    }

    [Fact(DisplayName = "Skill gap analysis reads the name property as well as the title property")]
    public async Task GenerateSkillGapAsync_ShouldReadNameProperty_AsWellAsTitle()
    {
        _output.WriteLine("Arrange: Creating fake O*NET service where technology uses 'name' instead of 'title'.");

        var fakeOnet = new FakeOnetService
        {
            SearchResults = FakeOnetService.ParseArray("""
            [
              { "code": "15-1254.00", "title": "Web Developers" }
            ]
            """),
            TechnologyResults = FakeOnetService.ParseArray("""
            [
              {
                "example": [
                  { "name": "Node.js" }
                ]
              }
            ]
            """)
        };

        var service = new SkillGapService(fakeOnet);

        _output.WriteLine("Act: Generating skill gap for user skill 'node.js'.");

        var result = await service.GenerateSkillGapAsync("web developer", new List<string>
        {
            "node.js"
        });

        var gap = (Dictionary<string, object>)result["technology_gap"];
        var matched = (List<string>)gap["matched_skills"];

        _output.WriteLine($"Assert: Node.js should be matched. Matched count = {matched.Count}");

        Assert.Contains("Node.js", matched);
    }

    [Fact(DisplayName = "Skill gap analysis handles empty user skill list")]
public async Task GenerateSkillGapAsync_ShouldHandleEmptySkillList()
{
    _output.WriteLine("Arrange: Creating fake O*NET service with web developer technologies.");

    var fakeOnet = new FakeOnetService
    {
        SearchResults = FakeOnetService.ParseArray("""
        [
          { "code": "15-1254.00", "title": "Web Developers" }
        ]
        """),
        TechnologyResults = FakeOnetService.ParseArray("""
        [
          {
            "example": [
              { "title": "JavaScript" },
              { "title": "Vue.js" },
              { "title": "HTML" }
            ]
          }
        ]
        """)
    };

    var service = new SkillGapService(fakeOnet);

    _output.WriteLine("Act: Generating skill gap with empty user skill list.");

    var result = await service.GenerateSkillGapAsync("web developer", new List<string>());

    var gap = (Dictionary<string, object>)result["technology_gap"];
    var matched = (List<string>)gap["matched_skills"];
    var missing = (List<string>)gap["missing_skills"];

    _output.WriteLine($"Assert: Matched = {matched.Count}, Missing = {missing.Count}");

    Assert.Empty(matched); // no skills → nothing matched
    Assert.Contains("JavaScript", missing);
    Assert.Contains("Vue.js", missing);
    Assert.Contains("HTML", missing);
}

}