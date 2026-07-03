using System.Text.Json.Serialization;
using PitWall.Converters;

namespace PitWall.Models;

[JsonConverter(typeof(SessionKeyConverter))]
public readonly record struct SessionKey(int Value) : IApiQueryValue
{
    private const int LatestValue = -1;

    public static SessionKey Latest => new(LatestValue);

    public string ToQueryValue() =>
        Value == LatestValue ? "latest" : Value.ToString();

    public override string ToString() => Value.ToString();
}
