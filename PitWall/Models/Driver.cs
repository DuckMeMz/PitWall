namespace PitWall.Models;

public record Driver
{
    public string BroadcastName { get; init; } = string.Empty;
    public DriverNumber DriverNumber { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string HeadshotUrl { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string NameAcronym { get; init; } = string.Empty;
    public Color TeamColor { get; init; }
    public TeamName TeamName { get; init; }
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
}