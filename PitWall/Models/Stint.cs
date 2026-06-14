namespace PitWall.Models;
public record Stint
{
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
    public DriverNumber DriverNumber { get; init; }
    public TyreCompound? Compound { get; init; }
    public int? StartLap { get; init; }
    public int? EndLap { get; init; }
    public int? StintNumber { get; init; }
    public int? TyreAge { get; init; } //The age of the tyres at the start of the stint in laps completed.
}

public static class StintFields
{
    public static readonly ApiField<MeetingKey> MeetingKey = ApiFields.MeetingKey;
    public static readonly ApiField<SessionKey> SessionKey = ApiFields.SessionKey;
    public static readonly ApiField<DriverNumber> DriverNumber = ApiFields.DriverNumber;
    public static readonly ApiField<TyreCompound?> Compound = new("compound");
    public static readonly ApiField<int?> StartLap = new("lap_start");
    public static readonly ApiField<int?> EndLap = new("lap_end");
    public static readonly ApiField<int?> StintNumber = new("stint_number");
    public static readonly ApiField<int?> TyreAge = new("tyre_age_at_start");
}
