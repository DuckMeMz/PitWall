namespace PitWall.Models;

public record IntervalSample
{
    public DateTimeOffset Date { get; init; }
    public byte DriverNumber { get; init; }
    public ushort MeetingKey { get; init; }
    public ushort SessionKey { get; init; }
    public double? GapToLeader { get; init; }
    public double? IntervalToAhead { get; init; }
}
