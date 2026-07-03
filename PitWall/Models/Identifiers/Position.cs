using System.Text.Json.Serialization;
using PitWall.Converters;

namespace PitWall.Models;

[JsonConverter(typeof(PositionConverter))]
public readonly record struct Position(byte Value) : IApiQueryValue
{
    public string ToQueryValue() => Value.ToString();

    public override string ToString() => Value.ToString();
}
