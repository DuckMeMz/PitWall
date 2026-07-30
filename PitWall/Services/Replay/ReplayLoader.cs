using System.Diagnostics;
using PitWall.Models;

namespace PitWall.Services;

public class ReplayLoader
{
    private readonly SessionDataService _sessionDataService;
    private readonly ReplayBuilder _replayBuilder;

    public ReplayLoader(SessionDataService sessionDataService, ReplayBuilder replayBuilder)
    {
        _sessionDataService = sessionDataService
            ?? throw new ArgumentNullException(nameof(sessionDataService));
        _replayBuilder = replayBuilder
            ?? throw new ArgumentNullException(nameof(replayBuilder));
    }

    public async Task<ReplayLoadResult> LoadAsync(SessionKey sessionKey, CancellationToken cancellationToken = default)
    {
        Stopwatch loadTimer = Stopwatch.StartNew();
        InitialReplayData initalReplayData =
            await _sessionDataService.LoadInitalReplayChunk(sessionKey, TimeSpan.FromMinutes(1), cancellationToken);

        Stopwatch buildTimer = Stopwatch.StartNew();
        ReplayTimeline timeline = _replayBuilder.BuildInitialTimeline(initalReplayData);
        buildTimer.Stop();
        loadTimer.Stop();

        return new ReplayLoadResult(
            initalReplayData,
            timeline,
            loadTimer.Elapsed,
            buildTimer.Elapsed);
    }
}

public record ReplayLoadResult(
    InitialReplayData Data,
    ReplayTimeline Timeline,
    TimeSpan TotalElapsed,
    TimeSpan BuildElapsed);
