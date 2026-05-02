using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LeafClient.Services
{
    public sealed record LeafApiAuthResult(
        string AccessToken,
        string RefreshToken,
        LeafApiUser User);

    public sealed record LeafApiUser(
        string Id,
        string Username,
        [property: JsonPropertyName("minecraft_username")] string MinecraftUsername);

    public sealed record LeafApiOwnedCosmetic(
        string Id,
        string Name,
        string Category,
        [property: JsonPropertyName("assetUrl")] string AssetUrl,
        [property: JsonPropertyName("price_coins")] int PriceCoins);

    public sealed record LeafApiEquippedCosmetic(
        string Category,
        string Name,
        [property: JsonPropertyName("assetUrl")] string AssetUrl);

    public sealed record LeafApiConfig(
        [property: JsonPropertyName("onlineCount")] int OnlineCount,
        [property: JsonPropertyName("authRequired")] bool AuthRequired,
        [property: JsonPropertyName("motd")] string? Motd,
        [property: JsonPropertyName("motdColor")] string? MotdColor = null,
        [property: JsonPropertyName("minLauncherVersion")] string? MinLauncherVersion = null,
        [property: JsonPropertyName("killMessage")] string? KillMessage = null);

    public sealed record LeafApiNewsItem(
        [property: JsonPropertyName("slot")] int Slot,
        [property: JsonPropertyName("imageUrl")] string? ImageUrl,
        [property: JsonPropertyName("tagText")] string TagText,
        [property: JsonPropertyName("tagColorStart")] string TagColorStart,
        [property: JsonPropertyName("tagColorEnd")] string TagColorEnd,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("subtitle")] string Subtitle,
        [property: JsonPropertyName("linkUrl")] string LinkUrl);

    public sealed record LeafApiNewsResponse(
        [property: JsonPropertyName("items")] List<LeafApiNewsItem> Items);

    public sealed record LeafApiRegisterRequest(
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("password")] string Password);

    public sealed record LeafApiLoginRequest(
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("password")] string Password);

    public sealed record LeafApiMicrosoftAuthRequest(
        [property: JsonPropertyName("uuid")] string Uuid,
        [property: JsonPropertyName("accessToken")] string AccessToken);

    public sealed record LeafApiRefreshRequest(
        [property: JsonPropertyName("refreshToken")] string RefreshToken);

    public sealed record LeafApiEquipRequest(
        [property: JsonPropertyName("cosmetic_id")] string CosmeticId);

    public sealed record LeafApiDropCosmetic(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("category")] string Category,
        [property: JsonPropertyName("assetUrl")] string AssetUrl,
        [property: JsonPropertyName("rarity")] string Rarity);

    public sealed record LeafApiDropInfo(
        [property: JsonPropertyName("month")] string Month,
        [property: JsonPropertyName("cosmetics")] List<LeafApiDropCosmetic> Cosmetics,
        [property: JsonPropertyName("lp_compensation")] int LpCompensation,
        [property: JsonPropertyName("has_active_drop")] bool HasActiveDrop);

    public sealed record LeafApiDropClaimResult(
        [property: JsonPropertyName("month")] string Month,
        [property: JsonPropertyName("granted")] List<LeafApiDropCosmetic> Granted,
        [property: JsonPropertyName("already_owned")] List<LeafApiDropCosmetic> AlreadyOwned,
        [property: JsonPropertyName("lp_compensation")] int LpCompensation,
        [property: JsonPropertyName("new_coin_balance")] int? NewCoinBalance,
        [property: JsonPropertyName("is_first_claim")] bool IsFirstClaim);

    public sealed record LeafApiBalance(
        [property: JsonPropertyName("coins")] int Coins,
        [property: JsonPropertyName("is_leaf_plus")] bool IsLeafPlus = false,
        [property: JsonPropertyName("leaf_plus_tier")] string? LeafPlusTier = null,
        [property: JsonPropertyName("leaf_plus_period_end")] string? LeafPlusPeriodEnd = null,
        [property: JsonPropertyName("leaf_plus_revoke_seen_at")] string? LeafPlusRevokeSeenAt = null,
        [property: JsonPropertyName("had_leaf_plus_revoked")] bool HadLeafPlusRevoked = false);

    public sealed record LeafApiPlaytimeRequest(
        [property: JsonPropertyName("minutes")] int Minutes);

    public sealed record LeafApiPlaytimeResult(
        [property: JsonPropertyName("coins")] int Coins,
        [property: JsonPropertyName("awarded")] int Awarded);

    internal sealed record LeafApiErrorResponse(
        [property: JsonPropertyName("error")] string? Error);

    public sealed record LeafApiCoinPurchaseRequest(
        [property: JsonPropertyName("cosmetic_id")] string CosmeticId);

    public sealed record LeafApiCoinPurchaseResult(
        [property: JsonPropertyName("ok")] bool Ok,
        [property: JsonPropertyName("coins")] int Coins,
        [property: JsonPropertyName("error")] string? Error);

    public sealed record LeafApiLeafPlusSubscribeRequest(
        [property: JsonPropertyName("tier")] string Tier);

    public sealed record LeafApiLeafPlusSubscribeResult(
        [property: JsonPropertyName("ok")] bool Ok,
        [property: JsonPropertyName("coins")] int Coins,
        [property: JsonPropertyName("error")] string? Error,
        [property: JsonPropertyName("is_leaf_plus")] bool IsLeafPlus = false,
        [property: JsonPropertyName("leaf_plus_tier")] string? LeafPlusTier = null,
        [property: JsonPropertyName("leaf_plus_period_end")] string? LeafPlusPeriodEnd = null,
        [property: JsonPropertyName("required")] int? Required = null,
        [property: JsonPropertyName("current")] int? Current = null);

    public sealed record LeafApiHeartbeatRequest(
        [property: JsonPropertyName("identifier")] string Identifier,
        [property: JsonPropertyName("type")] string Type);

    public sealed record LeafApiDeleteHeartbeatRequest(
        [property: JsonPropertyName("identifier")] string Identifier);

    public sealed record LeafApiManifest(
        [property: JsonPropertyName("version")] string Version,
        [property: JsonPropertyName("jarUrl")] string JarUrl,
        [property: JsonPropertyName("sha256")] string Sha256,
        [property: JsonPropertyName("clientVersion")] string? ClientVersion = null,
        [property: JsonPropertyName("mcVersion")] string? McVersion = null,
        [property: JsonPropertyName("signature")] string? Signature = null);

    public sealed record LeafApiWebLinkCompleteRequest(
        [property: JsonPropertyName("linkCode")] string LinkCode,
        [property: JsonPropertyName("uuid")] string Uuid,
        [property: JsonPropertyName("accessToken")] string AccessToken);

    public sealed record LeafApiSyncPullResult(
        [property: JsonPropertyName("launcher_settings")] System.Text.Json.JsonElement? LauncherSettings,
        [property: JsonPropertyName("mod_settings")] System.Text.Json.JsonElement? ModSettings,
        [property: JsonPropertyName("server_list")] System.Text.Json.JsonElement? ServerList,
        [property: JsonPropertyName("resource_packs")] System.Text.Json.JsonElement? ResourcePacks,
        [property: JsonPropertyName("last_synced_at")] string? LastSyncedAt);

    public sealed record LeafApiSyncPushRequest(
        [property: JsonPropertyName("launcher_settings")] System.Text.Json.JsonElement? LauncherSettings,
        [property: JsonPropertyName("mod_settings")] System.Text.Json.JsonElement? ModSettings,
        [property: JsonPropertyName("server_list")] System.Text.Json.JsonElement? ServerList,
        [property: JsonPropertyName("resource_packs")] System.Text.Json.JsonElement? ResourcePacks);

    public sealed record LeafApiSyncPushResult(
        [property: JsonPropertyName("ok")] bool Ok,
        [property: JsonPropertyName("last_synced_at")] string? LastSyncedAt,
        [property: JsonPropertyName("error")] string? Error);

    public sealed record LeafApiFeaturedServer(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("ip_address")] string IpAddress,
        [property: JsonPropertyName("tagline")] string? Tagline,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("tag_text")] string? TagText,
        [property: JsonPropertyName("tag_color_start")] string? TagColorStart,
        [property: JsonPropertyName("tag_color_end")] string? TagColorEnd,
        [property: JsonPropertyName("slot")] int Slot,
        [property: JsonPropertyName("icon_url")] string? IconUrl);

    public sealed record LeafApiFeaturedServersResponse(
        [property: JsonPropertyName("items")] List<LeafApiFeaturedServer> Items);
}
