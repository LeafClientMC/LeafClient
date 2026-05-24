// File: XboxAuthNet/XboxLive/Requests/XboxAuthRequestModels.cs
using System.Text.Json.Serialization;

namespace XboxAuthNet.XboxLive.Requests
{
    // --- Types for XboxUserTokenRequest ---
    public record XboxUserTokenRequestProperties(
        [property: JsonPropertyName("AuthMethod")] string AuthMethod,
        [property: JsonPropertyName("SiteName")] string SiteName,
        [property: JsonPropertyName("RpsTicket")] string RpsTicket
    );

    public record XboxUserTokenRequestPayload(
        [property: JsonPropertyName("RelyingParty")] string RelyingParty,
        [property: JsonPropertyName("TokenType")] string TokenType,
        [property: JsonPropertyName("Properties")] XboxUserTokenRequestProperties Properties
    );

    // --- Types for XboxXstsRequest (specifically for /xsts/authorize endpoint) ---
    public record XboxXstsAuthorizeRequestProperties(
        [property: JsonPropertyName("UserTokens")] string[] UserTokens,
        [property: JsonPropertyName("DeviceToken")] string? DeviceToken,
        [property: JsonPropertyName("TitleToken")] string? TitleToken,
        [property: JsonPropertyName("OptionalDisplayClaims")] string[]? OptionalDisplayClaims,
        [property: JsonPropertyName("SandboxId")] string SandboxId
    );

    public record XboxXstsAuthorizeRequestPayload(
        [property: JsonPropertyName("RelyingParty")] string RelyingParty,
        [property: JsonPropertyName("TokenType")] string TokenType,
        [property: JsonPropertyName("Properties")] XboxXstsAuthorizeRequestProperties Properties
    );

    // --- NEW: Types for XboxSignedUserTokenRequest ---
    public record XboxSignedUserTokenRequestProperties(
        [property: JsonPropertyName("AuthMethod")] string AuthMethod,
        [property: JsonPropertyName("SiteName")] string SiteName,
        [property: JsonPropertyName("RpsTicket")] string RpsTicket
    );

    public record XboxSignedUserTokenRequestPayload(
        [property: JsonPropertyName("RelyingParty")] string RelyingParty,
        [property: JsonPropertyName("TokenType")] string TokenType,
        [property: JsonPropertyName("Properties")] XboxSignedUserTokenRequestProperties Properties
    );

    // --- NEW: Types for XboxDeviceAuthRequest ---
    public record XboxDeviceAuthRequestProperties(
    [property: JsonPropertyName("AuthMethod")] string AuthMethod,
    [property: JsonPropertyName("DeviceType")] string DeviceType,
    [property: JsonPropertyName("Version")] string Version,
    [property: JsonPropertyName("ProofKey")] object ProofKey
    // Note: Removed Id and SerialNumber as they're not in the original anonymous type
);

    public record XboxDeviceAuthRequestPayload(
        [property: JsonPropertyName("RelyingParty")] string RelyingParty,
        [property: JsonPropertyName("TokenType")] string TokenType,
        [property: JsonPropertyName("Properties")] XboxDeviceAuthRequestProperties Properties
    );
}