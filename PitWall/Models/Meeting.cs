namespace PitWall.Models;

//Holds a limited amount of data about the "Meeting" (Location). Complete data is held within CompleteMeeting.
public record Meeting
{
    public ushort MeetingKey { get; init; }
    public ushort Year { get; init; }
    public string MeetingName { get; init; } = string.Empty;
    public string OfficialName { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public string CountryName { get; init; } = string.Empty;
    public string CircuitShortName { get; init; } = string.Empty;
    public DateTimeOffset? DateStart { get; init; }
    public string GmtOffset { get; init; } = string.Empty;
}