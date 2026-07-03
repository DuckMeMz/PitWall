using PitWall.Models.OpenF1Api;

namespace PitWall.Models;

public record SessionOverview(
    OpenF1Session Session,
    IReadOnlyList<OpenF1Driver> Drivers,
    IReadOnlyList<OpenF1Lap> Laps
    )
{

}