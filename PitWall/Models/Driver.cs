namespace PitWall.Models;

public record Driver
{
    public string? BroadcastName { get; init; }
    public DriverNumber DriverNumber { get; init; }
    public string? FirstName { get; init; }
    public string? FullName { get; init; }
    public string? HeadshotUrl { get; init; }
    public string? LastName { get; init; }
    public string? NameAcronym { get; init; }
    public Color? TeamColor { get; init; }
    public TeamName? TeamName { get; init; }
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }

    public override string ToString() => $"{FullName} ({DriverNumber})";
}

public static class DriverFields
{
    public static readonly ApiField<string?> BroadcastName = new("broadcast_name");
    public static readonly ApiField<DriverNumber> DriverNumber = ApiFields.DriverNumber;
    public static readonly ApiField<string?> FirstName = new("first_name");
    public static readonly ApiField<string?> FullName = new("full_name");
    public static readonly ApiField<string?> HeadshotUrl = new("headshot_url");
    public static readonly ApiField<string?> LastName = new("last_name");
    public static readonly ApiField<string?> NameAcronym = new("name_acronym");
    public static readonly ApiField<Color?> TeamColor = new("team_colour");
    public static readonly ApiField<TeamName?> TeamName = new("team_name");
}