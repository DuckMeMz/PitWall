using System.Collections.ObjectModel;
using System.Windows.Media;
using PitWall.Common;
using PitWall.Models;
using PitWall.Models.OpenF1Api;
using PitWall.Services;

namespace PitWall.ViewModels;

public class TrackMapViewModel : BindableBase
{
    private static readonly TimeSpan MarkerStaleAfter = TimeSpan.FromSeconds(30);

    private readonly TrackMapLoader _trackMapLoader;
    private readonly Dictionary<DriverNumber, ReplayMapMarker> _markersByDriver = new();
    private TrackMapProjector _projector = TrackMapProjector.Empty;
    private string _trackTitle = "No session loaded";
    private PointCollection _trackPath = new();

    public TrackMapViewModel(TrackMapLoader trackMapLoader)
    {
        _trackMapLoader = trackMapLoader ?? throw new ArgumentNullException(nameof(trackMapLoader));
    }

    public ObservableCollection<ReplayMapMarker> MapMarkers { get; } = new();

    public string TrackTitle
    {
        get => _trackTitle;
        private set => SetProperty(ref _trackTitle, value);
    }

    public PointCollection TrackMapPath
    {
        get => _trackPath;
        private set => SetProperty(ref _trackPath, value);
    }

    public async Task InitialiseAsync(InitialReplayData replayData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(replayData);

        Clear();
        TrackMapData mapData = await _trackMapLoader.LoadAsync(replayData, cancellationToken);

        _projector = TrackMapProjector.FromLocations(mapData.CircuitLocations);
        TrackTitle = mapData.Title;
        TrackMapPath = BuildTrackPath(mapData.CircuitLocations);

        foreach (OpenF1Driver driver in replayData.Drivers
            .OrderBy(driver => driver.DriverNumber.Value))
        {
            ReplayMapMarker marker = new(driver);
            MapMarkers.Add(marker);
            _markersByDriver[driver.DriverNumber] = marker;
        }
    }

    public void Update(ReplayTimeline timeline, TimeSpan targetTime)
    {
        ArgumentNullException.ThrowIfNull(timeline);

        DateTimeOffset playheadTimestamp = timeline.SessionStart + targetTime;

        for (int driverIndex = 0; driverIndex < timeline.DriverCount; driverIndex++)
        {
            DriverReplayState state = timeline.GetStateAt(driverIndex, targetTime);

            if (!_markersByDriver.TryGetValue(
                state.DriverNumber,
                out ReplayMapMarker? marker))
            {
                continue;
            }

            TrackMapPoint? position = _projector.TryProject(
                state.Location,
                out TrackMapPoint projectedPosition)
                    ? projectedPosition
                    : null;

            bool isStale =
                state.Location is ReplayLocation location &&
                playheadTimestamp - location.LastMovementTimestamp >
                    MarkerStaleAfter;

            marker.Apply(position, isStale);
        }
    }

    public void SelectDriver(DriverNumber? driverNumber)
    {
        foreach (ReplayMapMarker marker in MapMarkers)
        {
            marker.IsSelected = driverNumber.HasValue && marker.DriverNumber == driverNumber.Value;
        }
    }

    public void Clear()
    {
        _projector = TrackMapProjector.Empty;
        TrackTitle = "No session loaded";
        TrackMapPath = new PointCollection();
        MapMarkers.Clear();
        _markersByDriver.Clear();
    }

    private PointCollection BuildTrackPath(IReadOnlyList<OpenF1Location> circuitLocations)
    {
        PointCollection path = new();

        foreach (OpenF1Location location in circuitLocations
            .Where(location =>
                location.Timestamp.HasValue &&
                location.X.HasValue &&
                location.Y.HasValue)
            .OrderBy(location => location.Timestamp))
        {
            if (!_projector.TryProject(
                location.X,
                location.Y,
                out TrackMapPoint projectedPoint))
            {
                continue;
            }

            System.Windows.Point point = new(projectedPoint.X, projectedPoint.Y);

            if (path.Count == 0 || !path[^1].Equals(point))
            {
                path.Add(point);
            }
        }

        if (path.Count > 1 && !path[^1].Equals(path[0]))
        {
            path.Add(path[0]);
        }

        path.Freeze();
        return path;
    }
}
