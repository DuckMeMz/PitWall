using PitWall.Configuration;
using PitWall.Models;
using System.Linq.Expressions;
using System.Net.Http;
using System.Text.Json;

namespace PitWall.Services;

public class OpenF1APIService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "https://api.openf1.org/v1/";

    public OpenF1APIService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<T>> FetchDataAsync<T>(APIParam parameters)
    {
        string fullUrl = $"{_baseUrl}{parameters}";

        string response = await _httpClient.GetStringAsync(fullUrl);

        //Need error for if the endpoint doesn't match the model.

        return JsonSerializer.Deserialize<List<T>>(response, JsonPolicies.OpenF1Options) ?? [];
    }
}
