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

public static class WeatherFields
{
    public static readonly ApiField<DateTimeOffset> Timestamp = ApiFields.Timestamp;
    public static readonly ApiField<MeetingKey> MeetingKey = ApiFields.MeetingKey;
    public static readonly ApiField<SessionKey> SessionKey = ApiFields.SessionKey;
    public static readonly ApiField<sbyte> AirTemperature = new("air_temperature");
    public static readonly ApiField<sbyte> TrackTemperature = new("track_temperature");
    public static readonly ApiField<byte> Humidity = new("humidity");
    public static readonly ApiField<byte> Rainfall = new("rainfall");
    public static readonly ApiField<ushort> Pressure = new("pressure");
    public static readonly ApiField<ushort> WindDirection = new("wind_direction");
    public static readonly ApiField<byte> WindSpeed = new("wind_speed");
}
