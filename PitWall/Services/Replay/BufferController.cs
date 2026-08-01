using PitWall.Models;

namespace PitWall.Services;

public class BufferController
{
    private readonly ReplayLoader _replayLoader;
    private readonly ReplayBufferSettings _settings;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isBuffering;
    private TimeSpan? _lastAttemptedBufferEnd;

    public BufferController(ReplayLoader replayLoader, ReplayBufferSettings settings)
    {
        _replayLoader = replayLoader ?? throw new ArgumentNullException(nameof(replayLoader));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public event EventHandler<ReplayBufferCompletedEventArgs>? BufferingCompleted;

    public void StartSession()
    {
        ClearSession();
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public void ClearSession()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _isBuffering = false;
        _lastAttemptedBufferEnd = null;
    }

    public bool BufferAheadIfNeeded(ReplayTimeline timeline, TimeSpan playbackPosition, bool isPlaying)
    {
        ArgumentNullException.ThrowIfNull(timeline);

        if (!isPlaying ||
            _isBuffering ||
            timeline.BufferedDuration >= timeline.Duration)
        {
            return false;
        }

        TimeSpan remainingBufferedTime = timeline.BufferedDuration - playbackPosition;

        if (remainingBufferedTime > _settings.AutoBufferThreshold || 
            _lastAttemptedBufferEnd == timeline.BufferedDuration)
        {
            return false;
        }

        _lastAttemptedBufferEnd = timeline.BufferedDuration;
        _isBuffering = true;

        CancellationToken cancellationToken = _cancellationTokenSource?.Token ?? CancellationToken.None;

        _ = BufferNextChunkAsync(timeline, cancellationToken);
        return true;
    }

    private async Task BufferNextChunkAsync(ReplayTimeline timeline, CancellationToken cancellationToken)
    {
        Exception? failure = null;

        try
        {
            await _replayLoader.LoadNextChunkAsync(
                timeline,
                _settings.NextChunkLength,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception)
        {
            failure = exception;
        }
        finally
        {
            _isBuffering = false;
        }

        BufferingCompleted?.Invoke(this, new ReplayBufferCompletedEventArgs(timeline, failure));
    }
}

public class ReplayBufferCompletedEventArgs(
    ReplayTimeline timeline,
    Exception? failure) : EventArgs
{
    public ReplayTimeline Timeline { get; } = timeline;
    public Exception? Failure { get; } = failure;
    public bool Succeeded => Failure is null;
}
