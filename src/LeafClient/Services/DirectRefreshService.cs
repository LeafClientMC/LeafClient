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
    public record XblAuthRequest(XblAuthProperties Properties, string RelyingParty, string TokenType);
    public record XblAuthProperties(string AuthMethod, string SiteName, string RpsTicket);
    public record XstsAuthRequest(XstsAuthProperties Properties, string RelyingParty, string TokenType);
    public record XstsAuthProperties(string SandboxId, string[] UserTokens);
    public record McLoginRequest(string identityToken);

    public static class DirectRefreshService
    {
        private static string ClientId => AuthConfig.MicrosoftClientId;

        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        public static async Task<MSession?> TryRefreshAsync(
            string refreshToken,
            LauncherSettings settings,
            SettingsService settingsService)
        {
            try
            {
                LeafLog.Info("DirectRefresh", "Step 1: Refreshing MS OAuth token...");
                HttpResponseMessage? msResp = null;
                string? msBody = null;
                for (int attempt = 1; attempt <= 2; attempt++)
                {
                    var form = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["grant_type"]    = "refresh_token",
                        ["client_id"]     = ClientId,
                        ["refresh_token"] = refreshToken,
                        ["scope"]         = "XboxLive.signin offline_access",
                    });
                    try
                    {
                        msResp = await Http.PostAsync(
                            "https://login.microsoftonline.com/consumers/oauth2/v2.0/token", form);
                    }
                    catch (Exception ex)
                    {
                        LeafLog.Error("DirectRefresh", $"MS OAuth network error (attempt {attempt}): {ex.GetType().Name}: {ex.Message}");
                        if (attempt == 2) return null;
                        await Task.Delay(1000);
                        continue;
                    }
                    msBody = await msResp.Content.ReadAsStringAsync();
                    if (msResp.IsSuccessStatusCode) break;

                    int code = (int)msResp.StatusCode;
                    bool transient = code >= 500 || code == 429;
                    LeafLog.Info("DirectRefresh", $"MS OAuth failed: {msResp.StatusCode} (attempt {attempt}). Body: {Truncate(msBody, 400)}");
                    if (!transient || attempt == 2) return null;
                    await Task.Delay(1000);
                }
                if (msResp == null || !msResp.IsSuccessStatusCode || msBody == null) return null;
                using var msDoc = JsonDocument.Parse(msBody);
                var msRoot         = msDoc.RootElement;
                var msAccessToken  = msRoot.GetProperty("access_token").GetString()!;
                var newRefreshToken = msRoot.TryGetProperty("refresh_token", out var rt)
                    ? rt.GetString()
                    : null;

                LeafLog.Info("DirectRefresh", "Step 2: Xbox Live auth...");
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
                    LeafLog.Info("DirectRefresh", $"XBL failed: {xblResp.StatusCode}");
                    return null;
                }
                using var xblDoc = JsonDocument.Parse(await xblResp.Content.ReadAsStringAsync());
                var xblRoot  = xblDoc.RootElement;
                var xblToken = xblRoot.GetProperty("Token").GetString()!;
                var uhs      = xblRoot.GetProperty("DisplayClaims")
                                      .GetProperty("xui")[0]
                                      .GetProperty("uhs").GetString()!;

                LeafLog.Info("DirectRefresh", "Step 3: XSTS token...");
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
                    LeafLog.Info("DirectRefresh", $"XSTS failed: {xstsResp.StatusCode}");
                    return null;
                }
                using var xstsDoc = JsonDocument.Parse(await xstsResp.Content.ReadAsStringAsync());
                var xstsToken = xstsDoc.RootElement.GetProperty("Token").GetString()!;

                LeafLog.Info("DirectRefresh", "Step 4: Minecraft login...");
                var mcBody = JsonSerializer.Serialize(
                    new McLoginRequest($"XBL3.0 x={uhs};{xstsToken}"),
                    LeafClient.JsonContext.Default.McLoginRequest);
                using var mcContent = new StringContent(mcBody, Encoding.UTF8, "application/json");
                var mcResp = await Http.PostAsync(
                    "https://api.minecraftservices.com/authentication/login_with_xbox", mcContent);
                if (!mcResp.IsSuccessStatusCode)
                {
                    LeafLog.Info("DirectRefresh", $"MC login failed: {mcResp.StatusCode}");
                    return null;
                }
                using var mcDoc = JsonDocument.Parse(await mcResp.Content.ReadAsStringAsync());
                var mcAccessToken = mcDoc.RootElement.GetProperty("access_token").GetString()!;

                LeafLog.Info("DirectRefresh", "Step 5: Fetching MC profile...");
                using var profileReq = new HttpRequestMessage(
                    HttpMethod.Get, "https://api.minecraftservices.com/minecraft/profile");
                profileReq.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", mcAccessToken);
                var profileResp = await Http.SendAsync(profileReq);
                if (!profileResp.IsSuccessStatusCode)
                {
                    LeafLog.Info("DirectRefresh", $"Profile fetch failed: {profileResp.StatusCode}");
                    return null;
                }
                using var profileDoc = JsonDocument.Parse(await profileResp.Content.ReadAsStringAsync());
                var profileRoot = profileDoc.RootElement;
                var mcUsername  = profileRoot.GetProperty("name").GetString()!;
                var mcUuid      = profileRoot.GetProperty("id").GetString()!;

                if (!string.IsNullOrWhiteSpace(newRefreshToken))
                {
                    settings.MicrosoftRefreshToken = newRefreshToken;
                    LeafLog.Info("DirectRefresh", "Refresh token rotated.");
                }
                settings.SessionUsername    = mcUsername;
                settings.SessionUuid        = mcUuid;
                settings.SessionAccessToken = mcAccessToken;
                await settingsService.SaveSettingsAsync(settings);

                LeafLog.Info("DirectRefresh", "Success - credentials refreshed");
                return new MSession
                {
                    Username    = mcUsername,
                    UUID        = mcUuid,
                    AccessToken = mcAccessToken,
                };
            }
            catch (Exception ex)
            {
                LeafLog.Info("DirectRefresh", $"Exception: {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        private static string Truncate(string? s, int max)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Length <= max ? s : s.Substring(0, max) + "…";
        }
    }
}
