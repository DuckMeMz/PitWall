namespace PitWall.Models;

public record ReplayFrame(
     DateTimeOffset Timestamp,
     TimeSpan SessionTime,
     IReadOnlyDictionary<DriverNumber, DriverReplayState> Drivers,
     Weather? Weather,
     IReadOnlyList<ReplayEvent> Events)
{
   
}