using System.Text.Json.Serialization;
using PitWall.Converters;

namespace PitWall.Models;

[JsonConverter(typeof(LapNumberConverter))]
public readonly record struct LapNumber(int Value) : IApiQueryValue
{
    public string ToQueryValue() => Value.ToString();

    public override string ToString() => Value.ToString();
}
