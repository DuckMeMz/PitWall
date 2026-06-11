namespace PitWall.Models;

public record Location
{
    public DateTimeOffset TimestampStart { get; init; }
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
    public DriverNumber DriverNumber { get; init; }
    public ushort X { get; init; }
    public ushort Y { get; init; }
    public ushort Z { get; init; }
}