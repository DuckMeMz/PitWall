namespace PitWall.Models;

public record Meeting
{
    public MeetingKey MeetingKey { get; init; }
    public CircuitKey CircuitKey { get; init; }
    public int Year { get; init; }
    public string MeetingName { get; init; } = string.Empty;
    public string OfficialName { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public string CountryName { get; init; } = string.Empty;
    public string CircuitShortName { get; init; } = string.Empty;
    public DateTimeOffset? TimestampStart { get; init; }
    public DateTimeOffset? TimestampEnd { get; init; }
    public string GmtOffset { get; init; } = string.Empty;

    public string CircuitImage { get; init; } = string.Empty;
    public string CircuitInfo { get; init; } = string.Empty;
    public string CircuitType { get; init; } = string.Empty;
    public string CountryFlag { get; init; } = string.Empty;
    public CountryKey CountryKey { get; init; }
    public bool IsCancelled { get; init; }

}