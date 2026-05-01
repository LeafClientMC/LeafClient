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

        public async Task<Bitmap?> LoadSkinImageFromFileAsync(string skinFilePath, string pose, string view)
        {
            if (!File.Exists(skinFilePath))
            {
                Console.WriteLine($"[SkinRenderService] File not found: {skinFilePath}");
                return null;
            }

            try
            {
                Console.WriteLine($"[SkinRenderService] Building local render for: {skinFilePath}");

                using (Image<Rgba32> skinTexture = await Image.LoadAsync<Rgba32>(skinFilePath))
                {
                    int modelWidth = 16;
                    int modelHeight = 32;

                    using (var canvas = new Image<Rgba32>(modelWidth, modelHeight, Color.Transparent))
                    {
                        // --- 1. DEFINE SOURCE COORDINATES (Standard Skin Layout) ---
                        // Format: Rectangle(X, Y, Width, Height)

                        // Head Parts
                        var headSrc = new Rectangle(8, 8, 8, 8);
                        var headOverlaySrc = new Rectangle(40, 8, 8, 8);

                        // Body Parts
                        var bodySrc = new Rectangle(20, 20, 8, 12);
                        var bodyOverlaySrc = new Rectangle(20, 36, 8, 12);

                        // Arms (Steve/4px width)
                        var rightArmSrc = new Rectangle(44, 20, 4, 12);
                        var rightArmOverlaySrc = new Rectangle(44, 36, 4, 12);
                        var leftArmSrc = new Rectangle(36, 52, 4, 12);
                        var leftArmOverlaySrc = new Rectangle(52, 52, 4, 12);

                        // Legs
                        var rightLegSrc = new Rectangle(4, 20, 4, 12);
                        var rightLegOverlaySrc = new Rectangle(4, 36, 4, 12);
                        var leftLegSrc = new Rectangle(20, 52, 4, 12);
                        var leftLegOverlaySrc = new Rectangle(4, 52, 4, 12);

                        // --- 2. DEFINE DESTINATION POINTS (On the 16x32 Canvas) ---
                        // Head: Top Center
                        var headDest = new Point(4, 0);
                        // Torso: Center
                        var bodyDest = new Point(4, 8);
                        // Legs: Bottom
                        var leftLegDest = new Point(4, 20);
                        var rightLegDest = new Point(8, 20);
                        // Arms: Sides
                        var leftArmDest = new Point(0, 8);
                        var rightArmDest = new Point(12, 8);

                        // --- 3. DRAWING OPERATIONS ---

                        // Helper Local Function to handle the Crop/Paste logic
                        void DrawPart(IImageProcessingContext ctx, Image<Rgba32> src, Rectangle srcRect, Point dest)
                        {
                            using (var part = src.Clone(x => x.Crop(srcRect)))
                            {
                                ctx.DrawImage(part, dest, 1.0f);
                            }
                        }

                        canvas.Mutate(ctx =>
                        {
                            // A. Draw Base Layers (Behind)
                            DrawPart(ctx, skinTexture, rightLegSrc, leftLegDest);
                            DrawPart(ctx, skinTexture, leftLegSrc, rightLegDest);
                            DrawPart(ctx, skinTexture, bodySrc, bodyDest);
                            DrawPart(ctx, skinTexture, rightArmSrc, leftArmDest);
                            DrawPart(ctx, skinTexture, leftArmSrc, rightArmDest);
                            DrawPart(ctx, skinTexture, headSrc, headDest);

                            // B. Draw Overlays (Jackets/Hats)
                            // Always draw head overlay
                            DrawPart(ctx, skinTexture, headOverlaySrc, headDest);

                            // Only draw body overlays if the skin is modern (64x64)
                            if (skinTexture.Height >= 64)
                            {
                                DrawPart(ctx, skinTexture, rightLegOverlaySrc, leftLegDest);
                                DrawPart(ctx, skinTexture, leftLegOverlaySrc, rightLegDest);
                                DrawPart(ctx, skinTexture, bodyOverlaySrc, bodyDest);
                                DrawPart(ctx, skinTexture, rightArmOverlaySrc, leftArmDest);
                                DrawPart(ctx, skinTexture, leftArmOverlaySrc, rightArmDest);
                            }

                            // C. Resize to final display size (10x scale)
                            ctx.Resize(new ResizeOptions
                            {
                                Size = new Size(160, 320),
                                Sampler = KnownResamplers.NearestNeighbor, // Critical for pixel art
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
                Console.WriteLine($"[SkinRenderService] Error rendering locally: {ex.Message}");
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

        public async Task<Bitmap?> LoadSkinImageAsync(string username, string poseName, string viewType, string? uuid = null)
        {
            if (string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(uuid))
                return null;

            string cacheKey = $"{username}_{poseName}_{viewType}";
            if (_skinCache.TryGetValue(cacheKey, out var cachedBitmap))
            {
                Console.WriteLine($"[SkinRenderService] Cache hit for {cacheKey}");
                return cachedBitmap;
            }

            Bitmap? loadedBitmap = null;
            string displayName = username ?? uuid ?? "Player";

            loadedBitmap = await TryStarlightskins(username, poseName, viewType);
            if (loadedBitmap != null)
            {
                _skinCache[cacheKey] = loadedBitmap;
                return loadedBitmap;
            }

            loadedBitmap = await TryVisage(username, viewType);
            if (loadedBitmap != null)
            {
                _skinCache[cacheKey] = loadedBitmap;
                return loadedBitmap;
            }

            loadedBitmap = await TryMinotar(username, viewType);
            if (loadedBitmap != null)
            {
                _skinCache[cacheKey] = loadedBitmap;
                return loadedBitmap;
            }

            loadedBitmap = await TryCrafatar(username, viewType);
            if (loadedBitmap != null)
            {
                _skinCache[cacheKey] = loadedBitmap;
                return loadedBitmap;
            }

            Console.WriteLine($"[SkinRenderService] All fallbacks exhausted for {displayName}, returning null");
            _skinCache[cacheKey] = null;
            return null;
        }

        private async Task<Bitmap?> TryStarlightskins(string? username, string poseName, string viewType)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            string url = $"https://starlightskins.lunareclipse.studio/render/{poseName}/{username}/{viewType}";
            Console.WriteLine($"[SkinRenderService] Trying Starlightskins: {url}");

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
                            Console.WriteLine("[SkinRenderService] Starlightskins succeeded");
                            return bitmap;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SkinRenderService] Starlightskins failed: {ex.Message}");
            }

            return null;
        }

        private async Task<Bitmap?> TryVisage(string? username, string viewType)
        {
            if (string.IsNullOrWhiteSpace(username))
                return null;

            string url = $"https://visage.surgeplay.com/{viewType}/{username}";
            Console.WriteLine($"[SkinRenderService] Trying Visage: {url}");

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
                            Console.WriteLine("[SkinRenderService] Visage succeeded");
                            return bitmap;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SkinRenderService] Visage failed: {ex.Message}");
            }

            return null;
        }

        private async Task<Bitmap?> TryMinotar(string? username, string viewType)
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
            Console.WriteLine($"[SkinRenderService] Trying Minotar: {url}");

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
                            Console.WriteLine("[SkinRenderService] Minotar succeeded");
                            return bitmap;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SkinRenderService] Minotar failed: {ex.Message}");
            }

            return null;
        }

        private async Task<Bitmap?> TryCrafatar(string? username, string viewType)
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
            Console.WriteLine($"[SkinRenderService] Trying Crafatar: {url}");

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
                            Console.WriteLine("[SkinRenderService] Crafatar succeeded");
                            return bitmap;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SkinRenderService] Crafatar failed: {ex.Message}");
            }

            return null;
        }
    }
}
