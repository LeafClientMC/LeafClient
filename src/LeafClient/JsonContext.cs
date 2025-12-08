using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft.Authenticators;
using CmlLib.Core.Auth.Microsoft.Sessions;
using CmlLib.Core.Files;
using CmlLib.Core.Java;
using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core.Version;
using CmlLib.Core.VersionMetadata;
using LeafClient.Models;
using LeafClient.PrivateServices; // Required for FeatureSuggestionPayload, BugReportPayload, ErrorResponse
using LeafClient.Services;
using LeafClient.Views;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using XboxAuthNet.Game.Jwt;
using XboxAuthNet.Game.SessionStorages;
using XboxAuthNet.Game.XboxAuth;
using XboxAuthNet.XboxLive.Responses; // Required for XboxErrorResponse
using static LeafClient.Models.LauncherSettings;

// Added for XboxAuthNet.XboxLive.Requests
using XboxAuthNet.XboxLive.Requests;

namespace LeafClient
{
    [JsonSourceGenerationOptions(
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    )]
    [JsonSerializable(typeof(LauncherSettings))]
    [JsonSerializable(typeof(ServerInfo))]
    [JsonSerializable(typeof(SkinInfo))]
    [JsonSerializable(typeof(InstalledMod))]
    [JsonSerializable(typeof(ModrinthSearchResponse))]
    [JsonSerializable(typeof(List<ModrinthVersionDetailed>))]
    [JsonSerializable(typeof(List<ModrinthVersion>))]
    [JsonSerializable(typeof(ModrinthVersion))]
    [JsonSerializable(typeof(ModrinthFile))]
    [JsonSerializable(typeof(ModrinthPack))]
    [JsonSerializable(typeof(ModrinthPackFile))]
    [JsonSerializable(typeof(MojangApiService.PlayerProfileResponse))]
    [JsonSerializable(typeof(MojangApiService.NameChangeStatusResponse))]
    [JsonSerializable(typeof(MojangApiService.MojangErrorResponse))]
    [JsonSerializable(typeof(ServerStatusResponse))]
    [JsonSerializable(typeof(PlayersInfo))]
    [JsonSerializable(typeof(MotdInfo))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(object))]

    // NEW: Add payload types for SuggestionsService
    [JsonSerializable(typeof(FeatureSuggestionPayload))]
    [JsonSerializable(typeof(BugReportPayload))]
    [JsonSerializable(typeof(ErrorResponse))] // Ensure ErrorResponse is also serializable

    // CmlLib VersionMetadata types
    [JsonSerializable(typeof(JsonVersionManifestModel))]
    [JsonSerializable(typeof(LatestVersion))]
    [JsonSerializable(typeof(JsonVersionMetadataModel))]
    [JsonSerializable(typeof(IEnumerable<JsonVersionMetadataModel>))]
    [JsonSerializable(typeof(List<JsonVersionMetadataModel>))]

    // CmlLib FabricMC types
    [JsonSerializable(typeof(FabricLoader))]
    [JsonSerializable(typeof(IEnumerable<FabricLoader>))]
    [JsonSerializable(typeof(IReadOnlyCollection<FabricLoader>))]
    [JsonSerializable(typeof(List<FabricLoader>))]

    // CmlLib Files types
    [JsonSerializable(typeof(MFileMetadata))]
    [JsonSerializable(typeof(MLogFileMetadata))]
    [JsonSerializable(typeof(AssetMetadata))]
    [JsonSerializable(typeof(IReadOnlyCollection<string>))]

    // CmlLib Version types
    [JsonSerializable(typeof(JsonVersionDTO))]

    // CmlLib Java types
    [JsonSerializable(typeof(JavaVersion))]

    // GitHub
    [JsonSerializable(typeof(OnlineCountData))]
    [JsonSerializable(typeof(GitHubFileContent))]
    [JsonSerializable(typeof(GitHubUpdateFileRequest))]
    [JsonSerializable(typeof(GitHubUpdateFileResponse))]

    // Existing entries for CmlLib.Core.Auth.Microsoft and XboxAuthNet.Game
    [JsonSerializable(typeof(JEProfile))]
    [JsonSerializable(typeof(JEProfileSkin))]
    [JsonSerializable(typeof(JEProfileCape))]
    [JsonSerializable(typeof(JEToken))]
    [JsonSerializable(typeof(XboxAuthTokens))]
    [JsonSerializable(typeof(XboxAuthResponse))]
    [JsonSerializable(typeof(JsonElement))]
    [JsonSerializable(typeof(JsonObject))]
    [JsonSerializable(typeof(JsonNode))]

    // Existing entries for XboxUserTokenRequest payload
    [JsonSerializable(typeof(XboxUserTokenRequestPayload))]
    [JsonSerializable(typeof(XboxUserTokenRequestProperties))]

    // CRITICAL: Add XboxErrorResponse for AOT serialization from its CORRECT namespace
    [JsonSerializable(typeof(XboxErrorResponse))]

    // START: NEW entries for XboxXstsRequest payload (legacy /authorize)
    [JsonSerializable(typeof(XboxXstsRequestPayload))]
    [JsonSerializable(typeof(XboxXstsRequestProperties))]
    // END: NEW entries

    // CRITICAL: Add XboxAuthNet request types for device token authentication
    [JsonSerializable(typeof(XboxSignedUserTokenRequest))]
    [JsonSerializable(typeof(XboxSisuAuthRequest))]
    [JsonSerializable(typeof(XboxXstsRequest))]

    // In your LeafClient JsonContext.cs, add these:
    [JsonSerializable(typeof(XboxSignedUserTokenRequestPayload))]
    [JsonSerializable(typeof(XboxSignedUserTokenRequestProperties))]
    [JsonSerializable(typeof(XboxDeviceAuthRequestPayload))]
    [JsonSerializable(typeof(XboxDeviceAuthRequestProperties))]

    internal partial class JsonContext : JsonSerializerContext
    {
    }
}
