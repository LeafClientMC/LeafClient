using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LeafClient.Utils;

namespace LeafClient.Services
{
    public static class LeafApiService
    {
        private const string BaseUrl = "https://api.leafclient.com";
        private const string CdnHost = "cdn.leafclient.com";
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        private static readonly HttpClient JarHttp = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };

        private static readonly Regex UsernameRegex = new Regex(@"^[a-zA-Z0-9_]+$", RegexOptions.Compiled);
        private static readonly Regex UuidRegex = new Regex(
            @"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$",
            RegexOptions.Compiled);

        private static string SanitizeString(string input, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(input)) throw new ArgumentException("Input must not be empty.");
            var trimmed = input.Trim();
            if (trimmed.Length > maxLength) throw new ArgumentException($"Input exceeds maximum length of {maxLength}.");
            return trimmed;
        }

        public static string? ValidatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 6)
                return "Password must be at least 6 characters long.";
            bool hasDigit = false;
            bool hasSymbol = false;
            foreach (var c in password)
            {
                if (c >= '0' && c <= '9') hasDigit = true;
                else if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))) hasSymbol = true;
            }
            if (!hasDigit) return "Password must contain at least one number.";
            if (!hasSymbol) return "Password must contain at least one symbol.";
            return null;
        }

        private static async Task<string> ParseErrorMessageAsync(HttpResponseMessage response, string fallback)
        {
            string raw;
            try { raw = await response.Content.ReadAsStringAsync(); }
            catch { return fallback; }

            if (string.IsNullOrWhiteSpace(raw)) return fallback;

            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return fallback;

                if (root.TryGetProperty("error", out var errEl))
                {
                    if (errEl.ValueKind == JsonValueKind.String)
                    {
                        var s = errEl.GetString();
                        if (!string.IsNullOrWhiteSpace(s)) return s!;
                    }
                    else if (errEl.ValueKind == JsonValueKind.Object)
                    {
                        if (errEl.TryGetProperty("issues", out var issues)
                            && issues.ValueKind == JsonValueKind.Array
                            && issues.GetArrayLength() > 0)
                        {
                            var first = issues[0];
                            string? field = null;
                            if (first.TryGetProperty("path", out var pathEl)
                                && pathEl.ValueKind == JsonValueKind.Array
                                && pathEl.GetArrayLength() > 0)
                            {
                                var p0 = pathEl[0];
                                if (p0.ValueKind == JsonValueKind.String)
                                    field = p0.GetString();
                            }
                            var msg = first.TryGetProperty("message", out var mEl) && mEl.ValueKind == JsonValueKind.String
                                ? mEl.GetString() ?? "Invalid input"
                                : "Invalid input";
                            if (!string.IsNullOrWhiteSpace(field))
                                return char.ToUpperInvariant(field![0]) + field.Substring(1) + ": " + msg;
                            return msg;
                        }
                        if (errEl.TryGetProperty("message", out var mEl2) && mEl2.ValueKind == JsonValueKind.String)
                        {
                            var s = mEl2.GetString();
                            if (!string.IsNullOrWhiteSpace(s)) return s!;
                        }
                    }
                }

                if (root.TryGetProperty("message", out var topMsg) && topMsg.ValueKind == JsonValueKind.String)
                {
                    var s = topMsg.GetString();
                    if (!string.IsNullOrWhiteSpace(s)) return s!;
                }
            }
            catch (JsonException) { }

            return fallback;
        }

        public static async Task<(LeafApiAuthResult? Result, string? Error)> RegisterWithErrorAsync(string username, string password)
        {
            var u = SanitizeString(username, 16);
            var p = SanitizeString(password, 72);

            if (u.Length < 3 || !UsernameRegex.IsMatch(u))
                return (null, "Username must be 3-16 alphanumeric characters or underscores.");
            var pwErr = ValidatePasswordStrength(p);
            if (pwErr != null)
                return (null, pwErr);

            try
            {
                var body = new LeafApiRegisterRequest(u, p);
                var response = await Http.PostAsJsonAsync(
                    $"{BaseUrl}/auth/register",
                    body,
                    LeafClient.JsonContext.Default.LeafApiRegisterRequest);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync(LeafClient.JsonContext.Default.LeafApiAuthResult);
                    return (result, null);
                }

                var errMsg = await ParseErrorMessageAsync(response, "Registration failed.");
                return (null, errMsg);
            }
            catch (HttpRequestException)
            {
                return (null, "Unable to connect to LeafClient servers.");
            }
            catch (TaskCanceledException)
            {
                return (null, "Connection timed out.");
            }
            catch
            {
                return (null, "Unable to connect to LeafClient servers.");
            }
        }

        public static async Task<(LeafApiAuthResult? Result, string? Error)> LoginWithErrorAsync(string username, string password)
        {
            var u = SanitizeString(username, 16);
            var p = SanitizeString(password, 100);

            if (u.Length < 3 || !UsernameRegex.IsMatch(u))
                return (null, "Invalid username format.");

            try
            {
                var body = new LeafApiLoginRequest(u, p);
                var response = await Http.PostAsJsonAsync(
                    $"{BaseUrl}/auth/login",
                    body,
                    LeafClient.JsonContext.Default.LeafApiLoginRequest);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync(LeafClient.JsonContext.Default.LeafApiAuthResult);
                    return (result, null);
                }

                var errMsg = await ParseErrorMessageAsync(response, "Login failed.");
                return (null, errMsg);
            }
            catch (HttpRequestException)
            {
                return (null, "Unable to connect to LeafClient servers.");
            }
            catch (TaskCanceledException)
            {
                return (null, "Connection timed out.");
            }
            catch
            {
                return (null, "Unable to connect to LeafClient servers.");
            }
        }

        private static string NormalizeUuid(string uuid)
        {
            var clean = uuid.Replace("-", "").ToLowerInvariant();
            if (clean.Length == 32)
                return $"{clean[..8]}-{clean[8..12]}-{clean[12..16]}-{clean[16..20]}-{clean[20..]}";
            return uuid.ToLowerInvariant();
        }

        public static async Task<LeafApiAuthResult?> MicrosoftLoginAsync(string uuid, string minecraftAccessToken)
        {
            var u = NormalizeUuid(SanitizeString(uuid, 36));
            var t = SanitizeString(minecraftAccessToken, 2048);

            if (!UuidRegex.IsMatch(u))
                throw new ArgumentException("Invalid UUID format.");

            try
            {
                var body = new LeafApiMicrosoftAuthRequest(u, t);
                var response = await Http.PostAsJsonAsync(
                    $"{BaseUrl}/auth/microsoft",
                    body,
                    LeafClient.JsonContext.Default.LeafApiMicrosoftAuthRequest);

                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadFromJsonAsync(LeafClient.JsonContext.Default.LeafApiAuthResult);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<(bool Success, string? Error)> WebLinkCompleteAsync(string linkCode, string uuid, string accessToken)
        {
            var code = SanitizeString(linkCode, 10);
            var u = NormalizeUuid(SanitizeString(uuid, 36));
            var t = SanitizeString(accessToken, 2048);
            try
            {
                var body = new LeafApiWebLinkCompleteRequest(code, u, t);
                var response = await Http.PostAsJsonAsync(
                    $"{BaseUrl}/auth/microsoft/weblink/complete",
                    body,
                    LeafClient.JsonContext.Default.LeafApiWebLinkCompleteRequest);
                if (response.IsSuccessStatusCode) return (true, null);
                try
                {
                    var err = await response.Content.ReadFromJsonAsync(LeafClient.JsonContext.Default.LeafApiErrorResponse);
                    return (false, err?.Error ?? "Link failed. Check the code and try again.");
                }
                catch
                {
                    return (false, "Link failed. Check the code and try again.");
                }
            }
            catch
            {
                return (false, "Unable to connect to LeafClient servers.");
            }
        }

        public static async Task<LeafApiAuthResult?> RefreshAsync(string refreshToken)
        {
            var t = SanitizeString(refreshToken, 512);
            try
            {
                var body = new LeafApiRefreshRequest(t);
                var response = await Http.PostAsJsonAsync(
                    $"{BaseUrl}/auth/refresh",
                    body,
                    LeafClient.JsonContext.Default.LeafApiRefreshRequest);

                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadFromJsonAsync(LeafClient.JsonContext.Default.LeafApiAuthResult);
            }
            catch
            {
                return null;
            }
        }

        public static async Task LogoutAsync(string refreshToken)
        {
            var t = SanitizeString(refreshToken, 512);
            try
            {
                var body = new LeafApiRefreshRequest(t);
                await Http.PostAsJsonAsync(
                    $"{BaseUrl}/auth/logout",
                    body,
                    LeafClient.JsonContext.Default.LeafApiRefreshRequest);
            }
            catch { }
        }

        public static async Task<LeafApiConfig?> GetConfigAsync()
        {
            try
            {
                var response = await Http.GetAsync($"{BaseUrl}/config");
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadFromJsonAsync(LeafClient.JsonContext.Default.LeafApiConfig);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<List<LeafApiOwnedCosmetic>?> GetOwnedCosmeticsAsync(string accessToken)
        {
            var t = SanitizeString(accessToken, 2048);
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/cosmetics/owned");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", t);
                var response = await Http.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadFromJsonAsync(LeafClient.JsonContext.Default.ListLeafApiOwnedCosmetic);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<List<LeafApiEquippedCosmetic>?> GetEquippedCosmeticsAsync(string playerIdentifier)
        {
            var id = SanitizeString(playerIdentifier, 100);
            try
            {
                var response = await Http.GetAsync($"{BaseUrl}/cosmetics/player/{Uri.EscapeDataString(id)}");
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadFromJsonAsync(LeafClient.JsonContext.Default.ListLeafApiEquippedCosmetic);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<LeafApiPlaytimeResult?> ReportPlaytimeAsync(string accessToken, int minutes)
        {
            var t = SanitizeString(accessToken, 2048);
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/user/playtime");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", t);
                request.Content = JsonContent.Create(
                    new LeafApiPlaytimeRequest(minutes),
                    LeafClient.JsonContext.Default.LeafApiPlaytimeRequest);
                var response = await Http.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadFromJsonAsync(LeafClient.JsonContext.Default.LeafApiPlaytimeResult);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<LeafApiBalance?> GetUserBalanceAsync(string accessToken)
        {
            var t = SanitizeString(accessToken, 2048);
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/user/me");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", t);
                var response = await Http.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadFromJsonAsync(LeafClient.JsonContext.Default.LeafApiBalance);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<(bool Success, int NewCoins, string? Error)> PurchaseCosmeticWithCoinsAsync(string accessToken, string cosmeticId)
        {
            var t = SanitizeString(accessToken, 2048);
            var id = SanitizeString(cosmeticId, 100);
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/cosmetics/purchase");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", t);
                request.Content = JsonContent.Create(
                    new LeafApiCoinPurchaseRequest(id),
                    LeafClient.JsonContext.Default.LeafApiCoinPurchaseRequest);
                var response = await Http.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync(LeafClient.JsonContext.Default.LeafApiCoinPurchaseResult);
                    return (true, result?.Coins ?? 0, null);
                }

                var errMsg = await ParseErrorMessageAsync(response, "Purchase failed.");
                return (false, 0, errMsg);
            }
            catch (HttpRequestException)
            {
                return (false, 0, "Unable to connect to LeafClient servers.");
            }
            catch (TaskCanceledException)
            {
                return (false, 0, "Connection timed out.");
            }
            catch
            {
                return (false, 0, "Unable to connect to LeafClient servers.");
            }
        }

        public static async Task<LeafApiLeafPlusSubscribeResult?> PurchaseLeafPlusWithCoinsAsync(string accessToken, string tier)
        {
            var t = SanitizeString(accessToken, 2048);
            var tr = SanitizeString(tier, 20);
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/subscription/subscribe");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", t);
                request.Content = JsonContent.Create(
                    new LeafApiLeafPlusSubscribeRequest(tr),
                    LeafClient.JsonContext.Default.LeafApiLeafPlusSubscribeRequest);
                var response = await Http.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync(LeafClient.JsonContext.Default.LeafApiLeafPlusSubscribeResult);
                    return result ?? new LeafApiLeafPlusSubscribeResult(true, 0, null);
                }

                var errMsg = await ParseErrorMessageAsync(response, "Subscription failed.");
                return new LeafApiLeafPlusSubscribeResult(false, 0, errMsg);
            }
            catch (HttpRequestException)
            {
                return new LeafApiLeafPlusSubscribeResult(false, 0, "Unable to connect to LeafClient servers.");
            }
            catch (TaskCanceledException)
            {
                return new LeafApiLeafPlusSubscribeResult(false, 0, "Connection timed out.");
            }
            catch
            {
                return new LeafApiLeafPlusSubscribeResult(false, 0, "Unable to connect to LeafClient servers.");
            }
        }

        public static async Task SendHeartbeatAsync(string identifier, string type, string? accessToken = null)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/ping");
                if (!string.IsNullOrWhiteSpace(accessToken))
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = JsonContent.Create(
                    new LeafApiHeartbeatRequest(identifier, type),
                    LeafClient.JsonContext.Default.LeafApiHeartbeatRequest);
                await Http.SendAsync(request);
            }
            catch { }
        }

        public static async Task DeleteHeartbeatAsync(string identifier, string? accessToken = null, CancellationToken ct = default)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/ping");
                if (!string.IsNullOrWhiteSpace(accessToken))
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                request.Content = JsonContent.Create(
                    new LeafApiDeleteHeartbeatRequest(identifier),
                    LeafClient.JsonContext.Default.LeafApiDeleteHeartbeatRequest);
                await Http.SendAsync(request, ct);
            }
            catch { }
        }

        private const string ModJarSigningPublicKeyB64 =
            "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEh2mUsltx9o2LZzuW6XW8uZslSUTJ+OqfAC52P4nSlcTfdetFUMcY1I2bt178Ay99r9PBGsZfZN1wDBsrwJuDmg==";

        public static async Task<LeafApiManifest?> GetModManifestAsync(string mcVersion, CancellationToken ct = default)
        {
            var v = SanitizeString(mcVersion, 32);
            if (!System.Text.RegularExpressions.Regex.IsMatch(v, @"^[a-zA-Z0-9._-]+$"))
                throw new ArgumentException("Invalid minecraft version format.");
            try
            {
                var manifestUri = new Uri($"https://{CdnHost}/versions/{Uri.EscapeDataString(v)}/manifest.json");
                if (!JarVerifier.IsAllowedHost(manifestUri, CdnHost))
                    throw new InvalidOperationException("Manifest URL host not allowed.");

                using var response = await Http.GetAsync(manifestUri, ct);
                if (!response.IsSuccessStatusCode) return null;
                var manifest = await response.Content.ReadFromJsonAsync(
                    LeafClient.JsonContext.Default.LeafApiManifest, ct);
                if (manifest is null) return null;

                if (!VerifyManifestSignature(manifest, v))
                {
                    Console.WriteLine($"[ManifestVerify] Signature verification FAILED for mc={v}. Refusing manifest.");
                    return null;
                }

                if (manifest.McVersion != null
                    && !string.Equals(manifest.McVersion, v, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[ManifestVerify] mcVersion mismatch (manifest='{manifest.McVersion}', requested='{v}'). Refusing.");
                    return null;
                }

                return manifest;
            }
            catch
            {
                return null;
            }
        }

        private static bool VerifyManifestSignature(LeafApiManifest manifest, string requestedMcVersion)
        {
            if (string.IsNullOrWhiteSpace(manifest.Signature))
            {
                Console.WriteLine("[ManifestVerify] Missing 'signature' field on manifest.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(manifest.Sha256))
            {
                Console.WriteLine("[ManifestVerify] Missing 'sha256' field on manifest.");
                return false;
            }
            string clientVersion = manifest.ClientVersion ?? manifest.Version ?? "";
            if (string.IsNullOrWhiteSpace(clientVersion))
            {
                Console.WriteLine("[ManifestVerify] Missing client version on manifest.");
                return false;
            }
            string canonical = $"leaf-jar/v1/{requestedMcVersion}/{clientVersion}/{manifest.Sha256.ToLowerInvariant()}";
            byte[] msgBytes = System.Text.Encoding.UTF8.GetBytes(canonical);
            byte[] sigBytes;
            try { sigBytes = Convert.FromBase64String(manifest.Signature); }
            catch
            {
                Console.WriteLine("[ManifestVerify] Signature is not valid base64.");
                return false;
            }
            try
            {
                using var ecdsa = System.Security.Cryptography.ECDsa.Create();
                if (ecdsa is null) return false;
                ecdsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(ModJarSigningPublicKeyB64), out _);
                return ecdsa.VerifyData(msgBytes, sigBytes, System.Security.Cryptography.HashAlgorithmName.SHA256);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ManifestVerify] Verify error: {ex.Message}");
                return false;
            }
        }

        public static async Task DownloadVerifiedModJarAsync(
            LeafApiManifest manifest,
            string destinationPath,
            CancellationToken ct = default)
        {
            if (manifest is null) throw new ArgumentNullException(nameof(manifest));
            if (string.IsNullOrWhiteSpace(manifest.JarUrl))
                throw new InvalidOperationException("Manifest missing jarUrl.");
            if (string.IsNullOrWhiteSpace(manifest.Sha256))
                throw new InvalidOperationException("Manifest missing sha256.");

            if (!Uri.TryCreate(manifest.JarUrl, UriKind.Absolute, out var jarUri)
                || !JarVerifier.IsAllowedHost(jarUri, CdnHost))
            {
                Console.WriteLine($"[JarVerify] Rejected jarUrl (host not allowed): {manifest.JarUrl}");
                throw new InvalidOperationException("Jar integrity check failed");
            }

            byte[] bytes;
            using (var response = await JarHttp.GetAsync(jarUri, HttpCompletionOption.ResponseContentRead, ct))
            {
                response.EnsureSuccessStatusCode();
                bytes = await response.Content.ReadAsByteArrayAsync(ct);
            }

            if (!JarVerifier.VerifySha256(bytes, manifest.Sha256))
            {
                var actual = System.Security.Cryptography.SHA256.HashData(bytes);
                Console.WriteLine(
                    $"[JarVerify] SHA-256 mismatch. expected={manifest.Sha256.ToLowerInvariant()} actual={Convert.ToHexString(actual).ToLowerInvariant()} url={jarUri}");
                TryDeleteFile(destinationPath);
                throw new InvalidOperationException("Jar integrity check failed");
            }

            var dir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            try
            {
                await File.WriteAllBytesAsync(destinationPath, bytes, ct);
            }
            catch
            {
                TryDeleteFile(destinationPath);
                throw;
            }

            if (!await VerifyOnDiskAsync(destinationPath, manifest.Sha256, ct))
            {
                TryDeleteFile(destinationPath);
                Console.WriteLine("[JarVerify] On-disk re-verification failed.");
                throw new InvalidOperationException("Jar integrity check failed");
            }

            Console.WriteLine($"[JarVerify] Verified {Path.GetFileName(destinationPath)} against manifest sha256.");
        }

        private static async Task<bool> VerifyOnDiskAsync(string path, string expectedHex, CancellationToken ct)
        {
            try
            {
                var bytes = await File.ReadAllBytesAsync(path, ct);
                return JarVerifier.VerifySha256(bytes, expectedHex);
            }
            catch
            {
                return false;
            }
        }

        private static void TryDeleteFile(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception ex) { Console.WriteLine($"[JarVerify] Failed to delete {path}: {ex.Message}"); }
        }

        public static async Task<bool> EquipCosmeticAsync(string accessToken, string cosmeticId)
        {
            var t = SanitizeString(accessToken, 2048);
            var id = SanitizeString(cosmeticId, 100);
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/cosmetics/equip");
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", t);
                request.Content = JsonContent.Create(
                    new LeafApiEquipRequest(id),
                    LeafClient.JsonContext.Default.LeafApiEquipRequest);
                var response = await Http.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
