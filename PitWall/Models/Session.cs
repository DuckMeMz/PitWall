namespace PitWall.Models;

public record Session
{
    public DateTimeOffset TimestampStart { get; init; }
    public DateTimeOffset TimestampEnd { get; init; }
    public string GmtOffset { get; init; } = string.Empty;
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
    public string SessionName { get; init; } = string.Empty;
    public SessionType SessionType { get; init; }
    public int Year { get; init; }
    public CircuitKey CircuitKey { get; init; }
    public string CircuitShortName { get; init; } = string.Empty;
    public CountryKey CountryKey { get; init; }
    public string CountryName { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
}

public static class SessionFields
{
    public static readonly ApiField<DateTimeOffset> TimestampStart = new("date_start");
    public static readonly ApiField<DateTimeOffset> TimestampEnd = new("date_end");
    public static readonly ApiField<MeetingKey> MeetingKey = ApiFields.MeetingKey;
    public static readonly ApiField<SessionKey> SessionKey = ApiFields.SessionKey;
    public static readonly ApiField<string> GmtOffset = new("gmt_offset");
    public static readonly ApiField<string> SessionName = new("session_name");
    public static readonly ApiField<SessionType> SessionType = new("session_type");
    public static readonly ApiField<int> Year = new("year");
    public static readonly ApiField<CircuitKey> CircuitKey = new("circuit_key");
    public static readonly ApiField<string> CircuitShortName = new("circuit_short_name");
    public static readonly ApiField<CountryKey> CountryKey = new("country_key");
    public static readonly ApiField<string> CountryName = new("country_name");
    public static readonly ApiField<string> CountryCode = new("country_code");
    public static readonly ApiField<string> Location = new("location");
}
