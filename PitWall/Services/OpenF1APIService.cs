using System.Net.Http;
using Polly;
using PitWall.Models;
using System.Diagnostics;
using PitWall.Services.Exceptions;
using System.Text.Json;
using PitWall.Configuration;
using System.Net;

namespace PitWall.Services;

public class OpenF1APIService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "https://api.openf1.org/v1/";
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

    public OpenF1APIService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        
        _pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRateLimiter(new System.Threading.RateLimiting.FixedWindowRateLimiter(
                new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                {
                    PermitLimit = 1,
                    Window = TimeSpan.FromSeconds(1),
                    QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                    QueueLimit = 1000
                }))
            .AddRetry(new Polly.Retry.RetryStrategyOptions<HttpResponseMessage>
            {
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(response => response.StatusCode == System.Net.HttpStatusCode.TooManyRequests),
                MaxRetryAttempts = 5,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential
            })
            .Build();
    }

    public async Task<IReadOnlyList<T>> FetchDataAsync<T>(APIParam parameters, CancellationToken cancellationToken = default)
    {
        string finalUrl = $"{_baseUrl}{parameters.GetRelativeUrl()}";

        Debug.WriteLine($"Fetching: {finalUrl}");

        HttpResponseMessage response = await _pipeline.ExecuteAsync(
            async token => await _httpClient.GetAsync(finalUrl, token),
            cancellationToken);

        string json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            Debug.WriteLine($"OpenF1 returned no data for {finalUrl}.");
            return [];
        }

        if(!response.IsSuccessStatusCode)
        {
            throw new OpenF1RequestException(finalUrl, response.StatusCode, json);
        }

        if (response.StatusCode == HttpStatusCode.NoContent || string.IsNullOrWhiteSpace(json))
        {
            return [];
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
