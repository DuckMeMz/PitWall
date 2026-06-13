namespace PitWall.Models;

public record Location
{
    public DateTimeOffset TimestampStart { get; init; }
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
    public DriverNumber DriverNumber { get; init; }
    public ushort X { get; init; }
    public ushort Y { get; init; }
    public ushort Z { get; init; }
}

public static class LocationFields
{
    public static readonly ApiField<DateTimeOffset> TimestampStart = new("date");
    public static readonly ApiField<MeetingKey> MeetingKey = ApiFields.MeetingKey;
    public static readonly ApiField<SessionKey> SessionKey = ApiFields.SessionKey;
    public static readonly ApiField<DriverNumber> DriverNumber = ApiFields.DriverNumber;
    public static readonly ApiField<ushort> X = new("x");
    public static readonly ApiField<ushort> Y = new("y");
    public static readonly ApiField<ushort> Z = new("z");
}
