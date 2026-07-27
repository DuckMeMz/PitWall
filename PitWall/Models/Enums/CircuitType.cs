using System.Text.Json.Serialization;
using PitWall.Converters;

namespace PitWall.Models;


[JsonConverter(typeof(ApiEnumJsonConverter<CircuitType>))]
public enum CircuitType : byte
{
    Unknown = 0,

    Permanent,

    [ApiQueryValue("Temporary - Street")]
    TemporaryStreet,

    [ApiQueryValue("Temporary - Road")]
    TemporaryRoad
}