using System.Text.Json.Serialization;
using PitWall.Converters;

namespace PitWall.Models;

[JsonConverter(typeof(TeamNameConverter))]
public readonly record struct TeamName(string Value) : IApiQueryValue
{
    public string ToQueryValue() => Value.ToString();

    public override string ToString() => Value;
}
