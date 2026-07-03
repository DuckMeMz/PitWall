using System.Text.Json.Serialization;
using PitWall.Converters;

namespace PitWall.Models;

[JsonConverter(typeof(MeetingKeyConverter))]
public readonly record struct MeetingKey(int Value) : IApiQueryValue
{
    private const int LatestValue = -1;

    public static MeetingKey Latest => new(LatestValue);

    public string ToQueryValue() =>
        Value == LatestValue ? "latest" : Value.ToString();

    public override string ToString() => Value.ToString();
}
