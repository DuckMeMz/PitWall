namespace PitWall.Models;

public class ReplayBufferSettings
{
    public TimeSpan InitialChunkLength { get; init; } = TimeSpan.FromMinutes(1);
    public TimeSpan NextChunkLength { get; init; } = TimeSpan.FromMinutes(2);
    public TimeSpan AutoBufferThreshold { get; init; } = TimeSpan.FromSeconds(60);
    public TimeSpan PreBufferSize { get; init; } = TimeSpan.FromSeconds(10);
}
