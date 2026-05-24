using Avalonia.Platform;
using Avalonia.Threading;
using LeafClient.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LeafClient.Services
{
    public static class CosmeticHelpers
    {
        private static readonly Dictionary<string, string> AuraTypeMap = new()
        {
            ["aura-darkness"]      = "darkness",
            ["aura-inferno"]       = "flames",
            ["aura-broken-hearts"] = "hearts",
        };

        public static string? AuraIdToType(string auraId) =>
            AuraTypeMap.TryGetValue(auraId, out var t) ? t : null;

        public static readonly Dictionary<string, string> CosmeticAssetPaths = new()
        {
            ["cape-leaf"]         = "avares://LeafClient/Assets/LeafClientCape.png",
            ["hat-crown"]         = "avares://LeafClient/Assets/LeafClientHat.png",
            ["wings-dragon"]      = "avares://LeafClient/Assets/LeafClientWings.png",
            ["wings-black"]       = "avares://LeafClient/Assets/BlackDemonWings.png",
            ["wings-purple"]      = "avares://LeafClient/Assets/PurpleDemonWings.png",
            ["hat-horns"]         = "avares://LeafClient/Assets/ShadowHorns.png",
            ["hat-shadow-horns"]  = "avares://LeafClient/Assets/ShadowHorns.png",
        };

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte[]?> AssetCache = new();

        public static byte[]? TryLoadCosmeticAsset(string cosmeticId)
        {
            if (AssetCache.TryGetValue(cosmeticId, out var cached)) return cached;
            if (LeafClient.Services.BBModel.BBModelCatalog.IsBBModel(cosmeticId))
            {
                AssetCache[cosmeticId] = null;
                return null;
            }
            if (!CosmeticAssetPaths.TryGetValue(cosmeticId, out var uri))
            {
                AssetCache[cosmeticId] = null;
                return null;
            }

            try
            {
                using var stream = AssetLoader.Open(new Uri(uri));
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                var bytes = ms.ToArray();
                AssetCache[cosmeticId] = bytes;
                return bytes;
            }
            catch (Exception ex)
            {
                LeafLog.Info("Cosmetics", $"Embedded resource not found for '{cosmeticId}': {ex.Message}");
            }

            try
            {
                var relativePath = new Uri(uri).AbsolutePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
                if (File.Exists(fullPath))
                {
                    var bytes = File.ReadAllBytes(fullPath);
                    AssetCache[cosmeticId] = bytes;
                    return bytes;
                }
            }
            catch (Exception ex)
            {
                LeafLog.Error("Cosmetics", $"File system fallback failed for '{cosmeticId}': {ex.Message}");
            }

            AssetCache[cosmeticId] = null;
            return null;
        }

        public static Task<byte[]?> TryLoadCosmeticAssetAsync(string cosmeticId)
        {
            if (AssetCache.TryGetValue(cosmeticId, out var cached))
                return Task.FromResult(cached);
            return Task.Run(() => TryLoadCosmeticAsset(cosmeticId));
        }

        private static readonly Dictionary<string, byte[]?> CardPreviewCache = new();
        private static readonly object CardPreviewLock = new();

        public static byte[]? GetCachedCardPreview(string cosmeticId)
        {
            lock (CardPreviewLock)
            {
                return CardPreviewCache.TryGetValue(cosmeticId, out var v) ? v : null;
            }
        }

        public static void InvalidateCardPreviewCache()
        {
            lock (CardPreviewLock)
            {
                CardPreviewCache.Clear();
            }
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string dir = Path.Combine(appData, "LeafClient", "CosmeticPreviews");
                if (Directory.Exists(dir))
                {
                    foreach (var f in Directory.EnumerateFiles(dir, "*_front.png"))
                    {
                        try { File.Delete(f); } catch { }
                    }
                }
            }
            catch (Exception ex) { LeafLog.Info("CardPreview", $"disk-cache nuke failed: {ex.Message}"); }
        }

        public static async Task<byte[]?> RenderCardPreviewAsync(string cosmeticId, string category, byte[]? userSkinBytes)
        {
            lock (CardPreviewLock)
            {
                if (CardPreviewCache.TryGetValue(cosmeticId, out var v))
                    return v;
            }
            byte[]? bytes = null;
            bool hasUserSkin = userSkinBytes != null && userSkinBytes.Length > 0;
            if (!hasUserSkin)
            {
                bytes = await Task.Run(() => TryLoadBakedCardPreview(cosmeticId)).ConfigureAwait(false);
            }
            if (bytes == null)
            {
                bytes = await RenderFreshCardPreviewAsync(cosmeticId, category, userSkinBytes).ConfigureAwait(false);
            }
            lock (CardPreviewLock) { CardPreviewCache[cosmeticId] = bytes; }
            return bytes;
        }

        private static async Task<byte[]?> RenderFreshCardPreviewAsync(string cosmeticId, string category, byte[]? userSkinBytes)
        {
            try { await BBModel.BBModelCatalog.EnsureLoadedAsync().ConfigureAwait(false); }
            catch { }

            BBModel.BBModelInstance? bbInstance = null;
            string bbAttachment = "head_top";
            float bbOx = 0f, bbOy = 0f, bbOz = 0f, bbRy = 0f, bbSc = 1f;
            if (BBModel.BBModelCatalog.IsBBModel(cosmeticId))
            {
                var meta = BBModel.BBModelCatalog.Get(cosmeticId);
                if (meta != null && !string.IsNullOrEmpty(meta.BBModelUrl))
                {
                    try { bbInstance = await BBModel.BBModelDownloader.FetchAsync(meta.BBModelUrl).ConfigureAwait(false); }
                    catch (Exception ex) { LeafLog.Error("CardPreview", $"bbmodel fetch failed for {cosmeticId}: {ex.Message}"); }
                    if (!string.IsNullOrEmpty(meta.Attachment)) bbAttachment = meta.Attachment;
                    bbOx = meta.OffsetX; bbOy = meta.OffsetY; bbOz = meta.OffsetZ;
                    bbRy = meta.RotationY; bbSc = meta.Scale <= 0f ? 1f : meta.Scale;
                }
            }

            return await Task.Run(() => RenderFreshCardPreview(cosmeticId, category, userSkinBytes, bbInstance, bbAttachment, bbOx, bbOy, bbOz, bbRy, bbSc)).ConfigureAwait(false);
        }

        private static byte[]? TryLoadBakedCardPreview(string cosmeticId)
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string path = Path.Combine(appData, "LeafClient", "CosmeticPreviews", $"{cosmeticId}_front.png");
                if (File.Exists(path)) return File.ReadAllBytes(path);
            }
            catch { }
            return null;
        }

        private static byte[]? RenderFreshCardPreview(string cosmeticId, string category, byte[]? userSkinBytes,
            BBModel.BBModelInstance? bbInstance, string bbAttachment,
            float bbOx, float bbOy, float bbOz, float bbRy, float bbSc)
        {
            SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>? baseSkin = null;
            SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>? cape = null;
            SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>? hat = null;
            SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>? wings = null;
            bool isHorns = false;
            string? auraType = null;
            try
            {
                baseSkin = (userSkinBytes != null && userSkinBytes.Length > 0)
                    ? SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(userSkinBytes)
                    : LeafClient.Services.CosmeticPreviewBaker.CreateMannequinSkin();

                string cat = (category ?? "").ToLowerInvariant();
                bool isBB = bbInstance != null;
                if (!isBB)
                {
                    if (cat == "wings")
                    {
                        var b = TryLoadCosmeticAsset(cosmeticId);
                        if (b == null) return null;
                        wings = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(b);
                    }
                    else if (cat == "hat" || cat == "hats")
                    {
                        var b = TryLoadCosmeticAsset(cosmeticId);
                        if (b == null) return null;
                        hat = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(b);
                        isHorns = cosmeticId.Contains("horns");
                    }
                    else if (cat == "cape" || cat == "capes")
                    {
                        var b = TryLoadCosmeticAsset(cosmeticId);
                        if (b == null) return null;
                        cape = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(b);
                    }
                    else if (cat == "aura" || cat == "auras")
                    {
                        auraType = AuraIdToType(cosmeticId);
                        if (string.IsNullOrEmpty(auraType)) return null;
                    }
                    else
                    {
                        return null;
                    }
                }

                List<LeafClient.Controls.SkinRendererControl.BBSlot>? slots = null;
                if (isBB && bbInstance != null)
                {
                    var slot = new LeafClient.Controls.SkinRendererControl.BBSlot(bbInstance.Model, bbInstance.Textures, bbAttachment);
                    slot.OffsetX = bbOx; slot.OffsetY = bbOy; slot.OffsetZ = bbOz; slot.RotationY = bbRy;
                    slot.Scale = bbSc <= 0f ? 1f : bbSc;
                    slots = new List<LeafClient.Controls.SkinRendererControl.BBSlot> { slot };
                }

                const int sz = 256;
                float yaw = (cat == "hat" || cat == "hats" || cat == "face" || cat == "aura" || cat == "auras") ? 180f : 25f;
                byte[]? bgra = LeafClient.Controls.SkinRendererControl.RenderFrame(
                    baseSkin!, cape, hat, wings,
                    sz, sz,
                    rotationY: yaw, rotationX: -10f,
                    zoom: 1.05f,
                    capeSwingX: 0f,
                    wingFlapPhase: 0.75f,
                    isHorns: isHorns,
                    auraType: auraType,
                    auraPhase: 0.5f,
                    previewLit: false,
                    bbmodel: null,
                    bbmodelTextures: null,
                    bbmodelAttachment: null,
                    bbmodelTime: 0f,
                    bbmodelSlots: slots);
                if (bgra == null) return null;

                using var pngImg = SixLabors.ImageSharp.Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Bgra32>(bgra, sz, sz);
                using var ms = new MemoryStream();
                SixLabors.ImageSharp.ImageExtensions.SaveAsPng(pngImg, ms);
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                LeafLog.Info("CardPreview", $"{cosmeticId}: {ex.Message}");
                return null;
            }
            finally
            {
                baseSkin?.Dispose();
                cape?.Dispose();
                hat?.Dispose();
                wings?.Dispose();
            }
        }

        private static async Task<(BBModel.BBModelInstance? inst, string attach, float ox, float oy, float oz, float ry, float sc)> ResolveBBAsync(string? cosId, string? variantSlug = null, double? userScale = null, double? userOffsetX = null, double? userOffsetY = null, double? userOffsetZ = null)
        {
            if (string.IsNullOrEmpty(cosId) || !BBModel.BBModelCatalog.IsBBModel(cosId)) return (null, "", 0f, 0f, 0f, 0f, 1f);
            var meta = BBModel.BBModelCatalog.Get(cosId);
            if (meta == null) return (null, "", 0f, 0f, 0f, 0f, 1f);

            string? bbUrl = meta.BBModelUrl;
            if (!string.IsNullOrEmpty(variantSlug) && meta.SupportsVariants && meta.Variants != null)
            {
                var v = meta.Variants.FirstOrDefault(x => string.Equals(x.Slug, variantSlug, StringComparison.Ordinal));
                if (v != null && !string.IsNullOrEmpty(v.Url)) bbUrl = v.Url;
            }
            if (string.IsNullOrEmpty(bbUrl)) return (null, "", 0f, 0f, 0f, 0f, 1f);

            var inst = await BBModel.BBModelDownloader.FetchAsync(bbUrl);
            float baseSc = meta.Scale <= 0f ? 1f : meta.Scale;
            float sc = userScale.HasValue && userScale.Value > 0 ? baseSc * (float)userScale.Value : baseSc;
            float ox = meta.OffsetX + (userOffsetX.HasValue ? (float)userOffsetX.Value : 0f);
            float oy = meta.OffsetY + (userOffsetY.HasValue ? (float)userOffsetY.Value : 0f);
            float oz = meta.OffsetZ + (userOffsetZ.HasValue ? (float)userOffsetZ.Value : 0f);
            return (inst, string.IsNullOrEmpty(meta.Attachment) ? "head_top" : meta.Attachment, ox, oy, oz, meta.RotationY, sc);
        }

        private const string MojangCapePrefix = "mojang:";

        private static async Task<byte[]?> TryLoadMojangCapeBytesAsync(string capeId, string mcAccessToken)
        {
            try
            {
                if (!capeId.StartsWith(MojangCapePrefix, StringComparison.Ordinal)) return null;
                if (string.IsNullOrWhiteSpace(mcAccessToken)) return null;
                string rawId = capeId.Substring(MojangCapePrefix.Length);
                var profile = await LeafClient.Services.MojangCapeService.GetProfileAsync(mcAccessToken);
                if (profile == null) return null;
                foreach (var c in profile.Capes)
                {
                    if (string.Equals(c.Id, rawId, StringComparison.OrdinalIgnoreCase)
                        && !string.IsNullOrWhiteSpace(c.Url))
                    {
                        return await LeafClient.Services.MojangCapeService.GetCapeTextureBytesAsync(c.Url);
                    }
                }
            }
            catch (Exception ex) { LeafLog.Info("CosmeticHelpers", $"Mojang cape texture fetch failed: {ex.Message}"); }
            return null;
        }

        public static async Task<byte[]?> RenderMojangCapeCardPreviewAsync(string mojangCapeId, byte[]? userSkinBytes, string? mcAccessToken)
        {
            string cacheKey = mojangCapeId + "_card";
            lock (CardPreviewLock)
            {
                if (CardPreviewCache.TryGetValue(cacheKey, out var cached))
                    return cached;
            }

            byte[]? capeBytes = await TryLoadMojangCapeBytesAsync(mojangCapeId, mcAccessToken ?? "");
            if (capeBytes == null) return null;

            byte[]? result = await Task.Run(() => RenderMojangCapeCardPreview(capeBytes, userSkinBytes));
            lock (CardPreviewLock) { CardPreviewCache[cacheKey] = result; }
            return result;
        }

        private static byte[]? RenderMojangCapeCardPreview(byte[] capeBytes, byte[]? userSkinBytes)
        {
            SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>? baseSkin = null;
            SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>? cape = null;
            try
            {
                baseSkin = (userSkinBytes != null && userSkinBytes.Length > 0)
                    ? SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(userSkinBytes)
                    : CosmeticPreviewBaker.CreateMannequinSkin();

                cape = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(capeBytes);

                const int sz = 256;
                byte[]? bgra = LeafClient.Controls.SkinRendererControl.RenderFrame(
                    baseSkin, cape, null, null,
                    sz, sz,
                    rotationY: 25f, rotationX: -10f,
                    zoom: 1.05f,
                    capeSwingX: 0f,
                    wingFlapPhase: 0.75f,
                    isHorns: false,
                    auraType: null,
                    auraPhase: 0.5f,
                    previewLit: false,
                    bbmodel: null,
                    bbmodelTextures: null,
                    bbmodelAttachment: null,
                    bbmodelTime: 0f,
                    bbmodelSlots: null,
                    isMojangCape: true);
                if (bgra == null) return null;

                using var pngImg = SixLabors.ImageSharp.Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Bgra32>(bgra, sz, sz);
                using var ms = new MemoryStream();
                SixLabors.ImageSharp.ImageExtensions.SaveAsPng(pngImg, ms);
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                LeafLog.Info("CardPreview", $"Mojang cape render failed: {ex.Message}");
                return null;
            }
            finally
            {
                baseSkin?.Dispose();
                cape?.Dispose();
            }
        }

        private static EquippedCosmetics WithCleared(EquippedCosmetics eq, string slot)
        {
            switch (slot)
            {
                case "hat":   eq.HatId = null;   eq.HatVariant = null;   eq.HatScale = null;   eq.HatOffsetX = null;   eq.HatOffsetY = null;   eq.HatOffsetZ = null;   break;
                case "cape":  eq.CapeId = null;  eq.CapeVariant = null;  eq.CapeScale = null;  eq.CapeOffsetX = null;  eq.CapeOffsetY = null;  eq.CapeOffsetZ = null;  break;
                case "wings": eq.WingsId = null; eq.WingsVariant = null; eq.WingsScale = null; eq.WingsOffsetX = null; eq.WingsOffsetY = null; eq.WingsOffsetZ = null; break;
                case "face":  eq.FaceId = null;  eq.FaceVariant = null;  eq.FaceScale = null;  eq.FaceOffsetX = null;  eq.FaceOffsetY = null;  eq.FaceOffsetZ = null;  break;
            }
            return eq;
        }

        public static async Task ApplyEquippedToRendererAsync(
            LeafClient.Controls.SkinRendererControl renderer, EquippedCosmetics equipped, string? mcAccessToken = null)
        {
            await BBModel.BBModelCatalog.EnsureLoadedAsync();

            string? SlotForId(string? id) {
                if (string.IsNullOrEmpty(id)) return null;
                if (id.StartsWith(MojangCapePrefix, StringComparison.Ordinal)) return "cape";
                var m = BBModel.BBModelCatalog.Get(id);
                if (m == null) return null;
                return m.Category;
            }
            if (SlotForId(equipped.HatId)  is string c1 && c1 != "hat")   equipped = WithCleared(equipped, "hat");
            if (SlotForId(equipped.CapeId) is string c2 && c2 != "cape")  equipped = WithCleared(equipped, "cape");
            if (SlotForId(equipped.WingsId)is string c3 && c3 != "wings") equipped = WithCleared(equipped, "wings");
            if (SlotForId(equipped.FaceId) is string c4 && c4 != "face")  equipped = WithCleared(equipped, "face");

            bool capeIsMojang = !string.IsNullOrEmpty(equipped.CapeId)
                                && equipped.CapeId!.StartsWith(MojangCapePrefix, StringComparison.Ordinal);

            bool hatIsBB   = !string.IsNullOrEmpty(equipped.HatId)   && BBModel.BBModelCatalog.IsBBModel(equipped.HatId);
            bool wingsIsBB = !string.IsNullOrEmpty(equipped.WingsId) && BBModel.BBModelCatalog.IsBBModel(equipped.WingsId);
            bool capeIsBB  = !capeIsMojang && !string.IsNullOrEmpty(equipped.CapeId) && BBModel.BBModelCatalog.IsBBModel(equipped.CapeId);
            bool faceIsBB  = !string.IsNullOrEmpty(equipped.FaceId)  && BBModel.BBModelCatalog.IsBBModel(equipped.FaceId);

            byte[]? capeBytes;
            if (capeIsMojang)
                capeBytes = await TryLoadMojangCapeBytesAsync(equipped.CapeId!, mcAccessToken ?? "");
            else
                capeBytes = (string.IsNullOrEmpty(equipped.CapeId) || capeIsBB) ? null : await TryLoadCosmeticAssetAsync(equipped.CapeId);
            var hatBytes   = (string.IsNullOrEmpty(equipped.HatId)  || hatIsBB)   ? null : await TryLoadCosmeticAssetAsync(equipped.HatId);
            var wingsBytes = (string.IsNullOrEmpty(equipped.WingsId)|| wingsIsBB) ? null : await TryLoadCosmeticAssetAsync(equipped.WingsId);

            var hatBB   = hatIsBB   ? await ResolveBBAsync(equipped.HatId, equipped.HatVariant,   equipped.HatScale,   equipped.HatOffsetX,   equipped.HatOffsetY,   equipped.HatOffsetZ)   : (null, "", 0f, 0f, 0f, 0f, 1f);
            var wingsBB = wingsIsBB ? await ResolveBBAsync(equipped.WingsId, equipped.WingsVariant, equipped.WingsScale, equipped.WingsOffsetX, equipped.WingsOffsetY, equipped.WingsOffsetZ) : (null, "", 0f, 0f, 0f, 0f, 1f);
            var capeBB  = capeIsBB  ? await ResolveBBAsync(equipped.CapeId, equipped.CapeVariant,   equipped.CapeScale,   equipped.CapeOffsetX,   equipped.CapeOffsetY,   equipped.CapeOffsetZ)   : (null, "", 0f, 0f, 0f, 0f, 1f);
            var faceBB  = faceIsBB  ? await ResolveBBAsync(equipped.FaceId, equipped.FaceVariant,   equipped.FaceScale,   equipped.FaceOffsetX,   equipped.FaceOffsetY,   equipped.FaceOffsetZ)   : (null, "", 0f, 0f, 0f, 0f, 1f);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (capeIsBB)
                {
                    renderer.ClearCape();
                    if (capeBB.inst != null) renderer.SetBBModelSlot("cape", capeBB.inst.Model, capeBB.inst.Textures, capeBB.attach, capeBB.ox, capeBB.oy, capeBB.oz, capeBB.ry, capeBB.sc);
                    else renderer.ClearBBModelSlot("cape");
                }
                else
                {
                    renderer.ClearBBModelSlot("cape");
                    if (!string.IsNullOrEmpty(equipped.CapeId))
                    { if (capeBytes != null) renderer.UpdateCapeTexture(capeBytes, capeIsMojang); else renderer.ClearCape(); }
                    else renderer.ClearCape();
                }

                if (hatIsBB)
                {
                    renderer.ClearHat();
                    if (hatBB.inst != null) renderer.SetBBModelSlot("hat", hatBB.inst.Model, hatBB.inst.Textures, hatBB.attach, hatBB.ox, hatBB.oy, hatBB.oz, hatBB.ry, hatBB.sc);
                    else renderer.ClearBBModelSlot("hat");
                }
                else
                {
                    renderer.ClearBBModelSlot("hat");
                    if (!string.IsNullOrEmpty(equipped.HatId))
                    { if (hatBytes != null) renderer.UpdateHatTexture(hatBytes, equipped.HatId.Contains("horns")); else renderer.ClearHat(); }
                    else renderer.ClearHat();
                }

                if (wingsIsBB)
                {
                    renderer.ClearWings();
                    if (wingsBB.inst != null) renderer.SetBBModelSlot("wings", wingsBB.inst.Model, wingsBB.inst.Textures, wingsBB.attach, wingsBB.ox, wingsBB.oy, wingsBB.oz, wingsBB.ry, wingsBB.sc);
                    else renderer.ClearBBModelSlot("wings");
                }
                else
                {
                    renderer.ClearBBModelSlot("wings");
                    if (!string.IsNullOrEmpty(equipped.WingsId))
                    { if (wingsBytes != null) renderer.UpdateWingsTexture(wingsBytes); else renderer.ClearWings(); }
                    else renderer.ClearWings();
                }

                if (!string.IsNullOrEmpty(equipped.AuraId))
                    renderer.SetAura(AuraIdToType(equipped.AuraId));
                else
                    renderer.ClearAura();

                if (faceIsBB && faceBB.inst != null)
                    renderer.SetBBModelSlot("face", faceBB.inst.Model, faceBB.inst.Textures, faceBB.attach, faceBB.ox, faceBB.oy, faceBB.oz, faceBB.ry, faceBB.sc);
                else
                    renderer.ClearBBModelSlot("face");
            });
        }

        public static async Task ApplyCosmeticPreviewToRendererAsync(
            LeafClient.Controls.SkinRendererControl renderer, string cosId, string category,
            LauncherSettings? settings)
        {
            await BBModel.BBModelCatalog.EnsureLoadedAsync();

            string slotKey = category switch
            {
                "wings" => "wings",
                "capes" or "cape" => "cape",
                "hats" or "hat" => "hat",
                "face" or "faces" => "face",
                _ => ""
            };

            bool previewIsBB = !string.IsNullOrEmpty(slotKey) && BBModel.BBModelCatalog.IsBBModel(cosId);
            var previewBB = previewIsBB ? await ResolveBBAsync(cosId) : ((BBModel.BBModelInstance?)null, "", 0f, 0f, 0f, 0f, 1f);
            var mainBytes = (previewIsBB || string.IsNullOrEmpty(slotKey)) ? null : await TryLoadCosmeticAssetAsync(cosId);

            string? equippedHatId   = settings?.Equipped?.HatId;
            string? equippedCapeId  = settings?.Equipped?.CapeId;
            string? equippedWingsId = settings?.Equipped?.WingsId;
            string? equippedFaceId  = settings?.Equipped?.FaceId;
            bool keepEquippedHat   = !string.IsNullOrEmpty(equippedHatId)   && (category != "hats" && category != "hat");
            bool keepEquippedCape  = !string.IsNullOrEmpty(equippedCapeId)  && (category != "capes" && category != "cape");
            bool keepEquippedWings = !string.IsNullOrEmpty(equippedWingsId) && (category != "wings");
            bool keepEquippedFace  = !string.IsNullOrEmpty(equippedFaceId)  && (category != "face" && category != "faces");

            (BBModel.BBModelInstance? inst, string attach, float ox, float oy, float oz, float ry, float sc) hatBB = (null, "", 0f, 0f, 0f, 0f, 1f);
            (BBModel.BBModelInstance? inst, string attach, float ox, float oy, float oz, float ry, float sc) capeBB = (null, "", 0f, 0f, 0f, 0f, 1f);
            (BBModel.BBModelInstance? inst, string attach, float ox, float oy, float oz, float ry, float sc) wingsBB = (null, "", 0f, 0f, 0f, 0f, 1f);
            (BBModel.BBModelInstance? inst, string attach, float ox, float oy, float oz, float ry, float sc) faceBB = (null, "", 0f, 0f, 0f, 0f, 1f);
            byte[]? hatBytes = null, capeBytes = null, wingsBytes = null;

            if (keepEquippedHat)
            {
                if (BBModel.BBModelCatalog.IsBBModel(equippedHatId!)) hatBB = await ResolveBBAsync(equippedHatId, settings?.Equipped?.HatVariant, settings?.Equipped?.HatScale, settings?.Equipped?.HatOffsetX, settings?.Equipped?.HatOffsetY, settings?.Equipped?.HatOffsetZ);
                else hatBytes = await TryLoadCosmeticAssetAsync(equippedHatId!);
            }
            if (keepEquippedCape)
            {
                if (BBModel.BBModelCatalog.IsBBModel(equippedCapeId!)) capeBB = await ResolveBBAsync(equippedCapeId, settings?.Equipped?.CapeVariant, settings?.Equipped?.CapeScale, settings?.Equipped?.CapeOffsetX, settings?.Equipped?.CapeOffsetY, settings?.Equipped?.CapeOffsetZ);
                else capeBytes = await TryLoadCosmeticAssetAsync(equippedCapeId!);
            }
            if (keepEquippedWings)
            {
                if (BBModel.BBModelCatalog.IsBBModel(equippedWingsId!)) wingsBB = await ResolveBBAsync(equippedWingsId, settings?.Equipped?.WingsVariant, settings?.Equipped?.WingsScale, settings?.Equipped?.WingsOffsetX, settings?.Equipped?.WingsOffsetY, settings?.Equipped?.WingsOffsetZ);
                else wingsBytes = await TryLoadCosmeticAssetAsync(equippedWingsId!);
            }
            if (keepEquippedFace)
            {
                if (BBModel.BBModelCatalog.IsBBModel(equippedFaceId!)) faceBB = await ResolveBBAsync(equippedFaceId, settings?.Equipped?.FaceVariant, settings?.Equipped?.FaceScale, settings?.Equipped?.FaceOffsetX, settings?.Equipped?.FaceOffsetY, settings?.Equipped?.FaceOffsetZ);
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                void ApplySlot(string key, string cat, bool isBB, BBModel.BBModelInstance? inst, string attach,
                               byte[]? bytes, string? id, bool isHorns = false,
                               float ox = 0f, float oy = 0f, float oz = 0f, float ry = 0f, float sc = 1f)
                {
                    if (isBB)
                    {
                        if (cat == "hat") renderer.ClearHat();
                        else if (cat == "wings") renderer.ClearWings();
                        else if (cat == "cape") renderer.ClearCape();
                        if (inst != null) renderer.SetBBModelSlot(key, inst.Model, inst.Textures, attach, ox, oy, oz, ry, sc);
                        else renderer.ClearBBModelSlot(key);
                    }
                    else
                    {
                        renderer.ClearBBModelSlot(key);
                        if (bytes != null)
                        {
                            if (cat == "hat") renderer.UpdateHatTexture(bytes, isHorns);
                            else if (cat == "wings") renderer.UpdateWingsTexture(bytes);
                            else if (cat == "cape") renderer.UpdateCapeTexture(bytes);
                        }
                        else
                        {
                            if (cat == "hat") renderer.ClearHat();
                            else if (cat == "wings") renderer.ClearWings();
                            else if (cat == "cape") renderer.ClearCape();
                        }
                    }
                }

                void ApplyKeepFace()
                {
                    if (keepEquippedFace && faceBB.inst != null)
                        renderer.SetBBModelSlot("face", faceBB.inst.Model, faceBB.inst.Textures, faceBB.attach, faceBB.ox, faceBB.oy, faceBB.oz, faceBB.ry, faceBB.sc);
                    else
                        renderer.ClearBBModelSlot("face");
                }

                switch (category)
                {
                    case "capes":
                    case "cape":
                        ApplySlot("cape", "cape", previewIsBB, previewBB.Item1, previewBB.Item2, mainBytes, cosId, false, previewBB.Item3, previewBB.Item4, previewBB.Item5, previewBB.Item6, previewBB.Item7);
                        ApplySlot("wings", "wings", false, null, "", null, null);
                        if (keepEquippedHat) ApplySlot("hat", "hat", hatBB.inst != null, hatBB.inst, hatBB.attach, hatBytes, equippedHatId, equippedHatId!.Contains("horns"), hatBB.ox, hatBB.oy, hatBB.oz, hatBB.ry, hatBB.sc);
                        else ApplySlot("hat", "hat", false, null, "", null, null);
                        ApplyKeepFace();
                        break;
                    case "wings":
                        ApplySlot("wings", "wings", previewIsBB, previewBB.Item1, previewBB.Item2, mainBytes, cosId, false, previewBB.Item3, previewBB.Item4, previewBB.Item5, previewBB.Item6, previewBB.Item7);
                        ApplySlot("cape", "cape", false, null, "", null, null);
                        if (keepEquippedHat) ApplySlot("hat", "hat", hatBB.inst != null, hatBB.inst, hatBB.attach, hatBytes, equippedHatId, equippedHatId!.Contains("horns"), hatBB.ox, hatBB.oy, hatBB.oz, hatBB.ry, hatBB.sc);
                        else ApplySlot("hat", "hat", false, null, "", null, null);
                        ApplyKeepFace();
                        break;
                    case "hat":
                    case "hats":
                        ApplySlot("hat", "hat", previewIsBB, previewBB.Item1, previewBB.Item2, mainBytes, cosId, cosId.Contains("horns"), previewBB.Item3, previewBB.Item4, previewBB.Item5, previewBB.Item6, previewBB.Item7);
                        if (keepEquippedCape) ApplySlot("cape", "cape", capeBB.inst != null, capeBB.inst, capeBB.attach, capeBytes, equippedCapeId, false, capeBB.ox, capeBB.oy, capeBB.oz, capeBB.ry, capeBB.sc);
                        else ApplySlot("cape", "cape", false, null, "", null, null);
                        if (keepEquippedWings) ApplySlot("wings", "wings", wingsBB.inst != null, wingsBB.inst, wingsBB.attach, wingsBytes, equippedWingsId, false, wingsBB.ox, wingsBB.oy, wingsBB.oz, wingsBB.ry, wingsBB.sc);
                        else ApplySlot("wings", "wings", false, null, "", null, null);
                        ApplyKeepFace();
                        break;
                    case "face":
                    case "faces":
                        if (previewBB.Item1 != null)
                            renderer.SetBBModelSlot("face", previewBB.Item1.Model, previewBB.Item1.Textures, previewBB.Item2, previewBB.Item3, previewBB.Item4, previewBB.Item5, previewBB.Item6, previewBB.Item7);
                        else renderer.ClearBBModelSlot("face");
                        if (keepEquippedHat) ApplySlot("hat", "hat", hatBB.inst != null, hatBB.inst, hatBB.attach, hatBytes, equippedHatId, equippedHatId!.Contains("horns"), hatBB.ox, hatBB.oy, hatBB.oz, hatBB.ry, hatBB.sc);
                        else ApplySlot("hat", "hat", false, null, "", null, null);
                        if (keepEquippedCape) ApplySlot("cape", "cape", capeBB.inst != null, capeBB.inst, capeBB.attach, capeBytes, equippedCapeId, false, capeBB.ox, capeBB.oy, capeBB.oz, capeBB.ry, capeBB.sc);
                        else ApplySlot("cape", "cape", false, null, "", null, null);
                        if (keepEquippedWings) ApplySlot("wings", "wings", wingsBB.inst != null, wingsBB.inst, wingsBB.attach, wingsBytes, equippedWingsId, false, wingsBB.ox, wingsBB.oy, wingsBB.oz, wingsBB.ry, wingsBB.sc);
                        else ApplySlot("wings", "wings", false, null, "", null, null);
                        break;
                    case "auras":
                        renderer.SetAura(AuraIdToType(cosId));
                        break;
                }
            });
        }

        public static void ApplyEquippedToRenderer(
            LeafClient.Controls.SkinRendererControl renderer, EquippedCosmetics equipped)
        {
            _ = ApplyEquippedToRendererAsync(renderer, equipped);
        }

        public static void ApplyCosmeticPreviewToRenderer(
            LeafClient.Controls.SkinRendererControl renderer, string cosId, string category,
            LauncherSettings? settings)
        {
            _ = ApplyCosmeticPreviewToRendererAsync(renderer, cosId, category, settings);
        }
    }
}
