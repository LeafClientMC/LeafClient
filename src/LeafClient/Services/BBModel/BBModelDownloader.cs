using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace LeafClient.Services.BBModel
{
    public sealed class BBModelInstance
    {
        public BBModel Model { get; }
        public List<Image<Rgba32>?> Textures { get; }
        public BBModelInstance(BBModel model, List<Image<Rgba32>?> textures) { Model = model; Textures = textures; }
    }

    public static class BBModelDownloader
    {
        public const long MaxBytes = 5L * 1024 * 1024;

        private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
        {
            "api.leafclient.com",
            "cdn.leafclient.com"
        };

        private static readonly HttpClient Http = new(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(15) };
        private static readonly ConcurrentDictionary<string, BBModelInstance> Cache = new();
        private static readonly ConcurrentDictionary<string, Task<BBModelInstance?>> InFlight = new();

        public static BBModelInstance? GetCached(string url) => Cache.TryGetValue(url, out var v) ? v : null;

        public static Task<BBModelInstance?> FetchAsync(string url)
        {
            if (string.IsNullOrEmpty(url)) return Task.FromResult<BBModelInstance?>(null);
            if (Cache.TryGetValue(url, out var cached)) return Task.FromResult<BBModelInstance?>(cached);
            return InFlight.GetOrAdd(url, FetchInternalAsync);
        }

        private static async Task<BBModelInstance?> FetchInternalAsync(string url)
        {
            try
            {
                if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    LeafLog.Info("BBModel", $"non-HTTPS URL refused: {url}");
                    return null;
                }
                Uri uri;
                try { uri = new Uri(url); }
                catch { LeafLog.Info("BBModel", $"malformed URL: {url}"); return null; }
                if (!AllowedHosts.Contains(uri.Host))
                {
                    LeafLog.Info("BBModel", $"host not whitelisted: {uri.Host}");
                    return null;
                }
                using var resp = await Http.GetAsync(url);
                if (!resp.IsSuccessStatusCode)
                {
                    LeafLog.Info("BBModel", $"HTTP {(int)resp.StatusCode} for {url}");
                    return null;
                }
                var bytes = await resp.Content.ReadAsByteArrayAsync();
                if (bytes.Length > MaxBytes)
                {
                    LeafLog.Info("BBModel", $"file too large: {bytes.Length} bytes");
                    return null;
                }
                var model = BBModelParser.Parse(bytes);
                var textures = new List<Image<Rgba32>?>();
                foreach (var t in model.Textures)
                {
                    if (t.DecodedPng == null || t.DecodedPng.Length == 0) { textures.Add(null); continue; }
                    try
                    {
                        var img = SixLabors.ImageSharp.Image.Load<Rgba32>(t.DecodedPng);
                        textures.Add(img);
                    }
                    catch
                    {
                        textures.Add(null);
                    }
                }
                var inst = new BBModelInstance(model, textures);
                Cache[url] = inst;
                return inst;
            }
            catch (Exception ex)
            {
                LeafLog.Error("BBModel", $"fetch failed for {url}: {ex.Message}");
                return null;
            }
            finally
            {
                InFlight.TryRemove(url, out _);
            }
        }
    }

    public static class BBModelCatalog
    {
        private static volatile Dictionary<string, LeafApiCatalogCosmetic>? _catalog;
        private static Task<bool>? _loading;
        private static readonly object _lock = new();

        public static Task<bool> EnsureLoadedAsync()
        {
            if (_catalog != null) return Task.FromResult(true);
            lock (_lock)
            {
                if (_catalog != null) return Task.FromResult(true);
                if (_loading != null) return _loading;
                _loading = LoadAsync();
                return _loading;
            }
        }

        public static Task<bool> RefreshAsync()
        {
            lock (_lock)
            {
                _catalog = null;
                _loading = null;
            }
            LeafClient.Services.CosmeticHelpers.InvalidateCardPreviewCache();
            return EnsureLoadedAsync();
        }

        private static async Task<bool> LoadAsync()
        {
            var list = await LeafApiService.GetCosmeticsCatalogAsync();
            if (list == null) { lock (_lock) { _loading = null; } return false; }
            var dict = new Dictionary<string, LeafApiCatalogCosmetic>(StringComparer.Ordinal);
            foreach (var c in list) dict[c.Id] = c;
            _catalog = dict;
            lock (_lock) { _loading = null; }
            return true;
        }

        public static LeafApiCatalogCosmetic? Get(string slug) =>
            _catalog != null && _catalog.TryGetValue(slug, out var c) ? c : null;

        public static bool IsBBModel(string slug)
        {
            var c = Get(slug);
            return c != null && string.Equals(c.ModelFormat, "bbmodel", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(c.BBModelUrl);
        }
    }
}
