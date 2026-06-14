using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using PitWall.Models;

namespace PitWall.Converters;

public class SessionKeyConverter : JsonConverter<SessionKey>
{
    public override SessionKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new SessionKey(ApiJsonReader.GetInt32(ref reader));
    }

    public override void Write(Utf8JsonWriter writer, SessionKey value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}

public class MeetingKeyConverter : JsonConverter<MeetingKey>
{
    public override MeetingKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new MeetingKey(ApiJsonReader.GetInt32(ref reader));
    }

    public override void Write(Utf8JsonWriter writer, MeetingKey value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}

public class CircuitKeyConverter : JsonConverter<CircuitKey>
{
    public override CircuitKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new CircuitKey(ApiJsonReader.GetInt32(ref reader));
    }

    public override void Write(Utf8JsonWriter writer, CircuitKey value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}

public class CountryKeyConverter : JsonConverter<CountryKey>
{
    public override CountryKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new CountryKey(ApiJsonReader.GetInt32(ref reader));
    }

    public override void Write(Utf8JsonWriter writer, CountryKey value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}

public class DriverNumberConverter : JsonConverter<DriverNumber>
{
    public override DriverNumber Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new DriverNumber(ApiJsonReader.GetByte(ref reader));
    }

    public override void Write(Utf8JsonWriter writer, DriverNumber value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}

public class TeamNameConverter : JsonConverter<TeamName>
{
    public override TeamName Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new TeamName(ApiJsonReader.GetString(ref reader));
    }

    public override void Write(Utf8JsonWriter writer, TeamName value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}

public class LapNumberConverter : JsonConverter<LapNumber>
{
    public override LapNumber Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new LapNumber(ApiJsonReader.GetInt32(ref reader));
    }

    public override void Write(Utf8JsonWriter writer, LapNumber value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}

public class PositionConverter : JsonConverter<Position>
{
    public override Position Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new Position(ApiJsonReader.GetByte(ref reader));
    }

    public override void Write(Utf8JsonWriter writer, Position value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}

public class ColorConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new Color(ApiJsonReader.GetString(ref reader));
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.HexCode);
    }
}

public class TimingGapConverter : JsonConverter<TimingGap>
{
    public override TimingGap Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch(reader.TokenType)
        {
            case JsonTokenType.Number:
                return new TimingGap(reader.GetDouble(), null);
            case JsonTokenType.String:
                return ReadStringGap(reader.GetString());
            case JsonTokenType.Null:
                return new TimingGap(null, null);
            default:
                throw new JsonException($"Can't convert {reader.TokenType} to {nameof(TimingGap)}.");
        }
    }

    public override void Write(Utf8JsonWriter writer, TimingGap value, JsonSerializerOptions options)
    {
        if (value.Seconds.HasValue)
        {
            writer.WriteNumberValue(value.Seconds.Value);
            return;
        }

        writer.WriteStringValue(value.RawValue);
    }

    private static TimingGap ReadStringGap(string? value)
    {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double seconds))
        {
            return new TimingGap(seconds, value);
        }

        return new TimingGap(null, value);
    }
}

public class ApiEnumJsonConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            long rawValue = reader.GetInt64();

            if (Enum.IsDefined(typeof(TEnum), rawValue))
            {
                return (TEnum)Enum.ToObject(typeof(TEnum), rawValue);
            }

            return GetUnknownOrDefault();
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Cannot convert {reader.TokenType} to {typeof(TEnum).Name}.");
        }

        string? value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return GetUnknownOrDefault();
        }

        foreach (FieldInfo field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            string name = field.Name;
            string? apiValue = field.GetCustomAttribute<ApiQueryValueAttribute>()?.Value;

            if (Matches(value, name) || (apiValue is not null && Matches(value, apiValue)))
            {
                return (TEnum)field.GetValue(null)!;
            }
        }

        return Enum.TryParse(value, ignoreCase: true, out TEnum parsed) ? parsed : GetUnknownOrDefault();
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToApiQueryValue());
    }

    private static bool Matches(string left, string right)
    {
        return string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string value)
    {
        return value.Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty);
    }

    private static TEnum GetUnknownOrDefault()
    {
        return Enum.TryParse("Unknown", ignoreCase: true, out TEnum unknown) ? unknown : default;
    }
}

internal static class ApiJsonReader
{
    public static int GetInt32(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetInt32(),
            JsonTokenType.String when int.TryParse(reader.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) => value,
            _ => throw new JsonException($"Cannot convert {reader.TokenType} to {nameof(Int32)}.")
        };
    }

    public static byte GetByte(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Number => reader.GetByte(),
            JsonTokenType.String when byte.TryParse(reader.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out byte value) => value,
            _ => throw new JsonException($"Cannot convert {reader.TokenType} to {nameof(Byte)}.")
        };
    }

    public static string GetString(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString() ?? string.Empty,
            JsonTokenType.Number => reader.GetDouble().ToString(CultureInfo.InvariantCulture),
            JsonTokenType.True => "true",
            JsonTokenType.False => "false",
            JsonTokenType.Null => string.Empty,
            _ => throw new JsonException($"Cannot convert {reader.TokenType} to {nameof(String)}.")
        };
    }
}
