using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using LeafClient.Models;

namespace LeafClient.Services.ModFolderManagement
{
    public sealed record ModrinthCompatCacheEntry(
        string Slug,
        string McVersion,
        string Loader,
        long FetchedAtUnix,
        List<ModrinthVersion> Versions
    );

    public sealed record ModrinthCompatCache(
        Dictionary<string, ModrinthCompatCacheEntry> Entries
    );

    public sealed record ModrinthCompatResult(
        string Slug,
        ModrinthVersion? Recommended,
        List<ModrinthVersion> Candidates,
        string? Reason
    );

    public static class ModrinthCompatService
    {
        private const long CacheTtlSeconds = 24 * 60 * 60;
        private const string CacheFileName = "modrinth-compat.json";
        private const string ModrinthApiBase = "https://api.modrinth.com/v2";

        private static readonly object CacheLock = new();
        private static ModrinthCompatCache? _cache;

        private static string CacheFilePath
        {
            get
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var dir = Path.Combine(appData, "LeafClient", "cache");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, CacheFileName);
            }
        }

        private static ModrinthCompatCache LoadCache()
        {
            lock (CacheLock)
            {
                if (_cache != null) return _cache;
                try
                {
                    if (File.Exists(CacheFilePath))
                    {
                        var json = File.ReadAllText(CacheFilePath);
                        var loaded = JsonSerializer.Deserialize(json, JsonContext.Default.ModrinthCompatCache);
                        if (loaded?.Entries != null)
                        {
                            _cache = loaded;
                            return _cache;
                        }
                    }
                }
                catch { }
                _cache = new ModrinthCompatCache(new Dictionary<string, ModrinthCompatCacheEntry>(StringComparer.OrdinalIgnoreCase));
                return _cache;
            }
        }

        private static void SaveCache()
        {
            lock (CacheLock)
            {
                if (_cache == null) return;
                try
                {
                    var json = JsonSerializer.Serialize(_cache, JsonContext.Default.ModrinthCompatCache);
                    File.WriteAllText(CacheFilePath, json);
                }
                catch { }
            }
        }

        private static string CacheKey(string slug, string mcVersion, string loader)
        {
            return $"{slug.ToLowerInvariant()}|{mcVersion}|{loader.ToLowerInvariant()}";
        }

        public static async Task<List<ModrinthVersion>> GetVersionsAsync(
            HttpClient http,
            string slug,
            string mcVersion,
            string loader)
        {
            var key = CacheKey(slug, mcVersion, loader);
            var cache = LoadCache();
            long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            lock (CacheLock)
            {
                if (cache.Entries.TryGetValue(key, out var hit))
                {
                    if (nowUnix - hit.FetchedAtUnix < CacheTtlSeconds)
                    {
                        return hit.Versions;
                    }
                }
            }

            try
            {
                var url = $"{ModrinthApiBase}/project/{Uri.EscapeDataString(slug)}/version" +
                          $"?game_versions=%5B%22{Uri.EscapeDataString(mcVersion)}%22%5D" +
                          $"&loaders=%5B%22{Uri.EscapeDataString(loader)}%22%5D";
                using var resp = await http.GetAsync(url).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode) return new List<ModrinthVersion>();
                var json = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                var versions = JsonSerializer.Deserialize(json, JsonContext.Default.ListModrinthVersion)
                               ?? new List<ModrinthVersion>();

                lock (CacheLock)
                {
                    cache.Entries[key] = new ModrinthCompatCacheEntry(slug, mcVersion, loader, nowUnix, versions);
                }
                SaveCache();
                return versions;
            }
            catch
            {
                lock (CacheLock)
                {
                    if (cache.Entries.TryGetValue(key, out var stale))
                    {
                        return stale.Versions;
                    }
                }
                return new List<ModrinthVersion>();
            }
        }

        public static ModrinthVersion? PickStable(IEnumerable<ModrinthVersion>? versions)
        {
            if (versions == null) return null;
            var list = versions.ToList();
            if (list.Count == 0) return null;
            var release = list.FirstOrDefault(v => string.Equals(v.VersionType, "release", StringComparison.OrdinalIgnoreCase));
            if (release != null) return release;
            var beta = list.FirstOrDefault(v => string.Equals(v.VersionType, "beta", StringComparison.OrdinalIgnoreCase));
            if (beta != null) return beta;
            bool anyTagged = list.Any(v => !string.IsNullOrEmpty(v.VersionType));
            if (anyTagged) return null;
            return list[0];
        }

        public static async Task<ModrinthCompatResult> ResolveAsync(
            HttpClient http,
            string slug,
            string mcVersion,
            string loader)
        {
            var versions = await GetVersionsAsync(http, slug, mcVersion, loader).ConfigureAwait(false);
            if (versions.Count == 0)
            {
                return new ModrinthCompatResult(slug, null, versions, "No versions returned by Modrinth for this MC version + loader");
            }
            var recommended = PickStable(versions);
            string? reason = recommended == null
                ? "Only alpha builds available - skipping to avoid unstable installs"
                : null;
            return new ModrinthCompatResult(slug, recommended, versions, reason);
        }

        public static void InvalidateAll()
        {
            lock (CacheLock)
            {
                _cache = new ModrinthCompatCache(new Dictionary<string, ModrinthCompatCacheEntry>(StringComparer.OrdinalIgnoreCase));
            }
            try { if (File.Exists(CacheFilePath)) File.Delete(CacheFilePath); } catch { }
        }

        public static void InvalidateForMcVersion(string mcVersion)
        {
            lock (CacheLock)
            {
                var cache = LoadCache();
                var keysToRemove = cache.Entries
                    .Where(kvp => string.Equals(kvp.Value.McVersion, mcVersion, StringComparison.OrdinalIgnoreCase))
                    .Select(kvp => kvp.Key)
                    .ToList();
                foreach (var k in keysToRemove) cache.Entries.Remove(k);
            }
            SaveCache();
        }
    }
}
