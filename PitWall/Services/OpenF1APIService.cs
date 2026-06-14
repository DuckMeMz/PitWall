using System.Net.Http;
using Polly;
using PitWall.Models;
using System.Diagnostics;
using PitWall.Services.Exceptions;
using System.Text.Json.Serialization;
using System.Text.Json;
using PitWall.Configuration;

namespace PitWall.Services;

public class OpenF1APIService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "https://api.openf1.org/v1/";

    public OpenF1APIService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<T>> FetchDataAsync<T>(APIParam parameters, CancellationToken cancellationToken = default)
    {
        string finalUrl = $"{_baseUrl}{parameters.GetRelativeUrl()}";

        ResiliencePipeline pipeline = new ResiliencePipelineBuilder().Build();

        Debug.WriteLine($"Fetching: {finalUrl}");

        HttpResponseMessage response = await pipeline.ExecuteAsync(
            async token => await _httpClient.GetAsync(finalUrl, cancellationToken),
            cancellationToken);

        string json = await response.Content.ReadAsStringAsync(cancellationToken);

        if(!response.IsSuccessStatusCode)
        {
            throw new OpenF1RequestException(finalUrl, response.StatusCode, json);
        }

        try
        {
            return JsonSerializer.Deserialize<List<T>>(json, JsonPolicies.OpenF1Options) ?? [];
        }
        catch(JsonException jsonException)
        {
            throw new OpenF1DeserializeException(finalUrl, typeof(T), jsonException);
        }
    }
}
