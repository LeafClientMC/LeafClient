using System.Text.Json;
using CmlLib.Core.Json; // Add this

namespace CmlLib.Core.Version;

public static class JsonVersionParser
{
    public static IVersion ParseFromJsonString(string json, JsonVersionParserOptions options)
    {
        var document = JsonDocument.Parse(json);
        return ParseFromJson(document, options);
    }

    public static IVersion ParseFromJsonStream(Stream stream, JsonVersionParserOptions options)
    {
        var document = JsonDocument.Parse(stream);
        return ParseFromJson(document, options);
    }

    public static IVersion ParseFromJson(JsonDocument json, JsonVersionParserOptions options)
    {
        try
        {
            // Fix: Use source-generated deserialization
            var dto = json.RootElement.Deserialize(
                CmlLibJsonContext.Default.JsonVersionDTO);

            if (dto == null)
                throw new VersionParseException("Failed to deserialize version DTO");

            return new JsonVersion(json, dto, options);
        }
        catch (VersionParseException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new VersionParseException(ex);
        }
    }
}