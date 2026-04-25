using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LeafClient.Services
{
    /// <summary>
    /// One-shot utility that renders every Leaf Client cosmetic to disk as a
    /// set of standalone PNG images — four camera angles per cosmetic (front,
    /// back, angled-left, angled-right).
    ///
    /// These images are meant to be used as source material for banners,
    /// marketing, and store artwork — they're NOT loaded at runtime by the
    /// launcher itself.  Output goes to %APPDATA%\LeafClient\CosmeticPreviews\
    /// so the Desktop stays clean and the files live alongside the rest of
    /// Leaf Client's user data.
    ///
    /// Invoked by the dev-only "Bake Cosmetic Previews" button in Settings →
    /// About, which calls <see cref="BakeAllAsync"/> on a background task.
    /// </summary>
    public static class CosmeticPreviewBaker
    {
        private const int ImageSize = 512;

        /// <summary>Four camera angles we render each cosmetic from.</summary>
        private static readonly (string name, float rotationY)[] Angles =
        {
            ("front",        0f),
            ("back",         180f),
            ("angled-left",  315f),
            ("angled-right", 45f),
        };

        /// <summary>
        /// Every cosmetic we want to bake.  These are the IDs used by
        /// <see cref="CosmeticHelpers.CosmeticAssetPaths"/> + every aura type
        /// (auras have no texture asset — just a rendered particle effect).
        /// </summary>
        private static readonly (string id, string category)[] Items =
        {
            ("cape-leaf",          "capes"),
            ("hat-crown",          "hats"),
            ("hat-horns",          "hats"),
            ("wings-angel",        "wings"),
            ("wings-dragon",       "wings"),
            ("wings-purple",       "wings"),
            ("wings-black",        "wings"),
            ("aura-inferno",       "auras"),
            ("aura-broken-hearts", "auras"),
            ("aura-darkness",      "auras"),
        };

        /// <summary>
        /// Runs the full bake.  Safe to call from a UI thread — the heavy
        /// rasterisation work runs on a background task.  Returns the output
        /// directory so the caller can surface it / open it in Explorer.
        /// </summary>
        /// <param name="userSkinBytes">
        /// Optional raw PNG bytes of the user's Minecraft skin.  When provided,
        /// the baker renders every cosmetic on top of that skin (so the user's
        /// own character shows up in every preview).  When null, falls back to
        /// a neutral grey mannequin — handy for offline/headless testing.
        /// Pass in the result of <c>MainWindow.FetchSkinBytesAsync()</c>, which
        /// already works for any logged-in Minecraft account because it uses
        /// Mojang's public session server + minotar.net fallback.
        /// </param>
        public static async Task<BakeResult> BakeAllAsync(byte[]? userSkinBytes = null, Action<string>? log = null)
        {
            log ??= _ => { };

            string outDir = GetOutputDirectory();
            Directory.CreateDirectory(outDir);
            log($"[Bake] Output directory: {outDir}");

            int written = 0;
            int skipped = 0;
            var errors = new List<string>();

            // Prefer the user's actual Minecraft skin when we have it — makes
            // previews feel personal ("here's ME wearing the Leaf cape") and
            // gives proper skin tones for arms/legs that peek out around
            // wings and hats.  If we can't get it (offline, brand-new account,
            // network hiccup), fall back to a neutral grey mannequin so the
            // bake still produces useful output.
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
                    bool isAngelWings = false;
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
                                    isAngelWings = id.Contains("angel");
                                    break;
                                }
                            case "auras":
                                auraType = CosmeticHelpers.AuraIdToType(id);
                                break;
                        }

                        foreach (var (angleName, rotationY) in Angles)
                        {
                            // Wings look much better mid-flap; use a fixed
                            // phase so both wings are extended symmetrically
                            // rather than folded into the body.
                            const float wingFlapPhase = 0.75f;

                            // Slight pitch so the character isn't perfectly
                            // flat to camera — reads as 3D instead of 2D.
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
                                isAngelWings: isAngelWings,
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

            log($"[Bake] Done — wrote {written} PNGs, skipped {skipped}, errors {errors.Count}");

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

        // ────────────────────────────────────────────────────────────────────

        private static string GetOutputDirectory()
        {
            // %APPDATA%\LeafClient\CosmeticPreviews — matches where the
            // launcher stores the rest of its per-user data (settings,
            // owned.json, etc.) instead of cluttering the Desktop.
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "LeafClient", "CosmeticPreviews");
        }

        /// <summary>
        /// Builds a 64×64 light-grey mannequin skin with every player region
        /// filled in a neutral base colour.  The Minecraft skin format uses
        /// a fixed UV layout — we just paint every pixel so all surfaces are
        /// rendered, then leave the overlay (hat layer) half fully transparent.
        /// </summary>
        private static Image<Rgba32> CreateMannequinSkin()
        {
            const int size = 64;
            var skin = new Image<Rgba32>(size, size, new Rgba32(0, 0, 0, 0));

            // Main body half (rows 0-31): neutral grey + subtle face shading.
            // The renderer reads from specific UV rectangles; filling every
            // pixel ensures every face of every cuboid has a colour to sample.
            var body    = new Rgba32(200, 200, 200, 255); // light grey
            var shadow  = new Rgba32(160, 160, 160, 255); // mild side-shadow
            var accent  = new Rgba32(90,  90,  110, 255); // eyes / belt hint

            for (int y = 0; y < 32; y++)
            for (int x = 0; x < size; x++)
            {
                skin[x, y] = ((x + y) & 1) == 0 ? body : shadow;
            }

            // Head front face (8,8)-(15,15): small hint of face geometry.
            // Eye dots at (9,12) & (13,12) and a mouth pixel at (11,14).
            skin[9,  12] = accent;
            skin[13, 12] = accent;
            skin[11, 14] = accent;

            // Rows 32-63 are the second body half (legs-back etc.) in the
            // modern 64×64 format — fill those with the same base colour so
            // the back of the character isn't invisible.
            for (int y = 32; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Leave the overlay/hat-layer regions transparent by skipping
                // the hat slot (rows 0-7 of this half maps to hat layer in the
                // upper half — so nothing to do here for the body half).
                skin[x, y] = ((x + y) & 1) == 0 ? body : shadow;
            }

            return skin;
        }

        /// <summary>
        /// The renderer emits a BGRA8888 buffer (for Avalonia WriteableBitmap).
        /// ImageSharp has a Bgra32 pixel format that accepts this directly.
        /// </summary>
        private static void SaveBgraBufferAsPng(byte[] buffer, int width, int height, string path)
        {
            using var img = Image.LoadPixelData<Bgra32>(buffer, width, height);
            img.SaveAsPng(path);
        }
    }
}
