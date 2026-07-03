using System.Text.Json.Serialization;
using PitWall.Converters;

namespace PitWall.Models;

[JsonConverter(typeof(ColorConverter))]
public readonly record struct Color(string HexCode) : IApiQueryValue
{
    public byte R => Convert.ToByte(HexCode.Substring(0, 2), 16);
    public byte G => Convert.ToByte(HexCode.Substring(2, 2), 16);
    public byte B => Convert.ToByte(HexCode.Substring(4, 2), 16);

    public string ToQueryValue() => HexCode;
}
