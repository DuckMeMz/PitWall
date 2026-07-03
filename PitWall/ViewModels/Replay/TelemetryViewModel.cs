using System.ComponentModel;
using System.Windows.Media;
using PitWall.Common;
using PitWall.Models;

namespace PitWall.ViewModels;

public class TelemetryViewModel : BindableBase, IDisposable
{
    private ReplayDriverRow? _driver;

    public string DriverName => _driver?.DriverName ?? "Select driver";
    public DriverNumber? DriverNumber => _driver?.DriverNumber;
    public Brush TeamBrush => _driver?.TeamBrush ?? Brushes.Gray;

    public Position? Position => _driver?.Position;
    public int? Speed => _driver?.Speed;
    public int? Throttle => _driver?.Throttle;
    public int? Brake => _driver?.Brake;
    public int? Rpm => _driver?.Rpm;
    public double RpmPercent => _driver?.RpmPercent ?? 0;

    public void SelectDriver(ReplayDriverRow? driver)
    {
        if (ReferenceEquals(_driver, driver))
        {
            return;
        }

        if (_driver is not null)
        {
            _driver.PropertyChanged -= OnDriverPropertyChanged;
        }

        _driver = driver;

        if (_driver is not null)
        {
            _driver.PropertyChanged += OnDriverPropertyChanged;
        }

        OnPropertyChanged(string.Empty);
    }

    public void Dispose()
    {
        SelectDriver(null);
    }

    private void OnDriverPropertyChanged(
        object? sender,
        PropertyChangedEventArgs eventArgs)
    {
        OnPropertyChanged(eventArgs.PropertyName);
    }
}
