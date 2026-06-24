using System.Text.Json.Serialization;

namespace PitWall.Models;

public record OpenF1Session
{
    [JsonPropertyName("date_start")]
    public DateTimeOffset? TimestampStart { get; init; }
    [JsonPropertyName("date_end")]
    public DateTimeOffset? TimestampEnd { get; init; }
    public string? GmtOffset { get; init; }
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
    public string? SessionName { get; init; }
    public SessionType? SessionType { get; init; }
    public int? Year { get; init; }
    public CircuitKey? CircuitKey { get; init; }
    public string? CircuitShortName { get; init; }
    public CountryKey? CountryKey { get; init; }
    public string? CountryName { get; init; }
    public string? CountryCode { get; init; }
    public string? Location { get; init; }
    public bool? IsCancelled { get; init; }
}

public static class SessionFields
{
    public static readonly ApiField<DateTimeOffset?> TimestampStart = new("date_start");
    public static readonly ApiField<DateTimeOffset?> TimestampEnd = new("date_end");
    public static readonly ApiField<MeetingKey> MeetingKey = ApiFields.MeetingKey;
    public static readonly ApiField<SessionKey> SessionKey = ApiFields.SessionKey;
    public static readonly ApiField<string?> GmtOffset = new("gmt_offset");
    public static readonly ApiField<string?> SessionName = new("session_name");
    public static readonly ApiField<SessionType?> SessionType = new("session_type");
    public static readonly ApiField<int?> Year = new("year");
    public static readonly ApiField<CircuitKey?> CircuitKey = new("circuit_key");
    public static readonly ApiField<string?> CircuitShortName = new("circuit_short_name");
    public static readonly ApiField<CountryKey?> CountryKey = new("country_key");
    public static readonly ApiField<string?> CountryName = new("country_name");
    public static readonly ApiField<string?> CountryCode = new("country_code");
    public static readonly ApiField<string?> Location = new("location");
    public static readonly ApiField<bool?> IsCancelled = new("is_cancelled");
}
