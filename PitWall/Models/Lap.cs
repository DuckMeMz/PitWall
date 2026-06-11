namespace PitWall.Models;

public record Lap
{
    public DateTimeOffset DateStart { get; init; }
    public byte DriverNumber { get; init; }
    public ushort MeetingKey { get; init; }
    public ushort SessionKey { get; init; }
    public ushort LapNumber { get; init; }
    public double LapDuration { get; init; }
    public double DurationSector1 { get; init; }
    public double DurationSector2 { get; init; }
    public double DurationSector3 { get; init; }
    public ushort I1Speed { get; init; }
    public ushort I2Speed { get; init; }
    public ushort StSpeed { get; init; }
    public bool IsPitOutLap { get; init; }
    public IReadOnlyList<ushort> SegmentsSector1 { get; init; } = [];
    public IReadOnlyList<ushort> SegmentsSector2 { get; init; } = [];
    public IReadOnlyList<ushort> SegmentsSector3 { get; init; } = [];
}