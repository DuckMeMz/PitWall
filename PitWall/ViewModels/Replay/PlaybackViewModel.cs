using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;
using PitWall.Commands;
using PitWall.Common;
using PitWall.Models;

namespace PitWall.ViewModels;

public class PlaybackViewModel : BindableBase, IDisposable
{
    private readonly Stopwatch _playbackClock = new();
    private ReplayTimeline? _timeline;
    private TimeSpan _playbackStartTime;
    private double _currentTimeSeconds;
    private double _durationSeconds;
    private double _playbackSpeed = 1.0;
    private bool _isPlaying;
    private bool _isEnabled = true;
    private bool _isRendering;
    private bool _isDisposed;

    public PlaybackViewModel()
    {
        PlayPauseCommand = new RelayCommand(TogglePlayback);
        StopCommand = new RelayCommand(Stop);
    }

    public event EventHandler<PlaybackPositionChangedEventArgs>? PositionChanged;

    public ICommand PlayPauseCommand { get; }
    public ICommand StopCommand { get; }

    public double CurrentTimeSeconds
    {
        get => _currentTimeSeconds;
        set
        {
            double clampedValue = ClampTimeSeconds(value);

            if (Math.Abs(_currentTimeSeconds - clampedValue) >= 0.001)
            {
                SeekTo(TimeSpan.FromSeconds(clampedValue), resetPlaybackClock: true);
            }
        }
    }

    public double DurationSeconds
    {
        get => _durationSeconds;
        private set
        {
            double boundedValue = double.IsFinite(value) ? Math.Max(0, value) : 0;

            if (SetProperty(ref _durationSeconds, boundedValue))
            {
                OnPropertyChanged(nameof(DurationText));
            }
        }
    }

    public double PlaybackSpeed
    {
        get => _playbackSpeed;
        set
        {
            double boundedValue = double.IsFinite(value) ? Math.Max(0.1, value) : 1.0;

            SetProperty(ref _playbackSpeed, boundedValue);
        }
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

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetProperty(ref _isEnabled, value) && !value)
            {
                Pause();
            }
        }
    }

    public string CurrentTimeText => FormatTime(TimeSpan.FromSeconds(CurrentTimeSeconds));
    public string DurationText => FormatTime(TimeSpan.FromSeconds(DurationSeconds));
    public string PlayPauseText => IsPlaying ? "Pause" : "Play";

    public string FrameText => 
        _timeline is null
            ? "Playhead 0.000s | Drivers 0"
            : $"Playhead {CurrentTimeSeconds:0.000}s | Drivers {_timeline.DriverCount}";

    public void Load(ReplayTimeline timeline)
    {
        ArgumentNullException.ThrowIfNull(timeline);

        Pause();
        _timeline = timeline;
        DurationSeconds = timeline.Duration.TotalSeconds;
        OnPropertyChanged(nameof(FrameText));
        ApplyPosition(TimeSpan.Zero, forceNotification: true);
    }

    public void Clear()
    {
        Pause();
        _timeline = null;
        DurationSeconds = 0;
        SetCurrentTimeSeconds(0);
        OnPropertyChanged(nameof(FrameText));
    }

    public void Stop()
    {
        Pause();
        SeekTo(TimeSpan.Zero, resetPlaybackClock: false);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        StopRendering();
        _playbackClock.Stop();
    }

    private bool HasReplay => _timeline is not null && _timeline.DriverCount > 0;

    private void TogglePlayback()
    {
        if (!HasReplay || !IsEnabled)
        {
            return;
        }

        if (IsPlaying)
        {
            Pause();
            return;
        }

        if (CurrentTimeSeconds >= DurationSeconds)
        {
            SeekTo(TimeSpan.Zero, resetPlaybackClock: false);
        }

        _playbackStartTime = TimeSpan.FromSeconds(CurrentTimeSeconds);
        _playbackClock.Restart();
        StartRendering();
        IsPlaying = true;
    }

    private void Pause()
    {
        StopRendering();
        _playbackClock.Stop();
        IsPlaying = false;
    }

    private void StartRendering()
    {
        if (_isRendering)
        {
            return;
        }

        CompositionTarget.Rendering += OnRendering;
        _isRendering = true;
    }

    private void StopRendering()
    {
        if (!_isRendering)
        {
            return;
        }

        CompositionTarget.Rendering -= OnRendering;
        _isRendering = false;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        TimeSpan scaledElapsed = TimeSpan.FromTicks((long)(_playbackClock.Elapsed.Ticks * PlaybackSpeed));
        TimeSpan targetTime = _playbackStartTime + scaledElapsed;
        TimeSpan duration = TimeSpan.FromSeconds(DurationSeconds);

        if (targetTime >= duration)
        {
            SeekTo(duration, resetPlaybackClock: false);
            Pause();
            return;
        }

        SeekTo(targetTime, resetPlaybackClock: false);
    }

    private void SeekTo(TimeSpan targetTime, bool resetPlaybackClock)
    {
        if (_timeline is null)
        {
            return;
        }

        ApplyPosition(ClampToReplayDuration(targetTime), forceNotification: false);

        if (resetPlaybackClock && IsPlaying)
        {
            _playbackStartTime = TimeSpan.FromSeconds(CurrentTimeSeconds);
            _playbackClock.Restart();
        }
    }

    private void ApplyPosition(TimeSpan position, bool forceNotification)
    {
        bool changed = SetCurrentTimeSeconds(position.TotalSeconds);

        if (changed || forceNotification)
        {
            PositionChanged?.Invoke(
                this, 
                new PlaybackPositionChangedEventArgs(position));
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

    private TimeSpan ClampToReplayDuration(TimeSpan targetTime)
    {
        ReplayTimeline timeline = _timeline
            ?? throw new InvalidOperationException(
                "Cannot clamp replay time without a loaded timeline.");

        if (targetTime <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        return targetTime >= timeline.Duration
            ? timeline.Duration
            : targetTime;
    }

    private double ClampTimeSeconds(double value)
    {
        if (!double.IsFinite(value))
        {
            return 0;
        }

        return DurationSeconds <= 0 
            ? 0 
            : Math.Clamp(value, 0, DurationSeconds);
    }

    private static string FormatTime(TimeSpan time)
    {
        return time.TotalHours >= 1 
            ? time.ToString(@"h\:mm\:ss") 
            : time.ToString(@"m\:ss");
    }
}

public class PlaybackPositionChangedEventArgs(TimeSpan position) : EventArgs
{
    public TimeSpan Position { get; } = position;
}
