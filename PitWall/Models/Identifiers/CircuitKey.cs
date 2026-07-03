using System.Text.Json.Serialization;
using PitWall.Converters;

namespace PitWall.Models;

[JsonConverter(typeof(CircuitKeyConverter))]
public readonly record struct CircuitKey(int Value) : IApiQueryValue
{
    public string ToQueryValue() => Value.ToString();

    public override string ToString() => Value.ToString();
}
