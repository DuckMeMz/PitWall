using System.ComponentModel;

namespace PitWall.Models;

public readonly record struct MeetingKey(int Value);
public readonly record struct SessionKey(int Value);
public readonly record struct CircuitKey(int Value);
public readonly record struct CountryKey(int Value);

public readonly record struct DriverNumber(byte Value);
public readonly record struct TeamName(string Value);

public readonly record struct LapNumber(int Value);
public readonly record struct Position(byte Value);

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
    Clear,
    Green,
    Yellow,
    DoubleYellow,
    Red,
    Blue,
    White, //Slow car
    Black, //Dsq
    BlackAndWhite, //Track limits
    BlackAndOrange, //Mech fail
    Chequered,
    Unknown
}

public enum SessionType : byte
{
    Unknown = 0,
    Practice,
    Qualifying,
    Sprint,
    Race
}

public enum TyreCompound : byte
{
    Unknown = 0,
    Soft,
    Medium,
    Hard,
    Intermediate,
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

public readonly record struct Filter(string Field, string Operator, string Value)
{
    public string Key => $"{Field}{Operator}";
}