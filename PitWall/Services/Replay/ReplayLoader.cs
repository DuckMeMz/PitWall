using System.Diagnostics;
using PitWall.Models;

namespace PitWall.Services;

public class ReplayLoader
{
    private readonly SessionDataService _sessionDataService;
    private readonly ReplayBuilder _replayBuilder;
    private readonly TimeSpan _initialChunkLength = TimeSpan.FromMinutes(1);

    public ReplayLoader(SessionDataService sessionDataService, ReplayBuilder replayBuilder)
    {
        _sessionDataService = sessionDataService
            ?? throw new ArgumentNullException(nameof(sessionDataService));
        _replayBuilder = replayBuilder
            ?? throw new ArgumentNullException(nameof(replayBuilder));
    }

    public async Task<ReplayLoadResult> LoadInitialAsync(SessionKey sessionKey, CancellationToken cancellationToken = default)
    {
        Stopwatch loadTimer = Stopwatch.StartNew();
        InitialReplayData initialReplayData =
            await _sessionDataService.LoadInitialReplayChunk(sessionKey, _initialChunkLength, cancellationToken);

        Stopwatch buildTimer = Stopwatch.StartNew();
        ReplayTimeline timeline = _replayBuilder.BuildInitialTimeline(initialReplayData);
        buildTimer.Stop();
        loadTimer.Stop();

        return new ReplayLoadResult(
            initialReplayData,
            timeline,
            loadTimer.Elapsed,
            buildTimer.Elapsed);
    }

    public async Task LoadNextChunkAsync(ReplayTimeline timeline, TimeSpan chunkLength, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(timeline);

        TimeSpan remainingDuration = timeline.Duration - timeline.BufferedDuration;

        if (remainingDuration <= TimeSpan.Zero)
        {
            return;
        }

        chunkLength = TimeSpan.FromTicks(Math.Min(chunkLength.Ticks, remainingDuration.Ticks));
        DateTimeOffset chunkStart = timeline.SessionStart + timeline.BufferedDuration;

        ReplayDataChunk chunk = await _sessionDataService.LoadReplayChunk(
            timeline.SessionKey,
            chunkStart,
            chunkLength,
            cancellationToken);

        _replayBuilder.AppendChunk(timeline, chunk);
    }
}

public record ReplayLoadResult(
    InitialReplayData Data,
    ReplayTimeline Timeline,
    TimeSpan TotalElapsed,
    TimeSpan BuildElapsed);
