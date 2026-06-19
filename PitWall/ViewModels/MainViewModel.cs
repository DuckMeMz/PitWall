using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
    private readonly Dictionary<DriverNumber, ReplayMapMarker> _mapMarkersByNumber = new();
    private TrackMapBounds _trackMapBounds = TrackMapBounds.Empty;
    private ReplayDriverRow? _selectedDriver;
    private TimeSpan _playbackStartTime;
    private string _sessionKeyText = "latest";
    private string _statusText = "Enter a session key and load replay data.";
    private string _trackTitle = "No session loaded";
    private ImageSource? _circuitImage;
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
    public ObservableCollection<ReplayMapMarker> MapMarkers { get; } = new();
    public ObservableCollection<TrackMapPoint> TrackMapPoints { get; } = new();

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

    public string TrackTitle
    {
        get => _trackTitle;
        private set => SetProperty(ref _trackTitle, value);
    }

    public ImageSource? CircuitImage
    {
        get => _circuitImage;
        private set => SetProperty(ref _circuitImage, value);
    }

    public ReplayDriverRow? SelectedDriver
    {
        get => _selectedDriver;
        set
        {
            if (SetProperty(ref _selectedDriver, value))
            {
                UpdateSelectedMapMarker();
            }
        }
    }

    public double CurrentTimeSeconds
    {
        get => _currentTimeSeconds;
        set
        {
            double boundedValue = ClampTimeSeconds(value);

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
            double boundedValue = double.IsNaN(value) || double.IsInfinity(value)
                ? 0
                : Math.Max(0, value);

            if (SetProperty(ref _durationSeconds, boundedValue))
            {
                if (_currentTimeSeconds > boundedValue)
                {
                    SetCurrentTimeSeconds(boundedValue);
                }

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
        DurationSeconds = Math.Max(0, timeline.Duration.TotalSeconds);
        _trackMapBounds = TrackMapBounds.From(replayData.Locations);
        LoadTrackMapPoints(replayData.Locations);
        InitializeDriverRows(replayData.Drivers);
        LoadTrackMetadata(replayData);

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
        DateTimeOffset playheadTimestamp = timeline.SessionStart + targetTime;

        for (int driverIndex = 0; driverIndex < timeline.DriverCount; driverIndex++)
        {
            DriverReplayState displayState = timeline.GetStateAt(driverIndex, targetTime);

            if (_driverRowsByNumber.TryGetValue(displayState.DriverNumber, out ReplayDriverRow? row))
            {
                shouldSortRows |= row.Update(displayState);
            }

            if (_mapMarkersByNumber.TryGetValue(displayState.DriverNumber, out ReplayMapMarker? marker))
            {
                marker.Update(displayState, _trackMapBounds, playheadTimestamp);
            }
        }

        if (shouldSortRows)
        {
            SortDriverRowsByPosition();
        }
    }

    private void LoadTrackMetadata(ReplayData replayData)
    {
        TrackTitle =
            replayData.Meeting?.CircuitShortName ??
            replayData.Session.CircuitShortName ??
            replayData.Meeting?.MeetingName ??
            replayData.Session.Location ??
            "Track map";

        CircuitImage = CreateCircuitImage(replayData.Meeting?.CircuitImage);
    }

    private void LoadTrackMapPoints(IReadOnlyList<OpenF1Location> locations)
    {
        TrackMapPoints.Clear();

        if (!_trackMapBounds.HasBounds || locations.Count == 0)
        {
            return;
        }

        int stride = Math.Max(1, locations.Count / 1600);
        HashSet<(int X, int Y)> projectedPoints = new();
        int sampleIndex = 0;

        foreach (OpenF1Location location in locations)
        {
            if (sampleIndex++ % stride != 0 ||
                location.X is not int locationX ||
                location.Y is not int locationY)
            {
                continue;
            }

            int projectedX = (int)Math.Round(_trackMapBounds.ProjectPointX(locationX));
            int projectedY = (int)Math.Round(_trackMapBounds.ProjectPointY(locationY));

            if (!projectedPoints.Add((projectedX, projectedY)))
            {
                continue;
            }

            TrackMapPoints.Add(new TrackMapPoint(projectedX, projectedY));
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

    private void UpdateSelectedMapMarker()
    {
        foreach (ReplayMapMarker marker in MapMarkers)
        {
            marker.IsSelected = SelectedDriver is not null &&
                marker.DriverNumber == SelectedDriver.DriverNumberKey;
        }
    }

    private void InitializeDriverRows(IReadOnlyList<OpenF1Driver> drivers)
    {
        DriverStates.Clear();
        MapMarkers.Clear();
        _driverRowsByNumber.Clear();
        _mapMarkersByNumber.Clear();

        foreach (OpenF1Driver driver in drivers
            .OrderBy(driver => driver.DriverNumber.Value))
        {
            ReplayDriverRow row = ReplayDriverRow.From(driver);
            ReplayMapMarker marker = ReplayMapMarker.From(driver);

            DriverStates.Add(row);
            MapMarkers.Add(marker);
            _driverRowsByNumber[driver.DriverNumber] = row;
            _mapMarkersByNumber[driver.DriverNumber] = marker;
        }

        SelectedDriver = DriverStates.FirstOrDefault();
    }

    private void ClearReplay()
    {
        _replayTimeline = null;
        _trackMapBounds = TrackMapBounds.Empty;
        TrackTitle = "No session loaded";
        CircuitImage = null;
        SelectedDriver = null;
        DurationSeconds = 0;
        SetCurrentTimeSeconds(0);
        DriverStates.Clear();
        MapMarkers.Clear();
        TrackMapPoints.Clear();
        _driverRowsByNumber.Clear();
        _mapMarkersByNumber.Clear();
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

    private static ImageSource? CreateCircuitImage(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) ||
            !Uri.TryCreate(imageUrl, UriKind.Absolute, out Uri? uri))
        {
            return null;
        }

        try
        {
            BitmapImage image = new();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnDemand;
            image.UriSource = uri;
            image.EndInit();

            return image;
        }
        catch
        {
            return null;
        }
    }

    private bool SetCurrentTimeSeconds(double value)
    {
        value = ClampTimeSeconds(value);

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

    private double ClampTimeSeconds(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0;
        }

        double duration = double.IsNaN(DurationSeconds) || double.IsInfinity(DurationSeconds)
            ? 0
            : Math.Max(0, DurationSeconds);

        return duration <= 0 ? 0 : Math.Clamp(value, 0, duration);
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
    private string _throttleText = "-";
    private string _brakeText = "-";
    private string _rpmText = "-";
    private double _throttlePercent;
    private double _brakePercent;
    private double _rpmPercent;
    private double _speedPercent;

    private ReplayDriverRow(OpenF1Driver? driver)
    {
        DriverNumberKey = driver?.DriverNumber ?? default;
        SortDriverNumber = driver?.DriverNumber.Value ?? int.MaxValue;
        DriverNumber = driver?.DriverNumber.Value.ToString(CultureInfo.InvariantCulture) ?? "-";
        DriverName = driver?.NameAcronym ?? driver?.BroadcastName ?? driver?.FullName ?? "-";
        TeamBrush = ReplayDriverRowBrushes.CreateTeamBrush(driver?.TeamColor);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Position
    {
        get => _position;
        private set => SetProperty(ref _position, value);
    }

    public string DriverNumber { get; }
    public DriverNumber DriverNumberKey { get; }
    public string DriverName { get; }
    public Brush TeamBrush { get; }
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

    public string ThrottleText
    {
        get => _throttleText;
        private set => SetProperty(ref _throttleText, value);
    }

    public string BrakeText
    {
        get => _brakeText;
        private set => SetProperty(ref _brakeText, value);
    }

    public string RpmText
    {
        get => _rpmText;
        private set => SetProperty(ref _rpmText, value);
    }

    public double ThrottlePercent
    {
        get => _throttlePercent;
        private set => SetProperty(ref _throttlePercent, value);
    }

    public double BrakePercent
    {
        get => _brakePercent;
        private set => SetProperty(ref _brakePercent, value);
    }

    public double RpmPercent
    {
        get => _rpmPercent;
        private set => SetProperty(ref _rpmPercent, value);
    }

    public double SpeedPercent
    {
        get => _speedPercent;
        private set => SetProperty(ref _speedPercent, value);
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
        ThrottleText = FormatPercent(state.Telemetry?.Throttle);
        BrakeText = FormatPercent(state.Telemetry?.Brake);
        RpmText = state.Telemetry?.Rpm?.ToString("N0", CultureInfo.InvariantCulture) ?? "-";
        ThrottlePercent = ClampPercent(state.Telemetry?.Throttle);
        BrakePercent = ClampPercent(state.Telemetry?.Brake);
        RpmPercent = ClampPercent((state.Telemetry?.Rpm ?? 0) / 15000.0 * 100.0);
        SpeedPercent = ClampPercent((state.Telemetry?.Speed ?? 0) / 380.0 * 100.0);
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

    private static string FormatPercent(int? value)
    {
        return value.HasValue
            ? $"{Math.Clamp(value.Value, 0, 100).ToString(CultureInfo.InvariantCulture)}%"
            : "-";
    }

    private static double ClampPercent(int? value)
    {
        return value.HasValue ? Math.Clamp(value.Value, 0, 100) : 0;
    }

    private static double ClampPercent(double value)
    {
        return Math.Clamp(value, 0, 100);
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

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

public class ReplayMapMarker : INotifyPropertyChanged
{
    private const double MovementDeadband = 0.35;
    private const int SpeedChangeDeadband = 1;
    private static readonly TimeSpan StationaryAfter = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan ResetTrackingAfterJump = TimeSpan.FromSeconds(5);

    private double _x = -100;
    private double _y = -100;
    private double _markerOpacity = 1.0;
    private Visibility _visibility = Visibility.Hidden;
    private bool _isSelected;
    private int? _lastSpeed;
    private DateTimeOffset? _lastActivityTimestamp;
    private DateTimeOffset? _lastUpdateTimestamp;
    private Brush _strokeBrush = Brushes.White;
    private double _strokeThickness = 1.5;

    private ReplayMapMarker(OpenF1Driver driver)
    {
        DriverNumber = driver.DriverNumber;
        Label = driver.NameAcronym ?? driver.DriverNumber.Value.ToString(CultureInfo.InvariantCulture);
        MarkerBrush = ReplayDriverRowBrushes.CreateTeamBrush(driver.TeamColor);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public DriverNumber DriverNumber { get; }
    public string Label { get; }
    public Brush MarkerBrush { get; }

    public double X
    {
        get => _x;
        private set => SetProperty(ref _x, value);
    }

    public double Y
    {
        get => _y;
        private set => SetProperty(ref _y, value);
    }

    public Visibility Visibility
    {
        get => _visibility;
        private set => SetProperty(ref _visibility, value);
    }

    public double MarkerOpacity
    {
        get => _markerOpacity;
        private set => SetProperty(ref _markerOpacity, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                StrokeBrush = value ? Brushes.White : Brushes.Black;
                StrokeThickness = value ? 3.0 : 1.5;
            }
        }
    }

    public Brush StrokeBrush
    {
        get => _strokeBrush;
        private set => SetProperty(ref _strokeBrush, value);
    }

    public double StrokeThickness
    {
        get => _strokeThickness;
        private set => SetProperty(ref _strokeThickness, value);
    }

    public static ReplayMapMarker From(OpenF1Driver driver)
    {
        return new ReplayMapMarker(driver);
    }

    public void Update(DriverReplayState state, TrackMapBounds bounds, DateTimeOffset playheadTimestamp)
    {
        if (!bounds.HasBounds ||
            state.Location?.X is not int locationX ||
            state.Location?.Y is not int locationY)
        {
            Visibility = Visibility.Hidden;
            ResetActivityTracking();
            return;
        }

        double targetX = bounds.ProjectX(locationX);
        double targetY = bounds.ProjectY(locationY);
        int? speed = state.Telemetry?.Speed;

        ResetActivityTrackingIfNeeded(playheadTimestamp);

        bool locationChanged = ShouldMoveTo(targetX, targetY);
        bool speedChanged = HasSpeedChanged(speed);

        if (locationChanged)
        {
            X = targetX;
            Y = targetY;
        }

        if (locationChanged || speedChanged)
        {
            _lastActivityTimestamp = playheadTimestamp;
        }

        MarkerOpacity = IsInactive(speed, speedChanged, playheadTimestamp) ? 0.22 : 1.0;
        Visibility = Visibility.Visible;
        _lastSpeed = speed;
        _lastUpdateTimestamp = playheadTimestamp;
    }

    private bool ShouldMoveTo(double targetX, double targetY)
    {
        return GetDistanceTo(targetX, targetY) > MovementDeadband;
    }

    private double GetDistanceTo(double targetX, double targetY)
    {
        double deltaX = targetX - X;
        double deltaY = targetY - Y;

        return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
    }

    private bool HasSpeedChanged(int? speed)
    {
        if (speed is null || _lastSpeed is null)
        {
            return false;
        }

        return Math.Abs(speed.Value - _lastSpeed.Value) > SpeedChangeDeadband;
    }

    private bool IsInactive(int? speed, bool speedChanged, DateTimeOffset playheadTimestamp)
    {
        if (_lastActivityTimestamp is not DateTimeOffset lastActivity ||
            playheadTimestamp - lastActivity <= StationaryAfter)
        {
            return false;
        }

        return speed is null || speed.Value <= SpeedChangeDeadband || !speedChanged;
    }

    private void ResetActivityTrackingIfNeeded(DateTimeOffset playheadTimestamp)
    {
        if (_lastUpdateTimestamp is null ||
            playheadTimestamp < _lastUpdateTimestamp ||
            playheadTimestamp - _lastUpdateTimestamp > ResetTrackingAfterJump)
        {
            _lastActivityTimestamp = playheadTimestamp;
            _lastSpeed = null;
        }
    }

    private void ResetActivityTracking()
    {
        _lastActivityTimestamp = null;
        _lastUpdateTimestamp = null;
        _lastSpeed = null;
        MarkerOpacity = 1.0;
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}

public readonly record struct TrackMapBounds(
    int MinX,
    int MaxX,
    int MinY,
    int MaxY,
    bool HasBounds)
{
    private const double CanvasWidth = 1200;
    private const double CanvasHeight = 360;
    private const double Padding = 34;
    private const double MarkerHalfWidth = 21;
    private const double MarkerHalfHeight = 10;

    public static TrackMapBounds Empty => new(0, 0, 0, 0, false);

    public static TrackMapBounds From(IEnumerable<OpenF1Location> locations)
    {
        List<OpenF1Location> validLocations = locations
            .Where(location => location.X.HasValue && location.Y.HasValue)
            .ToList();

        if (validLocations.Count == 0)
        {
            return Empty;
        }

        int minX = validLocations.Min(location => location.X!.Value);
        int maxX = validLocations.Max(location => location.X!.Value);
        int minY = validLocations.Min(location => location.Y!.Value);
        int maxY = validLocations.Max(location => location.Y!.Value);

        if (minX == maxX)
        {
            minX--;
            maxX++;
        }

        if (minY == maxY)
        {
            minY--;
            maxY++;
        }

        return new TrackMapBounds(minX, maxX, minY, maxY, true);
    }

    public double ProjectX(int x)
    {
        return ProjectPointX(x) - MarkerHalfWidth;
    }

    public double ProjectY(int y)
    {
        return ProjectPointY(y) - MarkerHalfHeight;
    }

    public double ProjectPointX(double x)
    {
        double amount = (x - MinX) / (double)(MaxX - MinX);
        return Padding + amount * (CanvasWidth - Padding * 2);
    }

    public double ProjectPointY(double y)
    {
        double amount = (y - MinY) / (double)(MaxY - MinY);
        return Padding + (1 - amount) * (CanvasHeight - Padding * 2);
    }
}

public readonly record struct TrackMapPoint(double X, double Y);

public static class ReplayDriverRowBrushes
{
    public static Brush CreateTeamBrush(PitWall.Models.Color? teamColor)
    {
        if (teamColor is null || teamColor.Value.HexCode.Length < 6)
        {
            return Brushes.Gray;
        }

        try
        {
            SolidColorBrush brush = new(System.Windows.Media.Color.FromRgb(
                teamColor.Value.R,
                teamColor.Value.G,
                teamColor.Value.B));
            brush.Freeze();
            return brush;
        }
        catch
        {
            return Brushes.Gray;
        }
    }
}
