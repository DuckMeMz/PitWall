namespace PitWall.Models;

public static class ApiFields
{
    public static readonly ApiField<DateTimeOffset> Timestamp = new("date");
    public static readonly ApiField<MeetingKey> MeetingKey = new("meeting_key");
    public static readonly ApiField<SessionKey> SessionKey = new("session_key");
    public static readonly ApiField<DriverNumber> DriverNumber = new("driver_number");
}