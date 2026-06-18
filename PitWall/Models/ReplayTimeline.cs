using PitWall.Models.OpenF1Api;

namespace PitWall.Models;

public record ReplayTimeline
{
    public DateTimeOffset SessionStart { get; init; }
    public TimeSpan Duration { get; init; }
    public OpenF1Driver[] Drivers { get; init; }
    public DriverReplayStream[] DriverStreams { get; init; }
    public int DriverCount => Drivers.Length;

    public ReplayTimeline(
        DateTimeOffset sessionStart,
        TimeSpan duration,
        OpenF1Driver[] drivers,
        DriverReplayStream[] driverStreams)
    {
        if (drivers.Length != driverStreams.Length)
        {
            throw new ArgumentException(
                $"Replay timeline must have the same number of drivers and driver streams. " +
                $"Drivers: {drivers.Length}, streams: {driverStreams.Length}.");
        }

        SessionStart = sessionStart;
        Duration = duration;
        Drivers = drivers;
        DriverStreams = driverStreams;
    }

  
    public DriverReplayState GetStateAt(int driverIndex, TimeSpan sessionTime)
    {
        ValidateDriverIndex(driverIndex);

        DateTimeOffset timestamp = SessionStart + ClampSessionTime(sessionTime);

        return DriverStreams[driverIndex].GetStateAt(timestamp);
    }

    public DriverReplayState GetStateAt(int driverIndex, DateTimeOffset timestamp)
    {
        ValidateDriverIndex(driverIndex);

        DateTimeOffset clampedTimestamp = ClampSessionTimestamp(timestamp);

        return DriverStreams[driverIndex].GetStateAt(clampedTimestamp);
    }
    public DriverReplayStream GetDriverStream(int driverIndex)
    {
        ValidateDriverIndex(driverIndex);
        return DriverStreams[driverIndex];
    }

    private void ValidateDriverIndex(int driverIndex)
    {
        if(driverIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(driverIndex), "Driver index can't be less than 0.");
        }

        if(driverIndex >= DriverCount)
        {
            throw new ArgumentOutOfRangeException(
            nameof(driverIndex), $"Driver index {driverIndex} can't be greater than or equal to driver count {DriverCount}.");
        }
    }

    private TimeSpan ClampSessionTime(TimeSpan sessionTime)
    {
        if (sessionTime <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        if(sessionTime >= Duration)
        {
            return Duration;
        }

        return sessionTime;
    }

    private DateTimeOffset ClampSessionTimestamp(DateTimeOffset timestamp)
    {
        DateTimeOffset sessionEnd = SessionStart + Duration;

        if(timestamp <= SessionStart)
        {
            return SessionStart;
        }

        if(timestamp >= sessionEnd)
        {
            return sessionEnd;
        }

        return timestamp;
    }
}