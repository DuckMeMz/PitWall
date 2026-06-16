namespace PitWall.Models;

public record ReplayData(
    OpenF1Session Session,
    IReadOnlyList<PositionUpdate> Positions,
    IReadOnlyList<CarTelemetrySample> CarTelementry)
{

}
