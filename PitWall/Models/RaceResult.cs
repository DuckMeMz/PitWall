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