using System.Text.Json.Serialization;
using PitWall.Converters;

namespace PitWall.Models;

[JsonConverter(typeof(ApiEnumJsonConverter<RaceControlScope>))]
public enum RaceControlScope : byte
{
    Unknown = 0,
    Track,
    Driver,
    Sector
}
