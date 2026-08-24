using PitWall.Models;

namespace PitWall.Services;

public class BufferController
{
    private readonly ReplayLoader _replayLoader;
    private readonly ReplayBufferSettings _settings;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isBuffering;

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
    }

    public bool BufferAheadIfNeeded(ReplayTimeline timeline, TimeSpan playbackPosition, bool isPlaying)
    {
        ArgumentNullException.ThrowIfNull(timeline);

        if (!isPlaying || _isBuffering)
        {
            return false;
        }

        DateTimeOffset playbackTime = timeline.SessionStart + playbackPosition;

        if(!timeline.TryGetBufferedRange(playbackTime, out ReplayBufferRange? range))
        {
            return false;
        }

        if(range!.End >= timeline.SessionStart + timeline.Duration)
        {
            return false;
        }

        TimeSpan remainingBufferedTime = range.End - playbackTime;

        if (remainingBufferedTime > _settings.AutoBufferThreshold)
        {
            return false;
        }

        return StartBuffering(timeline, range.End);
    }

    public bool BufferAt(ReplayTimeline timeline, TimeSpan requestedPosition)
    {
        DateTimeOffset requestedTime = timeline.SessionStart + requestedPosition;

        if(timeline.IsTimeBuffered(requestedTime))
        {
            return false;
        }

        DateTimeOffset chunkStart = requestedTime - _settings.PreBufferSize;

        if(chunkStart < timeline.SessionStart)
        {
            chunkStart = timeline.SessionStart;
        }

        return StartBuffering(timeline, chunkStart);
    }

    private bool StartBuffering(ReplayTimeline timeline, DateTimeOffset chunkStart)
    {
        if(_isBuffering)
        {
            return false;
        }

        _isBuffering = true;

        CancellationToken cancellationToken = _cancellationTokenSource?.Token ?? CancellationToken.None;

        _ = BufferChunkAsync(timeline, chunkStart, cancellationToken);
        return true;
    }

    private async Task BufferChunkAsync(
        ReplayTimeline timeline,
        DateTimeOffset chunkStart,
        CancellationToken cancellationToken)
    {
        Exception? failure = null;

        try
        {
            await _replayLoader.LoadChunkAsync(
                timeline,
                chunkStart,
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
