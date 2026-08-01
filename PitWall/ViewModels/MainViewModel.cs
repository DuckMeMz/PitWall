using System.Diagnostics;
using System.Windows.Input;
using PitWall.Commands;
using PitWall.Common;
using PitWall.Models;
using PitWall.Models.OpenF1Api;
using PitWall.Services;

namespace PitWall.ViewModels;

public class MainViewModel : BindableBase, IDisposable
{
    private static readonly TimeSpan AutoBufferThreshold = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan BufferChunkLength = TimeSpan.FromMinutes(2);
    private const double MinimumTrackMapLapDurationSeconds = 45;
    private const double MaximumTrackMapLapDurationSeconds = 200;
    private const int TrackMapCandidateLapCount = 3;
    private const int MinimumTrackMapCoordinateCount = 100;
    private readonly ReplayLoader _replayLoader;
    private readonly SessionDataService _sessionData;
    private ReplayTimeline? _timeline;
    private string _sessionKeyText = "latest";
    private string _statusText = "Enter a session key and load replay data.";
    private bool _isLoading;
    private bool _isSessionFinderOpen;
    private bool _isDisposed;
    private bool _isBuffering;
    private TimeSpan? _lastAutoBufferAttemptedEnd; //Holds the time at the end of the currently buffered replay when a new chunk was requested to stop duplicating requests that fail.

    public MainViewModel(ReplayLoader replayLoader, SessionDataService sessionDataService, SessionFinderViewModel sessionFinderViewModel)
    {
        _replayLoader = replayLoader ?? throw new ArgumentNullException(nameof(replayLoader));
        _sessionData = sessionDataService ?? throw new ArgumentNullException(nameof(replayLoader));

        Playback = new PlaybackViewModel();
        TrackMap = new TrackMapViewModel();
        DriverTable = new DriverTableViewModel();
        Telemetry = new TelemetryViewModel();
        SessionFinder = sessionFinderViewModel ?? throw new ArgumentNullException(nameof(sessionFinderViewModel));

        Playback.PositionChanged += OnPlaybackPositionChanged;
        SessionFinder.SessionSelected += OnSessionSelected;
        DriverTable.SelectedDriverChanged += OnSelectedDriverChanged;

        LoadReplayCommand = new AsyncRelayCommand(
            LoadReplayAsync,
            () => !IsLoading);

        OpenSessionFinderCommand = new RelayCommand(() => IsSessionFinderOpen = true);
        CloseSessionFinderCommand = new RelayCommand(() => IsSessionFinderOpen = false);
    }

    public async Task InitialiseAsync()
    {
        await SessionFinder.InitialiseAsync();
    }

    public ICommand LoadReplayCommand { get; }
    public ICommand OpenSessionFinderCommand { get; }
    public ICommand CloseSessionFinderCommand { get; }

    public PlaybackViewModel Playback { get; }
    public TrackMapViewModel TrackMap { get; }
    public DriverTableViewModel DriverTable { get; }
    public TelemetryViewModel Telemetry { get; }
    public SessionFinderViewModel SessionFinder { get; }

    public bool IsSessionFinderOpen
    {
        get => _isSessionFinderOpen;
        set => SetProperty(ref _isSessionFinderOpen, value);
    }

    public string SessionKeyText
    {
        get => _sessionKeyText;
        set => SetProperty(ref _sessionKeyText, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (!SetProperty(ref _isLoading, value))
            {
                return;
            }

            Playback.IsEnabled = !value;

            if (LoadReplayCommand is AsyncRelayCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        Playback.PositionChanged -= OnPlaybackPositionChanged;
        DriverTable.SelectedDriverChanged -= OnSelectedDriverChanged;
        SessionFinder.SessionSelected -= OnSessionSelected;
        Playback.Dispose();
        Telemetry.Dispose();
    }

    private async Task LoadReplayAsync()
    {
        if (!TryParseSessionKey(
            SessionKeyText,
            out SessionKey sessionKey,
            out string? errorMessage))
        {
            StatusText = errorMessage ?? "Session key is invalid.";
            return;
        }

        await LoadReplayAsync(sessionKey, SessionKeyText.Trim(), CancellationToken.None);
    }

    private async Task LoadReplayAsync(SessionKey sessionKey, string sessionDescription, CancellationToken cancellationToken)
    {
        if (IsLoading)
        {
            return;
        }

        ClearReplay();

        try
        {
            IsLoading = true;
            StatusText = $"Loading OpenF1 data for {sessionDescription}...";

            ReplayLoadResult result = await _replayLoader.LoadInitialAsync(sessionKey);
            await LoadReplay(result, cancellationToken);
            StatusText = BuildLoadedStatus(result);
        }
        catch (Exception exception)
        {
            StatusText = $"Replay load failed: {exception.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnSessionSelected(SessionFinderSession session)
    {
        SessionKeyText = session.SessionKey.Value.ToString();
        IsSessionFinderOpen = false;
        _ = LoadReplayAsync(session.SessionKey, session.SessionName, CancellationToken.None);
    }
    private async Task LoadReplay(ReplayLoadResult result, CancellationToken cancellationToken)
    {
        _timeline = result.Timeline;

        string TrackName =
            result.Data.Meeting!.CircuitShortName ??
            result.Data.Session.CircuitShortName ??
            result.Data.Meeting.MeetingName ??
            result.Data.Session.Location ??
            "Track map";

        IReadOnlyList<OpenF1Lap> mapLaps = result.Data.Session.SessionType == SessionType.Qualifying
            ? result.Data.Laps
            : await _sessionData.GetQualifyingLapsAsync(
                result.Data.Session.MeetingKey,
                cancellationToken);

        if (mapLaps.Count == 0)
        {
            Debug.WriteLine("No normal qualifying session was found; using the replay session for the track map.");
            mapLaps = result.Data.Laps;
        }

        IReadOnlyList<OpenF1Location> mapLocations = await LoadTrackMapLocationsAsync(mapLaps, cancellationToken);

        TrackMap.Init(
            TrackName,
            result.Data.Drivers,
            mapLocations);

        DriverTable.Init(result.Data.Drivers);
        Playback.Load(result.Timeline);
    }

    private async Task<IReadOnlyList<OpenF1Location>> LoadTrackMapLocationsAsync(IReadOnlyList<OpenF1Lap> laps, CancellationToken cancellationToken)
    {
        IReadOnlyList<OpenF1Location>? fallbackLocations = null;

        foreach (OpenF1Lap lap in laps
            .Where(lap =>
                lap.TimestampStart.HasValue &&
                lap.LapDuration is >= MinimumTrackMapLapDurationSeconds and <= MaximumTrackMapLapDurationSeconds &&
                lap.IsPitOutLap is not true)
            .OrderBy(lap => lap.LapDuration)
            .Take(TrackMapCandidateLapCount))
        {
            IReadOnlyList<OpenF1Location> locations = await _sessionData.GetLapLocationsAsync(lap, cancellationToken);

            fallbackLocations ??= locations;

            int coordinateCount = locations
                .Where(location => location.X.HasValue && location.Y.HasValue)
                .Select(location => (location.X!.Value, location.Y!.Value))
                .Distinct()
                .Count();

            if (coordinateCount >= MinimumTrackMapCoordinateCount)
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
    private async Task BufferNextChunkAsync(TimeSpan chunkLength)
    {
        if (!CanBufferNextChunk() || _timeline is not ReplayTimeline timeline)
        {
            return;
        }

        _isBuffering = true;
        StatusText = $"Buffering the next chunk. Size: {chunkLength.TotalMinutes} minute[s]";

        try
        {
            await _replayLoader.LoadNextChunkAsync(timeline, chunkLength);
            Playback.RefreshBufferedDuration();
            Playback.ResumeAfterBuffering();
            StatusText = $"Buffered {timeline.BufferedDuration:hh\\:mm\\:ss} of {timeline.Duration:hh\\:mm\\:ss}.";
        }
        catch (Exception exception)
        {
            StatusText = $"Buffering failed: {exception.Message}";
        }
        finally
        {
            _isBuffering = false;
        }
    }
    private void ClearReplay()
    {
        _timeline = null;
        _lastAutoBufferAttemptedEnd = null;
        Playback.Clear();
        DriverTable.Clear();
        TrackMap.Clear();
        Telemetry.SelectDriver(null);
    }

    private void OnPlaybackPositionChanged(
        object? sender,
        PlaybackPositionChangedEventArgs eventArgs)
    {
        if (_timeline is not ReplayTimeline timeline)
        {
            return;
        }

        DriverTable.Update(
            timeline,
            eventArgs.Position,
            Playback.IsPlaying);
        TrackMap.Update(timeline, eventArgs.Position);
        TryAutoBuffer(timeline, eventArgs.Position);
    }

    private void TryAutoBuffer(ReplayTimeline timeline, TimeSpan playheadPosition)
    {
        if (!Playback.IsPlaying || !CanBufferNextChunk())
        {
            return;
        }

        TimeSpan remainingBufferedTime = timeline.BufferedDuration - playheadPosition;

        if (remainingBufferedTime > AutoBufferThreshold || _lastAutoBufferAttemptedEnd == timeline.BufferedDuration)
        {
            return;
        }

        _lastAutoBufferAttemptedEnd = timeline.BufferedDuration;
        _ = BufferNextChunkAsync(BufferChunkLength);
    }

    private bool CanBufferNextChunk()
    {
        return !IsLoading && !_isBuffering && _timeline is { BufferedDuration: var buffered, Duration: var duration } && buffered < duration;
    }
    private void OnSelectedDriverChanged(object? sender, EventArgs eventArgs)
    {
        ReplayDriverRow? selectedDriver = DriverTable.SelectedDriver;

        Telemetry.SelectDriver(selectedDriver);
        TrackMap.SelectDriver(selectedDriver?.DriverNumber);
    }

    private static string BuildLoadedStatus(ReplayLoadResult result)
    {
        static double ToMb(long bytes) => bytes / 1024.0 / 1024.0;

        long managedBytes = GC.GetTotalMemory(forceFullCollection: false);

        using Process process = Process.GetCurrentProcess();
        process.Refresh();

        ReplayTimeline timeline = result.Timeline;

        return
            $"Loaded {timeline.DriverCount:N0} driver streams in " +
            $"{result.TotalElapsed.TotalSeconds:0.0}s " +
            $"(timeline build {result.BuildElapsed.TotalMilliseconds:N0}ms). " +
            $"Managed heap: {ToMb(managedBytes):0.0} MB, " +
            $"Working set: {ToMb(process.WorkingSet64):0.0} MB, " +
            $"Private memory: {ToMb(process.PrivateMemorySize64):0.0} MB";
    }

    private static bool TryParseSessionKey(
        string text,
        out SessionKey sessionKey,
        out string? errorMessage)
    {
        string trimmed = text.Trim();

        if (trimmed.Equals("latest", StringComparison.OrdinalIgnoreCase))
        {
            sessionKey = SessionKey.Latest;
            errorMessage = null;
            return true;
        }

        if (int.TryParse(trimmed, out int value))
        {
            sessionKey = new SessionKey(value);
            errorMessage = null;
            return true;
        }

        sessionKey = default;
        errorMessage = "Session key must be an integer or latest.";
        return false;
    }
}
