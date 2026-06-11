using System;
using System.Globalization;
using System.Windows.Data;

namespace PitWall.Converters;

public class SvgPathConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path))
            return null!;

        var cleanPath = path.TrimStart('/');
        return new Uri($"pack://application:,,,/{cleanPath}", UriKind.Absolute);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}