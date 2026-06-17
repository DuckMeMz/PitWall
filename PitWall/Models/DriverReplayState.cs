using PitWall.Models.OpenF1Api;

namespace PitWall.Models;

public record DriverReplayState(
      DriverNumber DriverNumber,
      Position? Position,
      OpenF1Location? Location,
      OpenF1CarTelemetrySample? Telemetry,
      OpenF1IntervalSample? Interval,
      OpenF1Lap? CurrentLap,
      OpenF1Stint? CurrentStint,
      OpenF1PitInfo? CurrentPitStop);