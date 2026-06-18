using PitWall.Models.OpenF1Api;

namespace PitWall.Models;

public record DriverDataCursor(
    DataCursor<OpenF1Location> Locations,
    DataCursor<OpenF1PositionUpdate> Positions,
    DataCursor<OpenF1CarTelemetrySample> CarTelementry,
    DataCursor<OpenF1IntervalSample> Intervals,
    DataCursor<OpenF1Lap> Laps,
    DataCursor<OpenF1RaceControlMessage> RaceControlMessages
    );

public class DataCursor<T> where T : class
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