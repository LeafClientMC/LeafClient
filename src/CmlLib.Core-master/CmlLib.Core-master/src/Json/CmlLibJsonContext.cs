using CmlLib.Core.Files;
using CmlLib.Core.Internals;
using CmlLib.Core.Java;
using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ModLoaders.LiteLoader;
using CmlLib.Core.ModLoaders.QuiltMC;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core.Version;
using CmlLib.Core.VersionLoader;
using CmlLib.Core.VersionMetadata;
using System.Text.Json.Serialization;

namespace CmlLib.Core.Json;

[JsonSerializable(typeof(JsonVersionManifestModel))]
[JsonSerializable(typeof(JsonVersionMetadataModel))]
[JsonSerializable(typeof(FabricLoader))]
[JsonSerializable(typeof(QuiltLoader))]
[JsonSerializable(typeof(LiteLoaderVersion))]
[JsonSerializable(typeof(LiteLoaderLibrary))]
[JsonSerializable(typeof(IEnumerable<FabricLoader>))]
[JsonSerializable(typeof(IReadOnlyCollection<FabricLoader>))]
[JsonSerializable(typeof(IEnumerable<QuiltLoader>))]
[JsonSerializable(typeof(IReadOnlyCollection<QuiltLoader>))]
[JsonSerializable(typeof(IEnumerable<string>))]
[JsonSerializable(typeof(IReadOnlyCollection<string>))]
// Add these for version parsing
[JsonSerializable(typeof(JsonVersionDTO))]
[JsonSerializable(typeof(AssetMetadata))]
[JsonSerializable(typeof(MFileMetadata))]
[JsonSerializable(typeof(JavaVersion))]
[JsonSerializable(typeof(MLogFileMetadata))]
[JsonSerializable(typeof(MLibrary))]
[JsonSerializable(typeof(MArgument))]
[JsonSerializable(typeof(Dictionary<string, MFileMetadata>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
// Add these for internal types that might be used
[JsonSerializable(typeof(Rules.LauncherRule))]
[JsonSerializable(typeof(IEnumerable<Rules.LauncherRule>))]
[JsonSerializable(typeof(IReadOnlyCollection<Rules.LauncherRule>))]
// Add the converter types
[JsonSerializable(typeof(NumberToStringConverter))]
[JsonSerializable(typeof(SafeDateTimeOffsetJsonConverter))]
public partial class CmlLibJsonContext : JsonSerializerContext
{
}