using PitWall.Models;
using PitWall.Models.OpenF1Api;

namespace PitWall.Services;

public class ReplayBuilder()
{
    public ReplayTimeline BuildReplay(ReplayData replayData)
    {
        DateTimeOffset sessionStart = replayData.Session.TimestampStart
            ?? throw new InvalidOperationException("Can't build a replay when the session start time is missing.");

        DateTimeOffset sessionEnd = replayData.Session.TimestampEnd
            ?? throw new InvalidOperationException("Can't build a replay when the session end time is missing.");

        if (sessionEnd < sessionStart)
        {
            throw new InvalidOperationException("Can't build a replay when the session end time is before the session start time.");
        }

        TimeSpan duration = sessionEnd - sessionStart;

        Dictionary<DriverNumber, OpenF1Location[]> locationsByDriver = GroupByDriver(replayData.Locations, location => location.DriverNumber);
        Dictionary<DriverNumber, OpenF1CarTelemetrySample[]> telemetryByDriver = GroupByDriver(replayData.CarTelementry, sample => sample.DriverNumber);
        Dictionary<DriverNumber, OpenF1IntervalSample[]> intervalsByDriver = GroupByDriver(replayData.Intervals, sample => sample.DriverNumber);
        Dictionary<DriverNumber, OpenF1PositionUpdate[]> positionsByDriver = GroupByDriver(replayData.Positions, sample => sample.DriverNumber);
        Dictionary<DriverNumber, OpenF1Lap[]> lapsByDriver = GroupByDriver(replayData.Laps, lap => lap.DriverNumber);

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

        return new ReplayTimeline(sessionStart, duration, drivers, streams);
    }

    private static Dictionary<DriverNumber, T[]> GroupByDriver<T>(IEnumerable<T> samples, Func<T, DriverNumber> getDriverNumber)
    {
        return samples
            .GroupBy(sample => getDriverNumber(sample))
            .ToDictionary(
                group => group.Key,
                group => group.ToArray());
    }

    private static T[] GetDriverData<T>(IReadOnlyDictionary<DriverNumber, T[]> dataByDriver, DriverNumber driverNumber)
    {
        return dataByDriver.TryGetValue(driverNumber, out T[]? data) ? data : [];
    }
}
