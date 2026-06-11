namespace PitWall.Models;

public record CarTelementrySample
{
    public DateTimeOffset Date { get; init; }
    public int DriverNumber { get; init; }
    public int MeetingKey { get; init; }
    public int SessionKey { get; init; }
    public int Throttle { get; init; }
    public int Brake { get; init; }
    public int Speed { get; init; }
    public int Rpm { get; init; }
    public int Gear { get; init; }
    public int Drs { get; init; }
}