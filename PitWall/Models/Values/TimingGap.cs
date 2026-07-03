using System.Text.Json.Serialization;
using PitWall.Converters;

namespace PitWall.Models;

[JsonConverter(typeof(TimingGapConverter))]
public readonly record struct TimingGap(
    double? Seconds,
    string? RawValue) : IApiQueryValue
{
    public string ToQueryValue()
    {
        if (Seconds.HasValue)
        {
            return Seconds.Value.ToString();
        }

        return RawValue ?? string.Empty;
    }

    public override string ToString()
    {
        if (Seconds.HasValue)
        {
            return Seconds.Value.ToString();
        }

        return RawValue ?? string.Empty;
    }
}
