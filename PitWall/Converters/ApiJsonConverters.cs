using System.Text.Json;
using System.Text.Json.Serialization;
using PitWall.Models;

namespace PitWall.Converters;

public class SessionKeyConverter : JsonConverter<SessionKey>
{
    public override SessionKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new SessionKey(reader.GetInt32());
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
        return new MeetingKey(reader.GetInt32());
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
        return new CircuitKey(reader.GetInt32());
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
        return new CountryKey(reader.GetInt32());
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
        return new DriverNumber(reader.GetByte());
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
        return new TeamName(reader.GetString() ?? string.Empty);
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
        return new LapNumber(reader.GetInt32());
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
        return new Position(reader.GetByte());
    }
    public override void Write(Utf8JsonWriter writer, Position value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}