using PitWall.Models.OpenF1Api;

namespace PitWall.Models;

public record ReplayDataChunk(
    OpenF1Session Session,
    IReadOnlyList<OpenF1Location> Locations,
    IReadOnlyList<OpenF1PositionUpdate> Positions,
    IReadOnlyList<OpenF1CarTelemetrySample> CarTelemetry,
    IReadOnlyList<OpenF1IntervalSample> Intervals,
    DateTimeOffset ChunkTimestampStart,
    TimeSpan ChunkLength,
    OpenF1Meeting? Meeting = null)
{

}

public record InitialReplayData(
    OpenF1Session Session, //Non-Buffered
    IReadOnlyList<OpenF1Driver> Drivers, //Non-Buffered
    IReadOnlyList<OpenF1Location> Locations, //Buffered 
    IReadOnlyList<OpenF1PositionUpdate> Positions, //Buffered
    IReadOnlyList<OpenF1CarTelemetrySample> CarTelemetry, //Buffered
    IReadOnlyList<OpenF1IntervalSample> Intervals, //Buffered
    IReadOnlyList<OpenF1Lap> Laps, //Non-Buffered
    TimeSpan LoadedLength,
    OpenF1Meeting? Meeting = null)
{
}
