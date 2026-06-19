using PitWall.Models.OpenF1Api;

namespace PitWall.Models;

public class DriverReplayStream
{
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

        Locations = locations
        .Where(location => location.Timestamp is not null)
        .OrderBy(location => location.Timestamp)
        .ToArray();

        Telemetry = telemetry
        .Where(telemetry => telemetry.Timestamp is not null)
        .OrderBy(telemetry => telemetry.Timestamp)
        .ToArray();

        Intervals = intervals
        .Where(position => position.Timestamp is not null)
        .OrderBy(position => position.Timestamp)
        .ToArray();

        Positions = positions
        .Where(position => position.Timestamp is not null)
        .OrderBy(position => position.Timestamp)
        .ToArray();

        Laps = laps
        .Where(laps => laps.TimestampStart is not null)
        .OrderBy(laps => laps.TimestampStart)
        .ToArray();
    }

    public DriverReplayState GetStateAt(DateTimeOffset timestamp)
    {
        return new DriverReplayState(
            DriverNumber,
            Position: SampleLatest(Positions, timestamp, position => position.Timestamp)?.Position,
            Location: SampleLightlyInterpolatedLocation(Locations, timestamp),
            Telemetry: SampleInterpolatedTelemetry(Telemetry, timestamp),
            Interval: SampleInterpolatedInterval(Intervals, timestamp),
            CurrentLap: SampleLatest(Laps, timestamp, lap => lap.TimestampStart),
            CurrentStint: null,
            CurrentPitStop: null);
    }

    private static T? SampleLatest<T>(IReadOnlyList<T> samples , DateTimeOffset timestamp, Func<T, DateTimeOffset?> getTimestamp) where T : class
    {
        int index = FindLatestIndexAtOrBefore(samples, timestamp, getTimestamp);

        return index >= 0 ? samples[index] : null;
    }

    private static OpenF1Location? SampleLightlyInterpolatedLocation(IReadOnlyList<OpenF1Location> samples, DateTimeOffset timestamp)
    {
        if (!TryGetInterpolationSamples(
            samples,
            timestamp,
            location => location.Timestamp,
            TimeSpan.FromMilliseconds(500),
            out OpenF1Location? previous,
            out OpenF1Location? next,
            out double amount))
        {
            return SampleLatest(samples, timestamp, location => location.Timestamp);
        }

        return previous! with
        {
            Timestamp = timestamp,
            X = Lerp(previous.X, next!.X, amount),
            Y = Lerp(previous.Y, next.Y, amount),
            Z = Lerp(previous.Z, next.Z, amount)
        };
    }

    private static OpenF1CarTelemetrySample? SampleInterpolatedTelemetry(IReadOnlyList<OpenF1CarTelemetrySample> samples, DateTimeOffset timestamp)
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
            return SampleLatest(samples, timestamp, sample => sample.Timestamp);
        }

        return previous! with
        {
            Timestamp = timestamp,
            Speed = Lerp(previous.Speed, next!.Speed, amount),
            Rpm = Lerp(previous.Rpm, next.Rpm, amount),
            Throttle = Lerp(previous.Throttle, next.Throttle, amount),
            Brake = Lerp(previous.Brake, next.Brake, amount),
            Gear = previous.Gear,
            Drs = previous.Drs
        };
    }
    private static OpenF1IntervalSample? SampleInterpolatedInterval(IReadOnlyList<OpenF1IntervalSample> samples, DateTimeOffset timestamp)
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
            return SampleLatest(samples, timestamp, sample => sample.Timestamp);
        }

        return previous! with
        {
            Timestamp = timestamp,
            GapToLeader = LerpTimingGap(previous.GapToLeader, next!.GapToLeader, amount),
            IntervalToAhead = LerpTimingGap(previous.IntervalToAhead, next!.IntervalToAhead, amount)
        };
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
