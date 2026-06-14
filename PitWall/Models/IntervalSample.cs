namespace PitWall.Models;

public record IntervalSample
{
    public DateTimeOffset? Timestamp { get; init; }
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
    public DriverNumber DriverNumber { get; init; }
    public TimingGap? GapToLeader { get; init; }
    public TimingGap? IntervalToAhead { get; init; }
}

public static class IntervalSampleFields
{
    public static readonly ApiField<DateTimeOffset?> Timestamp = new("date");
    public static readonly ApiField<MeetingKey> MeetingKey = ApiFields.MeetingKey;
    public static readonly ApiField<SessionKey> SessionKey = ApiFields.SessionKey;
    public static readonly ApiField<DriverNumber> DriverNumber = ApiFields.DriverNumber;

    public static readonly ApiField<TimingGap?> GapToLeader = new("gap_to_leader");
    public static readonly ApiField<TimingGap?> IntervalToAhead = new("interval");
}
