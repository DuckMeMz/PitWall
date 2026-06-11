namespace PitWall.Models;

public record PitInfo
{
    public DateTimeOffset Timestamp { get; init; }
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
    public DriverNumber DriverNumber { get; init; }
    public double LaneDuration { get; init; }
    public double StopDuration { get; init; }
    public LapNumber LapNumber { get; init; } //The lap that the driver came in on and completed by driving through the pits.
}