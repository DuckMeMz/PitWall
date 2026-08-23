using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media.Animation;
using PitWall.Commands;
using PitWall.Common;
using PitWall.Models;
using PitWall.Services;

namespace PitWall.ViewModels;

public class MainViewModel : BindableBase, IDisposable
{
    private readonly ReplayLoader _replayLoader;
    private readonly BufferController _bufferController;
    private ReplayTimeline? _timeline;
    private CancellationTokenSource? _replayCancellationTokenSource;
    private string _sessionKeyText = "latest";
    private string _statusText = "Enter a session key and load replay data.";
    private bool _isLoading;
    private bool _isSessionFinderOpen;
    private bool _isDisposed;

    public MainViewModel(
        ReplayLoader replayLoader,
        BufferController bufferCoordinator,
        TrackMapViewModel trackMapViewModel,
        SessionFinderViewModel sessionFinderViewModel)
    {
        _replayLoader = replayLoader ?? throw new ArgumentNullException(nameof(replayLoader));
        _bufferController = bufferCoordinator ?? throw new ArgumentNullException(nameof(bufferCoordinator));

        Playback = new PlaybackViewModel();
        TrackMap = trackMapViewModel ?? throw new ArgumentNullException(nameof(trackMapViewModel));
        DriverTable = new DriverTableViewModel();
        Telemetry = new TelemetryViewModel();
        SessionFinder = sessionFinderViewModel ?? throw new ArgumentNullException(nameof(sessionFinderViewModel));

        Playback.PositionChanged += OnPlaybackPositionChanged;
        SessionFinder.SessionSelected += OnSessionSelected;
        DriverTable.SelectedDriverChanged += OnSelectedDriverChanged;
        _bufferController.BufferingCompleted += OnBufferingCompleted;

        LoadReplayCommand = new AsyncRelayCommand(
            LoadReplayFromSessionKeyFieldAsync,
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
        CancelReplayLoad();
        _bufferController.ClearSession();
        Playback.PositionChanged -= OnPlaybackPositionChanged;
        SessionFinder.SessionSelected -= OnSessionSelected;
        DriverTable.SelectedDriverChanged -= OnSelectedDriverChanged;
        _bufferController.BufferingCompleted -= OnBufferingCompleted;
        Playback.Dispose();
        Telemetry.Dispose();
    }

    private async Task LoadReplayFromSessionKeyFieldAsync()
    {
        if (!TryParseSessionKey(SessionKeyText, out SessionKey sessionKey, out string? errorMessage))
        {
            StatusText = errorMessage ?? "Session key is invalid.";
            return;
        }

        await LoadReplayAsync(sessionKey, SessionKeyText.Trim());
    }

    private async Task LoadReplayAsync(SessionKey sessionKey, string sessionDescription)
    {
        ClearReplay();
        CancellationTokenSource cancellationTokenSource = new();
        _replayCancellationTokenSource = cancellationTokenSource;

        try
        {
            IsLoading = true;
            StatusText = $"Loading OpenF1 data for {sessionDescription}...";

            ReplayLoadResult result = await _replayLoader.LoadInitialAsync(sessionKey, cancellationTokenSource.Token);

            if (!IsCurrentReplayLoad(cancellationTokenSource))
            {
                return;
            }

            await InitialiseLoadedReplayAsync(result, cancellationTokenSource.Token);

            if (!IsCurrentReplayLoad(cancellationTokenSource))
            {
                return;
            }

            StatusText = BuildLoadedStatus(result);
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
            if (IsCurrentReplayLoad(cancellationTokenSource))
            {
                StatusText = "Replay loading was cancelled.";
            }
        }
        catch (Exception exception)
        {
            if (IsCurrentReplayLoad(cancellationTokenSource))
            {
                StatusText = $"Replay load failed: {exception.Message}";
            }
        }
        finally
        {
            if (IsCurrentReplayLoad(cancellationTokenSource))
            {
                _replayCancellationTokenSource = null;
                IsLoading = false;
            }

            cancellationTokenSource.Dispose();
        }
    }
    private async Task InitialiseLoadedReplayAsync(ReplayLoadResult result, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _timeline = result.Timeline;

        await TrackMap.InitialiseAsync(result.Data, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        DriverTable.Initialise(result.Data.Drivers);
        Playback.Load(result.Timeline);
        _bufferController.StartSession();
    }

    private void OnSessionSelected(SessionFinderSession session)
    {
        SessionKeyText = session.SessionKey.Value.ToString();
        IsSessionFinderOpen = false;
        _ = LoadReplayAsync(session.SessionKey, session.SessionName);
    }

    private void ClearReplay()
    {
        CancelReplayLoad();
        _bufferController.ClearSession();
        _timeline = null;
        Playback.Clear();
        DriverTable.Clear();
        TrackMap.Clear();
        Telemetry.SelectDriver(null);
    }

    private void CancelReplayLoad()
    {
        if(_replayCancellationTokenSource is not null)
        {
            _replayCancellationTokenSource!.Cancel();
            _replayCancellationTokenSource = null;
        }
    }

    private bool IsCurrentReplayLoad(CancellationTokenSource cancellationTokenSource)
    {
        return ReferenceEquals(_replayCancellationTokenSource, cancellationTokenSource);
    }

    private void OnPlaybackPositionChanged(object? sender, PlaybackPositionChangedEventArgs eventArgs)
    {
        if (_timeline is not ReplayTimeline timeline)
        {
            return;
        }

        DateTimeOffset playbackTime = timeline.SessionStart + eventArgs.Position;

        if(!timeline.IsTimeBuffered(playbackTime))
        {
            Playback.WaitForBuffer(resumeWhenBuffered: Playback.IsPlaying);

            if(_bufferController.BufferAt(timeline, eventArgs.Position))
            {
                StatusText = "Buffering selected replay time...";
            }

            return;
        }

        DriverTable.Update(
            timeline,
            eventArgs.Position,
            Playback.IsPlaying);

        TrackMap.Update(timeline, eventArgs.Position);

        if (_bufferController.BufferAheadIfNeeded(
            timeline,
            eventArgs.Position,
            Playback.IsPlaying))
        {
            StatusText = "Buffering the next replay chunk...";
        }
    }

    private void OnBufferingCompleted(object? sender, ReplayBufferCompletedEventArgs eventArgs)
    {
        if (!ReferenceEquals(_timeline, eventArgs.Timeline))
        {
            return;
        }

        if (!eventArgs.Succeeded)
        {
            StatusText = $"Buffering failed: {eventArgs.Failure!.Message}";
            return;
        }

        Playback.RefreshBufferedDuration();
        Playback.RefreshCurrentPosition();
        Playback.ResumeAfterBuffering();
        StatusText = BuildBufferStatus(_timeline, TimeSpan.FromSeconds(Playback.CurrentTimeSeconds));
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

    private static string BuildBufferStatus(ReplayTimeline timeline, TimeSpan playbackPosition)
    {
        DateTimeOffset playbackTime = timeline.SessionStart + playbackPosition;

        if (timeline.TryGetBufferedRange(playbackTime, out ReplayBufferRange? range))
        {
            TimeSpan rangeStart = range!.Start - timeline.SessionStart;
            TimeSpan rangeEnd = range.End - timeline.SessionStart;

            return $"Buffered replay data from {rangeStart:hh\\:mm\\:ss} to {rangeEnd:hh\\:mm\\:ss}.";
        }

        return $"Buffered {timeline.LoadedRanges.Count} replay section(s).";
    }

    private static bool TryParseSessionKey(string text, out SessionKey sessionKey, out string? errorMessage)
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
