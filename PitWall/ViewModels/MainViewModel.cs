using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using PitWall.Commands;
using PitWall.Models;
using PitWall.Models.OpenF1Api;
using PitWall.Services;

namespace PitWall.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly SessionDataService _sessionDataService;
    private readonly ReplayBuilder _replayFrameBuilder;
    private readonly Stopwatch _playbackClock = new();
    private ReplayTimeline? _replayTimeline;
    private readonly Dictionary<DriverNumber, ReplayDriverRow> _driverRowsByNumber = new();
    private TimeSpan _playbackStartTime;
    private string _sessionKeyText = "latest";
    private string _statusText = "Enter a session key and load replay data.";
    private double _currentTimeSeconds;
    private double _durationSeconds;
    private double _playbackSpeed = 1.0;
    private bool _isPlaying;
    private bool _isLoading;
    private bool _isPlaybackRendering;

    public MainViewModel()
        : this(CreateDefaultSessionDataService(), new ReplayBuilder())
    {
    }

    public MainViewModel(SessionDataService sessionDataService, ReplayBuilder replayFrameBuilder)
    {
        _sessionDataService = sessionDataService;
        _replayFrameBuilder = replayFrameBuilder;

        LoadReplayCommand = new AsyncRelayCommand(LoadReplayAsync, () => !IsLoading);
        PlayPauseCommand = new RelayCommand(TogglePlayback);
        StopCommand = new RelayCommand(StopPlayback);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ReplayDriverRow> DriverStates { get; } = new();

    public ICommand LoadReplayCommand { get; }
    public ICommand PlayPauseCommand { get; }
    public ICommand StopCommand { get; }

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

    public double CurrentTimeSeconds
    {
        get => _currentTimeSeconds;
        set
        {
            double boundedValue = Math.Clamp(value, 0, DurationSeconds);

            if (SetCurrentTimeSeconds(boundedValue))
            {
                SeekTo(TimeSpan.FromSeconds(boundedValue), resetPlaybackClock: true);
            }
        }
    }

    public double DurationSeconds
    {
        get => _durationSeconds;
        private set
        {
            if (SetProperty(ref _durationSeconds, value))
            {
                OnPropertyChanged(nameof(DurationText));
            }
        }
    }

    public string CurrentTimeText => FormatTime(TimeSpan.FromSeconds(CurrentTimeSeconds));
    public string DurationText => FormatTime(TimeSpan.FromSeconds(DurationSeconds));

    public double PlaybackSpeed
    {
        get => _playbackSpeed;
        set => SetProperty(ref _playbackSpeed, Math.Max(0.1, value));
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        private set
        {
            if (SetProperty(ref _isPlaying, value))
            {
                OnPropertyChanged(nameof(PlayPauseText));
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value) && LoadReplayCommand is AsyncRelayCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
        }
    }

    public string PlayPauseText => IsPlaying ? "Pause" : "Play";

    public string FrameText =>
        _replayTimeline is null
            ? "Playhead 0.000s | Drivers 0"
            : $"Playhead {CurrentTimeSeconds:0.000}s | Drivers {_replayTimeline.DriverCount:N0}";

    private bool HasReplay => _replayTimeline is { DriverCount: > 0 };

    private async Task LoadReplayAsync()
    {
        if (!TryParseSessionKey(SessionKeyText, out SessionKey sessionKey, out string? errorMessage))
        {
            StatusText = errorMessage ?? "Session key is invalid.";
            return;
        }

        StopPlayback();
        ClearReplay();

        try
        {
            IsLoading = true;
            StatusText = $"Loading OpenF1 data for session {SessionKeyText.Trim()}...";

            Stopwatch loadTimer = Stopwatch.StartNew();
            ReplayData replayData = await _sessionDataService.LoadReplayDataAsync(sessionKey);

            StatusText = "Building replay timeline...";

            Stopwatch buildTimer = Stopwatch.StartNew();
            ReplayTimeline timeline = _replayFrameBuilder.BuildReplay(replayData);
            buildTimer.Stop();
            loadTimer.Stop();

            LoadTimeline(replayData, timeline);

            static double ToMb(long bytes) => bytes / 1024.0 / 1024.0;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long managedBytes = GC.GetTotalMemory(forceFullCollection: true);

            using Process process = Process.GetCurrentProcess();
            process.Refresh();

            double managedMb = ToMb(managedBytes);
            double workingSetMb = ToMb(process.WorkingSet64);
            double privateMb = ToMb(process.PrivateMemorySize64);

            StatusText =
                $"Loaded {timeline.DriverCount:N0} driver streams in {loadTimer.Elapsed.TotalSeconds:0.0}s " +
                $"(timeline build {buildTimer.ElapsedMilliseconds:N0}ms). " +
                $"Streams: {replayData.Locations.Count:N0} locations, " +
                $"{replayData.Positions.Count:N0} positions, " +
                $"{replayData.CarTelementry.Count:N0} telemetry, " +
                $"{replayData.Intervals.Count:N0} intervals, " +
                $"{replayData.Laps.Count:N0} laps. " +
                $"Managed heap: {managedMb:0.0} MB, " +
                $"Working set: {workingSetMb:0.0} MB, " +
                $"Private memory: {privateMb:0.0} MB";
        }
        catch (Exception ex)
        {
            StatusText = $"Replay load failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void LoadTimeline(ReplayData replayData, ReplayTimeline timeline)
    {
        _replayTimeline = timeline;
        InitializeDriverRows(replayData.Drivers);
        DurationSeconds = timeline.Duration.TotalSeconds;

        if (timeline.DriverCount > 0)
        {
            ApplyPlayhead(TimeSpan.Zero);
        }
    }

    private void TogglePlayback()
    {
        if (!HasReplay || IsLoading)
        {
            return;
        }

        if (IsPlaying)
        {
            PausePlayback();
            return;
        }

        if (CurrentTimeSeconds >= DurationSeconds)
        {
            SeekTo(TimeSpan.Zero, resetPlaybackClock: false);
        }

        _playbackStartTime = TimeSpan.FromSeconds(CurrentTimeSeconds);
        _playbackClock.Restart();
        StartPlaybackRendering();
        IsPlaying = true;
    }

    private void PausePlayback()
    {
        StopPlaybackRendering();
        _playbackClock.Stop();
        IsPlaying = false;
    }

    private void StopPlayback()
    {
        PausePlayback();
        SeekTo(TimeSpan.Zero, resetPlaybackClock: false);
    }

    private void StartPlaybackRendering()
    {
        if (_isPlaybackRendering)
        {
            return;
        }

        CompositionTarget.Rendering += OnPlaybackRendering;
        _isPlaybackRendering = true;
    }

    private void StopPlaybackRendering()
    {
        if (!_isPlaybackRendering)
        {
            return;
        }

        CompositionTarget.Rendering -= OnPlaybackRendering;
        _isPlaybackRendering = false;
    }

    private void OnPlaybackRendering(object? sender, EventArgs e)
    {
        TimeSpan scaledElapsed = TimeSpan.FromTicks((long)(_playbackClock.Elapsed.Ticks * PlaybackSpeed));
        TimeSpan targetTime = _playbackStartTime + scaledElapsed;

        if (targetTime >= TimeSpan.FromSeconds(DurationSeconds))
        {
            SeekTo(TimeSpan.FromSeconds(DurationSeconds), resetPlaybackClock: false);
            PausePlayback();
            return;
        }

        SeekTo(targetTime, resetPlaybackClock: false);
    }

    private void SeekTo(TimeSpan targetTime, bool resetPlaybackClock)
    {
        if (!HasReplay)
        {
            return;
        }

        targetTime = ClampToReplayDuration(targetTime);
        ApplyPlayhead(targetTime);

        if (resetPlaybackClock && IsPlaying)
        {
            _playbackStartTime = TimeSpan.FromSeconds(CurrentTimeSeconds);
            _playbackClock.Restart();
        }
    }

    private TimeSpan ClampToReplayDuration(TimeSpan targetTime)
    {
        ReplayTimeline timeline = _replayTimeline
            ?? throw new InvalidOperationException("Can't clamp replay time without a loaded replay timeline.");

        if (targetTime <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return targetTime >= timeline.Duration ? timeline.Duration : targetTime;
    }

    private void ApplyPlayhead(TimeSpan targetTime)
    {
        ReplayTimeline? timeline = _replayTimeline;

        if (timeline is null)
        {
            return;
        }

        SetCurrentTimeSeconds(targetTime.TotalSeconds);
        RefreshDriverStates(timeline, targetTime);

        OnPropertyChanged(nameof(FrameText));
    }

    private void RefreshDriverStates(ReplayTimeline timeline, TimeSpan targetTime)
    {
        bool shouldSortRows = false;

        for (int driverIndex = 0; driverIndex < timeline.DriverCount; driverIndex++)
        {
            DriverReplayState displayState = timeline.GetStateAt(driverIndex, targetTime);

            if (_driverRowsByNumber.TryGetValue(displayState.DriverNumber, out ReplayDriverRow? row))
            {
                shouldSortRows |= row.Update(displayState);
            }
        }

        if (shouldSortRows)
        {
            SortDriverRowsByPosition();
        }
    }

    private void SortDriverRowsByPosition()
    {
        List<ReplayDriverRow> sortedRows = DriverStates
            .OrderBy(row => row.SortPosition)
            .ThenBy(row => row.SortDriverNumber)
            .ToList();

        for (int targetIndex = 0; targetIndex < sortedRows.Count; targetIndex++)
        {
            ReplayDriverRow row = sortedRows[targetIndex];
            int currentIndex = DriverStates.IndexOf(row);

            if (currentIndex >= 0 && currentIndex != targetIndex)
            {
                DriverStates.Move(currentIndex, targetIndex);
            }
        }
    }

    private void InitializeDriverRows(IReadOnlyList<OpenF1Driver> drivers)
    {
        DriverStates.Clear();
        _driverRowsByNumber.Clear();

        foreach (OpenF1Driver driver in drivers
            .OrderBy(driver => driver.DriverNumber.Value))
        {
            ReplayDriverRow row = ReplayDriverRow.From(driver);
            DriverStates.Add(row);
            _driverRowsByNumber[driver.DriverNumber] = row;
        }
    }

    private void ClearReplay()
    {
        _replayTimeline = null;
        DurationSeconds = 0;
        SetCurrentTimeSeconds(0);
        DriverStates.Clear();
        _driverRowsByNumber.Clear();
        OnPropertyChanged(nameof(FrameText));
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

    private static SessionDataService CreateDefaultSessionDataService()
    {
        HttpClient httpClient = new();
        OpenF1APIService apiService = new(httpClient);
        OpenF1Client client = new(apiService);
        SessionCatalogService sessionCatalog = new(client);

        return new SessionDataService(client, sessionCatalog);
    }

    private bool SetCurrentTimeSeconds(double value)
    {
        if (Math.Abs(_currentTimeSeconds - value) < 0.001)
        {
            return false;
        }

        _currentTimeSeconds = value;
        OnPropertyChanged(nameof(CurrentTimeSeconds));
        OnPropertyChanged(nameof(CurrentTimeText));
        OnPropertyChanged(nameof(FrameText));
        return true;
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static string FormatTime(TimeSpan time)
    {
        return time.TotalHours >= 1
            ? time.ToString(@"h\:mm\:ss")
            : time.ToString(@"m\:ss");
    }
}

public class ReplayDriverRow : INotifyPropertyChanged
{
    private string _position = "-";
    private string _speed = "-";
    private string _gear = "-";
    private string _drs = "-";
    private string _gap = "-";
    private string _interval = "-";
    private string _lap = "-";
    private string _x = "-";
    private string _y = "-";

    private ReplayDriverRow(OpenF1Driver? driver)
    {
        SortDriverNumber = driver?.DriverNumber.Value ?? int.MaxValue;
        DriverNumber = driver?.DriverNumber.Value.ToString(CultureInfo.InvariantCulture) ?? "-";
        DriverName = driver?.NameAcronym ?? driver?.BroadcastName ?? driver?.FullName ?? "-";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Position
    {
        get => _position;
        private set => SetProperty(ref _position, value);
    }

    public string DriverNumber { get; }
    public string DriverName { get; }
    public int SortPosition { get; private set; } = int.MaxValue;
    public int SortDriverNumber { get; }

    public string Speed
    {
        get => _speed;
        private set => SetProperty(ref _speed, value);
    }

    public string Gear
    {
        get => _gear;
        private set => SetProperty(ref _gear, value);
    }

    public string Drs
    {
        get => _drs;
        private set => SetProperty(ref _drs, value);
    }

    public string Gap
    {
        get => _gap;
        private set => SetProperty(ref _gap, value);
    }

    public string Interval
    {
        get => _interval;
        private set => SetProperty(ref _interval, value);
    }

    public string Lap
    {
        get => _lap;
        private set => SetProperty(ref _lap, value);
    }

    public string X
    {
        get => _x;
        private set => SetProperty(ref _x, value);
    }

    public string Y
    {
        get => _y;
        private set => SetProperty(ref _y, value);
    }

    public static ReplayDriverRow From(OpenF1Driver? driver)
    {
        return new ReplayDriverRow(driver);
    }

    public bool Update(DriverReplayState state)
    {
        int previousSortPosition = SortPosition;
        SortPosition = state.Position?.Value ?? int.MaxValue;

        Position = state.Position?.Value.ToString(CultureInfo.InvariantCulture) ?? "-";
        Speed = FormatNullable(state.Telemetry?.Speed, " km/h");
        Gear = state.Telemetry?.Gear?.ToString(CultureInfo.InvariantCulture) ?? "-";
        Drs = state.Telemetry?.Drs?.ToString() ?? "-";
        Gap = FormatTimingGap(state.Interval?.GapToLeader);
        Interval = FormatTimingGap(state.Interval?.IntervalToAhead);
        Lap = state.CurrentLap?.LapNumber.Value.ToString(CultureInfo.InvariantCulture) ?? "-";
        X = state.Location?.X?.ToString(CultureInfo.InvariantCulture) ?? "-";
        Y = state.Location?.Y?.ToString(CultureInfo.InvariantCulture) ?? "-";

        return SortPosition != previousSortPosition;
    }

    private static string FormatNullable(int? value, string suffix)
    {
        return value.HasValue
            ? $"{value.Value.ToString(CultureInfo.InvariantCulture)}{suffix}"
            : "-";
    }

    private static string FormatTimingGap(TimingGap? gap)
    {
        if (gap is null)
        {
            return "-";
        }

        if (gap.Value.Seconds is double seconds)
        {
            return seconds.ToString("0.000", CultureInfo.InvariantCulture);
        }

        return string.IsNullOrWhiteSpace(gap.Value.RawValue) ? "-" : gap.Value.RawValue;
    }

    private bool SetProperty(ref string field, string value, [CallerMemberName] string? propertyName = null)
    {
        if (field == value)
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
