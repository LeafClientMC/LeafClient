using CmlLib.Core.Auth;
using LeafClient.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeafClient.Services
{
    // AOT-safe request bodies (concrete records registered in JsonContext so we
    // can serialize without relying on reflection).
    public record XblAuthRequest(XblAuthProperties Properties, string RelyingParty, string TokenType);
    public record XblAuthProperties(string AuthMethod, string SiteName, string RpsTicket);
    public record XstsAuthRequest(XstsAuthProperties Properties, string RelyingParty, string TokenType);
    public record XstsAuthProperties(string SandboxId, string[] UserTokens);
    public record McLoginRequest(string identityToken);

    /// <summary>
    /// Lunar-style silent Microsoft auth: exchanges a stored OAuth refresh token for a
    /// fresh Minecraft session entirely over HTTP with no MSAL file-cache dependency.
    /// Chain: MS OAuth → Xbox Live → XSTS → Minecraft → profile.
    /// </summary>
    public static class DirectRefreshService
    {
        private const string ClientId = "499c8d36-be2a-4231-9ebd-ef291b7bb64c";

        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        /// <summary>
        /// Attempts a fully silent token refresh.
        /// On success: rotates the refresh token in <paramref name="settings"/> and saves via
        /// <paramref name="settingsService"/>, then returns a valid <see cref="MSession"/>.
        /// On any failure: returns null (caller decides what to do next).
        /// </summary>
        public static async Task<MSession?> TryRefreshAsync(
            string refreshToken,
            LauncherSettings settings,
            SettingsService settingsService)
        {
            try
            {
                // ── Step 1: MS OAuth refresh ──────────────────────────────────────────
                Console.WriteLine("[DirectRefresh] Step 1: Refreshing MS OAuth token...");
                var form = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"]    = "refresh_token",
                    ["client_id"]     = ClientId,
                    ["refresh_token"] = refreshToken,
                    ["scope"]         = "XboxLive.signin offline_access",
                });
                var msResp = await Http.PostAsync(
                    "https://login.microsoftonline.com/consumers/oauth2/v2.0/token", form);
                if (!msResp.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[DirectRefresh] MS OAuth failed: {msResp.StatusCode}");
                    return null;
                }
                using var msDoc = JsonDocument.Parse(await msResp.Content.ReadAsStringAsync());
                var msRoot         = msDoc.RootElement;
                var msAccessToken  = msRoot.GetProperty("access_token").GetString()!;
                var newRefreshToken = msRoot.TryGetProperty("refresh_token", out var rt)
                    ? rt.GetString()
                    : null;

                // ── Step 2: Xbox Live ─────────────────────────────────────────────────
                Console.WriteLine("[DirectRefresh] Step 2: Xbox Live auth...");
                var xblBody = JsonSerializer.Serialize(
                    new XblAuthRequest(
                        new XblAuthProperties("RPS", "user.auth.xboxlive.com", $"d={msAccessToken}"),
                        "http://auth.xboxlive.com",
                        "JWT"),
                    LeafClient.JsonContext.Default.XblAuthRequest);
                using var xblContent = new StringContent(xblBody, Encoding.UTF8, "application/json");
                xblContent.Headers.Add("x-xbl-contract-version", "1");
                var xblResp = await Http.PostAsync(
                    "https://user.auth.xboxlive.com/user/authenticate", xblContent);
                if (!xblResp.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[DirectRefresh] XBL failed: {xblResp.StatusCode}");
                    return null;
                }
                using var xblDoc = JsonDocument.Parse(await xblResp.Content.ReadAsStringAsync());
                var xblRoot  = xblDoc.RootElement;
                var xblToken = xblRoot.GetProperty("Token").GetString()!;
                var uhs      = xblRoot.GetProperty("DisplayClaims")
                                      .GetProperty("xui")[0]
                                      .GetProperty("uhs").GetString()!;

                // ── Step 3: XSTS ──────────────────────────────────────────────────────
                Console.WriteLine("[DirectRefresh] Step 3: XSTS token...");
                var xstsBody = JsonSerializer.Serialize(
                    new XstsAuthRequest(
                        new XstsAuthProperties("RETAIL", new[] { xblToken }),
                        "rp://api.minecraftservices.com/",
                        "JWT"),
                    LeafClient.JsonContext.Default.XstsAuthRequest);
                using var xstsContent = new StringContent(xstsBody, Encoding.UTF8, "application/json");
                xstsContent.Headers.Add("x-xbl-contract-version", "1");
                var xstsResp = await Http.PostAsync(
                    "https://xsts.auth.xboxlive.com/xsts/authorize", xstsContent);
                if (!xstsResp.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[DirectRefresh] XSTS failed: {xstsResp.StatusCode}");
                    return null;
                }
                using var xstsDoc = JsonDocument.Parse(await xstsResp.Content.ReadAsStringAsync());
                var xstsToken = xstsDoc.RootElement.GetProperty("Token").GetString()!;

                // ── Step 4: Minecraft login ───────────────────────────────────────────
                Console.WriteLine("[DirectRefresh] Step 4: Minecraft login...");
                var mcBody = JsonSerializer.Serialize(
                    new McLoginRequest($"XBL3.0 x={uhs};{xstsToken}"),
                    LeafClient.JsonContext.Default.McLoginRequest);
                using var mcContent = new StringContent(mcBody, Encoding.UTF8, "application/json");
                var mcResp = await Http.PostAsync(
                    "https://api.minecraftservices.com/authentication/login_with_xbox", mcContent);
                if (!mcResp.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[DirectRefresh] MC login failed: {mcResp.StatusCode}");
                    return null;
                }
                using var mcDoc = JsonDocument.Parse(await mcResp.Content.ReadAsStringAsync());
                var mcAccessToken = mcDoc.RootElement.GetProperty("access_token").GetString()!;

                // ── Step 5: Minecraft profile ─────────────────────────────────────────
                Console.WriteLine("[DirectRefresh] Step 5: Fetching MC profile...");
                using var profileReq = new HttpRequestMessage(
                    HttpMethod.Get, "https://api.minecraftservices.com/minecraft/profile");
                profileReq.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", mcAccessToken);
                var profileResp = await Http.SendAsync(profileReq);
                if (!profileResp.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[DirectRefresh] Profile fetch failed: {profileResp.StatusCode}");
                    return null;
                }
                using var profileDoc = JsonDocument.Parse(await profileResp.Content.ReadAsStringAsync());
                var profileRoot = profileDoc.RootElement;
                var mcUsername  = profileRoot.GetProperty("name").GetString()!;
                var mcUuid      = profileRoot.GetProperty("id").GetString()!;

                // ── Rotate refresh token + persist ────────────────────────────────────
                if (!string.IsNullOrWhiteSpace(newRefreshToken))
                {
                    settings.MicrosoftRefreshToken = newRefreshToken;
                    Console.WriteLine("[DirectRefresh] Refresh token rotated.");
                }
                settings.SessionUsername    = mcUsername;
                settings.SessionUuid        = mcUuid;
                settings.SessionAccessToken = mcAccessToken;
                await settingsService.SaveSettingsAsync(settings);

                Console.WriteLine($"[DirectRefresh] Success — {mcUsername} ({mcUuid})");
                return new MSession
                {
                    Username    = mcUsername,
                    UUID        = mcUuid,
                    AccessToken = mcAccessToken,
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DirectRefresh] Exception: {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }
    }
}
