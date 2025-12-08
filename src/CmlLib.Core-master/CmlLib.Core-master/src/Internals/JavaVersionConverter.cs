using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using CmlLib.Core.Java;

namespace CmlLib.Core.Internals
{
    public class JavaVersionConverter : JsonConverter<JavaVersion>
    {
        public override JavaVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? component = null;
            string? majorVersion = null;

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        var propertyName = reader.GetString();
                        reader.Read();

                        switch (propertyName)
                        {
                            case "component":
                                component = reader.GetString();
                                break;
                            case "majorVersion":
                                if (reader.TokenType == JsonTokenType.Number)
                                {
                                    if (reader.TryGetInt32(out int intValue))
                                        majorVersion = intValue.ToString();
                                    else if (reader.TryGetInt64(out long longValue))
                                        majorVersion = longValue.ToString();
                                }
                                else if (reader.TokenType == JsonTokenType.String)
                                {
                                    majorVersion = reader.GetString();
                                }
                                break;
                        }
                    }
                }
            }

            if (component == null)
                throw new JsonException("Component is required for JavaVersion");

            return new JavaVersion(component, majorVersion);
        }

        public override void Write(Utf8JsonWriter writer, JavaVersion value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("component", value.Component);

            if (value.MajorVersion != null)
            {
                if (int.TryParse(value.MajorVersion, out int intValue))
                    writer.WriteNumber("majorVersion", intValue);
                else
                    writer.WriteString("majorVersion", value.MajorVersion);
            }

            writer.WriteEndObject();
        }
    }
}