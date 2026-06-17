using Microsoft.AspNetCore.WebUtilities;
using PitWall.Models;
using PitWall.Models.OpenF1Api;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace PitWall.Services;

public class ReplayFrameBuilder()
{
    private ReplayData? ReplayData { get; set; }

    public void SetReplayData(ReplayData replayData)
    {
        ReplayData = replayData;
    }

    public IReadOnlyList<ReplayFrame> BuildFrames(ReplayData? replayData = null, TimeSpan? frameStep = null)
    {
        replayData ??= ReplayData ??
            throw new InvalidOperationException("Can't build a replay without any replay data.");

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
        long frameCount = CalculateFrameCount(sessionLength, resolvedFrameStep);

        Debug.WriteLine($"Session Length: {sessionLength}.");
        Debug.WriteLine($"There are {frameCount} possible frames in {sessionLength} with a framestep of {resolvedFrameStep}.");

        IReadOnlyDictionary<DriverNumber, DriverDataCursor> cursorsByDriver = BuildDriverCursors(replayData);
        List<ReplayFrame> replaySession = new((int)frameCount);

        for (int i = 0; i < frameCount; i++)
        {
            TimeSpan currentFrameOffset = GetFrameOffset(i, resolvedFrameStep, sessionLength);
            DateTimeOffset currentFrameTimestamp = sessionStart + currentFrameOffset;
            Dictionary<DriverNumber, DriverReplayState> driverStates = new(replayData.Drivers.Count);

            foreach (OpenF1Driver driver in replayData.Drivers)
            {
                DriverNumber driverNumber = driver.DriverNumber;

                if(!cursorsByDriver.TryGetValue(driverNumber, out DriverDataCursor? cursors))
                {
                    driverStates[driverNumber] = new DriverReplayState(driverNumber, null, null, null, null, null, null, null);
                    continue;
                }

                OpenF1Location? currentLocation = cursors.Locations.AdvanceTo(currentFrameTimestamp);
                OpenF1PositionUpdate? currentPosition = cursors.Positions.AdvanceTo(currentFrameTimestamp);
                OpenF1CarTelemetrySample? currentTelemetry = cursors.CarTelementry.AdvanceTo(currentFrameTimestamp);
                OpenF1IntervalSample? currentInterval = cursors.Intervals.AdvanceTo(currentFrameTimestamp);
                OpenF1Lap? currentLap = cursors.Laps.AdvanceTo(currentFrameTimestamp);

                driverStates[driverNumber] = new DriverReplayState(
                    driverNumber,
                    currentPosition?.Position,
                    currentLocation,
                    currentTelemetry,
                    currentInterval,
                    currentLap,
                    null,
                    null);
            }

            replaySession.Add(new ReplayFrame(currentFrameTimestamp, currentFrameOffset, driverStates));
        }

        return replaySession;
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

    private static IReadOnlyDictionary<DriverNumber, DriverDataCursor> BuildDriverCursors(ReplayData replayData)
    {
        Dictionary<DriverNumber, DriverDataCursor> cursorsByDriver = new();
        foreach(OpenF1Driver driver in replayData.Drivers)
        {
            DataCursor<OpenF1Location> locationData = new(
                replayData.Locations.Where(location => location.DriverNumber == driver.DriverNumber),
                location => location.TimestampStart);

            DataCursor<OpenF1PositionUpdate> positionUpdates = new(
                replayData.Positions.Where(positionUpdate => positionUpdate.DriverNumber == driver.DriverNumber),
                positionUpdate => positionUpdate.Timestamp);

            DataCursor<OpenF1CarTelemetrySample> carTelementrySamples = new(
                replayData.CarTelementry.Where(carTelementrySample => carTelementrySample.DriverNumber == driver.DriverNumber),
                carTelementrySample => carTelementrySample.Timestamp);

            DataCursor<OpenF1IntervalSample> intervals = new(
                replayData.Intervals.Where(intervalSample => intervalSample.DriverNumber == driver.DriverNumber),
                intervalSample => intervalSample.Timestamp);

            DataCursor<OpenF1Lap> laps = new(
                replayData.Laps.Where(lap => lap.DriverNumber == driver.DriverNumber),
                lap => lap.TimestampStart);

            DataCursor<OpenF1RaceControlMessage> raceControlMessage = new(
                replayData.RaceControl.Where(message => message.DriverNumber == driver.DriverNumber),
                message => message.Timestamp);

             cursorsByDriver[driver.DriverNumber] = new DriverDataCursor(locationData, positionUpdates, carTelementrySamples, intervals, laps, raceControlMessage);
        }

        return cursorsByDriver;
    }

    private record DriverDataCursor(
        DataCursor<OpenF1Location> Locations,
        DataCursor<OpenF1PositionUpdate> Positions,
        DataCursor<OpenF1CarTelemetrySample> CarTelementry,
        DataCursor<OpenF1IntervalSample> Intervals,
        DataCursor<OpenF1Lap> Laps,
        DataCursor<OpenF1RaceControlMessage> RaceControlMessages);

    private class DataCursor<T> where T : class
    {
        private readonly IReadOnlyList<T> _data;
        private readonly Func<T, DateTimeOffset?> _getTimestamp;
        private int _nextIndex;

        public DataCursor(IEnumerable<T> data, Func<T, DateTimeOffset?> getTimestamp)
        {
            _data = data
                .Where(dataPoint => getTimestamp(dataPoint) is not null)
                .OrderBy(dataPoint => getTimestamp(dataPoint))
                .ToList();

            _getTimestamp = getTimestamp;
        }

        public T? Current { get; set; }

        public T? Next => _nextIndex < _data.Count ? _data[_nextIndex] : null;

        public T? AdvanceTo(DateTimeOffset timestamp)
        {
            while (_nextIndex < _data.Count)
            {
                T nextDataPoint = _data[_nextIndex];
                DateTimeOffset? nextTimestamp = _getTimestamp(nextDataPoint);

                if (nextTimestamp is null)
                {
                    _nextIndex++;
                    continue;
                }

                if (nextTimestamp.Value > timestamp)
                {
                    break;
                }

                Current = nextDataPoint;
                _nextIndex++;
            }

            return Current;
        }

    }
}
