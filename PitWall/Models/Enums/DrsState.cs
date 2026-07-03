using System.Text.Json.Serialization;
using PitWall.Converters;

namespace PitWall.Models;

[JsonConverter(typeof(ApiEnumJsonConverter<DrsState>))]
public enum DrsState : byte
{
    Off = 0,
    OffAlternative = 1,
    Unknown2 = 2,
    Unknown3 = 3,

    // Car is within one second of the car ahead and can open DRS in the next zone.
    DetectedEligible = 8,

    Unknown9 = 9,
    On = 10,
    OnAlternativeA = 12,
    OnAlternativeB = 14
}
