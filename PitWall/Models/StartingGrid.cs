namespace PitWall.Models;

public record StartingGrid
{
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
    public DriverNumber? DriverNumber { get; init; }
    public double? LapDuration { get; init; }
    public Position? Position { get; init; }
}

public static class StartingGridFields
{
    public static readonly ApiField<MeetingKey> MeetingKey = ApiFields.MeetingKey;
    public static readonly ApiField<SessionKey> SessionKey = ApiFields.SessionKey;
    public static readonly ApiField<DriverNumber?> DriverNumber = new("driver_number");
    public static readonly ApiField<double?> LapDuration = new("lap_duration");
    public static readonly ApiField<Position?> Position = new("position");
}
