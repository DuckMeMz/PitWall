namespace PitWall.Models;

public record Meeting
{
    public MeetingKey MeetingKey { get; init; }
    public CircuitKey CircuitKey { get; init; }
    public int? Year { get; init; }
    public string? MeetingName { get; init; }
    public string? OfficialName { get; init; }
    public string? Location { get; init; }
    public string? CountryCode { get; init; }
    public string? CountryName { get; init; }
    public string? CircuitShortName { get; init; }
    public DateTimeOffset? TimestampStart { get; init; }
    public DateTimeOffset? TimestampEnd { get; init; }
    public string? GmtOffset { get; init; }

    public string? CircuitImage { get; init; }
    public string? CircuitInfo { get; init; }
    public string? CircuitType { get; init; }
    public string? CountryFlag { get; init; }
    public CountryKey? CountryKey { get; init; }
    public bool? IsCancelled { get; init; }

}

public static class MeetingFields
{
    public static readonly ApiField<MeetingKey> MeetingKey = ApiFields.MeetingKey;
    public static readonly ApiField<CircuitKey> CircuitKey = new("circuit_key");
    public static readonly ApiField<int?> Year = new("year");
    public static readonly ApiField<string?> MeetingName = new("meeting_name");
    public static readonly ApiField<string?> OfficialName = new("meeting_official_name");
    public static readonly ApiField<string?> Location = new("location");
    public static readonly ApiField<string?> CountryCode = new("country_code");
    public static readonly ApiField<string?> CountryName = new("country_name");
    public static readonly ApiField<string?> CircuitShortName = new("circuit_short_name");
    public static readonly ApiField<DateTimeOffset?> TimestampStart = new("date_start");
    public static readonly ApiField<DateTimeOffset?> TimestampEnd = new("date_end");
    public static readonly ApiField<string?> GmtOffset = new("gmt_offset");
    public static readonly ApiField<string?> CircuitImage = new("circuit_image");
    public static readonly ApiField<string?> CircuitInfo = new("circuit_info_url");
    public static readonly ApiField<string?> CircuitType = new("circuit_type");
    public static readonly ApiField<string?> CountryFlag = new("country_flag");
    public static readonly ApiField<CountryKey?> CountryKey = new("country_key");
    public static readonly ApiField<bool?> IsCancelled = new("is_cancelled");
}
