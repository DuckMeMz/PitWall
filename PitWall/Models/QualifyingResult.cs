namespace PitWall.Models;

public record QualifyingResult
{
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
    public DriverNumber DriverNumber { get; init; }
    public Position Position { get; init; }
    public byte NumberOfLaps { get; init; }
    public IReadOnlyList<double> Duration { get; init; } = [];
    public IReadOnlyList<double> GapToLeader { get; init; } = [];
    public bool Dnf { get; init; }
    public bool Dns { get; init; }
    public bool Dsq { get; init; }
}