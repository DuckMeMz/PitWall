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

    private readonly Dictionary<DriverNumber, ReplayMapMarker> _markersByDriver = new();
    private TrackMapProjector _projector = TrackMapProjector.Empty;
    private string _trackTitle = "No session loaded";
    private PointCollection _trackPath = new();

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

    public void Load(ReplayData replayData)
    {
        ArgumentNullException.ThrowIfNull(replayData);

        Clear();
        _projector = TrackMapProjector.FromLocations(replayData.Locations);
        TrackTitle = GetTrackTitle(replayData);
        TrackMapPath = BuildTrackPath(replayData);

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

    private PointCollection BuildTrackPath(ReplayData replayData)
    {
        PointCollection path = new();

        foreach (OpenF1Location location in FindValidLapLocations(replayData))
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

    private static string GetTrackTitle(ReplayData replayData)
    {
        return
            replayData.Meeting?.CircuitShortName ??
            replayData.Session.CircuitShortName ??
            replayData.Meeting?.MeetingName ??
            replayData.Session.Location ??
            "Track map";
    }

    private static IReadOnlyList<OpenF1Location> FindValidLapLocations(
        ReplayData replayData)
    {
        Dictionary<DriverNumber, OpenF1Location[]> locationsByDriver =
            replayData.Locations
                .Where(location =>
                    location.Timestamp.HasValue &&
                    location.X.HasValue &&
                    location.Y.HasValue)
                .GroupBy(location => location.DriverNumber)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderBy(location => location.Timestamp)
                        .ToArray());

        foreach (OpenF1Lap lap in replayData.Laps
            .Where(lap =>
                lap.TimestampStart.HasValue &&
                lap.LapDuration is > 0 &&
                lap.IsPitOutLap is not true)
            .OrderBy(lap => lap.LapDuration))
        {
            if (!locationsByDriver.TryGetValue(
                lap.DriverNumber,
                out OpenF1Location[]? driverLocations))
            {
                continue;
            }

            DateTimeOffset lapStart = lap.TimestampStart!.Value;
            DateTimeOffset lapEnd = lapStart + TimeSpan.FromSeconds(lap.LapDuration!.Value);
            OpenF1Location[] lapLocations = driverLocations
                .Where(location =>
                    location.Timestamp >= lapStart &&
                    location.Timestamp <= lapEnd)
                .ToArray();

            if (lapLocations.Length >= 20)
            {
                return lapLocations;
            }
        }

        return locationsByDriver.Values
            .OrderByDescending(locations => locations.Length)
            .FirstOrDefault() ?? [];
    }
}
