using System.Text.Json;
using System.Text.Json.Serialization;

namespace CmlLib.Core.Internals;

// Change from internal to public
public class SafeDateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (DateTimeOffset.TryParse(stringValue, out var result))
                return result;
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            // Handle timestamp if needed
            return DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64());
        }

        return default;
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("O")); // ISO 8601 format
    }
}