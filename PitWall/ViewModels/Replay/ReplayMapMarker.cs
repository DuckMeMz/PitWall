using System.Globalization;
using System.Windows.Media;
using PitWall.Common;
using PitWall.Models;
using PitWall.Models.OpenF1Api;
using PitWall.Services;

namespace PitWall.ViewModels;

public class ReplayMapMarker : BindableBase
{
    private double _x;
    private double _y;
    private bool _isVisible;
    private bool _isStale;
    private bool _isSelected;

    public ReplayMapMarker(OpenF1Driver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);

        DriverNumber = driver.DriverNumber;
        Label =
            driver.NameAcronym ??
            driver.DriverNumber.Value.ToString(CultureInfo.InvariantCulture);
        MarkerBrush = ReplayDriverRow.CreateTeamBrush(driver.TeamColor);
    }

    public DriverNumber DriverNumber { get; }
    public string Label { get; }
    public Brush MarkerBrush { get; }

    public double X
    {
        get => _x;
        private set => SetProperty(ref _x, value);
    }

    public double Y
    {
        get => _y;
        private set => SetProperty(ref _y, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        private set => SetProperty(ref _isVisible, value);
    }

    public bool IsStale
    {
        get => _isStale;
        private set => SetProperty(ref _isStale, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public void Apply(TrackMapPoint? position, bool isStale)
    {
        if (position is not TrackMapPoint point)
        {
            IsVisible = false;
            IsStale = false;
            return;
        }

        X = point.X;
        Y = point.Y;
        IsStale = isStale;
        IsVisible = true;
    }
}