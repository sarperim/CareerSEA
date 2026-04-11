using CareerSEA.Contracts.Requests;
using CareerSEA.Data.Entities;
using CareerSEA.Services.Services;
using CareerSEA.Tests.Helpers;
using System.Net;
using System.Net.Http;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace CareerSEA.Tests.Services;

public class ExperiencePredictionServiceTests
{
    private readonly ITestOutputHelper _output;

    public ExperiencePredictionServiceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(DisplayName = "SaveForm returns success even when the prediction API throws an exception")]
    public async Task SaveForm_ShouldReturnSuccess_WhenHttpThrows()
    {
        _output.WriteLine("Arrange: Creating user and HTTP client that throws an exception.");

        using var db = TestDbFactory.Create();

        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            UserName = "alpha",
            Name = "A",
            LastName = "B",
            PasswordHash = "dummyhash"
        });
        await db.SaveChangesAsync();

        var handler = new FakeHttpMessageHandler(_ =>
        {
            throw new HttpRequestException("boom");
        });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        var service = new ExperiencePredictionService(db, httpClient);

        var request = new ExperienceRequest
        {
            Title = "Intern",
            Description = "Worked on backend",
            Skills = "C#, SQL"
        };

        _output.WriteLine("Act: Calling SaveForm while prediction API fails.");

        var result = await service.SaveForm(request, userId);

        _output.WriteLine("Assert: Method should still return success and save nothing to database.");
        _output.WriteLine($"Predictions count = {db.Predictions.Count()}, Experiences count = {db.Experiences.Count()}");

        Assert.True(result.Status);
        Assert.Equal("Success", result.Message);
        Assert.Empty(db.Experiences.Where(x => x.UserId == userId));
        Assert.Empty(db.Predictions.Where(x => x.UserId == userId));
    }

    [Fact(DisplayName = "SaveForm stores experience and prediction when prediction API succeeds")]
    public async Task SaveForm_ShouldSavePrediction_WhenHttpReturnsSuccess()
    {
        _output.WriteLine("Arrange: Creating user and fake successful prediction API response.");

        using var db = TestDbFactory.Create();

        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            UserName = "alpha",
            Name = "A",
            LastName = "B",
            PasswordHash = "dummyhash"
        });
        await db.SaveChangesAsync();

        var json = """
        {
          "best_job": "Backend Developer",
          "match_score": 0.91,
          "recommendations": [
            { "label": "Software Developer", "score": 0.91 },
            { "label": "Backend Developer", "score": 0.89 }
          ]
        }
        """;

        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        var service = new ExperiencePredictionService(db, httpClient);

        var request = new ExperienceRequest
        {
            Title = "Intern",
            Description = "Worked on backend",
            Skills = "C#, SQL"
        };

        _output.WriteLine("Act: Calling SaveForm with valid experience data.");

        var result = await service.SaveForm(request, userId);

        _output.WriteLine("Assert: Method should save both experience and prediction.");
        _output.WriteLine($"Predictions count = {db.Predictions.Count()}, Experiences count = {db.Experiences.Count()}");

        Assert.True(result.Status);
        Assert.Equal("Success", result.Message);
        Assert.Single(db.Predictions.Where(x => x.UserId == userId));
        Assert.Single(db.Experiences.Where(x => x.UserId == userId));
    }

[Fact(DisplayName = "SaveForm stores the prediction match score correctly when prediction API returns valid response")]
public async Task SaveForm_ShouldStorePredictionAccuracy_WhenHttpReturnsValidSuccess()
{
    _output.WriteLine("Arrange: Creating user and fake successful prediction API response with known accuracy values.");

    using var db = TestDbFactory.Create();

    var userId = Guid.NewGuid();
    db.Users.Add(new User
    {
        Id = userId,
        UserName = "alpha",
        Name = "A",
        LastName = "B",
        PasswordHash = "dummyhash"
    });
    await db.SaveChangesAsync();

    var json = """
    {
      "best_job": "Backend Developer",
      "match_score": 0.60,
      "recommendations": [
        { "label": "Backend Developer", "score": 0.60 },
        { "label": "Software Developer", "score": 0.34 }
      ]
    }
    """;

    var handler = new FakeHttpMessageHandler(_ =>
        new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

    var httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri("http://localhost")
    };

    var service = new ExperiencePredictionService(db, httpClient);

    var request = new ExperienceRequest
    {
        Title = "Intern",
        Description = "Worked on backend",
        Skills = "C#, SQL"
    };

    _output.WriteLine("Act: Calling SaveForm with valid prediction response containing 60% accuracy.");

    var result = await service.SaveForm(request, userId);

    _output.WriteLine("Assert: Method should save prediction and preserve the returned match score.");

    var savedPrediction = db.Predictions.SingleOrDefault(x => x.UserId == userId);

    Assert.True(result.Status);
    Assert.Equal("Success", result.Message);
    Assert.NotNull(savedPrediction);
    Assert.NotNull(savedPrediction!.Result);

    Assert.Equal("Backend Developer", savedPrediction.Result.BestJob);
    Assert.Equal(0.60, savedPrediction.Result.MatchScore,2);
}


[Fact(DisplayName = "SaveForm handles invalid prediction API response")]
public async Task SaveForm_ShouldHandleInvalidJson_WhenPredictionApiReturnsMalformedResponse()
{
    _output.WriteLine("Arrange: Creating user and fake prediction API response with invalid JSON.");

    using var db = TestDbFactory.Create();

    var userId = Guid.NewGuid();
    db.Users.Add(new User
    {
        Id = userId,
        UserName = "alpha",
        Name = "A",
        LastName = "B",
        PasswordHash = "dummyhash"
    });
    await db.SaveChangesAsync();

    var invalidJson = """
    { "best_job": "Backend Developer", "match_score":
    """;

    var handler = new FakeHttpMessageHandler(_ =>
        new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(invalidJson, Encoding.UTF8, "application/json")
        });

    var httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri("http://localhost")
    };

    var service = new ExperiencePredictionService(db, httpClient);

    var request = new ExperienceRequest
    {
        Title = "Intern",
        Description = "Worked on backend",
        Skills = "C#, SQL"
    };

    _output.WriteLine("Act: Calling SaveForm with malformed JSON response.");

    var result = await service.SaveForm(request, userId);

    _output.WriteLine("Assert: Method should handle invalid JSON safely and save nothing.");
    _output.WriteLine($"Predictions count = {db.Predictions.Count()}, Experiences count = {db.Experiences.Count()}");

    Assert.True(result.Status);
    Assert.Equal("Success", result.Message);
    Assert.Empty(db.Experiences.Where(x => x.UserId == userId));
    Assert.Empty(db.Predictions.Where(x => x.UserId == userId));
}

[Fact(DisplayName = "SaveForm handles empty recommendation list")]
public async Task SaveForm_ShouldSavePrediction_WhenRecommendationsAreEmpty()
{
    _output.WriteLine("Arrange: Creating user and fake prediction API response with empty recommendations.");

    using var db = TestDbFactory.Create();

    var userId = Guid.NewGuid();
    db.Users.Add(new User
    {
        Id = userId,
        UserName = "alpha",
        Name = "A",
        LastName = "B",
        PasswordHash = "dummyhash"
    });
    await db.SaveChangesAsync();

    var json = """
    {
      "best_job": "Backend Developer",
      "match_score": 0.60,
      "recommendations": []
    }
    """;

    var handler = new FakeHttpMessageHandler(_ =>
        new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

    var httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri("http://localhost")
    };

    var service = new ExperiencePredictionService(db, httpClient);

    var request = new ExperienceRequest
    {
        Title = "Intern",
        Description = "Worked on backend",
        Skills = "C#, SQL"
    };

    _output.WriteLine("Act: Calling SaveForm with empty recommendations list.");

    var result = await service.SaveForm(request, userId);

    _output.WriteLine("Assert: Method should still save prediction and experience.");

    var savedPrediction = db.Predictions.SingleOrDefault(x => x.UserId == userId);

    Assert.True(result.Status);
    Assert.Equal("Success", result.Message);
    Assert.Single(db.Experiences.Where(x => x.UserId == userId));
    Assert.Single(db.Predictions.Where(x => x.UserId == userId));
    Assert.NotNull(savedPrediction);
    Assert.NotNull(savedPrediction!.Result);
    Assert.Equal("Backend Developer", savedPrediction.Result.BestJob);
    Assert.Equal(0.60, savedPrediction.Result.MatchScore, 2);
    Assert.NotNull(savedPrediction.Result.Recommendations);
    Assert.Empty(savedPrediction.Result.Recommendations);
}

}

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_handler(request));
    }

}
