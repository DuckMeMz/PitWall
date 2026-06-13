using System.Text.Json;
using System.Text.Json.Serialization;

namespace PitWall.Configuration;

public static class JsonPolicies
{
    public static JsonSerializerOptions OpenF1Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        AllowTrailingCommas = true
    };
}