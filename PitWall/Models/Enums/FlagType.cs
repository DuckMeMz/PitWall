using System.Text.Json.Serialization;
using PitWall.Converters;

namespace PitWall.Models;

[JsonConverter(typeof(ApiEnumJsonConverter<FlagType>))]
public enum FlagType : byte
{
    None = 0,

    [ApiQueryValue("CLEAR")]
    Clear,

    [ApiQueryValue("GREEN")]
    Green,

    [ApiQueryValue("YELLOW")]
    Yellow,

    [ApiQueryValue("DOUBLE YELLOW")]
    DoubleYellow,

    [ApiQueryValue("RED")]
    Red,

    [ApiQueryValue("BLUE")]
    Blue,

    [ApiQueryValue("WHITE")]
    White,

    [ApiQueryValue("BLACK")]
    Black,

    [ApiQueryValue("BLACK AND WHITE")]
    BlackAndWhite,

    [ApiQueryValue("BLACK AND ORANGE")]
    BlackAndOrange,

    [ApiQueryValue("CHEQUERED")]
    Chequered,

    Unknown
}
