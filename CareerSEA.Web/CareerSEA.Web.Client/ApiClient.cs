using System.Net.Http.Json;
using System.Text.Json;

namespace CareerSEA.Web;

public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    // GET
    public async Task<T?> GetAsync<T>(string url)
    {
        return await _http.GetFromJsonAsync<T>(url, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    // POST (returns response body)
    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest body)
    {
        var response = await _http.PostAsJsonAsync(url, body);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"POST {url} failed ({response.StatusCode}).\nResponse:\n{content}"
            );
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    // POST (no return, just success/failure)
    public async Task PostAsync<TRequest>(string url, TRequest body)
    {
        var response = await _http.PostAsJsonAsync(url, body);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"POST {url} failed ({response.StatusCode}).\nResponse:\n{content}"
            );
        }
    }

    // PUT
    public async Task PutAsync<TRequest>(string url, TRequest body)
    {
        var response = await _http.PutAsJsonAsync(url, body);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"PUT {url} failed ({response.StatusCode}).\nResponse:\n{content}"
            );
        }
    }

    // DELETE
    public async Task DeleteAsync(string url)
    {
        var response = await _http.DeleteAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"DELETE {url} failed ({response.StatusCode}).\nResponse:\n{content}"
            );
        }
    }
}

