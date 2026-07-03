using System.Text.Json.Serialization;
using PitWall.Converters;

namespace PitWall.Models;

[JsonConverter(typeof(ApiEnumJsonConverter<SectorSegments>))]
public enum SectorSegments : ushort
{
    NotAvailable = 0,
    YellowSector = 2048,
    GreenSector = 2049,
    Unknown2050 = 2050,
    PurpleSector = 2051,
    Unknown2052 = 2052,
    PitLane = 2064,
    Unknown2068 = 2068
}
