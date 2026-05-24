using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LeafClient.Services
{
    public sealed record MojangCape(string Id, string State, string Url, string Alias);

    public sealed record MojangProfile(string Id, string Name, IReadOnlyList<MojangCape> Capes);

    public enum MojangCapeOperationOutcome
    {
        Success,
        Unauthorized,
        NetworkError,
        Unexpected,
    }

    public static class MojangCapeService
    {
        private const string ProfileUrl = "https://api.minecraftservices.com/minecraft/profile";
        private const string ActiveCapeUrl = "https://api.minecraftservices.com/minecraft/profile/capes/active";
        private const string AllowedTextureHost = "textures.minecraft.net";

        private static readonly TimeSpan ProfileCacheTtl = TimeSpan.FromMinutes(5);
        private static readonly object CacheLock = new object();
        private static MojangProfile? _cachedProfile;
        private static string? _cachedProfileTokenHash;
        private static DateTime _cachedProfileAt;
        private static readonly Dictionary<string, byte[]> _textureCache = new();
        private static readonly Dictionary<string, Task<byte[]?>> _textureInflight = new();

        private static readonly HttpClient Http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10),
        };

        static MojangCapeService()
        {
            Http.DefaultRequestHeaders.UserAgent.ParseAdd("LeafClient/1.0");
        }

        public static void InvalidateProfileCache()
        {
            lock (CacheLock)
            {
                _cachedProfile = null;
                _cachedProfileTokenHash = null;
                _cachedProfileAt = DateTime.MinValue;
            }
        }

        public static void InvalidateAllCaches()
        {
            lock (CacheLock)
            {
                _cachedProfile = null;
                _cachedProfileTokenHash = null;
                _cachedProfileAt = DateTime.MinValue;
                _textureCache.Clear();
            }
        }

        private static string HashToken(string token)
        {
            unchecked
            {
                int h = 17;
                foreach (var c in token) h = h * 31 + c;
                return h.ToString("X");
            }
        }

        public static Task<MojangProfile?> GetProfileAsync(string mcAccessToken, CancellationToken ct = default)
            => GetProfileAsync(mcAccessToken, forceRefresh: false, ct);

        public static async Task<MojangProfile?> GetProfileAsync(string mcAccessToken, bool forceRefresh, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(mcAccessToken)) return null;
            string tokenHash = HashToken(mcAccessToken);
            if (!forceRefresh)
            {
                lock (CacheLock)
                {
                    if (_cachedProfile != null
                        && _cachedProfileTokenHash == tokenHash
                        && DateTime.UtcNow - _cachedProfileAt < ProfileCacheTtl)
                    {
                        return _cachedProfile;
                    }
                }
            }
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, ProfileUrl);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", mcAccessToken);
                using var resp = await Http.SendAsync(req, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    LeafLog.Info("MojangCape", $"GET /profile returned {(int)resp.StatusCode}");
                    if ((int)resp.StatusCode == 429)
                    {
                        lock (CacheLock)
                        {
                            if (_cachedProfile != null && _cachedProfileTokenHash == tokenHash)
                                return _cachedProfile;
                        }
                    }
                    return null;
                }
                var body = await resp.Content.ReadAsStringAsync(ct);
                var parsed = ParseProfile(body);
                if (parsed != null)
                {
                    lock (CacheLock)
                    {
                        _cachedProfile = parsed;
                        _cachedProfileTokenHash = tokenHash;
                        _cachedProfileAt = DateTime.UtcNow;
                    }
                }
                return parsed;
            }
            catch (Exception ex)
            {
                LeafLog.Info("MojangCape", $"GET /profile failed: {ex.Message}");
                lock (CacheLock)
                {
                    if (_cachedProfile != null && _cachedProfileTokenHash == tokenHash)
                        return _cachedProfile;
                }
                return null;
            }
        }

        public static async Task<byte[]?> GetCapeTextureBytesAsync(string url, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Host != AllowedTextureHost) return null;

            Task<byte[]?> task;
            bool weStartedIt = false;
            lock (CacheLock)
            {
                if (_textureCache.TryGetValue(url, out var cached)) return cached;
                if (_textureInflight.TryGetValue(url, out var existing))
                {
                    task = existing;
                }
                else
                {
                    task = FetchTextureAsync(url, ct);
                    _textureInflight[url] = task;
                    weStartedIt = true;
                }
            }

            var result = await task.ConfigureAwait(false);
            if (weStartedIt)
            {
                lock (CacheLock)
                {
                    _textureInflight.Remove(url);
                    if (result != null) _textureCache[url] = result;
                }
            }
            return result;
        }

        private static async Task<byte[]?> FetchTextureAsync(string url, CancellationToken ct)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                using var resp = await Http.SendAsync(req, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    LeafLog.Info("MojangCape", $"GET texture {url} returned {(int)resp.StatusCode}");
                    return null;
                }
                return await resp.Content.ReadAsByteArrayAsync(ct);
            }
            catch (Exception ex)
            {
                LeafLog.Info("MojangCape", $"texture fetch failed: {ex.Message}");
                return null;
            }
        }

        public static async Task<MojangCapeOperationOutcome> SetActiveCapeAsync(string mcAccessToken, string capeId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(mcAccessToken)) return MojangCapeOperationOutcome.Unauthorized;
            if (string.IsNullOrWhiteSpace(capeId)) return MojangCapeOperationOutcome.Unexpected;
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Put, ActiveCapeUrl);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", mcAccessToken);
                string safeId = capeId.Replace("\\", "\\\\").Replace("\"", "\\\"");
                string payload = "{\"capeId\":\"" + safeId + "\"}";
                req.Content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var resp = await Http.SendAsync(req, ct);
                if (resp.IsSuccessStatusCode)
                {
                    InvalidateProfileCache();
                    return MojangCapeOperationOutcome.Success;
                }
                int code = (int)resp.StatusCode;
                LeafLog.Info("MojangCape", $"PUT /capes/active returned {code}");
                if (code == 401 || code == 403) return MojangCapeOperationOutcome.Unauthorized;
                return MojangCapeOperationOutcome.Unexpected;
            }
            catch (Exception ex)
            {
                LeafLog.Info("MojangCape", $"PUT /capes/active failed: {ex.Message}");
                return MojangCapeOperationOutcome.NetworkError;
            }
        }

        public static async Task<MojangCapeOperationOutcome> RemoveActiveCapeAsync(string mcAccessToken, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(mcAccessToken)) return MojangCapeOperationOutcome.Unauthorized;
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Delete, ActiveCapeUrl);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", mcAccessToken);
                using var resp = await Http.SendAsync(req, ct);
                if (resp.IsSuccessStatusCode)
                {
                    InvalidateProfileCache();
                    return MojangCapeOperationOutcome.Success;
                }
                int code = (int)resp.StatusCode;
                LeafLog.Info("MojangCape", $"DELETE /capes/active returned {code}");
                if (code == 401 || code == 403) return MojangCapeOperationOutcome.Unauthorized;
                return MojangCapeOperationOutcome.Unexpected;
            }
            catch (Exception ex)
            {
                LeafLog.Info("MojangCape", $"DELETE /capes/active failed: {ex.Message}");
                return MojangCapeOperationOutcome.NetworkError;
            }
        }

        private static MojangProfile? ParseProfile(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                string id = root.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
                string name = root.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "" : "";

                var capes = new List<MojangCape>();
                if (root.TryGetProperty("capes", out var capesEl) && capesEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var cape in capesEl.EnumerateArray())
                    {
                        string capeId = cape.TryGetProperty("id", out var cidEl) ? cidEl.GetString() ?? "" : "";
                        string state = cape.TryGetProperty("state", out var sEl) ? sEl.GetString() ?? "" : "";
                        string url = cape.TryGetProperty("url", out var uEl) ? uEl.GetString() ?? "" : "";
                        string alias = cape.TryGetProperty("alias", out var aEl) ? aEl.GetString() ?? "" : "";
                        if (!string.IsNullOrWhiteSpace(capeId))
                            capes.Add(new MojangCape(capeId, state, url, alias));
                    }
                }
                return new MojangProfile(id, name, capes);
            }
            catch (Exception ex)
            {
                LeafLog.Info("MojangCape", $"ParseProfile failed: {ex.Message}");
                return null;
            }
        }
    }
}
