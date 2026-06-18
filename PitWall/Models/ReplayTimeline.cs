using PitWall.Models.OpenF1Api;

namespace PitWall.Models;

public record ReplayTimeline(
    DateTimeOffset SessionStart,
    TimeSpan Duration,
    TimeSpan FrameStep,
    OpenF1Driver[] Drivers,
    DriverReplayState[] DriverStatesByFrame)
{
    public int DriverCount => Drivers.Length;
    public int UniqueFrameCount => DriverCount == 0 ? 0 : DriverStatesByFrame.Length / DriverCount;

    public int GetFrameStartIndex(int frameIndex)
    {
        return frameIndex * DriverCount;
    }

    public DriverReplayState GetSingleStateForFrame(int frameIndex, int driverIndex)
    {
        if(frameIndex < 0 || driverIndex < 0)
        {
            throw new InvalidOperationException("Frame or driver index can't be < 0.");
        }

        if(driverIndex >= DriverCount)
        {
            throw new InvalidOperationException($"Driver index: {driverIndex} can't be greater than or equal to the amount of drivers: {DriverCount}.");
        }

        if(frameIndex >= UniqueFrameCount)
        {
            throw new InvalidOperationException($"Frame index {frameIndex} can't be greater than or equal to the amount of unique frames {UniqueFrameCount}.");
        }

        int index = GetFrameStartIndex(frameIndex) + driverIndex;
        return DriverStatesByFrame[index];
    }

    public ReadOnlySpan<DriverReplayState> GetAllStatesForFrame(int frameIndex)
    {
        if (frameIndex < 0)
        {
            throw new InvalidOperationException("Frame index can't be < 0.");
        }

        if (frameIndex >= UniqueFrameCount)
        {
            throw new InvalidOperationException($"Frame index {frameIndex} can't be greater than or equal to the amount of unique frames {UniqueFrameCount}.");
        }

        int startIndex = GetFrameStartIndex(frameIndex);
        return DriverStatesByFrame.AsSpan(startIndex, DriverCount);
    }
}