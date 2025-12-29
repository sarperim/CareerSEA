using System.Net.Http.Headers;
using System.Net.Http.Json;
using CareerSEA.Contracts.DTOs; // Ensure this namespace matches where your DTO is

namespace CareerSEA.Web;

public class ServerApiClient
{
    private readonly HttpClient _client;
    private readonly TokenStore _tokenStore;

    public ServerApiClient(HttpClient client, TokenStore tokenStore)
    {
        _client = client;
        _tokenStore = tokenStore;
    }

    private async Task AddAuthHeader()
    {
        var token = await _tokenStore.GetAccessTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
        {
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    // --- GENERIC METHODS ---

    public async Task<HttpResponseMessage> PostAsync<T>(string url, T model)
    {
        await AddAuthHeader();
        return await _client.PostAsJsonAsync(url, model);
    }

    public async Task<T?> GetAsync<T>(string url)
    {
        await AddAuthHeader();
        return await _client.GetFromJsonAsync<T>(url);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest model)
    {
        await AddAuthHeader();
        var response = await _client.PostAsJsonAsync(url, model);

        try
        {
            return await response.Content.ReadFromJsonAsync<TResponse>();
        }
        catch
        {
            return default;
        }
    }

    // --- SPECIFIC METHODS (Business Logic) ---

    // This is the 2-argument method your UI is trying to call
    public async Task<List<JobListingDto>> SearchJobsAsync(string query, string country)
    {
        await AddAuthHeader();

        // Handle URL encoding safely
        var safeQuery = Uri.EscapeDataString(query);

        // Build the URL with query parameters
        // Example: api/JobPost/jobs?query=Developer&country=gb
        var url = $"api/JobPost/jobs?query={safeQuery}&country={country}";

        // Return empty list if null to prevent UI crashes
        return await _client.GetFromJsonAsync<List<JobListingDto>>(url) ?? new List<JobListingDto>();
    }
}