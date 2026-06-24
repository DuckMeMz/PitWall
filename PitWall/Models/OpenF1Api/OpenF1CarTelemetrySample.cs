using System.Text.Json.Serialization;

namespace PitWall.Models.OpenF1Api;

public record OpenF1CarTelemetrySample
{
    [property: JsonPropertyName("date")]
    public DateTimeOffset? Timestamp { get; init; }
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
    public DriverNumber DriverNumber { get; init; }
    public int? Throttle { get; init; }
    public int? Brake { get; init; }
    public int? Speed { get; init; }
    public int? Rpm { get; init; }
    [property: JsonPropertyName("n_gear")]
    public int? Gear { get; init; }
    public DrsState? Drs { get; init; }
}

public static class CarTelemetrySampleFields
{
    public static readonly ApiField<DateTimeOffset?> Timestamp = new("date");
    public static readonly ApiField<MeetingKey> MeetingKey = ApiFields.MeetingKey;
    public static readonly ApiField<SessionKey> SessionKey = ApiFields.SessionKey;
    public static readonly ApiField<DriverNumber> DriverNumber = ApiFields.DriverNumber;

    public static readonly ApiField<int?> Throttle = new("throttle");
    public static readonly ApiField<int?> Brake = new("brake");
    public static readonly ApiField<int?> Speed = new("speed");
    public static readonly ApiField<int?> Rpm = new("rpm");
    public static readonly ApiField<int?> Gear = new("n_gear");
    public static readonly ApiField<DrsState?> Drs = new("drs");
}
