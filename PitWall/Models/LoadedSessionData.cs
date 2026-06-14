namespace PitWall.Models;

public record LoadedSessionData(
    Session Session,
    IReadOnlyList<Driver> Drivers,
    IReadOnlyList<PositionUpdate> Positions,
    IReadOnlyList<CarTelemetrySample> CarTelemetry,
    IReadOnlyList<Lap> Laps);