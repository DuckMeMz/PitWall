namespace PitWall.Models;

public record PitInfo
{
    public DateTimeOffset? Timestamp { get; init; }
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
    public DriverNumber DriverNumber { get; init; }
    public double? LaneDuration { get; init; }
    public double? PitDuration { get; init; }
    public double? StopDuration { get; init; }
    public LapNumber LapNumber { get; init; } //The lap that the driver came in on and completed by driving through the pits.
}

public static class PitFields
{
    public static readonly ApiField<DateTimeOffset?> Timestamp = new("date");
    public static readonly ApiField<MeetingKey> MeetingKey = ApiFields.MeetingKey;
    public static readonly ApiField<SessionKey> SessionKey = ApiFields.SessionKey;
    public static readonly ApiField<DriverNumber> DriverNumber = ApiFields.DriverNumber;

    public static readonly ApiField<double?> LaneDuration = new("lane_duration");
    public static readonly ApiField<double?> PitDuration = new("pit_duration");
    public static readonly ApiField<double?> StopDuration = new("stop_duration");
    public static readonly ApiField<LapNumber> LapNumber = new("lap_number");
}
