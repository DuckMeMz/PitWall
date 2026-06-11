namespace PitWall.Models;

public record Location
{
    public DateTimeOffset DateTime { get; init; }
    public byte DriverNumber { get; init; }
    public ushort MeetingKey { get; init; }
    public ushort SessionKey { get; init; }
    public ushort X { get; init; }
    public ushort Y { get; init; }
    public ushort Z { get; init; }
}