namespace PitWall.Models;

public record SessionData(
    OpenF1Session Session,
    IReadOnlyList<Driver> Drivers,
    IReadOnlyList<PositionUpdate> Positions,
    IReadOnlyList<CarTelemetrySample> CarTelemetry,
    IReadOnlyList<Lap> Laps);