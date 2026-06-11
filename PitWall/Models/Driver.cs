namespace PitWall.Models;

public record Driver
{
    public string BroadcastName { get; init; } = string.Empty;
    public byte DriverNumber { get; init; } = 0;
    public string FirstName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string HeadshotUrl { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string NameAcronym { get; init; } = string.Empty;
    public string TeamColor { get; init; } = string.Empty;
    public string TeamName { get; init; } = string.Empty;
    public ushort MeetingKey { get; init; } = 0;
    public ushort SessionKey { get; init; } = 0;
}