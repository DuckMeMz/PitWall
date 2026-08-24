using System.Globalization;
using System.Windows.Data;
using PitWall.Models;

namespace PitWall.Converters;

public class RangeStartToPixelsConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 4 ||
            values[0] is not ReplayBufferRange range ||
            values[1] is not DateTimeOffset sessionStart ||
            values[2] is not double durationSeconds ||
            values[3] is not double availableWidth ||
            durationSeconds <= 0 ||
            availableWidth <= 0)
        {
            return 0.0;
        }

        double seconds = string.Equals(parameter?.ToString(), "Width", StringComparison.OrdinalIgnoreCase)
            ? range.Duration.TotalSeconds
            : (range.Start - sessionStart).TotalSeconds;

        return Math.Max(0.0, seconds / durationSeconds * availableWidth);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return Array.Empty<object>();
    }
}
