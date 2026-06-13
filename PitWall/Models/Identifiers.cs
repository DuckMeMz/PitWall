using System.ComponentModel;
using System.Globalization;
using System.Text.Json.Serialization;

using PitWall.Converters;

namespace PitWall.Models;

[JsonConverter(typeof(MeetingKeyConverter))]
public readonly record struct MeetingKey(int Value): IApiQueryValue
{
    public static MeetingKey Latest => new(-1);
    public string ToQueryValue() => Value == -1 ? "latest" : Value.ToString();
};

[JsonConverter(typeof(SessionKeyConverter))]
public readonly record struct SessionKey(int Value) : IApiQueryValue
{
    public static SessionKey Latest => new(-1);
    public string ToQueryValue() => Value == -1 ? "latest" : Value.ToString();
};
[JsonConverter(typeof(CircuitKeyConverter))]
public readonly record struct CircuitKey(int Value) : IApiQueryValue
{
    public string ToQueryValue() => Value.ToString();
};
[JsonConverter(typeof(CountryKeyConverter))]
public readonly record struct CountryKey(int Value) : IApiQueryValue
{
    public string ToQueryValue() => Value.ToString();
};
[JsonConverter(typeof(DriverNumberConverter))]
public readonly record struct DriverNumber(byte Value) : IApiQueryValue
{
    public string ToQueryValue() => Value.ToString();
};
[JsonConverter(typeof(TeamNameConverter))]
public readonly record struct TeamName(string Value) : IApiQueryValue
{
    public string ToQueryValue() => Value.ToString();
};
[JsonConverter(typeof(LapNumberConverter))]
public readonly record struct LapNumber(int Value) : IApiQueryValue
{
    public string ToQueryValue() => Value.ToString();
};
[JsonConverter(typeof(PositionConverter))]
public readonly record struct Position(byte Value) : IApiQueryValue
{
    public string ToQueryValue() => Value.ToString();
};

public enum DrsState : byte
{
    Off = 0,
    OffAlternative = 1,

    Unknown2 = 2,
    Unknown3 = 3,

    DetectedEligible = 8,  //Car is within 1 second of the car ahead and can open DRS in the next DRS zone.

    Unknown9 = 9,

    On = 10,
    OnAlternativeA = 12,
    OnAlternativeB = 14
}

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

public readonly record struct Color(string HexCode)
{
    public byte R => Convert.ToByte(HexCode.Substring(0, 2), 16);
    public byte G => Convert.ToByte(HexCode.Substring(2, 2), 16);
    public byte B => Convert.ToByte(HexCode.Substring(4, 2), 16);
}

public enum RaceControlCategory : byte
{
    Unknown = 0,
    SessionStatus,
    CarEvent,
    Drs,
    Flag,
    SafetyCar
}

public enum RaceControlScope : byte
{
    Unknown = 0,
    Track,
    Driver,
    Sector
}

public enum FlagType : byte
{
    None = 0,

    [ApiQueryValue("CLEAR")]
    Clear,
    [ApiQueryValue("GREEN")]
    Green,
    [ApiQueryValue("YELLOW")]
    Yellow,
    [ApiQueryValue("DOUBLE%20YELLOW")]
    DoubleYellow,
    [ApiQueryValue("RED")]
    Red,
    [ApiQueryValue("BLUE")]
    Blue,
    [ApiQueryValue("WHITE")]
    White, //Slow car
    [ApiQueryValue("BLACK")]
    Black, //Dsq
    [ApiQueryValue("BLACK%20AND%20WHITE")]
    BlackAndWhite, //Track limits
    [ApiQueryValue("BLACK%20AND%20ORANGE")]
    BlackAndOrange, //Mech fail
    [ApiQueryValue("CHEQUERED")]
    Chequered,

    Unknown
}

public enum SessionType : byte
{
    Unknown = 0,

    [ApiQueryValue("PRACTICE")]
    Practice,
    [ApiQueryValue("QUALIFYING")]
    Qualifying,
    [ApiQueryValue("SPRINT")]
    Sprint,
    [ApiQueryValue("RACE")]
    Race
}

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

public enum OpenF1APIEndpoint : byte
{
    Unknown = 0,

    [Description("car_data")] CarData,
    [Description("championship_drivers")] ChampionshipDrivers,
    [Description("championship_teams")] ChampionshipTeams,
    [Description("drivers")] Drivers,
    [Description("intervals")] Intervals,
    [Description("laps")] Laps,
    [Description("location")] Location,
    [Description("meetings")] Meetings,
    [Description("overtakes")] Overtakes,
    [Description("pit")] Pit,
    [Description("race_control")] RaceControl,
    [Description("sessions")] Sessions,
    [Description("session_result")] SessionResult,
    [Description("starting_grid")] StartingGrid,
    [Description("stints")] Stints,
    [Description("team_radio")] TeamRadio,
    [Description("weather")] Weather
}

public readonly record struct Filter(string Expression)
{
    public static Filter Equal<T>(ApiField<T> field, T value) => 
        new($"{field}={Format(value)}");

    public static Filter NotEqual<T>(ApiField<T> field, T value) =>
        new($"{field}!={Format(value)}");

    public static Filter GreaterThan<T>(ApiField<T> field, T value) =>
        new($"{field}>{Format(value)}");

    public static Filter GreaterThanOrEqual<T>(ApiField<T> field, T value) =>
        new($"{field}>={Format(value)}");

    public static Filter LessThan<T>(ApiField<T> field, T value) =>
        new($"{field}<{Format(value)}");

    public static Filter LessThanOrEqual<T>(ApiField<T> field, T value) =>
        new($"{field}<={Format(value)}");

    private static string Format<T>(T value)
    {
        switch(value)
        {
            case null:
                return string.Empty;

            case IApiQueryValue queryValue:
                return queryValue.ToQueryValue();

            case bool boolean:
                return boolean ? "true" : "false";

            case Enum enumValue:
                return enumValue.ToApiQueryValue();

            case IFormattable formattable:
                return formattable.ToString(null, CultureInfo.InvariantCulture);

            default:
                return value.ToString() ?? string.Empty;
        }
    }
}

public readonly record struct ApiField<T>(string Name)
{
    public override string ToString()
    {
        return Name;
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class ApiQueryValueAttribute : Attribute
{
    public string Value { get; }

    public ApiQueryValueAttribute(string value)
    {
        Value = value;
    }
}

public static class ApiQueryValueExtensions
{
    public static string ToApiQueryValue(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();

        var attribute = member?
            .GetCustomAttributes(typeof(ApiQueryValueAttribute), false)
            .OfType<ApiQueryValueAttribute>()
            .FirstOrDefault();

        return attribute?.Value ?? value.ToString();
    }
}

public interface IApiQueryValue
{
    string ToQueryValue();
}