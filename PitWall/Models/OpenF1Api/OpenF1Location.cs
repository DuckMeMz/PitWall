namespace PitWall.Models.OpenF1Api;

public record OpenF1Location
{
    [System.Text.Json.Serialization.JsonPropertyName("date")]
    public DateTimeOffset? Timestamp { get; init; }
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
    public DriverNumber DriverNumber { get; init; }
    public int? X { get; init; }
    public int? Y { get; init; }
    public int? Z { get; init; }
}

public static class LocationFields
{
    public static readonly ApiField<DateTimeOffset?> TimestampStart = new("date");
    public static readonly ApiField<MeetingKey> MeetingKey = ApiFields.MeetingKey;
    public static readonly ApiField<SessionKey> SessionKey = ApiFields.SessionKey;
    public static readonly ApiField<DriverNumber> DriverNumber = ApiFields.DriverNumber;
    public static readonly ApiField<int?> X = new("x");
    public static readonly ApiField<int?> Y = new("y");
    public static readonly ApiField<int?> Z = new("z");
}
