namespace PitWall.Models.OpenF1Api;

public record OpenF1TeamRadio
{
    [System.Text.Json.Serialization.JsonPropertyName("date")]
    public DateTimeOffset? Timestamp { get; init; }
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
    public DriverNumber DriverNumber { get; init; }
    public string? RecordingUrl { get; init; }
}

public static class TeamRadioFields
{
    public static readonly ApiField<DateTimeOffset?> Timestamp = new("date");
    public static readonly ApiField<MeetingKey> MeetingKey = ApiFields.MeetingKey;
    public static readonly ApiField<SessionKey> SessionKey = ApiFields.SessionKey;
    public static readonly ApiField<DriverNumber> DriverNumber = ApiFields.DriverNumber;
    public static readonly ApiField<string?> RecordingUrl = new("recording_url");
}
