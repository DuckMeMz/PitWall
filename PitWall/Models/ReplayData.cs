using PitWall.Models.OpenF1Api;

namespace PitWall.Models;

public record ReplayData(
    OpenF1Session Session,
    IReadOnlyList<OpenF1Driver> Drivers,
    IReadOnlyList<OpenF1Location> Locations,
    IReadOnlyList<OpenF1PositionUpdate> Positions,
    IReadOnlyList<OpenF1CarTelemetrySample> CarTelementry,
    IReadOnlyList<OpenF1IntervalSample> Intervals,
    IReadOnlyList<OpenF1Lap> Laps,
    IReadOnlyList<OpenF1RaceControlMessage> RaceControl,
    OpenF1Meeting? Meeting = null)
{
}
