namespace PitWall.Models.OpenF1Api;

public record OpenF1RaceResult
{
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
    public DriverNumber DriverNumber { get; init; }
    public Position? Position { get; init; }
    public double? Points { get; init; }
    public int? NumberOfLaps { get; init; }
    public double? Duration { get; init; }
    public TimingGap? GapToLeader { get; init; }
    public bool? Dnf { get; init; }
    public bool? Dns { get; init; }
    public bool? Dsq { get; init; }
}

public static class RaceResultFields
{
    public static readonly ApiField<MeetingKey> MeetingKey = ApiFields.MeetingKey;
    public static readonly ApiField<SessionKey> SessionKey = ApiFields.SessionKey;
    public static readonly ApiField<DriverNumber> DriverNumber = ApiFields.DriverNumber;
    public static readonly ApiField<Position?> Position = new("position");
    public static readonly ApiField<double?> Points = new("points");
    public static readonly ApiField<int?> NumberOfLaps = new("number_of_laps");
    public static readonly ApiField<double?> Duration = new("duration");
    public static readonly ApiField<TimingGap?> GapToLeader = new("gap_to_leader");
    public static readonly ApiField<bool?> Dnf = new("dnf");
    public static readonly ApiField<bool?> Dns = new("dns");
    public static readonly ApiField<bool?> Dsq = new("dsq");
}
