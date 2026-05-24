using System;
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

    public sealed record LeafApiCosmeticVariant(
        [property: JsonPropertyName("slug")] string Slug,
        [property: JsonPropertyName("label")] string Label,
        [property: JsonPropertyName("color")] string? Color = null,
        [property: JsonPropertyName("shade")] string? Shade = null,
        [property: JsonPropertyName("url")] string Url = "",
        [property: JsonPropertyName("sha256")] string? Sha256 = null);

    public sealed record LeafApiCatalogCosmetic(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("category")] string Category,
        [property: JsonPropertyName("assetUrl")] string AssetUrl,
        [property: JsonPropertyName("price_coins")] int PriceCoins,
        [property: JsonPropertyName("rarity")] string? Rarity = null,
        [property: JsonPropertyName("model_format")] string? ModelFormat = null,
        [property: JsonPropertyName("bbmodelUrl")] string? BBModelUrl = null,
        [property: JsonPropertyName("attachment")] string? Attachment = null,
        [property: JsonPropertyName("offset_x")] float OffsetX = 0f,
        [property: JsonPropertyName("offset_y")] float OffsetY = 0f,
        [property: JsonPropertyName("offset_z")] float OffsetZ = 0f,
        [property: JsonPropertyName("rotation_y")] float RotationY = 0f,
        [property: JsonPropertyName("scale")] float Scale = 1f,
        [property: JsonPropertyName("supports_variants")] bool SupportsVariants = false,
        [property: JsonPropertyName("variants")] List<LeafApiCosmeticVariant>? Variants = null,
        [property: JsonPropertyName("default_variant")] string? DefaultVariant = null,
        [property: JsonPropertyName("supports_scale")] bool SupportsScale = false,
        [property: JsonPropertyName("supports_offset")] bool SupportsOffset = false);

    public sealed record LeafApiEquippedCosmetic(
        string Category,
        string Name,
        [property: JsonPropertyName("assetUrl")] string AssetUrl,
        [property: JsonPropertyName("variant")] string? Variant = null,
        [property: JsonPropertyName("user_scale")] double? UserScale = null,
        [property: JsonPropertyName("user_offset_x")] double? UserOffsetX = null,
        [property: JsonPropertyName("user_offset_y")] double? UserOffsetY = null,
        [property: JsonPropertyName("user_offset_z")] double? UserOffsetZ = null);

    public sealed record LeafApiCustomizeCosmeticRequest(
        [property: JsonPropertyName("variant")] string? Variant = null,
        [property: JsonPropertyName("user_scale")] double? UserScale = null,
        [property: JsonPropertyName("user_offset_x")] double? UserOffsetX = null,
        [property: JsonPropertyName("user_offset_y")] double? UserOffsetY = null,
        [property: JsonPropertyName("user_offset_z")] double? UserOffsetZ = null);

    public sealed record LeafApiCustomizeCosmeticResponse(
        [property: JsonPropertyName("ok")] bool Ok,
        [property: JsonPropertyName("slug")] string? Slug,
        [property: JsonPropertyName("variant")] string? Variant,
        [property: JsonPropertyName("user_scale")] double? UserScale,
        [property: JsonPropertyName("user_offset_x")] double? UserOffsetX,
        [property: JsonPropertyName("user_offset_y")] double? UserOffsetY,
        [property: JsonPropertyName("user_offset_z")] double? UserOffsetZ);

    public sealed record LeafApiConfig(
        [property: JsonPropertyName("onlineCount")] int OnlineCount,
        [property: JsonPropertyName("authRequired")] bool AuthRequired,
        [property: JsonPropertyName("motd")] string? Motd,
        [property: JsonPropertyName("motdColor")] string? MotdColor = null,
        [property: JsonPropertyName("motdBgColor")] string? MotdBgColor = null,
        [property: JsonPropertyName("motdLink")] string? MotdLink = null,
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
        [property: JsonPropertyName("linkUrl")] string LinkUrl)
    {
        private static readonly HashSet<string> SafeLinkHosts = new(StringComparer.OrdinalIgnoreCase)
        {
            "leafclient.com",
            "www.leafclient.com",
            "api.leafclient.com",
            "cdn.leafclient.com",
            "discord.gg",
            "discord.com"
        };

        public static bool IsSafeLinkUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
            if (uri.Scheme != Uri.UriSchemeHttps) return false;
            return SafeLinkHosts.Contains(uri.Host);
        }
    }

    public sealed record LeafApiNewsResponse(
        [property: JsonPropertyName("items")] List<LeafApiNewsItem> Items);

    public sealed record LeafApiRegisterRequest(
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("password")] string Password,
        [property: JsonPropertyName("device_hash")] string? DeviceHash = null,
        [property: JsonPropertyName("referral_code")] string? ReferralCode = null);

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

    public sealed record LeafApiUnequipRequest(
        [property: JsonPropertyName("category")] string Category);

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
        [property: JsonPropertyName("had_leaf_plus_revoked")] bool HadLeafPlusRevoked = false,
        [property: JsonPropertyName("had_leaf_plus_trial")] bool HadLeafPlusTrial = false,
        [property: JsonPropertyName("is_trial")] bool IsTrial = false,
        [property: JsonPropertyName("referral_code")] string? ReferralCode = null,
        [property: JsonPropertyName("is_referral_creator")] bool IsReferralCreator = false,
        [property: JsonPropertyName("referral_qualified_count")] int ReferralQualifiedCount = 0,
        [property: JsonPropertyName("creator_leaf_plus_credit_days")] int CreatorLeafPlusCreditDays = 0);

    public sealed record LeafApiReferralMilestone(
        [property: JsonPropertyName("count")] int Count,
        [property: JsonPropertyName("cape_slug")] string CapeSlug);

    public sealed record LeafApiReferralStats(
        [property: JsonPropertyName("code")] string Code,
        [property: JsonPropertyName("qualified_count")] int QualifiedCount,
        [property: JsonPropertyName("pending_count")] int PendingCount,
        [property: JsonPropertyName("is_creator")] bool IsCreator,
        [property: JsonPropertyName("creator_leaf_plus_credit_days")] int CreatorLeafPlusCreditDays,
        [property: JsonPropertyName("next_milestone")] LeafApiReferralMilestone? NextMilestone);

    public sealed record LeafApiReferralCodeInfo(
        [property: JsonPropertyName("code")] string Code,
        [property: JsonPropertyName("is_creator")] bool IsCreator,
        [property: JsonPropertyName("can_change_vanity")] bool CanChangeVanity,
        [property: JsonPropertyName("vanity_cooldown_ms_remaining")] long VanityCooldownMsRemaining,
        [property: JsonPropertyName("qualified_count")] int QualifiedCount,
        [property: JsonPropertyName("creator_leaf_plus_credit_days")] int CreatorLeafPlusCreditDays);

    public sealed record LeafApiSetVanityCodeRequest(
        [property: JsonPropertyName("code")] string Code);

    public sealed record LeafApiSetVanityCodeResponse(
        [property: JsonPropertyName("ok")] bool Ok,
        [property: JsonPropertyName("code")] string? Code = null,
        [property: JsonPropertyName("reason")] string? Reason = null,
        [property: JsonPropertyName("cooldown_ms_remaining")] long? CooldownMsRemaining = null);

    public sealed record LeafApiTrialDeviceRequest(
        [property: JsonPropertyName("device_hash")] string DeviceHash);

    public sealed record LeafApiTrialEligibilityResponse(
        [property: JsonPropertyName("eligible")] bool Eligible,
        [property: JsonPropertyName("reason")] string? Reason,
        [property: JsonPropertyName("ms_until_eligible")] long? MsUntilEligible);

    public sealed record LeafApiTrialGrantResponse(
        [property: JsonPropertyName("granted")] bool Granted,
        [property: JsonPropertyName("reason")] string? Reason,
        [property: JsonPropertyName("leaf_plus_tier")] string? LeafPlusTier,
        [property: JsonPropertyName("leaf_plus_period_end")] string? LeafPlusPeriodEnd);

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
