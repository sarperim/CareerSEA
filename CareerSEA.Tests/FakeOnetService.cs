using System.Text.Json;
using CareerSEA.Services.Interfaces;

namespace CareerSEA.Tests.Fakes;

public sealed class FakeOnetService : IOnetService
{
    public List<JsonElement> SearchResults { get; set; } = new();
    public List<JsonElement> TechnologyResults { get; set; } = new();

    public Task<List<JsonElement>> SearchOccupationsAsync(string keyword, int end = 10)
        => Task.FromResult(SearchResults);

    public Task<List<JsonElement>> GetOccupationTechnologyAsync(string onetCode, int start = 1, int end = 10)
        => Task.FromResult(TechnologyResults);

    public string ExtractOnetCode(JsonElement item)
        => item.TryGetProperty("code", out var code) ? code.GetString() ?? "" : "";

    public static List<JsonElement> ParseArray(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.EnumerateArray().Select(x => x.Clone()).ToList();
    }
}