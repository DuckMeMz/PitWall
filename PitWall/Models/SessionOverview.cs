namespace PitWall.Models;

public record SessionOverview(
    OpenF1Session Session,
    IReadOnlyList<Driver> Drivers,
    IReadOnlyList<Lap> Laps
    )
{

}