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
        ReplayData replayData =
            await _sessionDataService.LoadReplayDataAsync(sessionKey, cancellationToken);

        Stopwatch buildTimer = Stopwatch.StartNew();
        ReplayTimeline timeline = _replayBuilder.BuildReplay(replayData);
        buildTimer.Stop();
        loadTimer.Stop();

        return new ReplayLoadResult(
            replayData,
            timeline,
            loadTimer.Elapsed,
            buildTimer.Elapsed);
    }
}

public record ReplayLoadResult(
    ReplayData Data,
    ReplayTimeline Timeline,
    TimeSpan TotalElapsed,
    TimeSpan BuildElapsed);
