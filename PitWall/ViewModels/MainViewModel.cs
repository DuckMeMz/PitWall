using System.Diagnostics;
using System.Net.Http;
using System.Windows.Input;
using PitWall.Commands;
using PitWall.Common;
using PitWall.Models;
using PitWall.Services;

namespace PitWall.ViewModels;

public class MainViewModel : BindableBase, IDisposable
{
    private readonly ReplayLoader _replayLoader;
    private ReplayTimeline? _timeline;
    private string _sessionKeyText = "latest";
    private string _statusText = "Enter a session key and load replay data.";
    private bool _isLoading;
    private bool _isDisposed;

    public MainViewModel()
        : this(CreateDefaultSessionDataService(), new ReplayBuilder())
    {
    }

    public MainViewModel(
        SessionDataService sessionDataService,
        ReplayBuilder replayBuilder)
        : this(new ReplayLoader(sessionDataService, replayBuilder))
    {
    }

    public MainViewModel(ReplayLoader replayLoader)
    {
        _replayLoader = replayLoader ?? throw new ArgumentNullException(nameof(replayLoader));

        Playback = new PlaybackViewModel();
        TrackMap = new TrackMapViewModel();
        DriverTable = new DriverTableViewModel();
        Telemetry = new TelemetryViewModel();

        Playback.PositionChanged += OnPlaybackPositionChanged;
        DriverTable.SelectedDriverChanged += OnSelectedDriverChanged;

        LoadReplayCommand = new AsyncRelayCommand(
            LoadReplayAsync,
            () => !IsLoading);
    }

    public ICommand LoadReplayCommand { get; }

    public PlaybackViewModel Playback { get; }
    public TrackMapViewModel TrackMap { get; }
    public DriverTableViewModel DriverTable { get; }
    public TelemetryViewModel Telemetry { get; }

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

        ClearReplay();

        try
        {
            IsLoading = true;
            StatusText = $"Loading OpenF1 data for session {SessionKeyText.Trim()}...";

            ReplayLoadResult result = await _replayLoader.LoadAsync(sessionKey);
            LoadReplay(result);
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

    private void LoadReplay(ReplayLoadResult result)
    {
        _timeline = result.Timeline;
        TrackMap.Load(result.Data);
        DriverTable.Load(result.Data.Drivers);
        Playback.Load(result.Timeline);
    }

    private void ClearReplay()
    {
        _timeline = null;
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

        ReplayData replayData = result.Data;
        ReplayTimeline timeline = result.Timeline;

        return
            $"Loaded {timeline.DriverCount:N0} driver streams in " +
            $"{result.TotalElapsed.TotalSeconds:0.0}s " +
            $"(timeline build {result.BuildElapsed.TotalMilliseconds:N0}ms). " +
            $"Streams: {replayData.Locations.Count:N0} locations, " +
            $"{replayData.Positions.Count:N0} positions, " +
            $"{replayData.CarTelemetry.Count:N0} telemetry, " +
            $"{replayData.Intervals.Count:N0} intervals, " +
            $"{replayData.Laps.Count:N0} laps. " +
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

    private static SessionDataService CreateDefaultSessionDataService()
    {
        HttpClient httpClient = new();
        OpenF1APIService apiService = new(httpClient);
        OpenF1Client client = new(apiService);
        SessionCatalogService sessionCatalog = new(client);

        return new SessionDataService(client, sessionCatalog);
    }
}
