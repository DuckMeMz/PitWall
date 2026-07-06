using PitWall.Models.OpenF1Api;

namespace PitWall.Models;

public class DriverReplayStream
{
    private readonly DateTimeOffset[] _locationLastMovementTimestamps;

    public DriverNumber DriverNumber { get; }

    public OpenF1Location[] Locations { get; }
    public OpenF1CarTelemetrySample[] Telemetry { get; }
    public OpenF1IntervalSample[] Intervals { get; }
    public OpenF1PositionUpdate[] Positions { get; }
    public OpenF1Lap[] Laps { get; }

    public DriverReplayStream(
        DriverNumber driverNumber,
        OpenF1Location[] locations,
        OpenF1CarTelemetrySample[] telemetry,
        OpenF1IntervalSample[] intervals,
        OpenF1PositionUpdate[] positions,
        OpenF1Lap[] laps)
    {
        DriverNumber = driverNumber;

        Locations = NormalizeSamples(
            locations.Where(location => location.X.HasValue && location.Y.HasValue),
            location => location.Timestamp);
        _locationLastMovementTimestamps = BuildLocationLastMovementTimestamps(Locations);
        Telemetry = NormalizeSamples(telemetry, sample => sample.Timestamp);
        Intervals = NormalizeSamples(intervals, sample => sample.Timestamp);
        Positions = NormalizeSamples(positions, sample => sample.Timestamp);
        Laps = NormalizeSamples(laps, lap => lap.TimestampStart);
    }

    private static T[] NormalizeSamples<T>(
        IEnumerable<T> samples,
        Func<T, DateTimeOffset?> getTimestamp)
        where T : class
    {
        return samples
            .Where(sample => getTimestamp(sample).HasValue)
            .OrderBy(sample => getTimestamp(sample)!.Value)
            .GroupBy(sample => getTimestamp(sample)!.Value)
            .Select(group => group.Last())
            .ToArray();
    }

    private static DateTimeOffset[] BuildLocationLastMovementTimestamps(
        IReadOnlyList<OpenF1Location> locations)
    {
        if (locations.Count == 0)
        {
            return [];
        }

        DateTimeOffset[] timestamps = new DateTimeOffset[locations.Count];
        DateTimeOffset lastMovementTimestamp = locations[0].Timestamp!.Value;
        timestamps[0] = lastMovementTimestamp;

        for (int i = 1; i < locations.Count; i++)
        {
            OpenF1Location previous = locations[i - 1];
            OpenF1Location current = locations[i];

            if (previous.X != current.X || previous.Y != current.Y)
            {
                lastMovementTimestamp = current.Timestamp!.Value;
            }

            timestamps[i] = lastMovementTimestamp;
        }

        return timestamps;
    }

    public DriverReplayState GetStateAt(DateTimeOffset timestamp)
    {
        return new DriverReplayState(
            DriverNumber,
            Position: SampleLatest(Positions, timestamp, position => position.Timestamp)?.Position,
            Location: SampleInterpolatedLocation(
                Locations,
                _locationLastMovementTimestamps,
                timestamp),
            Telemetry: SampleInterpolatedTelemetry(Telemetry, timestamp),
            Interval: SampleInterpolatedInterval(Intervals, timestamp),
            CurrentLap: SampleLatest(Laps, timestamp, lap => lap.TimestampStart),
            CurrentStint: null,
            CurrentPitStop: null);
    }

    private static T? SampleLatest<T>(
        IReadOnlyList<T> samples,
        DateTimeOffset timestamp,
        Func<T, DateTimeOffset?> getTimestamp)
        where T : class
    {
        int index = FindLatestIndexAtOrBefore(samples, timestamp, getTimestamp);

        return index >= 0 ? samples[index] : null;
    }

    private static ReplayLocation? SampleInterpolatedLocation(
        IReadOnlyList<OpenF1Location> samples,
        IReadOnlyList<DateTimeOffset> lastMovementTimestamps,
        DateTimeOffset timestamp)
    {
        int latestIndex = FindLatestIndexAtOrBefore(
            samples,
            timestamp,
            location => location.Timestamp);

        if (latestIndex < 0)
        {
            return null;
        }

        DateTimeOffset lastMovementTimestamp = lastMovementTimestamps[latestIndex];

        if (!TryGetInterpolationSamples(
            samples,
            timestamp,
            location => location.Timestamp,
            TimeSpan.MaxValue,
            out OpenF1Location? previous,
            out OpenF1Location? next,
            out double amount))
        {
            OpenF1Location latest = samples[latestIndex];

            return new ReplayLocation(
                latest.X,
                latest.Y,
                latest.Z,
                latest.Timestamp!.Value,
                lastMovementTimestamp);
        }

        return new ReplayLocation(
            Lerp(previous!.X, next!.X, amount),
            Lerp(previous.Y, next.Y, amount),
            Lerp(previous.Z, next.Z, amount),
            previous.Timestamp!.Value,
            lastMovementTimestamp);
    }

    private static ReplayTelemetry? SampleInterpolatedTelemetry(
        IReadOnlyList<OpenF1CarTelemetrySample> samples,
        DateTimeOffset timestamp)
    {
        if (!TryGetInterpolationSamples(
            samples,
            timestamp,
            sample => sample.Timestamp,
            TimeSpan.FromSeconds(1),
            out OpenF1CarTelemetrySample? previous,
            out OpenF1CarTelemetrySample? next,
            out double amount))
        {
            OpenF1CarTelemetrySample? latest =
                SampleLatest(samples, timestamp, sample => sample.Timestamp);

            return latest is null
                ? null
                : new ReplayTelemetry(
                    latest.Throttle,
                    latest.Brake,
                    latest.Speed,
                    latest.Rpm,
                    latest.Gear,
                    latest.Drs);
        }

        return new ReplayTelemetry(
            Lerp(previous!.Throttle, next!.Throttle, amount),
            Lerp(previous.Brake, next.Brake, amount),
            Lerp(previous.Speed, next.Speed, amount),
            Lerp(previous.Rpm, next.Rpm, amount),
            previous.Gear,
            previous.Drs);
    }

    private static ReplayInterval? SampleInterpolatedInterval(
        IReadOnlyList<OpenF1IntervalSample> samples,
        DateTimeOffset timestamp)
    {
        if (!TryGetInterpolationSamples(
            samples,
            timestamp,
            sample => sample.Timestamp,
            TimeSpan.FromSeconds(7.5),
            out OpenF1IntervalSample? previous,
            out OpenF1IntervalSample? next,
            out double amount))
        {
            OpenF1IntervalSample? latest =
                SampleLatest(samples, timestamp, sample => sample.Timestamp);

            return latest is null
                ? null
                : new ReplayInterval(latest.GapToLeader, latest.IntervalToAhead);
        }

        return new ReplayInterval(
            LerpTimingGap(previous!.GapToLeader, next!.GapToLeader, amount),
            LerpTimingGap(previous.IntervalToAhead, next.IntervalToAhead, amount));
    }

    private static int FindLatestIndexAtOrBefore<T>(
        IReadOnlyList<T> data,
        DateTimeOffset timestamp,
        Func<T, DateTimeOffset?> getTimestamp)
    {
        int low = 0;
        int high = data.Count - 1;
        int bestIndex = -1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;

            if(getTimestamp(data[middle]) <= timestamp)
            {
                bestIndex = middle;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return bestIndex;
    }

    private static bool TryGetInterpolationSamples<T>(
        IReadOnlyList<T> samples,
        DateTimeOffset timestamp,
        Func<T, DateTimeOffset?> getTimestamp,
        TimeSpan maxInterpolationGap,
        out T? previous,
        out T? next,
        out double amount)
        where T : class
    {
        previous = null;
        next = null;
        amount = 0;

        int nextIndex = FindFirstIndexAfter(samples, timestamp, getTimestamp);

        previous = nextIndex > 0 ? samples[nextIndex - 1] : null;
        next = nextIndex < samples.Count ? samples[nextIndex] : null;

        if(previous is null || next is null)
        {
            return false;
        }

        DateTimeOffset? previousTimestamp = getTimestamp(previous);
        DateTimeOffset? nextTimestamp = getTimestamp(next);

        if(previousTimestamp is null || nextTimestamp is null)
        {
            return false;
        }

        TimeSpan gap = nextTimestamp.Value - previousTimestamp.Value;

        if(gap <= TimeSpan.Zero || gap > maxInterpolationGap)
        {
            return false;
        }


        amount = (timestamp - previousTimestamp.Value).TotalMilliseconds / gap.TotalMilliseconds;
        amount = Math.Clamp(amount, 0, 1);

        return true;
    }

    private static int FindFirstIndexAfter<T>(
        IReadOnlyList<T> samples,
        DateTimeOffset timestamp,
        Func<T, DateTimeOffset?> getTimestamp)
    {
        int low = 0;
        int high = samples.Count;

        while (low < high)
        {
            int middle = low + (high - low) / 2;

            if (getTimestamp(samples[middle]) <= timestamp)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    private static int? Lerp(int? previous, int? next, double amount)
    {
        if (previous.HasValue && next.HasValue)
        {
            return (int)Math.Round(previous.Value + ((next.Value - previous.Value) * amount));
        }

        return previous ?? next;
    }
    private static double? Lerp(double? previous, double? next, double amount)
    {
        if (previous.HasValue && next.HasValue)
        {
            return previous.Value + ((next.Value - previous.Value) * amount);
        }

        return previous ?? next;
    }

    private static TimingGap? LerpTimingGap(TimingGap? previous, TimingGap? next, double amount)
    {
        double? seconds = Lerp(previous?.Seconds, next?.Seconds, amount);

        if(seconds.HasValue)
        {
            return new TimingGap(seconds.Value, null);
        }

        return previous ?? next;
    }
}
