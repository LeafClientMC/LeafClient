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
        [property: JsonPropertyName("motdColor")] string? MotdColor = null);

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

    public sealed record LeafApiBalance(
        [property: JsonPropertyName("coins")] int Coins);

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
        [property: JsonPropertyName("error")] string? Error);

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
}
