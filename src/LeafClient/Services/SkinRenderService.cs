using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json; // Keep this if you use JsonSerializer elsewhere in this file
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
        // Removed MojangApiService field as requested

        static SkinRenderService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        // MODIFIED: Reverted constructor to original form (no MojangApiService or logger Action)
        public SkinRenderService()
        {
            // No initialization needed here if _httpClient is static and _rand is readonly
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

        // MODIFIED: Reverted to original signature, removed accessToken parameter
        public async Task<Bitmap?> LoadSkinImageAsync(string username, string poseName, string viewType, string? uuid = null)
        {
            Bitmap? loadedBitmap = null;
            string? skinImageUrl = null;
            string? skinModel = null;

            // Removed MojangApiService call as requested.
            // If you want to use official Mojang API for skin URLs, it would need to be outside this method
            // or MojangApiService would need to be re-introduced with proper initialization.

            // Attempt 1: Starlight Skins
            string starlightUrl = $"https://starlightskins.lunareclipse.studio/render/{poseName}/{username}/{viewType}";
            Console.WriteLine($"[SkinRenderService] Starlight Request: {starlightUrl}"); // MODIFIED: Use Console.WriteLine

            try
            {
                var response = await _httpClient.GetAsync(starlightUrl);
                if (response.IsSuccessStatusCode)
                {
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        loadedBitmap = new Bitmap(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SkinRenderService] Starlight error: {ex.Message}"); // MODIFIED: Use Console.WriteLine
            }

            // Attempt 2: Visage fallback
            if (loadedBitmap == null && (!string.IsNullOrWhiteSpace(username) || !string.IsNullOrWhiteSpace(uuid)))
            {
                string visageUrl = $"https://visage.surgeplay.com/{viewType}/{username}";
                Console.WriteLine($"[SkinRenderService] Visage Request: {visageUrl}"); // MODIFIED: Use Console.WriteLine

                try
                {
                    var response = await _httpClient.GetAsync(visageUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        {
                            loadedBitmap = new Bitmap(stream);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SkinRenderService] Visage error: {ex.Message}"); // MODIFIED: Use Console.WriteLine
                }
            }

            // Removed direct Mojang skin download fallback as requested.

            if (loadedBitmap == null)
            {
                Console.WriteLine($"[SkinRenderService] No renderer succeeded for '{username}'. Returning null."); // MODIFIED: Use Console.WriteLine
            }

            return loadedBitmap;
        }
    }
}
