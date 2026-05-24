using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

namespace LeafClient.Services
{
    public class SkinRenderService
    {
        private static readonly HttpClient _httpClient;
        private readonly Random _rand = new Random();
        private readonly Dictionary<string, Bitmap?> _skinCache = new();

        static SkinRenderService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(6);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("LeafClient-Launcher/1.0 (+https://leafclient.com)");
            _httpClient.DefaultRequestHeaders.Accept.ParseAdd("image/png,image/*;q=0.8,*/*;q=0.5");
        }

        public SkinRenderService()
        {
        }

        private readonly List<string> _smallPreviewPoseNames = new List<string>
        {
            "marching",
            "walking",
            "cheering",
            "relaxing",
            "dungeons",
            "facepalm",
            "sleeping"
        };

        private readonly List<string> _largePreviewPoseNames = new List<string>
        {
            "marching",
            "crouching",
            "criss_cross",
            "cheering",
            "relaxing",
            "cowering",
            "lunging",
            "dungeons",
            "facepalm",
            "sleeping",
            "archer",
            "kicking"
        };

        public async Task<Bitmap?> LoadSkinHeadFromFileAsync(string skinFilePath, int size = 48, bool includeOverlay = true)
        {
            if (!File.Exists(skinFilePath)) return null;
            try
            {
                using var skinTexture = await Image.LoadAsync<Rgba32>(skinFilePath);
                int outSize = 8;
                using var canvas = new Image<Rgba32>(outSize, outSize, Color.Transparent);
                canvas.Mutate(ctx =>
                {
                    using (var head = skinTexture.Clone(x => x.Crop(new Rectangle(8, 8, 8, 8))))
                        ctx.DrawImage(head, new Point(0, 0), 1.0f);
                    if (includeOverlay)
                    {
                        using var overlay = skinTexture.Clone(x => x.Crop(new Rectangle(40, 8, 8, 8)));
                        ctx.DrawImage(overlay, new Point(0, 0), 1.0f);
                    }
                    ctx.Resize(new ResizeOptions
                    {
                        Size = new Size(size, size),
                        Sampler = KnownResamplers.NearestNeighbor,
                        Mode = ResizeMode.Stretch
                    });
                });
                using var stream = new MemoryStream();
                await canvas.SaveAsPngAsync(stream);
                stream.Position = 0;
                return new Bitmap(stream);
            }
            catch (Exception ex)
            {
                LeafLog.Info("SkinRenderService", $"LoadSkinHeadFromFileAsync error: {ex.Message}");
                return null;
            }
        }

        public async Task<Bitmap?> LoadSkinImageFromFileAsync(string skinFilePath, string pose, string view)
        {
            if (!File.Exists(skinFilePath))
            {
                LeafLog.Info("SkinRenderService", $"File not found: {skinFilePath}");
                return null;
            }

            try
            {
                LeafLog.Info("SkinRenderService", $"Building local render for: {skinFilePath}");

                using (Image<Rgba32> skinTexture = await Image.LoadAsync<Rgba32>(skinFilePath))
                {
                    int modelWidth = 16;
                    int modelHeight = 32;

                    using (var canvas = new Image<Rgba32>(modelWidth, modelHeight, Color.Transparent))
                    {

                        var headSrc = new Rectangle(8, 8, 8, 8);
                        var headOverlaySrc = new Rectangle(40, 8, 8, 8);

                        var bodySrc = new Rectangle(20, 20, 8, 12);
                        var bodyOverlaySrc = new Rectangle(20, 36, 8, 12);

                        var rightArmSrc = new Rectangle(44, 20, 4, 12);
                        var rightArmOverlaySrc = new Rectangle(44, 36, 4, 12);
                        var leftArmSrc = new Rectangle(36, 52, 4, 12);
                        var leftArmOverlaySrc = new Rectangle(52, 52, 4, 12);

                        var rightLegSrc = new Rectangle(4, 20, 4, 12);
                        var rightLegOverlaySrc = new Rectangle(4, 36, 4, 12);
                        var leftLegSrc = new Rectangle(20, 52, 4, 12);
                        var leftLegOverlaySrc = new Rectangle(4, 52, 4, 12);

                        var headDest = new Point(4, 0);
                        var bodyDest = new Point(4, 8);
                        var leftLegDest = new Point(4, 20);
                        var rightLegDest = new Point(8, 20);
                        var leftArmDest = new Point(0, 8);
                        var rightArmDest = new Point(12, 8);

                        void DrawPart(IImageProcessingContext ctx, Image<Rgba32> src, Rectangle srcRect, Point dest)
                        {
                            using (var part = src.Clone(x => x.Crop(srcRect)))
                            {
                                ctx.DrawImage(part, dest, 1.0f);
                            }
                        }

                        canvas.Mutate(ctx =>
                        {
                            DrawPart(ctx, skinTexture, rightLegSrc, leftLegDest);
                            DrawPart(ctx, skinTexture, leftLegSrc, rightLegDest);
                            DrawPart(ctx, skinTexture, bodySrc, bodyDest);
                            DrawPart(ctx, skinTexture, rightArmSrc, leftArmDest);
                            DrawPart(ctx, skinTexture, leftArmSrc, rightArmDest);
                            DrawPart(ctx, skinTexture, headSrc, headDest);

                            DrawPart(ctx, skinTexture, headOverlaySrc, headDest);

                            if (skinTexture.Height >= 64)
                            {
                                DrawPart(ctx, skinTexture, rightLegOverlaySrc, leftLegDest);
                                DrawPart(ctx, skinTexture, leftLegOverlaySrc, rightLegDest);
                                DrawPart(ctx, skinTexture, bodyOverlaySrc, bodyDest);
                                DrawPart(ctx, skinTexture, rightArmOverlaySrc, leftArmDest);
                                DrawPart(ctx, skinTexture, leftArmOverlaySrc, rightArmDest);
                            }

                            ctx.Resize(new ResizeOptions
                            {
                                Size = new Size(160, 320),
                                Sampler = KnownResamplers.NearestNeighbor,
                                Mode = ResizeMode.Stretch
                            });
                        });

                        using (var stream = new MemoryStream())
                        {
                            await canvas.SaveAsPngAsync(stream);
                            stream.Position = 0;
                            return new Bitmap(stream);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LeafLog.Error("SkinRenderService", $"Error rendering locally: {ex.Message}");
                return null;
            }
        }

        private void DrawPart(IImageProcessingContext ctx, Image<Rgba32> src, Rectangle srcRect, Point dest)
        {
            using (var part = src.Clone(x => x.Crop(srcRect)))
            {
                ctx.DrawImage(part, dest, 1.0f);
            }
        }

        public string GetRandomSmallPoseName() =>
            _smallPreviewPoseNames[_rand.Next(_smallPreviewPoseNames.Count)];

        public string GetRandomLargePoseName() =>
            _largePreviewPoseNames[_rand.Next(_largePreviewPoseNames.Count)];

        public void InvalidateCacheFor(string? username, string? uuid = null)
        {
            if (string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(uuid)) return;
            string? normUuid = string.IsNullOrWhiteSpace(uuid) ? null : uuid.Replace("-", "").ToLowerInvariant();
            var toRemove = new List<string>();
            foreach (var key in _skinCache.Keys)
            {
                if ((!string.IsNullOrEmpty(username) && key.StartsWith(username + "_", StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrEmpty(uuid) && key.StartsWith(uuid + "_", StringComparison.OrdinalIgnoreCase))
                    || (normUuid != null && key.StartsWith("mojangHead_" + normUuid, StringComparison.OrdinalIgnoreCase)))
                {
                    toRemove.Add(key);
                }
            }
            foreach (var k in toRemove) _skinCache.Remove(k);
            LeafLog.Info("SkinRenderService", $"Invalidated {toRemove.Count} cache entries for {username ?? uuid}");
        }

        public void InvalidateAllCache()
        {
            int count = _skinCache.Count;
            _skinCache.Clear();
            LeafLog.Info("SkinRenderService", $"Invalidated all {count} cache entries");
        }

        public async Task<Bitmap?> LoadSkinHeadByUsernameAsync(string? username, int size = 48, bool forceFresh = false)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;
            string cacheKey = $"mojangHeadByName_{username.ToLowerInvariant()}_{size}";
            if (forceFresh) _skinCache.Remove(cacheKey);
            else if (_skinCache.TryGetValue(cacheKey, out var cached)) return cached;

            string? resolvedUuid = null;
            try
            {
                var lookupResp = await _httpClient.GetAsync($"https://api.mojang.com/users/profiles/minecraft/{Uri.EscapeDataString(username)}");
                if (lookupResp.IsSuccessStatusCode)
                {
                    var body = await lookupResp.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        using var doc = JsonDocument.Parse(body);
                        if (doc.RootElement.TryGetProperty("id", out var idEl))
                            resolvedUuid = idEl.GetString();
                    }
                }
            }
            catch (Exception ex)
            {
                LeafLog.Info("SkinRenderService", $"Username lookup failed for {username}: {ex.Message}");
            }

            if (!string.IsNullOrEmpty(resolvedUuid))
            {
                var bmp = await LoadSkinHeadFromUuidAsync(resolvedUuid, username, size, forceFresh);
                if (bmp != null)
                {
                    _skinCache[cacheKey] = bmp;
                    return bmp;
                }
            }

            var defaultBmp = await LoadDefaultSteveOrAlexHeadAsync(username, size);
            if (defaultBmp != null) _skinCache[cacheKey] = defaultBmp;
            return defaultBmp;
        }

        private async Task<Bitmap?> LoadDefaultSteveOrAlexHeadAsync(string identifier, int size)
        {
            int sum = 0;
            foreach (var ch in (identifier ?? "").ToLowerInvariant()) sum += ch;
            bool isAlex = (sum & 1) == 1;
            string assetName = isAlex ? "Alex.png" : "Steve.png";
            var uri = new Uri($"avares://LeafClient/Assets/{assetName}");
            try
            {
                using var stream = Avalonia.Platform.AssetLoader.Open(uri);
                using var skinTexture = Image.Load<Rgba32>(stream);
                using var canvas = new Image<Rgba32>(8, 8, Color.Transparent);
                canvas.Mutate(ctx =>
                {
                    using (var head = skinTexture.Clone(x => x.Crop(new Rectangle(8, 8, 8, 8))))
                        ctx.DrawImage(head, new Point(0, 0), 1.0f);
                    using var overlay = skinTexture.Clone(x => x.Crop(new Rectangle(40, 8, 8, 8)));
                    ctx.DrawImage(overlay, new Point(0, 0), 1.0f);
                    ctx.Resize(new ResizeOptions
                    {
                        Size = new Size(size, size),
                        Sampler = KnownResamplers.NearestNeighbor,
                        Mode = ResizeMode.Stretch
                    });
                });
                using var outStream = new MemoryStream();
                await canvas.SaveAsPngAsync(outStream);
                outStream.Position = 0;
                return new Bitmap(outStream);
            }
            catch (Exception ex)
            {
                LeafLog.Info("SkinRenderService", $"Default skin head load failed: {ex.Message}");
                return null;
            }
        }

        public async Task<Bitmap?> LoadSkinHeadFromUuidAsync(string? uuid, string? username, int size = 48, bool forceFresh = false)
        {
            if (string.IsNullOrWhiteSpace(uuid)) return null;
            string normUuid = uuid.Replace("-", "").ToLowerInvariant();
            string cacheKey = $"mojangHead_{normUuid}_{size}";
            if (forceFresh)
            {
                _skinCache.Remove(cacheKey);
            }
            else if (_skinCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            try
            {
                string profileUrl = $"https://sessionserver.mojang.com/session/minecraft/profile/{normUuid}";
                var profileResp = await _httpClient.GetAsync(profileUrl);
                if (!profileResp.IsSuccessStatusCode) return null;
                var profileJson = await profileResp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(profileJson);
                if (!doc.RootElement.TryGetProperty("properties", out var props)) return null;
                string? textureUrl = null;
                foreach (var p in props.EnumerateArray())
                {
                    if (p.TryGetProperty("name", out var pn) && pn.GetString() == "textures"
                        && p.TryGetProperty("value", out var pv))
                    {
                        var b64 = pv.GetString();
                        if (string.IsNullOrEmpty(b64)) continue;
                        var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(b64));
                        using var txDoc = JsonDocument.Parse(decoded);
                        if (txDoc.RootElement.TryGetProperty("textures", out var textures)
                            && textures.TryGetProperty("SKIN", out var skin)
                            && skin.TryGetProperty("url", out var urlEl))
                        {
                            textureUrl = urlEl.GetString();
                            break;
                        }
                    }
                }
                if (string.IsNullOrEmpty(textureUrl)) return null;

                var pngResp = await _httpClient.GetAsync(textureUrl);
                if (!pngResp.IsSuccessStatusCode) return null;
                var pngBytes = await pngResp.Content.ReadAsByteArrayAsync();

                using var skinTexture = Image.Load<Rgba32>(pngBytes);
                using var canvas = new Image<Rgba32>(8, 8, Color.Transparent);
                canvas.Mutate(ctx =>
                {
                    using (var head = skinTexture.Clone(x => x.Crop(new Rectangle(8, 8, 8, 8))))
                        ctx.DrawImage(head, new Point(0, 0), 1.0f);
                    using var overlay = skinTexture.Clone(x => x.Crop(new Rectangle(40, 8, 8, 8)));
                    ctx.DrawImage(overlay, new Point(0, 0), 1.0f);
                    ctx.Resize(new ResizeOptions
                    {
                        Size = new Size(size, size),
                        Sampler = KnownResamplers.NearestNeighbor,
                        Mode = ResizeMode.Stretch
                    });
                });
                using var stream = new MemoryStream();
                await canvas.SaveAsPngAsync(stream);
                stream.Position = 0;
                var bmp = new Bitmap(stream);
                _skinCache[cacheKey] = bmp;
                return bmp;
            }
            catch (Exception ex)
            {
                LeafLog.Info("SkinRenderService", $"LoadSkinHeadFromUuidAsync error: {ex.Message}");
                return null;
            }
        }

        public async Task<Bitmap?> LoadSkinImageAsync(string username, string poseName, string viewType, string? uuid = null, bool forceFresh = false)
        {
            if (string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(uuid))
                return null;

            string cacheKey = $"{username}_{poseName}_{viewType}";
            if (forceFresh)
            {
                _skinCache.Remove(cacheKey);
            }
            else if (_skinCache.TryGetValue(cacheKey, out var cachedBitmap))
            {
                LeafLog.Info("SkinRenderService", $"Cache hit for {cacheKey}");
                return cachedBitmap;
            }

            Bitmap? loadedBitmap = null;
            string displayName = username ?? uuid ?? "Player";
            string? bust = forceFresh ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString() : null;

            loadedBitmap = await TryStarlightskins(username, poseName, viewType, bust);
            if (loadedBitmap != null)
            {
                _skinCache[cacheKey] = loadedBitmap;
                return loadedBitmap;
            }

            loadedBitmap = await TryVisage(username, viewType, bust);
            if (loadedBitmap != null)
            {
                _skinCache[cacheKey] = loadedBitmap;
                return loadedBitmap;
            }

            loadedBitmap = await TryMinotar(username, viewType, bust);
            if (loadedBitmap != null)
            {
                _skinCache[cacheKey] = loadedBitmap;
                return loadedBitmap;
            }

            loadedBitmap = await TryCrafatar(username, viewType, bust);
            if (loadedBitmap != null)
            {
                _skinCache[cacheKey] = loadedBitmap;
                return loadedBitmap;
            }

            LeafLog.Info("SkinRenderService", $"All fallbacks exhausted for {displayName}, returning null");
            _skinCache[cacheKey] = null;
            return null;
        }

        private async Task<Bitmap?> TryStarlightskins(string? username, string poseName, string viewType, string? bust = null)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            string url = $"https://starlightskins.lunareclipse.studio/render/{poseName}/{username}/{viewType}";
            if (!string.IsNullOrEmpty(bust)) url += $"?t={bust}";
            LeafLog.Info("SkinRenderService", $"Trying Starlightskins: {url}");

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode && response.Content.Headers.ContentLength > 0)
                {
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        if (stream.Length > 0)
                        {
                            var bitmap = new Bitmap(stream);
                            LeafLog.Info("SkinRenderService", "Starlightskins succeeded");
                            return bitmap;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LeafLog.Info("SkinRenderService", $"Starlightskins failed: {ex.Message}");
            }

            return null;
        }

        private async Task<Bitmap?> TryVisage(string? username, string viewType, string? bust = null)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            string url = $"https://visage.surgeplay.com/{viewType}/{username}";
            if (!string.IsNullOrEmpty(bust)) url += $"?t={bust}";
            LeafLog.Info("SkinRenderService", $"Trying Visage: {url}");

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode && response.Content.Headers.ContentLength > 0)
                {
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        if (stream.Length > 0)
                        {
                            var bitmap = new Bitmap(stream);
                            LeafLog.Info("SkinRenderService", "Visage succeeded");
                            return bitmap;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LeafLog.Info("SkinRenderService", $"Visage failed: {ex.Message}");
            }

            return null;
        }

        private async Task<Bitmap?> TryMinotar(string? username, string viewType, string? bust = null)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            string endpoint = viewType switch
            {
                "bust" => "avatar",
                "full" => "body",
                _ => "avatar"
            };

            string url = $"https://minotar.net/{endpoint}/{username}/256.png";
            if (!string.IsNullOrEmpty(bust)) url += $"?t={bust}";
            LeafLog.Info("SkinRenderService", $"Trying Minotar: {url}");

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode && response.Content.Headers.ContentLength > 0)
                {
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        if (stream.Length > 0)
                        {
                            var bitmap = new Bitmap(stream);
                            LeafLog.Info("SkinRenderService", "Minotar succeeded");
                            return bitmap;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LeafLog.Info("SkinRenderService", $"Minotar failed: {ex.Message}");
            }

            return null;
        }

        private async Task<Bitmap?> TryCrafatar(string? username, string viewType, string? bust = null)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            string endpoint = viewType switch
            {
                "bust" => "avatars",
                "full" => "renders/body",
                _ => "avatars"
            };

            string url = $"https://crafatar.com/{endpoint}/{username}?size=256&overlay";
            if (!string.IsNullOrEmpty(bust)) url += $"&t={bust}";
            LeafLog.Info("SkinRenderService", $"Trying Crafatar: {url}");

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode && response.Content.Headers.ContentLength > 0)
                {
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        if (stream.Length > 0)
                        {
                            var bitmap = new Bitmap(stream);
                            LeafLog.Info("SkinRenderService", "Crafatar succeeded");
                            return bitmap;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LeafLog.Info("SkinRenderService", $"Crafatar failed: {ex.Message}");
            }

            return null;
        }
    }
}
