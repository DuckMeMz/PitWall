namespace PitWall.Models;

public record TeamRadio
{
    public DateTimeOffset Timestamp { get; init; }
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
    public DriverNumber DriverNumber { get; init; }
    public string RecordingUrl { get; init; } = string.Empty;
}