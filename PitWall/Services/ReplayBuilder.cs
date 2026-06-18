using PitWall.Models;
using PitWall.Models.OpenF1Api;

namespace PitWall.Services;

public class ReplayBuilder()
{
    public ReplayTimeline BuildReplay(ReplayData replayData, TimeSpan? frameStep = null)
    {
        TimeSpan resolvedFrameStep = frameStep ?? TimeSpan.FromMilliseconds(250);

        if (resolvedFrameStep <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Can't build a replay with a 0 or negative frameStep value.");
        }

        DateTimeOffset sessionStart = replayData.Session.TimestampStart
            ?? throw new InvalidOperationException("Can't build a replay when the session start time is missing.");

        DateTimeOffset sessionEnd = replayData.Session.TimestampEnd
            ?? throw new InvalidOperationException("Can't build a replay when the session end time is missing.");

        if (sessionEnd < sessionStart)
        {
            throw new InvalidOperationException("Can't build a replay when the session end time is before the session start time.");
        }

        TimeSpan sessionLength = sessionEnd - sessionStart;

        int frameCount = checked((int)CalculateFrameCount(sessionLength, resolvedFrameStep));
        int driverCount = replayData.Drivers.Count;
        DriverReplayState[] replayStates = new DriverReplayState[checked(frameCount * driverCount)];

        IReadOnlyDictionary<DriverNumber, DriverDataCursor> cursorsByDriver = BuildDriverCursors(replayData);

        for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
        {
            TimeSpan currentFrameOffset = GetFrameOffset(frameIndex, resolvedFrameStep, sessionLength);
            DateTimeOffset currentFrameTimestamp = sessionStart + currentFrameOffset;

            for (int driverIndex = 0; driverIndex < replayData.Drivers.Count; driverIndex++)
            {
                OpenF1Driver driver = replayData.Drivers[driverIndex];
                DriverNumber driverNumber = driver.DriverNumber;
                int stateIndex = (frameIndex * driverCount) + driverIndex;

                if (!cursorsByDriver.TryGetValue(driverNumber, out DriverDataCursor? cursors))
                {
                    replayStates[stateIndex] = new DriverReplayState(driverNumber, null, null, null, null, null, null, null);
                    continue;
                }

                OpenF1PositionUpdate? currentPosition = cursors.Positions.AdvanceTo(currentFrameTimestamp);
                OpenF1Lap? currentLap = cursors.Laps.AdvanceTo(currentFrameTimestamp);
                cursors.Locations.AdvanceTo(currentFrameTimestamp);
                cursors.CarTelementry.AdvanceTo(currentFrameTimestamp);
                cursors.Intervals.AdvanceTo(currentFrameTimestamp);

                OpenF1Location? currentLocation = InterpolateLocation(cursors.Locations, currentFrameTimestamp);
                OpenF1CarTelemetrySample? currentTelemetry = InterpolateTelemetry(cursors.CarTelementry, currentFrameTimestamp);
                OpenF1IntervalSample? currentInterval = InterpolateInterval(cursors.Intervals, currentFrameTimestamp);

                replayStates[stateIndex] = new DriverReplayState(
                    driverNumber,
                    currentPosition?.Position,
                    currentLocation,
                    currentTelemetry,
                    currentInterval,
                    currentLap,
                    null,
                    null);
            }
        }

        ReplayTimeline timeline = new(sessionStart, sessionLength, resolvedFrameStep, replayData.Drivers.ToArray(), replayStates);

        return timeline;
    }

    private static long CalculateFrameCount(TimeSpan sessionLength, TimeSpan frameStep)
    {
        long frameCount = sessionLength.Ticks / frameStep.Ticks + 1;

        if (sessionLength.Ticks % frameStep.Ticks != 0)
        {
            frameCount++;
        }

        return frameCount;
    }

    private static TimeSpan GetFrameOffset(int frameIndex, TimeSpan frameStep, TimeSpan sessionLength)
    {
        long requestedTicks = checked(frameIndex * frameStep.Ticks);
        long clampedTicks = Math.Min(requestedTicks, sessionLength.Ticks);

        return TimeSpan.FromTicks(clampedTicks);
    }

    private static OpenF1Location? InterpolateLocation(DataCursor<OpenF1Location> cursor, DateTimeOffset timestamp)
    {
        OpenF1Location? previous = cursor.Current;
        OpenF1Location? next = cursor.Next;
        OpenF1Location? source = previous ?? next;

        if (source is null)
        {
            return null;
        }

        double amount = GetInterpolationAmount(timestamp, previous?.TimestampStart, next?.TimestampStart);

        return source with
        {
            TimestampStart = timestamp,
            X = InterpolateInt(previous?.X, next?.X, amount),
            Y = InterpolateInt(previous?.Y, next?.Y, amount),
            Z = InterpolateInt(previous?.Z, next?.Z, amount)
        };
    }

    private static OpenF1CarTelemetrySample? InterpolateTelemetry(DataCursor<OpenF1CarTelemetrySample> cursor, DateTimeOffset timestamp)
    {
        OpenF1CarTelemetrySample? previous = cursor.Current;
        OpenF1CarTelemetrySample? next = cursor.Next;
        OpenF1CarTelemetrySample? source = previous ?? next;

        if (source is null)
        {
            return null;
        }

        double amount = GetInterpolationAmount(timestamp, previous?.Timestamp, next?.Timestamp);

        return source with
        {
            Timestamp = timestamp,
            Throttle = InterpolateInt(previous?.Throttle, next?.Throttle, amount),
            Brake = InterpolateInt(previous?.Brake, next?.Brake, amount),
            Speed = InterpolateInt(previous?.Speed, next?.Speed, amount),
            Rpm = InterpolateInt(previous?.Rpm, next?.Rpm, amount),
            Gear = previous?.Gear ?? next?.Gear,
            Drs = previous?.Drs ?? next?.Drs
        };
    }

    private static OpenF1IntervalSample? InterpolateInterval(DataCursor<OpenF1IntervalSample> cursor, DateTimeOffset timestamp)
    {
        OpenF1IntervalSample? previous = cursor.Current;
        OpenF1IntervalSample? next = cursor.Next;
        OpenF1IntervalSample? source = previous ?? next;

        if (source is null)
        {
            return null;
        }

        double amount = GetInterpolationAmount(timestamp, previous?.Timestamp, next?.Timestamp);

        return source with
        {
            Timestamp = timestamp,
            GapToLeader = InterpolateTimingGap(previous?.GapToLeader, next?.GapToLeader, amount),
            IntervalToAhead = InterpolateTimingGap(previous?.IntervalToAhead, next?.IntervalToAhead, amount)
        };
    }

    private static double GetInterpolationAmount(
        DateTimeOffset timestamp,
        DateTimeOffset? previousTimestamp,
        DateTimeOffset? nextTimestamp)
    {
        if (previousTimestamp is null || nextTimestamp is null)
        {
            return 0;
        }

        TimeSpan sampleWindow = nextTimestamp.Value - previousTimestamp.Value;

        if (sampleWindow <= TimeSpan.Zero)
        {
            return 0;
        }

        double amount = (timestamp - previousTimestamp.Value).TotalMilliseconds / sampleWindow.TotalMilliseconds;

        return Math.Clamp(amount, 0, 1);
    }

    private static int? InterpolateInt(int? previous, int? next, double amount)
    {
        if (previous.HasValue && next.HasValue)
        {
            return (int)Math.Round(previous.Value + (next.Value - previous.Value) * amount);
        }

        return previous ?? next;
    }

    private static TimingGap? InterpolateTimingGap(TimingGap? previous, TimingGap? next, double amount)
    {
        if (previous?.Seconds is double previousSeconds && next?.Seconds is double nextSeconds)
        {
            double interpolatedSeconds = previousSeconds + (nextSeconds - previousSeconds) * amount;
            return new TimingGap(interpolatedSeconds, null);
        }

        return previous ?? next;
    }

    private static IReadOnlyDictionary<DriverNumber, DriverDataCursor> BuildDriverCursors(ReplayData replayData)
    {
        Dictionary<DriverNumber, List<OpenF1Location>> locationsByDriver = replayData.Locations
            .GroupBy(location => location.DriverNumber)
            .ToDictionary(group => group.Key, group => group.ToList()
        );

        Dictionary<DriverNumber, List<OpenF1PositionUpdate>> positionsByDriver = replayData.Positions
        .GroupBy(position => position.DriverNumber)
        .ToDictionary(group => group.Key, group => group.ToList());

        Dictionary<DriverNumber, List<OpenF1CarTelemetrySample>> telemetryByDriver = replayData.CarTelementry
            .GroupBy(sample => sample.DriverNumber)
            .ToDictionary(group => group.Key, group => group.ToList());

        Dictionary<DriverNumber, List<OpenF1IntervalSample>> intervalsByDriver = replayData.Intervals
            .GroupBy(interval => interval.DriverNumber)
            .ToDictionary(group => group.Key, group => group.ToList());

        Dictionary<DriverNumber, List<OpenF1Lap>> lapsByDriver = replayData.Laps
            .GroupBy(lap => lap.DriverNumber)
            .ToDictionary(group => group.Key, group => group.ToList());

        Dictionary<DriverNumber, List<OpenF1RaceControlMessage>> raceControlByDriver = replayData.RaceControl
            .Where(message => message.DriverNumber is not null)
            .GroupBy(message => message.DriverNumber!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());

        Dictionary<DriverNumber, DriverDataCursor> cursorsByDriver = new();

        static IReadOnlyList<T> GetDriverData<T>(IReadOnlyDictionary<DriverNumber, List<T>> dataByDriver, DriverNumber driverNumber)
            => dataByDriver.TryGetValue(driverNumber, out List<T>? data) ? data : [];

        foreach (OpenF1Driver driver in replayData.Drivers)
        {
            DriverNumber driverNumber = driver.DriverNumber;

            DataCursor<OpenF1Location> locationData = new(
            GetDriverData(locationsByDriver, driverNumber),
            location => location.TimestampStart);

            DataCursor<OpenF1PositionUpdate> positionUpdates = new(
                GetDriverData(positionsByDriver, driverNumber),
                position => position.Timestamp);

            DataCursor<OpenF1CarTelemetrySample> carTelemetrySamples = new(
                GetDriverData(telemetryByDriver, driverNumber),
                sample => sample.Timestamp);

            DataCursor<OpenF1IntervalSample> intervals = new(
                GetDriverData(intervalsByDriver, driverNumber),
                interval => interval.Timestamp);

            DataCursor<OpenF1Lap> laps = new(
                GetDriverData(lapsByDriver, driverNumber),
                lap => lap.TimestampStart);

            DataCursor<OpenF1RaceControlMessage> raceControlMessages = new(
                GetDriverData(raceControlByDriver, driverNumber),
                message => message.Timestamp);

            cursorsByDriver[driverNumber] = new DriverDataCursor(
                locationData,
                positionUpdates,
                carTelemetrySamples,
                intervals,
                laps,
                raceControlMessages);
        }

        return cursorsByDriver;
    }
}
