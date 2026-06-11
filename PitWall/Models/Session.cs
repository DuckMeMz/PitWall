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
