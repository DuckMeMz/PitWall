using System.Text.Json.Serialization;
using PitWall.Converters;

namespace PitWall.Models;

[JsonConverter(typeof(ApiEnumJsonConverter<TyreCompound>))]
public enum TyreCompound : byte
{
    Unknown = 0,

    [ApiQueryValue("SOFT")]
    Soft,

    [ApiQueryValue("MEDIUM")]
    Medium,

    [ApiQueryValue("HARD")]
    Hard,

    [ApiQueryValue("INTERMEDIATE")]
    Intermediate,

    [ApiQueryValue("WET")]
    Wet
}
