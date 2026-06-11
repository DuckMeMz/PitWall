namespace PitWall.Models;

public record StartingGrid
{
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
    public double LapDuration { get; init; }
    public Position Position { get; init; }
}