using System.Net.Http;
using Polly;
using PitWall.Models;
using System.Diagnostics;
using PitWall.Services.Exceptions;
using System.Text.Json;
using PitWall.Configuration;
using System.Net;
using System.Threading.RateLimiting;

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
        .AddRetry(new Polly.Retry.RetryStrategyOptions<HttpResponseMessage>
        {
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .HandleResult(response =>
                    response.StatusCode == HttpStatusCode.TooManyRequests),
            MaxRetryAttempts = 5,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential
        })
        .AddRateLimiter(CreateRateLimiter(
            permitLimit: 30,
            window: TimeSpan.FromMinutes(1),
            segmentsPerWindow: 60))
        .AddRateLimiter(CreateRateLimiter(
            permitLimit: 3,
            window: TimeSpan.FromSeconds(1),
            segmentsPerWindow: 10))
        .Build();
    }

    public async Task<IReadOnlyList<T>> FetchDataAsync<T>(ApiParams parameters, CancellationToken cancellationToken = default)
    {
        string finalUrl = $"{_baseUrl}{parameters.GetRelativeUrl()}";

        Debug.WriteLine($"Fetching: {finalUrl}");

        using HttpResponseMessage response = await _pipeline.ExecuteAsync(
            async token => await _httpClient.GetAsync(finalUrl, token),
            cancellationToken);

        string json = await response.Content.ReadAsStringAsync(cancellationToken);

        Debug.WriteLine($"Fetched: {finalUrl}.");

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
            Debug.WriteLine($"OpenF1 returned no content for {finalUrl}");
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

    private static SlidingWindowRateLimiter CreateRateLimiter(int permitLimit, TimeSpan window, int segmentsPerWindow)
    {
        return new SlidingWindowRateLimiter(
            new SlidingWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                SegmentsPerWindow = segmentsPerWindow,
                AutoReplenishment = true,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 100
            });
    }
}
