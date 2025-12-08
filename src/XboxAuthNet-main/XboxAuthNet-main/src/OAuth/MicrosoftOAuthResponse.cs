// File: C:\Users\ziadf\source\repos\XboxAuthNet-main\XboxAuthNet-main\src\XboxAuthNet\OAuth\MicrosoftOAuthResponse.cs
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using XboxAuthNet.Jwt;

namespace XboxAuthNet.OAuth;

public class MicrosoftOAuthResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpireIn { get; set; }

    [JsonPropertyName("expires_on")]
    public DateTimeOffset ExpiresOn { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RawRefreshToken { get; set; }

    [JsonPropertyName("ext_expires_in")]
    public int ExtExpireIn { get; set; }

    [JsonIgnore]
    public string? RefreshToken
    {
        get => RawRefreshToken?.Split('.')?.Last();
        set => RawRefreshToken = "M.R3_BAY." + value;
    }

    [JsonPropertyName("id_token")]
    public string? IdToken { get; set; }

    public MicrosoftUserPayload? DecodeIdTokenPayload()
    {
        if (string.IsNullOrEmpty(IdToken))
            return null;

        return JwtDecoder.DecodePayload<MicrosoftUserPayload>(IdToken!);
    }

    public bool Validate()
    {
        if (string.IsNullOrEmpty(AccessToken))
            return false;

        if (DateTimeOffset.UtcNow > ExpiresOn)
            return false;

        return true;
    }

    public static MicrosoftOAuthResponse FromHttpResponse(string resBody, int statusCode, string? reasonPhrase)
    {
        // --- CRITICAL DIAGNOSTIC LOGGING ---
        Console.WriteLine($"[MicrosoftOAuthResponse] Raw HTTP Response Body: {resBody}");
        // --- END CRITICAL DIAGNOSTIC LOGGING ---

        if (statusCode / 100 != 2)
            throw MicrosoftOAuthException.FromResponseBody(resBody, statusCode, reasonPhrase);

        try
        {
            var options = XboxAuthNet.JsonConfig.DefaultOptions ?? new JsonSerializerOptions();
            var resObj = JsonSerializer.Deserialize<MicrosoftOAuthResponse>(resBody, options);
            if (resObj == null)
                throw new MicrosoftOAuthException("The response was empty.", statusCode);

            Console.WriteLine($"[MicrosoftOAuthResponse] Deserialized ExpireIn: {resObj.ExpireIn}");
            Console.WriteLine($"[MicrosoftOAuthResponse] Deserialized ExtExpireIn: {resObj.ExtExpireIn}");
            Console.WriteLine($"[MicrosoftOAuthResponse] Deserialized ExpiresOn: {resObj.ExpiresOn}");
            Console.WriteLine($"[MicrosoftOAuthResponse] Deserialized AccessToken (shortened): {resObj.AccessToken?.Substring(0, Math.Min(50, resObj.AccessToken.Length)) ?? "NULL"}...");

            // FIX: Handle expiration logic more robustly for MSAL responses
            if (resObj.ExpireIn > 0)
            {
                // If we have ExpireIn seconds, calculate ExpiresOn from current time
                resObj.ExpiresOn = DateTimeOffset.UtcNow.AddSeconds(resObj.ExpireIn);
                Console.WriteLine($"[MicrosoftOAuthResponse] Calculated ExpiresOn from ExpireIn: {resObj.ExpiresOn}");
            }
            else if (resObj.ExtExpireIn > 0)
            {
                // MSAL sometimes uses ext_expires_in
                resObj.ExpiresOn = DateTimeOffset.UtcNow.AddSeconds(resObj.ExtExpireIn);
                Console.WriteLine($"[MicrosoftOAuthResponse] Calculated ExpiresOn from ExtExpireIn: {resObj.ExpiresOn}");
            }
            else if (resObj.ExpiresOn > DateTimeOffset.MinValue)
            {
                // If we have a valid ExpiresOn, use it directly
                Console.WriteLine($"[MicrosoftOAuthResponse] Using provided ExpiresOn: {resObj.ExpiresOn}");
            }
            else
            {
                // FIX: If no expiration info is provided, set a reasonable default
                // This prevents the token from being immediately considered expired
                resObj.ExpiresOn = DateTimeOffset.UtcNow.AddHours(1); // Default 1 hour
                Console.WriteLine($"[MicrosoftOAuthResponse] WARNING: No expiration information found. Setting default expiration to 1 hour.");
            }

            return resObj;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[MicrosoftOAuthResponse] JSON deserialization failed: {ex.Message}");
            throw MicrosoftOAuthException.FromResponseBody(resBody, statusCode, reasonPhrase);
        }
    }
}
