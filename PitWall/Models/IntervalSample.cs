namespace PitWall.Models;

public record IntervalSample
{
    public DateTimeOffset Timestamp { get; init; }
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
    public DriverNumber DriverNumber { get; init; }
    public double? GapToLeader { get; init; }
    public double? IntervalToAhead { get; init; }
}

public static class IntervalSampleFields
{
    public static readonly ApiField<DateTimeOffset> Timestamp = ApiFields.Timestamp;
    public static readonly ApiField<MeetingKey> MeetingKey = ApiFields.MeetingKey;
    public static readonly ApiField<SessionKey> SessionKey = ApiFields.SessionKey;
    public static readonly ApiField<DriverNumber> DriverNumber = ApiFields.DriverNumber;

    public static readonly ApiField<double?> GapToLeader = new("gap_to_leader");
    public static readonly ApiField<double?> IntervalToAhead = new("interval");
}
