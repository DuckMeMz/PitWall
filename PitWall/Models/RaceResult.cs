namespace PitWall.Models;

public record RaceResult
{
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
    public DriverNumber DriverNumber { get; init; }
    public Position Position { get; init; }
    public byte Points { get; init; }
    public byte NumberOfLaps { get; init; }
    public double? Duration { get; init; }
    public double? GapToLeader { get; init; }
    public bool Dnf { get; init; }
    public bool Dns { get; init; }
    public bool Dsq { get; init; }
}

public static class RaceResultFields
{
    public static readonly ApiField<MeetingKey> MeetingKey = ApiFields.MeetingKey;
    public static readonly ApiField<SessionKey> SessionKey = ApiFields.SessionKey;
    public static readonly ApiField<DriverNumber> DriverNumber = ApiFields.DriverNumber;
    public static readonly ApiField<Position> Position = new("position");
    public static readonly ApiField<byte> NumberOfLaps = new("number_of_laps");
    public static readonly ApiField<double?> Duration = new("duration");
    public static readonly ApiField<double?> GapToLeader = new("gap_to_leader");
    public static readonly ApiField<bool> Dnf = new("dnf");
    public static readonly ApiField<bool> Dns = new("dns");
    public static readonly ApiField<bool> Dsq = new("dsq");
}
