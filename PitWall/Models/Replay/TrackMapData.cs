using PitWall.Models.OpenF1Api;

namespace PitWall.Models;

public record TrackMapData(
    string Title,
    IReadOnlyList<OpenF1Location> CircuitLocations);
