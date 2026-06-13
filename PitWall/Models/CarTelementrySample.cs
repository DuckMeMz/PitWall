using System.Text.Json.Serialization;

namespace PitWall.Models;

public record CarTelementrySample
{
    [property: JsonPropertyName("date")]
    public DateTimeOffset Timestamp { get; init; }
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
    public DriverNumber DriverNumber { get; init; }
    public int Throttle { get; init; }
    public int Brake { get; init; }
    public int Speed { get; init; }
    public int Rpm { get; init; }
    [property: JsonPropertyName("n_gear")]
    public int Gear { get; init; }
    public DrsState Drs { get; init; }
}