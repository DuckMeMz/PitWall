namespace PitWall.Models;

public record RaceControlMessage
{
    public DateTimeOffset Timestamp { get; init; }
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
    public string Message { get; init; } = string.Empty;
    public RaceControlCategory Category { get; init; }
    public RaceControlScope Scope { get; init; }
    public DriverNumber DriverNumber { get; init; }
    public LapNumber LapNumber { get; init; }

    public byte? QualifyingPhase { get; init; }
    public byte? EventSector { get; init; }

    public string? RawFlag { get; init; } //Fallback for if flags are misspelt. They are hand-typed.
    public FlagType Flag { get; init; }
}

public static class RaceControlFields
{
    public static readonly ApiField<DateTimeOffset> Timestamp = ApiFields.Timestamp;
    public static readonly ApiField<MeetingKey> MeetingKey = ApiFields.MeetingKey;
    public static readonly ApiField<SessionKey> SessionKey = ApiFields.SessionKey;
    public static readonly ApiField<string> Message = new("message");
    public static readonly ApiField<RaceControlCategory> Category = new("category");
    public static readonly ApiField<RaceControlScope> Scope = new("scope");
    public static readonly ApiField<DriverNumber> DriverNumber = ApiFields.DriverNumber;
    public static readonly ApiField<LapNumber> LapNumber = new("lap_number");
    public static readonly ApiField<byte?> QualifyingPhase = new("qualifying_phase");
    public static readonly ApiField<byte?> EventSector = new("sector");
    public static readonly ApiField<FlagType> Flag = new("flag");
}
