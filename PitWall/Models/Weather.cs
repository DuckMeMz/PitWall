namespace PitWall.Models;

public record Weather
{
    public DateTimeOffset Timestamp { get; init; }
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
    public sbyte AirTemperature { get; init; }
    public sbyte TrackTemperature { get; init; }
    public byte Humidity { get; init; }
    public byte Rainfall { get; init; }
    public ushort Pressure { get; init; }
    public ushort WindDirection { get; init; } // 0-360 degrees
    public byte WindSpeed { get; init; } // M/s
}
