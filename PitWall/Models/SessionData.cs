using PitWall.Models.OpenF1Api;

namespace PitWall.Models;

public record SessionData(
    OpenF1Session Session,
    IReadOnlyList<OpenF1Driver> Drivers,
    IReadOnlyList<OpenF1PositionUpdate> Positions,
    IReadOnlyList<OpenF1CarTelemetrySample> CarTelemetry,
    IReadOnlyList<OpenF1Lap> Laps);