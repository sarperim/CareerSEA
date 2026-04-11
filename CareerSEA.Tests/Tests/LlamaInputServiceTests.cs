using CareerSEA.Contracts.Requests;
using CareerSEA.Services.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace CareerSEA.Tests.Services;

public class LlamaInputServiceTests
{
    private readonly ITestOutputHelper _output;

    public LlamaInputServiceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(DisplayName = "Llama input parser reads plain JSON correctly")]
    public async Task ExtractCareerDataAsync_ShouldParsePlainJson()
    {
        _output.WriteLine("Arrange: Mocking chat client to return plain JSON.");

        var raw = """
        {
          "jobs": [
            {
              "title": "Backend Developer - Acme",
              "description": "Built APIs",
              "skills": "c#, sql, asp.net"
            }
          ]
        }
        """;

        var chatClient = new Mock<IChatClient>();

        chatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, raw)));

        var service = new LlamaInputService(chatClient.Object, NullLogger<LlamaInputService>.Instance);

        _output.WriteLine("Act: Extracting career data from CV text.");

        var result = await service.ExtractCareerDataAsync("my cv");

        _output.WriteLine($"Assert: Parsed job count should be 1. Actual count = {result?.jobs?.Count}");

        Assert.NotNull(result);
        Assert.Single(result!.jobs);
        Assert.Equal("Backend Developer - Acme", result.jobs[0].title);
    }

    [Fact(DisplayName = "Llama input parser reads markdown-wrapped JSON correctly")]
    public async Task ExtractCareerDataAsync_ShouldParseMarkdownWrappedJson()
    {
        _output.WriteLine("Arrange: Mocking chat client to return JSON inside markdown code fences.");

        var raw = """
        ```json
        {
          "jobs": [
            {
              "title": "Frontend Developer - Acme",
              "description": "Built UI",
              "skills": "html, css, javascript"
            }
          ]
        }
        ```
        """;

        var chatClient = new Mock<IChatClient>();

        chatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, raw)));

        var service = new LlamaInputService(chatClient.Object, NullLogger<LlamaInputService>.Instance);

        _output.WriteLine("Act: Extracting career data from markdown-wrapped JSON.");

        var result = await service.ExtractCareerDataAsync("my cv");

        _output.WriteLine($"Assert: Parsed title should be 'Frontend Developer - Acme'. Actual title = {result?.jobs?[0].title}");

        Assert.NotNull(result);
        Assert.Single(result!.jobs);
        Assert.Equal("Frontend Developer - Acme", result.jobs[0].title);
    }

    [Fact(DisplayName = "Llama input parser trims extra surrounding text and still parses JSON")]
    public async Task ExtractCareerDataAsync_ShouldTrimExtraTextAroundJson()
    {
        _output.WriteLine("Arrange: Mocking chat client to return extra text before and after JSON.");

        var raw = """
        Here is the extracted data:
        {
          "jobs": [
            {
              "title": "Data Analyst - Acme",
              "description": "Reporting and dashboards",
              "skills": "excel, sql, power bi"
            }
          ]
        }
        Hope this helps.
        """;

        var chatClient = new Mock<IChatClient>();

        chatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, raw)));

        var service = new LlamaInputService(chatClient.Object, NullLogger<LlamaInputService>.Instance);

        _output.WriteLine("Act: Extracting career data from response with surrounding text.");

        var result = await service.ExtractCareerDataAsync("my cv");

        _output.WriteLine($"Assert: Parsed title should be 'Data Analyst - Acme'. Actual title = {result?.jobs?[0].title}");

        Assert.NotNull(result);
        Assert.Single(result!.jobs);
        Assert.Equal("Data Analyst - Acme", result.jobs[0].title);
    }

    [Fact(DisplayName = "Llama input parser returns null when JSON is invalid")]
    public async Task ExtractCareerDataAsync_ShouldReturnNull_WhenJsonIsInvalid()
    {
        _output.WriteLine("Arrange: Mocking chat client to return invalid JSON.");

        var raw = "not valid json";

        var chatClient = new Mock<IChatClient>();

        chatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, raw)));

        var service = new LlamaInputService(chatClient.Object, NullLogger<LlamaInputService>.Instance);

        _output.WriteLine("Act: Extracting career data from invalid JSON.");

        var result = await service.ExtractCareerDataAsync("my cv");

        _output.WriteLine("Assert: Result should be null.");

        Assert.Null(result);
    }

    [Fact(DisplayName = "Llama input parser returns null when chat client throws an exception")]
    public async Task ExtractCareerDataAsync_ShouldReturnNull_WhenChatClientThrows()
    {
        _output.WriteLine("Arrange: Mocking chat client to throw an exception.");

        var chatClient = new Mock<IChatClient>();

        chatClient
            .Setup(x => x.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("boom"));

        var service = new LlamaInputService(chatClient.Object, NullLogger<LlamaInputService>.Instance);

        _output.WriteLine("Act: Extracting career data while chat client fails.");

        var result = await service.ExtractCareerDataAsync("my cv");

        _output.WriteLine("Assert: Result should be null.");

        Assert.Null(result);
    }
}