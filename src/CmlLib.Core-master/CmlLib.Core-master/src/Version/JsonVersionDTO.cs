using System.Text.Json.Serialization;
using CmlLib.Core.Files;
using CmlLib.Core.Internals;
using CmlLib.Core.Java;

namespace CmlLib.Core.Version;

public class JsonVersionDTO
{
    [JsonPropertyName("inheritsFrom")]
    public string? InheritsFrom { get; set; }

    [JsonPropertyName("assetIndex")]
    public AssetMetadata? AssetIndex { get; set; }

    [JsonPropertyName("assets")]
    public string? Assets { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("javaVersion")]
    public JavaVersion? JavaVersion { get; set; }

    [JsonPropertyName("jar")]
    public string? Jar { get; set; }

    [JsonPropertyName("mainClass")]
    public string? MainClass { get; set; }

    [JsonPropertyName("minecraftArguments")]
    public string? MinecraftArguments { get; set; }

    // Handle DateTimeOffset with string properties
    [JsonPropertyName("releaseTime")]
    [JsonIgnore]
    public DateTimeOffset ReleaseTime { get; set; }

    [JsonPropertyName("releaseTime")]
    public string? ReleaseTimeString
    {
        set
        {
            if (!string.IsNullOrEmpty(value) && DateTimeOffset.TryParse(value, out var result))
                ReleaseTime = result;
        }
    }

    [JsonPropertyName("time")]
    [JsonIgnore]
    public DateTimeOffset Time { get; set; }

    [JsonPropertyName("time")]
    public string? TimeString
    {
        set
        {
            if (!string.IsNullOrEmpty(value) && DateTimeOffset.TryParse(value, out var result))
                Time = result;
        }
    }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    // Handle complianceLevel with dual properties (can be number or string)
    [JsonPropertyName("complianceLevel")]
    [JsonIgnore]
    public string? ComplianceLevel { get; set; }

    [JsonPropertyName("complianceLevel")]
    public int? ComplianceLevelNumber
    {
        set => ComplianceLevel = value?.ToString();
    }

    // Handle minimumLauncherVersion with dual properties (can be number or string)
    [JsonPropertyName("minimumLauncherVersion")]
    [JsonIgnore]
    public string? MinimumLauncherVersion { get; set; }

    [JsonPropertyName("minimumLauncherVersion")]
    public int? MinimumLauncherVersionNumber
    {
        set => MinimumLauncherVersion = value?.ToString();
    }
}