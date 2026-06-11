namespace PitWall.Models;

public record Lap
{
    public DateTimeOffset TimestampStart { get; init; }
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
    public DriverNumber DriverNumber { get; init; }
    public LapNumber LapNumber { get; init; }
    public double LapDuration { get; init; }
    public double DurationSector1 { get; init; }
    public double DurationSector2 { get; init; }
    public double DurationSector3 { get; init; }
    public ushort I1Speed { get; init; }
    public ushort I2Speed { get; init; }
    public ushort StSpeed { get; init; }
    public bool IsPitOutLap { get; init; }
    public IReadOnlyList<SectorSegments> SegmentsSector1 { get; init; } = [];
    public IReadOnlyList<SectorSegments> SegmentsSector2 { get; init; } = [];
    public IReadOnlyList<SectorSegments> SegmentsSector3 { get; init; } = [];
}