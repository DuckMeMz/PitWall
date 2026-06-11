namespace PitWall.Models;

//Holds the complete data about the "Meeting" (Location). A more limited record can be found under CompleteMeeting.cs
public record CompleteMeeting
{
    public ushort MeetingKey { get; init; }
    public ushort CircuitKey { get; init; }
    public ushort Year { get; init; }
    public string MeetingName { get; init; } = string.Empty;
    public string OfficialName { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public string CountryName { get; init; } = string.Empty;
    public string CircuitShortName { get; init; } = string.Empty;
    public DateTimeOffset? DateStart { get; init; }
    public DateTimeOffset? DateEnd { get; init; }
    public string GmtOffset { get; init; } = string.Empty;

    public string CircuitImage { get; init; } = string.Empty;
    public string CircuitInfo { get; init; } = string.Empty;
    public string CircuitType { get; init; } = string.Empty;
    public string CountryFlag { get; init; } = string.Empty;
    public string CountryKey { get; init; } = string.Empty;
    public bool IsCancelled { get; init; }

}