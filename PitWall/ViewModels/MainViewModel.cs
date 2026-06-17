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
    private readonly ReplayFrameBuilder _replayFrameBuilder;
    private readonly Stopwatch _playbackClock = new();
    private static readonly TimeSpan ReplayFrameStep = TimeSpan.FromMilliseconds(250);
    private IReadOnlyList<ReplayFrame> _frames = [];
    private IReadOnlyDictionary<DriverNumber, OpenF1Driver> _driversByNumber = new Dictionary<DriverNumber, OpenF1Driver>();
    private readonly Dictionary<DriverNumber, ReplayDriverRow> _driverRowsByNumber = new();
    private int _currentFrameIndex = -1;
    private TimeSpan _playbackStartTime;
    private string _sessionKeyText = "latest";
    private string _statusText = "Enter a session key and load replay data.";
    private double _currentTimeSeconds;
    private double _durationSeconds;
    private double _playbackSpeed = 1.0;
    private bool _isPlaying;
    private bool _isLoading;
    private bool _isPlaybackRendering;
    private ReplayFrame? _currentFrame;

    public MainViewModel()
        : this(CreateDefaultSessionDataService(), new ReplayFrameBuilder())
    {
    }

    public MainViewModel(SessionDataService sessionDataService, ReplayFrameBuilder replayFrameBuilder)
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

    public ReplayFrame? CurrentFrame
    {
        get => _currentFrame;
        private set => SetProperty(ref _currentFrame, value);
    }

    public string FrameText =>
        _frames.Count == 0 || _currentFrameIndex < 0
            ? "Frame 0 / 0"
            : $"Playhead {CurrentTimeSeconds:0.000}s | Keyframe {_currentFrameIndex + 1:N0} / {_frames.Count:N0}";

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

            StatusText = "Building replay frames...";

            Stopwatch buildTimer = Stopwatch.StartNew();
            IReadOnlyList<ReplayFrame> frames = _replayFrameBuilder.BuildFrames(replayData, ReplayFrameStep);
            buildTimer.Stop();
            loadTimer.Stop();

            LoadFrames(replayData, frames);

            StatusText =
                $"Loaded {frames.Count:N0} frames in {loadTimer.Elapsed.TotalSeconds:0.0}s " +
                $"(frame build {buildTimer.ElapsedMilliseconds:N0}ms at {ReplayFrameStep.TotalMilliseconds:0}ms/frame). " +
                $"Streams: {replayData.Locations.Count:N0} locations, " +
                $"{replayData.Positions.Count:N0} positions, " +
                $"{replayData.CarTelementry.Count:N0} telemetry, " +
                $"{replayData.Intervals.Count:N0} intervals, " +
                $"{replayData.Laps.Count:N0} laps.";
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

    private void LoadFrames(ReplayData replayData, IReadOnlyList<ReplayFrame> frames)
    {
        _frames = frames;
        _driversByNumber = replayData.Drivers.ToDictionary(driver => driver.DriverNumber);
        InitializeDriverRows(replayData.Drivers);
        DurationSeconds = frames.Count == 0 ? 0 : frames[^1].SessionTime.TotalSeconds;

        if (frames.Count > 0)
        {
            ApplyFrame(0, frames[0].SessionTime);
        }
    }

    private void TogglePlayback()
    {
        if (_frames.Count == 0 || IsLoading)
        {
            return;
        }

        if (IsPlaying)
        {
            PausePlayback();
            return;
        }

        if (_currentFrameIndex >= _frames.Count - 1)
        {
            SeekTo(TimeSpan.Zero, resetPlaybackClock: false);
        }

        _playbackStartTime = CurrentFrame?.SessionTime ?? TimeSpan.Zero;
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
        if (_frames.Count == 0)
        {
            return;
        }

        int frameIndex = FindFrameIndexAtOrBefore(targetTime);
        ApplyFrame(frameIndex, targetTime);

        if (resetPlaybackClock && IsPlaying)
        {
            _playbackStartTime = CurrentFrame?.SessionTime ?? TimeSpan.Zero;
            _playbackClock.Restart();
        }
    }

    private int FindFrameIndexAtOrBefore(TimeSpan targetTime)
    {
        int low = 0;
        int high = _frames.Count - 1;
        int bestIndex = 0;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;

            if (_frames[middle].SessionTime <= targetTime)
            {
                bestIndex = middle;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return bestIndex;
    }

    private void ApplyFrame(int frameIndex, TimeSpan targetTime)
    {
        if (frameIndex < 0 || frameIndex >= _frames.Count)
        {
            return;
        }

        ReplayFrame frame = BuildDisplayFrame(frameIndex, targetTime);

        _currentFrameIndex = frameIndex;
        CurrentFrame = frame;
        SetCurrentTimeSeconds(frame.SessionTime.TotalSeconds);
        RefreshDriverStates(frame);

        OnPropertyChanged(nameof(FrameText));
    }

    private ReplayFrame BuildDisplayFrame(int frameIndex, TimeSpan targetTime)
    {
        ReplayFrame currentFrame = _frames[frameIndex];

        if (frameIndex >= _frames.Count - 1)
        {
            return currentFrame;
        }

        ReplayFrame nextFrame = _frames[frameIndex + 1];
        double amount = GetInterpolationAmount(
            targetTime,
            currentFrame.SessionTime,
            nextFrame.SessionTime);

        if (amount <= 0)
        {
            return currentFrame;
        }

        if (amount >= 1)
        {
            return nextFrame;
        }

        DateTimeOffset timestamp = currentFrame.Timestamp +
            TimeSpan.FromTicks((long)((nextFrame.Timestamp - currentFrame.Timestamp).Ticks * amount));

        Dictionary<DriverNumber, DriverReplayState> driverStates = new(currentFrame.Drivers.Count);

        foreach (KeyValuePair<DriverNumber, DriverReplayState> currentDriver in currentFrame.Drivers)
        {
            nextFrame.Drivers.TryGetValue(currentDriver.Key, out DriverReplayState? nextState);
            driverStates[currentDriver.Key] = InterpolateDriverState(currentDriver.Value, nextState, timestamp, amount);
        }

        foreach (KeyValuePair<DriverNumber, DriverReplayState> nextDriver in nextFrame.Drivers)
        {
            if (!driverStates.ContainsKey(nextDriver.Key))
            {
                driverStates[nextDriver.Key] = nextDriver.Value;
            }
        }

        return new ReplayFrame(timestamp, targetTime, driverStates);
    }

    private static DriverReplayState InterpolateDriverState(
        DriverReplayState previous,
        DriverReplayState? next,
        DateTimeOffset timestamp,
        double amount)
    {
        return previous with
        {
            Location = InterpolateLocation(previous.Location, next?.Location, timestamp, amount),
            Telemetry = InterpolateTelemetry(previous.Telemetry, next?.Telemetry, timestamp, amount),
            Interval = InterpolateInterval(previous.Interval, next?.Interval, timestamp, amount),
            CurrentLap = previous.CurrentLap ?? next?.CurrentLap,
            CurrentStint = previous.CurrentStint ?? next?.CurrentStint,
            CurrentPitStop = previous.CurrentPitStop ?? next?.CurrentPitStop
        };
    }

    private static OpenF1Location? InterpolateLocation(OpenF1Location? previous, OpenF1Location? next, DateTimeOffset timestamp, double amount)
    {
        OpenF1Location? source = previous ?? next;

        if (source is null)
        {
            return null;
        }

        return source with
        {
            TimestampStart = timestamp,
            X = InterpolateInt(previous?.X, next?.X, amount),
            Y = InterpolateInt(previous?.Y, next?.Y, amount),
            Z = InterpolateInt(previous?.Z, next?.Z, amount)
        };
    }

    private static OpenF1CarTelemetrySample? InterpolateTelemetry(
        OpenF1CarTelemetrySample? previous,
        OpenF1CarTelemetrySample? next,
        DateTimeOffset timestamp,
        double amount)
    {
        OpenF1CarTelemetrySample? source = previous ?? next;

        if (source is null)
        {
            return null;
        }

        return source with
        {
            Timestamp = timestamp,
            Throttle = InterpolateInt(previous?.Throttle, next?.Throttle, amount),
            Brake = InterpolateInt(previous?.Brake, next?.Brake, amount),
            Speed = InterpolateInt(previous?.Speed, next?.Speed, amount),
            Rpm = InterpolateInt(previous?.Rpm, next?.Rpm, amount),
            Gear = previous?.Gear ?? next?.Gear,
            Drs = previous?.Drs ?? next?.Drs
        };
    }

    private static OpenF1IntervalSample? InterpolateInterval(
        OpenF1IntervalSample? previous,
        OpenF1IntervalSample? next,
        DateTimeOffset timestamp,
        double amount)
    {
        OpenF1IntervalSample? source = previous ?? next;

        if (source is null)
        {
            return null;
        }

        return source with
        {
            Timestamp = timestamp,
            GapToLeader = InterpolateTimingGap(previous?.GapToLeader, next?.GapToLeader, amount),
            IntervalToAhead = InterpolateTimingGap(previous?.IntervalToAhead, next?.IntervalToAhead, amount)
        };
    }

    private static double GetInterpolationAmount(TimeSpan targetTime, TimeSpan previousTime, TimeSpan nextTime)
    {
        TimeSpan window = nextTime - previousTime;

        if (window <= TimeSpan.Zero)
        {
            return 0;
        }

        double amount = (targetTime - previousTime).TotalMilliseconds / window.TotalMilliseconds;

        return Math.Clamp(amount, 0, 1);
    }

    private static int? InterpolateInt(int? previous, int? next, double amount)
    {
        if (previous.HasValue && next.HasValue)
        {
            return (int)Math.Round(previous.Value + (next.Value - previous.Value) * amount);
        }

        return previous ?? next;
    }

    private static TimingGap? InterpolateTimingGap(TimingGap? previous, TimingGap? next, double amount)
    {
        if (previous?.Seconds is double previousSeconds && next?.Seconds is double nextSeconds)
        {
            double interpolatedSeconds = previousSeconds + (nextSeconds - previousSeconds) * amount;
            return new TimingGap(interpolatedSeconds, null);
        }

        return previous ?? next;
    }

    private void RefreshDriverStates(ReplayFrame frame)
    {
        bool shouldSortRows = false;

        foreach (DriverReplayState state in frame.Drivers.Values)
        {
            if (_driverRowsByNumber.TryGetValue(state.DriverNumber, out ReplayDriverRow? row))
            {
                shouldSortRows |= row.Update(state);
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
        _frames = [];
        _driversByNumber = new Dictionary<DriverNumber, OpenF1Driver>();
        _currentFrameIndex = -1;
        CurrentFrame = null;
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
