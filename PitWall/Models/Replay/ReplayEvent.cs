using PitWall.Models.OpenF1Api;

namespace PitWall.Models;

public record ReplayEvent(
        IReadOnlyList<OpenF1PositionUpdate> PositionUpdates,
        IReadOnlyList<OpenF1RaceControlMessage> RaceControlMessages
    );