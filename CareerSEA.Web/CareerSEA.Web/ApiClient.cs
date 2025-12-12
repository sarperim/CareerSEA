using System.Net.Http.Headers;
using System.Net.Http.Json;

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

    // --- EXISTING METHODS (Used by Experience.razor) ---

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

    // --- NEW METHOD (Used by Login.razor) ---

    // This overload takes two types: Request and Response. 
    // It automatically reads the JSON from the result.
    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest model)
    {
        await AddAuthHeader();
        var response = await _client.PostAsJsonAsync(url, model);

        // We attempt to read the response even if it failed (e.g. 400 Bad Request),
        // because your API likely returns a standardized error message in the JSON body.
        try
        {
            return await response.Content.ReadFromJsonAsync<TResponse>();
        }
        catch
        {
            // If the server returns purely text or HTML (500 error), return default/null
            return default;
        }
    }
}