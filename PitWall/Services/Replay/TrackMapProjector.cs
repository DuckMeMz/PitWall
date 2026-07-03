using PitWall.Models;
using PitWall.Models.OpenF1Api;

namespace PitWall.Services;

public class TrackMapProjector
{
    public const double DefaultViewportWidth = 1200;
    public const double DefaultViewportHeight = 360;
    public const double DefaultPadding = 34;

    private readonly int _minX;
    private readonly int _maxX;
    private readonly int _minY;
    private readonly int _maxY;
    private readonly double _scale;
    private readonly double _left;
    private readonly double _top;

    private TrackMapProjector()
    {
    }

    private TrackMapProjector(
        int minX,
        int maxX,
        int minY,
        int maxY,
        double viewportWidth,
        double viewportHeight,
        double padding)
    {
        _minX = minX;
        _maxX = maxX;
        _minY = minY;
        _maxY = maxY;

        double sourceWidth = maxX - minX;
        double sourceHeight = maxY - minY;
        double availableWidth = viewportWidth - (padding * 2);
        double availableHeight = viewportHeight - (padding * 2);

        _scale = Math.Min(
            availableWidth / sourceWidth,
            availableHeight / sourceHeight);

        double projectedWidth = sourceWidth * _scale;
        double projectedHeight = sourceHeight * _scale;
        _left = (viewportWidth - projectedWidth) / 2;
        _top = (viewportHeight - projectedHeight) / 2;
        HasBounds = true;
    }

    public static TrackMapProjector Empty { get; } = new();

    public bool HasBounds { get; }

    public static TrackMapProjector FromLocations(
        IEnumerable<OpenF1Location> locations,
        double viewportWidth = DefaultViewportWidth,
        double viewportHeight = DefaultViewportHeight,
        double padding = DefaultPadding)
    {
        ArgumentNullException.ThrowIfNull(locations);
        ValidateViewport(viewportWidth, viewportHeight, padding);

        (int X, int Y)[] coordinates = locations
            .Where(location => location.X.HasValue && location.Y.HasValue)
            .Select(location => (location.X!.Value, location.Y!.Value))
            .ToArray();

        if (coordinates.Length == 0)
        {
            return Empty;
        }

        (int minX, int maxX) = GetRange(coordinates.Select(point => point.X));
        (int minY, int maxY) = GetRange(coordinates.Select(point => point.Y));

        EnsureNonZeroRange(ref minX, ref maxX);
        EnsureNonZeroRange(ref minY, ref maxY);

        return new TrackMapProjector(
            minX,
            maxX,
            minY,
            maxY,
            viewportWidth,
            viewportHeight,
            padding);
    }

    public bool TryProject(ReplayLocation? location, out TrackMapPoint point)
    {
        if (location is not ReplayLocation value)
        {
            point = default;
            return false;
        }

        return TryProject(value.X, value.Y, out point);
    }

    public bool TryProject(int? x, int? y, out TrackMapPoint point)
    {
        if (!HasBounds ||
            x is not int sourceX ||
            y is not int sourceY ||
            sourceX < _minX ||
            sourceX > _maxX ||
            sourceY < _minY ||
            sourceY > _maxY)
        {
            point = default;
            return false;
        }

        point = new TrackMapPoint(
            _left + ((sourceX - _minX) * _scale),
            _top + ((_maxY - sourceY) * _scale));
        return true;
    }

    private static (int min, int max) GetRange(IEnumerable<int> values)
    {
        int[] sortedValues = values.Order().ToArray();

        return (sortedValues[0], sortedValues[^1]);
    }

    private static void EnsureNonZeroRange(ref int minimum, ref int maximum)
    {
        if (minimum != maximum)
        {
            return;
        }

        minimum--;
        maximum++;
    }

    private static void ValidateViewport(double width, double height, double padding)
    {
        if (!double.IsFinite(width) ||
            !double.IsFinite(height) ||
            !double.IsFinite(padding) ||
            width <= 0 ||
            height <= 0 ||
            padding < 0 ||
            padding * 2 >= width ||
            padding * 2 >= height)
        {
            throw new ArgumentOutOfRangeException(
                nameof(width),
                "The viewport must have finite positive dimensions and usable space inside its padding.");
        }
    }
}

public readonly record struct TrackMapPoint(double X, double Y);