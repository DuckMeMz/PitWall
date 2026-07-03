using System.Text.Json.Serialization;
using PitWall.Converters;

namespace PitWall.Models;

[JsonConverter(typeof(ApiEnumJsonConverter<SessionType>))]
public enum SessionType : byte
{
    Unknown = 0,

    [ApiQueryValue("Practice")]
    Practice,

    [ApiQueryValue("Qualifying")]
    Qualifying,

    [ApiQueryValue("Sprint")]
    Sprint,

    [ApiQueryValue("Sprint Qualifying")]
    SprintQualifying,

    [ApiQueryValue("Race")]
    Race
}
