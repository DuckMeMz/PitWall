namespace PitWall.Models.OpenF1Api;

public record OpenF1Weather
{
    [System.Text.Json.Serialization.JsonPropertyName("date")]
    public DateTimeOffset? Timestamp { get; init; }
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
    public double? AirTemperature { get; init; }
    public double? TrackTemperature { get; init; }
    public double? Humidity { get; init; }
    public double? Rainfall { get; init; }
    public double? Pressure { get; init; }
    public double? WindDirection { get; init; } // 0-359 degrees
    public double? WindSpeed { get; init; } // M/s
}

public static class WeatherFields
{
    public static readonly ApiField<DateTimeOffset?> Timestamp = new("date");
    public static readonly ApiField<MeetingKey> MeetingKey = ApiFields.MeetingKey;
    public static readonly ApiField<SessionKey> SessionKey = ApiFields.SessionKey;
    public static readonly ApiField<double?> AirTemperature = new("air_temperature");
    public static readonly ApiField<double?> TrackTemperature = new("track_temperature");
    public static readonly ApiField<double?> Humidity = new("humidity");
    public static readonly ApiField<double?> Rainfall = new("rainfall");
    public static readonly ApiField<double?> Pressure = new("pressure");
    public static readonly ApiField<double?> WindDirection = new("wind_direction");
    public static readonly ApiField<double?> WindSpeed = new("wind_speed");
}
