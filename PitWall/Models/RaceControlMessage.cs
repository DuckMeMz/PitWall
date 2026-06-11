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
