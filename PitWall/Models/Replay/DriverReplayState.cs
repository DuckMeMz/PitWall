using PitWall.Models.OpenF1Api;

namespace PitWall.Models;

public readonly record struct DriverReplayState(
    DriverNumber DriverNumber,
    Position? Position,
    ReplayLocation? Location,
    ReplayTelemetry? Telemetry,
    ReplayInterval? Interval,
    OpenF1Lap? CurrentLap,
    OpenF1Stint? CurrentStint,
    OpenF1PitInfo? CurrentPitStop);

public readonly record struct ReplayLocation(
    int? X,
    int? Y,
    int? Z,
    DateTimeOffset SourceTimestamp,
    DateTimeOffset LastMovementTimestamp);

public readonly record struct ReplayTelemetry(
    int? Throttle,
    int? Brake,
    int? Speed,
    int? Rpm,
    int? Gear,
    DrsState? Drs);

public readonly record struct ReplayInterval(
    TimingGap? GapToLeader,
    TimingGap? IntervalToAhead);
