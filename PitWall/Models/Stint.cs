namespace PitWall.Models;
public record Stint
{
    public MeetingKey MeetingKey { get; init; }
    public SessionKey SessionKey { get; init; }
    public DriverNumber DriverNumber { get; init; }
    public TyreCompound Compound { get; init; }
    public ushort StartLap { get; init; }
    public ushort EndLap { get; init; }
    public byte StintNumber { get; init; }
    public byte TyreAge { get; init; } //The age of the tyres at the start of the stint in laps completed.
}
