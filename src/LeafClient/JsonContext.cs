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
using LeafClient.PrivateServices;
using LeafClient.Services;
using LeafClient.Views;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using XboxAuthNet.Game.Jwt;
using XboxAuthNet.Game.SessionStorages;
using XboxAuthNet.Game.XboxAuth;
using XboxAuthNet.XboxLive.Responses;
using static LeafClient.Models.LauncherSettings;

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
    [JsonSerializable(typeof(LauncherProfile))]
    [JsonSerializable(typeof(List<LauncherProfile>))]
    [JsonSerializable(typeof(AccountEntry))]
    [JsonSerializable(typeof(List<AccountEntry>))]
    [JsonSerializable(typeof(EquippedCosmetics))]
    [JsonSerializable(typeof(OwnedCosmeticsFile))]
    [JsonSerializable(typeof(SettingsSecrets))]
    [JsonSerializable(typeof(AccountSecrets))]
    [JsonSerializable(typeof(LeafClient.Services.UpdateService.UpdateState))]
    [JsonSerializable(typeof(CosmeticPreset))]
    [JsonSerializable(typeof(List<CosmeticPreset>))]
    [JsonSerializable(typeof(ServerInfo))]
    [JsonSerializable(typeof(SkinInfo))]
    [JsonSerializable(typeof(InstalledMod))]
    [JsonSerializable(typeof(InstalledModpackEntry))]
    [JsonSerializable(typeof(List<InstalledModpackEntry>))]
    [JsonSerializable(typeof(InstalledContentEntry))]
    [JsonSerializable(typeof(List<InstalledContentEntry>))]
    [JsonSerializable(typeof(ModrinthSearchResponse))]
    [JsonSerializable(typeof(List<ModrinthVersionDetailed>))]
    [JsonSerializable(typeof(List<ModrinthVersion>))]
    [JsonSerializable(typeof(ModrinthVersion))]
    [JsonSerializable(typeof(ModrinthFile))]
    [JsonSerializable(typeof(ModrinthVersionDependency))]
    [JsonSerializable(typeof(List<ModrinthVersionDependency>))]
    [JsonSerializable(typeof(LeafClient.Services.ModFolderManagement.ModrinthCompatCache))]
    [JsonSerializable(typeof(ModrinthPack))]
    [JsonSerializable(typeof(ModrinthPackFile))]
    [JsonSerializable(typeof(MojangApiService.PlayerProfileResponse))]
    [JsonSerializable(typeof(MojangApiService.NameChangeStatusResponse))]
    [JsonSerializable(typeof(MojangApiService.MojangErrorResponse))]
    [JsonSerializable(typeof(ServerStatusResponse))]
    [JsonSerializable(typeof(PlayersInfo))]
    [JsonSerializable(typeof(MotdInfo))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(Dictionary<string, bool>))]
    [JsonSerializable(typeof(List<string>))]
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(object))]

    [JsonSerializable(typeof(FeatureSuggestionPayload))]
    [JsonSerializable(typeof(BugReportPayload))]
    [JsonSerializable(typeof(CrashReportPayload))]
    [JsonSerializable(typeof(ErrorResponse))]

    [JsonSerializable(typeof(JsonVersionManifestModel))]
    [JsonSerializable(typeof(LatestVersion))]
    [JsonSerializable(typeof(JsonVersionMetadataModel))]
    [JsonSerializable(typeof(IEnumerable<JsonVersionMetadataModel>))]
    [JsonSerializable(typeof(List<JsonVersionMetadataModel>))]

    [JsonSerializable(typeof(FabricLoader))]
    [JsonSerializable(typeof(IEnumerable<FabricLoader>))]
    [JsonSerializable(typeof(IReadOnlyCollection<FabricLoader>))]
    [JsonSerializable(typeof(List<FabricLoader>))]

    [JsonSerializable(typeof(MFileMetadata))]
    [JsonSerializable(typeof(MLogFileMetadata))]
    [JsonSerializable(typeof(AssetMetadata))]
    [JsonSerializable(typeof(IReadOnlyCollection<string>))]

    [JsonSerializable(typeof(JsonVersionDTO))]

    [JsonSerializable(typeof(JavaVersion))]

    [JsonSerializable(typeof(OnlineCountData))]
    [JsonSerializable(typeof(GitHubFileContent))]
    [JsonSerializable(typeof(GitHubUpdateFileRequest))]
    [JsonSerializable(typeof(GitHubUpdateFileResponse))]

    [JsonSerializable(typeof(JEProfile))]
    [JsonSerializable(typeof(JEProfileSkin))]
    [JsonSerializable(typeof(JEProfileCape))]
    [JsonSerializable(typeof(JEToken))]
    [JsonSerializable(typeof(XboxAuthTokens))]
    [JsonSerializable(typeof(XboxAuthResponse))]
    [JsonSerializable(typeof(JsonElement))]
    [JsonSerializable(typeof(JsonObject))]
    [JsonSerializable(typeof(JsonNode))]

    [JsonSerializable(typeof(XboxUserTokenRequestPayload))]
    [JsonSerializable(typeof(XboxUserTokenRequestProperties))]

    [JsonSerializable(typeof(XboxErrorResponse))]

    [JsonSerializable(typeof(XboxXstsRequestPayload))]
    [JsonSerializable(typeof(XboxXstsRequestProperties))]

    [JsonSerializable(typeof(XboxSignedUserTokenRequest))]
    [JsonSerializable(typeof(XboxSisuAuthRequest))]
    [JsonSerializable(typeof(XboxXstsRequest))]

    [JsonSerializable(typeof(XboxSignedUserTokenRequestPayload))]
    [JsonSerializable(typeof(XboxSignedUserTokenRequestProperties))]
    [JsonSerializable(typeof(XboxDeviceAuthRequestPayload))]
    [JsonSerializable(typeof(XboxDeviceAuthRequestProperties))]

    [JsonSerializable(typeof(AAdsAdResponse))]
    [JsonSerializable(typeof(ModCleanupEntry))]
    [JsonSerializable(typeof(List<ModCleanupEntry>))]

    [JsonSerializable(typeof(XboxDeviceTokenRequestBody))]

    [JsonSerializable(typeof(XblAuthRequest))]
    [JsonSerializable(typeof(XblAuthProperties))]
    [JsonSerializable(typeof(XstsAuthRequest))]
    [JsonSerializable(typeof(XstsAuthProperties))]
    [JsonSerializable(typeof(McLoginRequest))]

    [JsonSerializable(typeof(LeafClient.Services.LeafApiAuthResult))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiUser))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiOwnedCosmetic))]
    [JsonSerializable(typeof(List<LeafClient.Services.LeafApiOwnedCosmetic>))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiCatalogCosmetic))]
    [JsonSerializable(typeof(List<LeafClient.Services.LeafApiCatalogCosmetic>))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiEquippedCosmetic))]
    [JsonSerializable(typeof(List<LeafClient.Services.LeafApiEquippedCosmetic>))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiCosmeticVariant))]
    [JsonSerializable(typeof(List<LeafClient.Services.LeafApiCosmeticVariant>))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiCustomizeCosmeticRequest))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiCustomizeCosmeticResponse))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiConfig))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiRegisterRequest))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiLoginRequest))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiMicrosoftAuthRequest))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiRefreshRequest))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiEquipRequest))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiUnequipRequest))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiDropCosmetic))]
    [JsonSerializable(typeof(List<LeafClient.Services.LeafApiDropCosmetic>))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiDropInfo))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiDropClaimResult))]
    [JsonSerializable(typeof(List<LeafClient.Services.LeafApiDropClaimResult>))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiBalance))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiTrialDeviceRequest))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiTrialEligibilityResponse))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiTrialGrantResponse))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiReferralStats))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiReferralCodeInfo))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiReferralMilestone))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiSetVanityCodeRequest))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiSetVanityCodeResponse))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiPlaytimeRequest))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiPlaytimeResult))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiErrorResponse))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiCoinPurchaseRequest))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiCoinPurchaseResult))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiLeafPlusSubscribeRequest))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiLeafPlusSubscribeResult))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiHeartbeatRequest))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiDeleteHeartbeatRequest))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiManifest))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiWebLinkCompleteRequest))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiNewsItem))]
    [JsonSerializable(typeof(List<LeafClient.Services.LeafApiNewsItem>))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiNewsResponse))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiSyncPullResult))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiSyncPushRequest))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiSyncPushResult))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiFeaturedServer))]
    [JsonSerializable(typeof(List<LeafClient.Services.LeafApiFeaturedServer>))]
    [JsonSerializable(typeof(LeafClient.Services.LeafApiFeaturedServersResponse))]

    [JsonSerializable(typeof(LeafClient.Services.ModrinthMigrationVersion))]
    [JsonSerializable(typeof(List<LeafClient.Services.ModrinthMigrationVersion>))]
    [JsonSerializable(typeof(LeafClient.Services.ModrinthMigrationFile))]
    [JsonSerializable(typeof(LeafClient.Services.ModrinthMigrationHashes))]
    [JsonSerializable(typeof(LeafClient.Services.ModrinthMigrationProjectStub))]
    [JsonSerializable(typeof(LeafClient.Services.ModrinthMigrationUpdateRequest))]
    [JsonSerializable(typeof(LeafClient.Services.ModMigrationService.MigrationResult))]

    internal partial class JsonContext : JsonSerializerContext
    {
    }
}
