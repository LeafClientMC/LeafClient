using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LeafClient.Services
{
    public static class CosmeticPreviewBaker
    {
        private const int ImageSize = 512;

        private static readonly (string name, float rotationY)[] Angles =
        {
            ("front",        0f),
            ("back",         180f),
            ("angled-left",  315f),
            ("angled-right", 45f),
        };

        private static readonly (string id, string category)[] Items =
        {
            ("cape-leaf",          "capes"),
            ("hat-crown",          "hats"),
            ("hat-horns",          "hats"),
            ("wings-dragon",       "wings"),
            ("wings-purple",       "wings"),
            ("wings-black",        "wings"),
            ("aura-inferno",       "auras"),
            ("aura-broken-hearts", "auras"),
            ("aura-darkness",      "auras"),
        };

        public static async Task<BakeResult> BakeAllAsync(byte[]? userSkinBytes = null, Action<string>? log = null)
        {
            log ??= _ => { };

            string outDir = GetOutputDirectory();
            Directory.CreateDirectory(outDir);
            log($"[Bake] Output directory: {outDir}");

            int written = 0;
            int skipped = 0;
            var errors = new List<string>();

            Image<Rgba32> baseSkin;
            if (userSkinBytes != null && userSkinBytes.Length > 0)
            {
                try
                {
                    baseSkin = Image.Load<Rgba32>(userSkinBytes);
                    log($"[Bake] Using user's Minecraft skin as the base ({baseSkin.Width}x{baseSkin.Height}).");
                }
                catch (Exception ex)
                {
                    log($"[Bake] Failed to decode user skin bytes ({ex.Message}); falling back to mannequin.");
                    baseSkin = CreateMannequinSkin();
                }
            }
            else
            {
                baseSkin = CreateMannequinSkin();
                log("[Bake] No user skin supplied; using neutral mannequin.");
            }

            using var _baseSkinDisposer = baseSkin;

            await Task.Run(() =>
            {
                foreach (var (id, category) in Items)
                {
                    Image<Rgba32>? cape = null;
                    Image<Rgba32>? hat = null;
                    Image<Rgba32>? wings = null;
                    bool isHorns = false;
                    string? auraType = null;

                    try
                    {
                        switch (category)
                        {
                            case "capes":
                                {
                                    var bytes = CosmeticHelpers.TryLoadCosmeticAsset(id);
                                    if (bytes == null) { skipped++; log($"[Bake] SKIP {id}: asset not found"); continue; }
                                    cape = Image.Load<Rgba32>(bytes);
                                    break;
                                }
                            case "hats":
                                {
                                    var bytes = CosmeticHelpers.TryLoadCosmeticAsset(id);
                                    if (bytes == null) { skipped++; log($"[Bake] SKIP {id}: asset not found"); continue; }
                                    hat = Image.Load<Rgba32>(bytes);
                                    isHorns = id.Contains("horns");
                                    break;
                                }
                            case "wings":
                                {
                                    var bytes = CosmeticHelpers.TryLoadCosmeticAsset(id);
                                    if (bytes == null) { skipped++; log($"[Bake] SKIP {id}: asset not found"); continue; }
                                    wings = Image.Load<Rgba32>(bytes);
                                    break;
                                }
                            case "auras":
                                auraType = CosmeticHelpers.AuraIdToType(id);
                                break;
                        }

                        foreach (var (angleName, rotationY) in Angles)
                        {
                            const float wingFlapPhase = 0.75f;

                            const float rotationX = -10f;

                            byte[]? bgra = LeafClient.Controls.SkinRendererControl.RenderFrame(
                                baseSkin,
                                cape,
                                hat,
                                wings,
                                ImageSize,
                                ImageSize,
                                rotationY,
                                rotationX,
                                zoom: 1.15f,
                                capeSwingX: 0f,
                                wingFlapPhase: wingFlapPhase,
                                isHorns: isHorns,
                                auraType: auraType,
                                auraPhase: 0.5f,
                                previewLit: false);

                            if (bgra == null)
                            {
                                errors.Add($"{id}/{angleName}: RenderFrame returned null");
                                continue;
                            }

                            string fileName = $"{id}_{angleName}.png";
                            string path = Path.Combine(outDir, fileName);
                            SaveBgraBufferAsPng(bgra, ImageSize, ImageSize, path);
                            written++;
                            log($"[Bake] wrote {fileName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{id}: {ex.Message}");
                        log($"[Bake] ERROR {id}: {ex.Message}");
                    }
                    finally
                    {
                        cape?.Dispose();
                        hat?.Dispose();
                        wings?.Dispose();
                    }
                }
            });

            log($"[Bake] Done - wrote {written} PNGs, skipped {skipped}, errors {errors.Count}");

            return new BakeResult
            {
                OutputDirectory = outDir,
                ImagesWritten   = written,
                Skipped         = skipped,
                Errors          = errors,
            };
        }

        public sealed class BakeResult
        {
            public required string OutputDirectory { get; init; }
            public required int ImagesWritten { get; init; }
            public required int Skipped { get; init; }
            public required List<string> Errors { get; init; }
        }

        private static string GetOutputDirectory()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "LeafClient", "CosmeticPreviews");
        }

        internal static Image<Rgba32> CreateMannequinSkin()
        {
            const int size = 64;
            var skin = new Image<Rgba32>(size, size, new Rgba32(0, 0, 0, 0));

            var body    = new Rgba32(200, 200, 200, 255);
            var shadow  = new Rgba32(160, 160, 160, 255);
            var accent  = new Rgba32(90,  90,  110, 255);

            for (int y = 0; y < 32; y++)
            for (int x = 0; x < size; x++)
            {
                skin[x, y] = ((x + y) & 1) == 0 ? body : shadow;
            }

            skin[9,  12] = accent;
            skin[13, 12] = accent;
            skin[11, 14] = accent;

            for (int y = 32; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                skin[x, y] = ((x + y) & 1) == 0 ? body : shadow;
            }

            return skin;
        }

        private static void SaveBgraBufferAsPng(byte[] buffer, int width, int height, string path)
        {
            using var img = Image.LoadPixelData<Bgra32>(buffer, width, height);
            img.SaveAsPng(path);
        }
    }
}
