namespace PitWall.Models;

public class Overtake
{
    public DateTimeOffset Timestamp { get; init; }
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
    public DriverNumber OvertakenDriverNumber { get; init; }
    public DriverNumber OvertakingDriverNumber { get; init; }
    public Position Position { get; init; } //The position of the overtaking driver once the overtake was complete
}