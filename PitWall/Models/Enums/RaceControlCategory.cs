using System.Text.Json.Serialization;
using PitWall.Converters;

namespace PitWall.Models;

[JsonConverter(typeof(ApiEnumJsonConverter<RaceControlCategory>))]
public enum RaceControlCategory : byte
{
    Unknown = 0,
    SessionStatus,
    CarEvent,
    Drs,
    Flag,
    SafetyCar
}
