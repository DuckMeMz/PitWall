using System.ComponentModel;

namespace PitWall.Models;

public enum OpenF1APIEndpoint : byte
{
    Unknown = 0,

    [Description("car_data")]
    CarData,

    [Description("championship_drivers")]
    ChampionshipDrivers,

    [Description("championship_teams")]
    ChampionshipTeams,

    [Description("drivers")]
    Drivers,

    [Description("intervals")]
    Intervals,

    [Description("laps")]
    Laps,

    [Description("location")]
    Location,

    [Description("meetings")]
    Meetings,

    [Description("overtakes")]
    Overtakes,

    [Description("pit")]
    Pit,

    [Description("position")]
    Position,

    [Description("race_control")]
    RaceControl,

    [Description("sessions")]
    Sessions,

    [Description("session_result")]
    SessionResult,

    [Description("starting_grid")]
    StartingGrid,

    [Description("stints")]
    Stints,

    [Description("team_radio")]
    TeamRadio,

    [Description("weather")]
    Weather
}
