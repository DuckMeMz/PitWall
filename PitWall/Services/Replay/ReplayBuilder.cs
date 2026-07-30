using PitWall.Models;
using PitWall.Models.OpenF1Api;

namespace PitWall.Services;

public class ReplayBuilder()
{
    public ReplayTimeline BuildInitialTimeline(InitialReplayData replayData)
    {
        ArgumentNullException.ThrowIfNull(replayData);

        DateTimeOffset sessionStart = replayData.Session.TimestampStart
            ?? throw new InvalidOperationException("Can't build a replay when the session start time is missing.");

        DateTimeOffset sessionEnd = replayData.Session.TimestampEnd
            ?? throw new InvalidOperationException("Can't build a replay when the session end time is missing.");

        if (sessionEnd < sessionStart)
        {
            throw new InvalidOperationException("Can't build a replay when the session end time is before the session start time.");
        }

        TimeSpan duration = sessionEnd - sessionStart;

        Dictionary<DriverNumber, List<OpenF1Location>> locationsByDriver = GroupByDriver(replayData.Locations, location => location.DriverNumber);
        Dictionary<DriverNumber, List<OpenF1CarTelemetrySample>> telemetryByDriver = GroupByDriver(replayData.CarTelemetry, sample => sample.DriverNumber);
        Dictionary<DriverNumber, List<OpenF1IntervalSample>> intervalsByDriver = GroupByDriver(replayData.Intervals, sample => sample.DriverNumber);
        Dictionary<DriverNumber, List<OpenF1PositionUpdate>> positionsByDriver = GroupByDriver(replayData.Positions, sample => sample.DriverNumber);
        Dictionary<DriverNumber, List<OpenF1Lap>> lapsByDriver = GroupByDriver(replayData.Laps, lap => lap.DriverNumber);

        OpenF1Driver[] drivers = replayData.Drivers.ToArray();
        DriverReplayStream[] streams = new DriverReplayStream[replayData.Drivers.Count];

        for (int i = 0; i < drivers.Length; i++)
        {
            OpenF1Driver driver = drivers[i];
            DriverNumber driverNumber = driver.DriverNumber;

            streams[i] = new DriverReplayStream(
                driverNumber,
                GetDriverData(locationsByDriver, driverNumber),
                GetDriverData(telemetryByDriver, driverNumber),
                GetDriverData(intervalsByDriver, driverNumber),
                GetDriverData(positionsByDriver, driverNumber),
                GetDriverData(lapsByDriver, driverNumber));
        }

        return new ReplayTimeline(
            replayData.Session.SessionKey,
            sessionStart,
            duration,
            replayData.LoadedLength,
            drivers,
            streams);
    }

    public void AppendChunk(ReplayTimeline timeline, ReplayDataChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(timeline);
        ArgumentNullException.ThrowIfNull(chunk);

        if (chunk.Session.SessionKey != timeline.SessionKey)
        {
            throw new InvalidOperationException(
                $"Cannot append session {chunk.Session.SessionKey.Value} to replay {timeline.SessionKey.Value}.");
        }

        DateTimeOffset expectedStart = timeline.SessionStart + timeline.BufferedDuration;

        if (chunk.ChunkTimestampStart != expectedStart)
        {
            throw new InvalidOperationException(
                $"Cannot append a chunk starting at {chunk.ChunkTimestampStart:O}. " +
                $"The next replay chunk must start at {expectedStart:O}.");
        }

        if (chunk.ChunkLength <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(chunk), "A replay chunk must have a positive length.");
        }

        TimeSpan newBufferedDuration = timeline.BufferedDuration + chunk.ChunkLength;

        if (newBufferedDuration > timeline.Duration)
        {
            throw new InvalidOperationException("Cannot append a chunk beyond the end of the replay session.");
        }

        Dictionary<DriverNumber, List<OpenF1Location>> locationsByDriver = GroupByDriver(chunk.Locations, location => location.DriverNumber);
        Dictionary<DriverNumber, List<OpenF1CarTelemetrySample>> telemetryByDriver = GroupByDriver(chunk.CarTelemetry, sample => sample.DriverNumber);
        Dictionary<DriverNumber, List<OpenF1IntervalSample>> intervalsByDriver = GroupByDriver(chunk.Intervals, sample => sample.DriverNumber);
        Dictionary<DriverNumber, List<OpenF1PositionUpdate>> positionsByDriver = GroupByDriver(chunk.Positions, sample => sample.DriverNumber);

        foreach (DriverReplayStream stream in timeline.DriverStreams)
        {
            DriverNumber driverNumber = stream.DriverNumber;

            stream.AppendStream(
                GetDriverData(locationsByDriver, driverNumber),
                GetDriverData(telemetryByDriver, driverNumber),
                GetDriverData(intervalsByDriver, driverNumber),
                GetDriverData(positionsByDriver, driverNumber));
        }

        timeline.UpdateBufferedDuration(newBufferedDuration);
    }
    private static Dictionary<DriverNumber, List<T>> GroupByDriver<T>(IEnumerable<T> samples, Func<T, DriverNumber> getDriverNumber)
    {
        return samples
            .GroupBy(sample => getDriverNumber(sample))
            .ToDictionary(
                group => group.Key,
                group => group.ToList());
    }

    private static List<T> GetDriverData<T>(IReadOnlyDictionary<DriverNumber, List<T>> dataByDriver, DriverNumber driverNumber)
    {
        return dataByDriver.TryGetValue(driverNumber, out List<T>? data) ? data : [];
    }
}
