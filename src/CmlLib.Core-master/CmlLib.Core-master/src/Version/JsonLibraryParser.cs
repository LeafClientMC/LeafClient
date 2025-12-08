using System.Text.Json;
using CmlLib.Core.Files;
using CmlLib.Core.Internals;
using CmlLib.Core.Rules;
using CmlLib.Core.Json; // Add this

namespace CmlLib.Core.Version;

public static class JsonLibraryParser
{
    public static MLibrary? Parse(JsonElement element)
    {
        var name = element.GetPropertyValue("name");
        if (string.IsNullOrEmpty(name))
            return null;

        // rules
        IReadOnlyCollection<LauncherRule> rules;
        if (element.TryGetProperty("rules", out var rulesProp))
            rules = JsonRulesParser.Parse(rulesProp);
        else
            rules = Array.Empty<LauncherRule>();

        // extract.exclude
        IReadOnlyCollection<string> extractExcludes = [];
        if (element.TryGetProperty("extract", out var extractProp) && extractProp.TryGetProperty("exclude", out var excludeProp))
        {
            if (excludeProp.ValueKind == JsonValueKind.String)
            {
                var excludeStr = excludeProp.GetString();
                if (!string.IsNullOrEmpty(excludeStr))
                    extractExcludes = [excludeStr];
            }
            else if (excludeProp.ValueKind == JsonValueKind.Array)
            {
                extractExcludes = excludeProp.EnumerateArray()
                    .Select(item => item.GetString())
                    .Where(item => !string.IsNullOrEmpty(item))
                    .ToList()!;
            }
        }

        // forge serverreq, clientreq
        var isServerRequired = element
            .GetPropertyOrNull("serverreq")?
            .GetBoolean() ??
            true; // default value is true

        var isClientRequired = element
            .GetPropertyOrNull("clientreq")?
            .GetBoolean() ??
            true; // default value is true

        // artifact
        MFileMetadata? artifact = null;
        var artifactProp = element.GetPropertyOrNull("artifact") ??
                           element.GetPropertyOrNull("downloads")?.GetPropertyOrNull("artifact");
        if (artifactProp.HasValue)
            artifact = artifactProp.Value.Deserialize(CmlLibJsonContext.Default.MFileMetadata);

        // classifiers
        IReadOnlyDictionary<string, MFileMetadata>? classifiers = null;
        var classifiersProp = element.GetPropertyOrNull("classifies") ??
                              element.GetPropertyOrNull("downloads")?.GetPropertyOrNull("classifiers");
        if (classifiersProp.HasValue)
            classifiers = classifiersProp.Value.Deserialize(CmlLibJsonContext.Default.DictionaryStringMFileMetadata);

        // natives
        IReadOnlyDictionary<string, string>? natives = null;
        var nativesProp = element.GetPropertyOrNull("natives");
        if (nativesProp.HasValue)
            natives = nativesProp.Value.Deserialize(CmlLibJsonContext.Default.DictionaryStringString);

        // some libraries (forge, optifine, fabric) lack 'artifacts' or 'classifiers' property;
        // instead they have metadata properties directly
        if (artifact == null && natives == null)
        {
            string? directUrl = element.GetPropertyOrNull("url")?.GetString();
            string? directSha1 = element.GetPropertyOrNull("sha1")?.GetString();
            long? directSize = element.GetPropertyOrNull("size")?.GetInt64();

            // If direct properties exist, create MFileMetadata from them
            if (!string.IsNullOrEmpty(directUrl) || !string.IsNullOrEmpty(directSha1))
            {
                artifact = new MFileMetadata
                {
                    Url = directUrl,
                    Sha1 = directSha1,
                    Size = directSize ?? 0 // Default to 0 if size is not found
                };
                Console.WriteLine($"[JsonLibraryParser DEBUG] Created artifact from direct properties for library: {name}");
            }
        }

        return new MLibrary(name)
        {
            Artifact = artifact,
            Classifiers = classifiers,
            Natives = natives,
            Rules = rules,
            ExtractExcludes = extractExcludes,
            IsClientRequired = isClientRequired,
            IsServerRequired = isServerRequired
        };
    }
}