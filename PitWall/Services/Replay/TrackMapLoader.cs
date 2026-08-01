using System.Diagnostics;
using PitWall.Models;
using PitWall.Models.OpenF1Api;

namespace PitWall.Services;

public class TrackMapLoader
{
    private readonly SessionDataService _sessionDataService;
    private readonly TrackMapSettings _settings;

    public TrackMapLoader(SessionDataService sessionDataService, TrackMapSettings settings)
    {
        _sessionDataService = sessionDataService ?? throw new ArgumentNullException(nameof(sessionDataService));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<TrackMapData> LoadAsync(InitialReplayData replayData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(replayData);

        IReadOnlyList<OpenF1Lap> mapLaps = replayData.Session.SessionType == SessionType.Qualifying
            ? replayData.Laps
            : await _sessionDataService.GetQualifyingLapsAsync(
                replayData.Session.MeetingKey,
                cancellationToken);

        if (mapLaps.Count == 0)
        {
            Debug.WriteLine("No normal qualifying session was found; using the replay session for the track map.");
            mapLaps = replayData.Laps;
        }

        IReadOnlyList<OpenF1Location> locations = await LoadCircuitLocationsAsync(mapLaps, cancellationToken);

        return new TrackMapData(GetTrackTitle(replayData), locations);
    }

    private async Task<IReadOnlyList<OpenF1Location>> LoadCircuitLocationsAsync(IReadOnlyList<OpenF1Lap> laps, CancellationToken cancellationToken)
    {
        IReadOnlyList<OpenF1Location>? fallbackLocations = null;

        foreach (OpenF1Lap lap in laps
            .Where(IsSuitableLap)
            .OrderBy(lap => lap.LapDuration)
            .Take(_settings.CandidateLapCount))
        {
            IReadOnlyList<OpenF1Location> locations =  await _sessionDataService.GetLapLocationsAsync(lap, cancellationToken);

            fallbackLocations ??= locations;

            int coordinateCount = CountUniqueCoordinates(locations);

            if (coordinateCount >= _settings.MinimumDistinctCoordinates)
            {
                Debug.WriteLine(
                    $"Selected track-map lap {lap.LapNumber.Value} for driver {lap.DriverNumber.Value}: " +
                    $"{coordinateCount} distinct coordinates.");

                return locations;
            }

            Debug.WriteLine(
                $"Rejected track-map lap {lap.LapNumber.Value} for driver {lap.DriverNumber.Value}: " +
                $"only {coordinateCount} distinct coordinates.");
        }

        return fallbackLocations ?? throw new InvalidOperationException("No suitable completed lap was found to build the track map.");
    }

    private bool IsSuitableLap(OpenF1Lap lap)
    {
        return lap.TimestampStart.HasValue &&
               lap.LapDuration is double duration &&
               duration >= _settings.MinimumLapDurationSeconds &&
               duration <= _settings.MaximumLapDurationSeconds &&
               lap.IsPitOutLap is not true;
    }

    private static int CountUniqueCoordinates(IReadOnlyList<OpenF1Location> locations)
    {
        return locations
            .Where(location => location.X.HasValue && location.Y.HasValue)
            .Select(location => (location.X!.Value, location.Y!.Value))
            .Distinct()
            .Count();
    }

    private static string GetTrackTitle(InitialReplayData replayData)
    {
        return replayData.Meeting?.CircuitShortName ??
               replayData.Session.CircuitShortName ??
               replayData.Meeting?.MeetingName ??
               replayData.Session.Location ??
               "Track map";
    }
}
