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

public static class DriverFields
{
    public static readonly ApiField<string> BroadcastName = new("broadcast_name");
    public static readonly ApiField<DriverNumber> DriverNumber = ApiFields.DriverNumber;
    public static readonly ApiField<string> FirstName = new("first_name");
    public static readonly ApiField<string> FullName = new("full_name");
    public static readonly ApiField<string> HeadshotUrl = new("headshot_url");
    public static readonly ApiField<string> LastName = new("last_name");
    public static readonly ApiField<string> NameAcronym = new("name_acronym");
    public static readonly ApiField<Color> TeamColor = new("team_colour");
    public static readonly ApiField<TeamName> TeamName = new("team_name");
}
