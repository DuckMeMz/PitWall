namespace PitWall.Models.OpenF1Api;

public class OpenF1Overtake
{
    [System.Text.Json.Serialization.JsonPropertyName("date")]
    public DateTimeOffset? Timestamp { get; init; }
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
    public DriverNumber OvertakenDriverNumber { get; init; }
    public DriverNumber OvertakingDriverNumber { get; init; }
    public Position? Position { get; init; } //The position of the overtaking driver once the overtake was complete
}

public static class OvertakeFields
{
    public static readonly ApiField<DateTimeOffset?> Timestamp = new("date");
    public static readonly ApiField<MeetingKey> MeetingKey = ApiFields.MeetingKey;
    public static readonly ApiField<SessionKey> SessionKey = ApiFields.SessionKey;
    public static readonly ApiField<DriverNumber> OvertakenDriverNumber = new("overtaken_driver_number");
    public static readonly ApiField<DriverNumber> OvertakingDriverNumber = new("overtaking_driver_number");
    public static readonly ApiField<Position?> Position = new("position");
}
