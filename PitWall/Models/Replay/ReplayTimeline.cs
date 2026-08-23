using PitWall.Models.OpenF1Api;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Windows.Media.Media3D;

namespace PitWall.Models;

public class ReplayTimeline
{
    public SessionKey SessionKey { get; init; }
    public DateTimeOffset SessionStart { get; init; }
    public TimeSpan Duration { get; init; }
    public TimeSpan BufferedDuration { get; private set; }
    public OpenF1Driver[] Drivers { get; init; }
    public DriverReplayStream[] DriverStreams { get; init; }
    public int DriverCount => Drivers.Length;

    public IEnumerable<OpenF1Location> AllLocations => DriverStreams.SelectMany(stream => stream.Locations);

    public List<ReplayBufferRange> LoadedRanges = new List<ReplayBufferRange>();

    public ReplayTimeline(
        SessionKey sessionKey,
        DateTimeOffset sessionStart,
        TimeSpan duration,
        TimeSpan bufferedDuration,
        OpenF1Driver[] drivers,
        DriverReplayStream[] driverStreams)
    {
        if (drivers.Length != driverStreams.Length)
        {
            throw new ArgumentException(
                $"Replay timeline must have the same number of drivers and driver streams. " +
                $"Drivers: {drivers.Length}, streams: {driverStreams.Length}.");
        }

        if (bufferedDuration < TimeSpan.Zero || bufferedDuration > duration)
        {
            throw new ArgumentOutOfRangeException(nameof(bufferedDuration), "Buffered duration must be within the replay duration.");
        }

        SessionKey = sessionKey;
        SessionStart = sessionStart;
        Duration = duration;
        BufferedDuration = bufferedDuration;
        Drivers = drivers;
        DriverStreams = driverStreams;

        AddLoadedRange(SessionStart, bufferedDuration);
    }

    public void UpdateBufferedDuration(TimeSpan bufferedDuration)
    {
        if (bufferedDuration < TimeSpan.Zero || bufferedDuration > Duration)
        {
            throw new ArgumentOutOfRangeException(nameof(bufferedDuration), "Buffered duration must be within the replay duration.");
        }

        BufferedDuration = bufferedDuration;
    }


    public DriverReplayState GetStateAt(int driverIndex, TimeSpan sessionTime)
    {
        ValidateDriverIndex(driverIndex);

        DateTimeOffset timestamp = SessionStart + ClampSessionTime(sessionTime);

        return GetStateAt(driverIndex, timestamp);
    }

    public DriverReplayState GetStateAt(int driverIndex, DateTimeOffset timestamp)
    {
        ValidateDriverIndex(driverIndex);

        DateTimeOffset clampedTimestamp = ClampSessionTimestamp(timestamp);
        DateTimeOffset rangeStart = DateTimeOffset.MinValue;

        foreach (ReplayBufferRange bufferedRange in LoadedRanges)
        {
            if (!bufferedRange.Contains(clampedTimestamp))
            {
                continue;
            }

            return DriverStreams[driverIndex].GetStateAt(clampedTimestamp, bufferedRange.Start);
        }

        throw new InvalidOperationException("Cannot get replay state from an unloaded range!");

    }
    public DriverReplayStream GetDriverStream(int driverIndex)
    {
        ValidateDriverIndex(driverIndex);
        return DriverStreams[driverIndex];
    }

    public bool IsTimeBuffered(TimeSpan time)
    {
        DateTimeOffset dateTime = SessionStart + time;
        return IsTimeBuffered(dateTime);
    }

    public bool IsTimeBuffered(DateTimeOffset time)
    {
        foreach(ReplayBufferRange range in LoadedRanges)
        {
            if (range.Contains(time))
            {
                return true;
            }
            else continue;
        }

        return false;
    }

    public bool TryGetBufferedRange(DateTimeOffset time, out ReplayBufferRange? range)
    {
        foreach (ReplayBufferRange bufferedRange in LoadedRanges)
        {
            if (bufferedRange.Contains(time))
            {
                range = bufferedRange;
                return true;
            }
            else continue;
        }

        range = null;
        return false;
    }

    public void AddLoadedRange(DateTimeOffset start, TimeSpan length)
    {
        if(length <= TimeSpan.Zero)
        {
            throw new ArgumentException("Length cannot be equal to or less than zero!");
        }

        if (start < SessionStart)
        {
            throw new ArgumentException("Loaded range can't be before the start of the session!");
        }

        DateTimeOffset end = start + length;

        if (end > SessionStart + Duration)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Loaded range cannot extend past the session.");
        }

        ReplayBufferRange mergedRange = new(start, end);

        List<ReplayBufferRange> rangesToMerge = LoadedRanges
            .Where(bufferedRange => bufferedRange.TouchesOrOverlaps(mergedRange))
            .ToList();

        foreach(ReplayBufferRange range in rangesToMerge)
        {
            mergedRange = new ReplayBufferRange(
                Start: mergedRange.Start < range.Start 
                ? mergedRange.Start 
                : range.Start, 
                End: mergedRange.End > range.End 
                ? mergedRange.End 
                : range.End);

            LoadedRanges.Remove(range);
        }

        LoadedRanges.Add(mergedRange);
        LoadedRanges.Sort((left, right) => left.Start.CompareTo(right.Start));

        if(LoadedRanges.Count > 0 && LoadedRanges[0].Start == SessionStart)
        {
            BufferedDuration = LoadedRanges[0].End - SessionStart;
        }
        else
        {
            BufferedDuration = TimeSpan.Zero;
        }
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