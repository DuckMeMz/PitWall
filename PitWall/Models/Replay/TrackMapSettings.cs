namespace PitWall.Models;

public sealed class TrackMapSettings
{
    public double MinimumLapDurationSeconds { get; init; } = 45;
    public double MaximumLapDurationSeconds { get; init; } = 200;
    public int CandidateLapCount { get; init; } = 3;
    public int MinimumDistinctCoordinates { get; init; } = 100;
}
