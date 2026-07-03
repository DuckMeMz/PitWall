using System.Globalization;

namespace PitWall.Models;

public readonly record struct Filter(string Expression)
{
    public static Filter Equal<T>(ApiField<T> field, T value) =>
        new($"{field}={Encode(value)}");

    public static Filter NotEqual<T>(ApiField<T> field, T value) =>
        new($"{field}!={Encode(value)}");

    public static Filter GreaterThan<T>(ApiField<T> field, T value) =>
        new($"{field}>{Encode(value)}");

    public static Filter GreaterThanOrEqual<T>(ApiField<T> field, T value) =>
        new($"{field}>={Encode(value)}");

    public static Filter LessThan<T>(ApiField<T> field, T value) =>
        new($"{field}<{Encode(value)}");

    public static Filter LessThanOrEqual<T>(ApiField<T> field, T value) =>
        new($"{field}<={Encode(value)}");

    private static string Encode<T>(T value)
    {
        return Uri.EscapeDataString(Format(value));
    }

    private static string Format<T>(T value)
    {
        switch (value)
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
