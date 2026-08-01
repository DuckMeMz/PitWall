using System.Collections.ObjectModel;
using PitWall.Common;
using PitWall.Models;
using PitWall.Models.OpenF1Api;

namespace PitWall.ViewModels;

public class DriverTableViewModel : BindableBase
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMilliseconds(100);

    private readonly Dictionary<DriverNumber, ReplayDriverRow> _rowsByDriver = new();
    private ReplayDriverRow? _selectedDriver;
    private TimeSpan? _lastRefreshTime;

    public event EventHandler? SelectedDriverChanged;

    public ObservableCollection<ReplayDriverRow> DriverStates { get; } = new();

    public ReplayDriverRow? SelectedDriver
    {
        get => _selectedDriver;
        set
        {
            if (SetProperty(ref _selectedDriver, value))
            {
                SelectedDriverChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public void Initialise(IReadOnlyList<OpenF1Driver> drivers)
    {
        ArgumentNullException.ThrowIfNull(drivers);

        Clear();

        foreach (OpenF1Driver driver in drivers
            .OrderBy(driver => driver.DriverNumber.Value))
        {
            ReplayDriverRow row = new(driver);

            DriverStates.Add(row);
            _rowsByDriver[driver.DriverNumber] = row;
        }

        SelectedDriver = DriverStates.FirstOrDefault();
    }

    public void Update(ReplayTimeline timeline, TimeSpan targetTime, bool isPlaying)
    {
        ArgumentNullException.ThrowIfNull(timeline);

        bool shouldSortRows = false;
        bool shouldRefreshAllRows =
            !isPlaying ||
            _lastRefreshTime is not TimeSpan lastRefreshTime ||
            targetTime < lastRefreshTime ||
            targetTime - lastRefreshTime >= RefreshInterval;

        for (int driverIndex = 0; driverIndex < timeline.DriverCount; driverIndex++)
        {
            DriverReplayState state = timeline.GetStateAt(driverIndex, targetTime);

            if (!_rowsByDriver.TryGetValue(state.DriverNumber, out ReplayDriverRow? row))
            {
                continue;
            }

            if (shouldRefreshAllRows || ReferenceEquals(row, SelectedDriver))
            {
                if (row.Update(in state))
                {
                    shouldSortRows = true;
                }
            }
        }

        if (shouldRefreshAllRows)
        {
            _lastRefreshTime = targetTime;
        }

        if (shouldSortRows)
        {
            SortRowsByPosition();
        }
    }

    public void Clear()
    {
        _lastRefreshTime = null;
        SelectedDriver = null;
        DriverStates.Clear();
        _rowsByDriver.Clear();
    }

    private void SortRowsByPosition()
    {
        List<ReplayDriverRow> sortedRows = DriverStates
            .OrderBy(row => row.Position?.Value ?? byte.MaxValue)
            .ThenBy(row => row.DriverNumber.Value)
            .ToList();

        for (int targetPosition = 0; targetPosition < sortedRows.Count; targetPosition++)
        {
            ReplayDriverRow row = sortedRows[targetPosition];
            int currentPosition = DriverStates.IndexOf(row);

            if (currentPosition >= 0 && currentPosition != targetPosition)
            {
                DriverStates.Move(currentPosition, targetPosition);
            }
        }
    }
}
