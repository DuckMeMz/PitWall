namespace PitWall.Models;

public record Lap
{
    public DateTimeOffset? TimestampStart { get; init; }
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
    public DriverNumber DriverNumber { get; init; }
    public LapNumber LapNumber { get; init; }
    public double? LapDuration { get; init; }
    public double? DurationSector1 { get; init; }
    public double? DurationSector2 { get; init; }
    public double? DurationSector3 { get; init; }
    public int? I1Speed { get; init; }
    public int? I2Speed { get; init; }
    public int? StSpeed { get; init; }
    public bool? IsPitOutLap { get; init; }
    public IReadOnlyList<SectorSegments>? SegmentsSector1 { get; init; }
    public IReadOnlyList<SectorSegments>? SegmentsSector2 { get; init; }
    public IReadOnlyList<SectorSegments>? SegmentsSector3 { get; init; }
}

public static class LapFields
{
    public static readonly ApiField<DateTimeOffset?> TimestampStart = new("date_start");
    public static readonly ApiField<MeetingKey> MeetingKey = ApiFields.MeetingKey;
    public static readonly ApiField<SessionKey> SessionKey = ApiFields.SessionKey;
    public static readonly ApiField<DriverNumber> DriverNumber = ApiFields.DriverNumber;
    public static readonly ApiField<LapNumber> LapNumber = new("lap_number");
    public static readonly ApiField<double?> LapDuration = new("lap_duration");
    public static readonly ApiField<double?> DurationSector1 = new("duration_sector_1");
    public static readonly ApiField<double?> DurationSector2 = new("duration_sector_2");
    public static readonly ApiField<double?> DurationSector3 = new("duration_sector_3");
    public static readonly ApiField<int?> I1Speed = new("i1_speed");
    public static readonly ApiField<int?> I2Speed = new("i2_speed");
    public static readonly ApiField<int?> StSpeed = new("st_speed");
    public static readonly ApiField<bool?> IsPitOutLap = new("is_pit_out_lap");
}
