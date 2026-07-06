using PitWall.Models;
using PitWall.Models.OpenF1Api;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace PitWall.ViewModels;

public class ReplayDriverRow : INotifyPropertyChanged
{
    private DriverReplayState _state;

    public ReplayDriverRow(OpenF1Driver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);

        DriverNumber = driver.DriverNumber;
        DriverName =
            driver.NameAcronym ??
            driver.BroadcastName ??
            driver.FullName ??
            "-";

        TeamBrush = CreateTeamBrush(driver.TeamColor);
    }

    public DriverNumber DriverNumber { get; }
    public string DriverName { get; }
    public Brush TeamBrush { get; }

    public Position? Position => _state.Position;

    public int? Speed => _state.Telemetry?.Speed;
    public int? Gear => _state.Telemetry?.Gear;
    public DrsState? Drs => _state.Telemetry?.Drs;
    public int? Throttle => _state.Telemetry?.Throttle;
    public int? Brake => _state.Telemetry?.Brake;
    public int? Rpm => _state.Telemetry?.Rpm;

    public string Gap => FormatTimingGap(_state.Interval?.GapToLeader);
    public string Interval => FormatTimingGap(_state.Interval?.IntervalToAhead);

    public LapNumber? Lap => _state.CurrentLap?.LapNumber;
    public int? X => _state.Location?.X;
    public int? Y => _state.Location?.Y;

    public double RpmPercent => Math.Clamp((Rpm ?? 0) / 15000.0 * 100, 0, 100);

    public event PropertyChangedEventHandler? PropertyChanged;

    public static ReplayDriverRow From(OpenF1Driver? inputDriver)
    {
        OpenF1Driver driver = inputDriver ?? throw new ArgumentNullException(nameof(inputDriver));
        return new ReplayDriverRow(driver);
    }

    public bool Update(in DriverReplayState state)
    {
        if (state.DriverNumber != DriverNumber)
        {
            throw new ArgumentException(
                $"Cannot update driver {DriverNumber.Value} with state for driver {state.DriverNumber.Value}.",
                nameof(state));
        }

        DriverReplayState previousState = _state;
        _state = state;

        bool positionChanged = NotifyIfChanged(
            previousState.Position,
            state.Position,
            nameof(Position));

        NotifyIfChanged(previousState.Telemetry?.Speed, state.Telemetry?.Speed, nameof(Speed));
        NotifyIfChanged(previousState.Telemetry?.Gear, state.Telemetry?.Gear, nameof(Gear));
        NotifyIfChanged(previousState.Telemetry?.Drs, state.Telemetry?.Drs, nameof(Drs));
        NotifyIfChanged(previousState.Telemetry?.Throttle, state.Telemetry?.Throttle, nameof(Throttle));
        NotifyIfChanged(previousState.Telemetry?.Brake, state.Telemetry?.Brake, nameof(Brake));

        if (NotifyIfChanged(previousState.Telemetry?.Rpm, state.Telemetry?.Rpm, nameof(Rpm)))
        {
            OnPropertyChanged(nameof(RpmPercent));
        }

        NotifyIfChanged(
            previousState.Interval?.GapToLeader,
            state.Interval?.GapToLeader,
            nameof(Gap));
        NotifyIfChanged(
            previousState.Interval?.IntervalToAhead,
            state.Interval?.IntervalToAhead,
            nameof(Interval));
        NotifyIfChanged(
            previousState.CurrentLap?.LapNumber,
            state.CurrentLap?.LapNumber,
            nameof(Lap));
        NotifyIfChanged(previousState.Location?.X, state.Location?.X, nameof(X));
        NotifyIfChanged(previousState.Location?.Y, state.Location?.Y, nameof(Y));

        return positionChanged;
    }

    private bool NotifyIfChanged<T>(T previousValue, T currentValue, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(previousValue, currentValue))
        {
            return false;
        }

        OnPropertyChanged(propertyName);
        return true;
    }

    private static string FormatTimingGap(TimingGap? timingGap)
    {
        if (timingGap is not TimingGap value)
        {
            return "-";
        }

        if (value.Seconds is double seconds)
        {
            return seconds.ToString("0.000", CultureInfo.InvariantCulture);
        }

        return string.IsNullOrWhiteSpace(value.RawValue)
            ? "-"
            : value.RawValue;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    internal static Brush CreateTeamBrush(PitWall.Models.Color? teamColor)
    {
        if (teamColor is null ||
            string.IsNullOrWhiteSpace(teamColor.Value.HexCode) ||
            teamColor.Value.HexCode.Length < 6)
        {
            return Brushes.Gray;
        }

        try
        {
            SolidColorBrush brush = new(System.Windows.Media.Color.FromRgb(
                teamColor.Value.R,
                teamColor.Value.G,
                teamColor.Value.B));
            brush.Freeze();
            return brush;
        }
        catch
        {
            return Brushes.Gray;
        }
    }
}