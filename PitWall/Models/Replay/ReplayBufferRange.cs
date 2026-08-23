
using System.Windows.Controls;

namespace PitWall.Models;

public record ReplayBufferRange(
    DateTimeOffset Start,
    DateTimeOffset End)
{
    public TimeSpan Duration => End - Start;

    public bool Contains(DateTimeOffset time)                        
    {
        return Start <= time && End >= time;
    }

    public bool TouchesOrOverlaps(ReplayBufferRange otherRange)
    {
        return Start <= otherRange.End && otherRange.Start <= End;
    }

    public ReplayBufferRange Combine(ReplayBufferRange otherRange)
    {
        if(!TouchesOrOverlaps(otherRange))
        {
            throw new ArgumentException($"Can't combine ranges that don't touch or overlap! Range 1: {Start} - {End}. Range 2: {otherRange.Start} - {otherRange.End}.");
        }

        if(otherRange.Start == Start && otherRange.End == End)
        {
            throw new ArgumentException("Can't combine identical ranges!");
        }

        DateTimeOffset newStart = Start >= otherRange.Start ? Start : otherRange.Start;
        DateTimeOffset newEnd = End >= otherRange.End ? End : otherRange.End;

        return new ReplayBufferRange(newStart, newEnd);
    }
}
