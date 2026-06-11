namespace PitWall.Models;

public class Overtake
{
    public DateTimeOffset Date { get; init; }
    public ushort MeetingKey { get; init; }
    public ushort SessionKey { get; init; }
    public byte OvertakenDriverNumber { get; init; }
    public byte OvertakingDriverNumber { get; init; }
    public byte Position { get; init; } //The position of the overtaking driver once the overtake was complete
}