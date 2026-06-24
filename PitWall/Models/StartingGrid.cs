namespace PitWall.Models;

public record StartingGrid(OpenF1Session Session, IReadOnlyList<OpenF1StartingGridEntry> Entries);