using Avalonia.Platform;
using Avalonia.Threading;
using LeafClient.Models;
using System;
using System.Collections.Generic;
using System.IO;
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
            ["wings-angel"]       = "avares://LeafClient/Assets/AngelWings.png",
            ["hat-horns"]         = "avares://LeafClient/Assets/ShadowHorns.png",
            ["hat-shadow-horns"]  = "avares://LeafClient/Assets/ShadowHorns.png",
        };

        private static readonly Dictionary<string, byte[]?> AssetCache = new();

        public static byte[]? TryLoadCosmeticAsset(string cosmeticId)
        {
            if (AssetCache.TryGetValue(cosmeticId, out var cached)) return cached;
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
                Console.WriteLine($"[Cosmetics] Embedded resource not found for '{cosmeticId}': {ex.Message}");
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
                Console.WriteLine($"[Cosmetics] File system fallback failed for '{cosmeticId}': {ex.Message}");
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

        public static async Task ApplyEquippedToRendererAsync(
            LeafClient.Controls.SkinRendererControl renderer, EquippedCosmetics equipped)
        {
            var capeBytes  = string.IsNullOrEmpty(equipped.CapeId)  ? null : await TryLoadCosmeticAssetAsync(equipped.CapeId);
            var hatBytes   = string.IsNullOrEmpty(equipped.HatId)   ? null : await TryLoadCosmeticAssetAsync(equipped.HatId);
            var wingsBytes = string.IsNullOrEmpty(equipped.WingsId) ? null : await TryLoadCosmeticAssetAsync(equipped.WingsId);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!string.IsNullOrEmpty(equipped.CapeId))
                { if (capeBytes != null) renderer.UpdateCapeTexture(capeBytes); else renderer.ClearCape(); }
                else renderer.ClearCape();

                if (!string.IsNullOrEmpty(equipped.HatId))
                { if (hatBytes != null) renderer.UpdateHatTexture(hatBytes, equipped.HatId.Contains("horns")); else renderer.ClearHat(); }
                else renderer.ClearHat();

                if (!string.IsNullOrEmpty(equipped.WingsId))
                { if (wingsBytes != null) renderer.UpdateWingsTexture(wingsBytes, equipped.WingsId.Contains("angel")); else renderer.ClearWings(); }
                else renderer.ClearWings();

                if (!string.IsNullOrEmpty(equipped.AuraId))
                    renderer.SetAura(AuraIdToType(equipped.AuraId));
                else
                    renderer.ClearAura();
            });
        }

        public static async Task ApplyCosmeticPreviewToRendererAsync(
            LeafClient.Controls.SkinRendererControl renderer, string cosId, string category,
            LauncherSettings? settings)
        {
            var mainBytes = await TryLoadCosmeticAssetAsync(cosId);

            byte[]? hatBytes   = null;
            byte[]? capeBytes  = null;
            byte[]? wingsBytes = null;

            switch (category)
            {
                case "capes":
                case "wings":
                    if (!string.IsNullOrEmpty(settings?.Equipped?.HatId))
                        hatBytes = await TryLoadCosmeticAssetAsync(settings.Equipped.HatId);
                    break;
                case "hats":
                    if (!string.IsNullOrEmpty(settings?.Equipped?.CapeId))
                        capeBytes = await TryLoadCosmeticAssetAsync(settings.Equipped.CapeId);
                    else if (!string.IsNullOrEmpty(settings?.Equipped?.WingsId))
                        wingsBytes = await TryLoadCosmeticAssetAsync(settings.Equipped.WingsId);
                    break;
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                switch (category)
                {
                    case "capes":
                        renderer.ClearWings();
                        if (mainBytes != null) renderer.UpdateCapeTexture(mainBytes); else renderer.ClearCape();
                        if (!string.IsNullOrEmpty(settings?.Equipped?.HatId))
                        { if (hatBytes != null) renderer.UpdateHatTexture(hatBytes, settings.Equipped.HatId.Contains("horns")); else renderer.ClearHat(); }
                        break;
                    case "wings":
                        renderer.ClearCape();
                        if (mainBytes != null) renderer.UpdateWingsTexture(mainBytes, cosId.Contains("angel")); else renderer.ClearWings();
                        if (!string.IsNullOrEmpty(settings?.Equipped?.HatId))
                        { if (hatBytes != null) renderer.UpdateHatTexture(hatBytes, settings.Equipped.HatId.Contains("horns")); else renderer.ClearHat(); }
                        break;
                    case "hats":
                        if (mainBytes != null) renderer.UpdateHatTexture(mainBytes, cosId.Contains("horns")); else renderer.ClearHat();
                        if (!string.IsNullOrEmpty(settings?.Equipped?.CapeId))
                        { if (capeBytes != null) renderer.UpdateCapeTexture(capeBytes); else renderer.ClearCape(); }
                        else if (!string.IsNullOrEmpty(settings?.Equipped?.WingsId))
                        { if (wingsBytes != null) renderer.UpdateWingsTexture(wingsBytes, settings.Equipped.WingsId.Contains("angel")); else renderer.ClearWings(); }
                        break;
                    case "auras":
                        renderer.SetAura(AuraIdToType(cosId));
                        break;
                }
            });
        }

        /// <summary>
        /// Applies the full set of equipped cosmetics to a 3D renderer.
        /// </summary>
        public static void ApplyEquippedToRenderer(
            LeafClient.Controls.SkinRendererControl renderer, EquippedCosmetics equipped)
        {
            // Cape
            if (!string.IsNullOrEmpty(equipped.CapeId))
            {
                var b = TryLoadCosmeticAsset(equipped.CapeId);
                if (b != null) renderer.UpdateCapeTexture(b); else renderer.ClearCape();
            }
            else renderer.ClearCape();

            // Hat
            if (!string.IsNullOrEmpty(equipped.HatId))
            {
                var b = TryLoadCosmeticAsset(equipped.HatId);
                if (b != null) renderer.UpdateHatTexture(b, equipped.HatId.Contains("horns")); else renderer.ClearHat();
            }
            else renderer.ClearHat();

            // Wings
            if (!string.IsNullOrEmpty(equipped.WingsId))
            {
                var b = TryLoadCosmeticAsset(equipped.WingsId);
                if (b != null) renderer.UpdateWingsTexture(b, equipped.WingsId.Contains("angel")); else renderer.ClearWings();
            }
            else renderer.ClearWings();

            // Aura
            if (!string.IsNullOrEmpty(equipped.AuraId))
                renderer.SetAura(AuraIdToType(equipped.AuraId));
            else
                renderer.ClearAura();
        }

        /// <summary>
        /// Applies a single cosmetic preview to the renderer while preserving other equipped items.
        /// </summary>
        public static void ApplyCosmeticPreviewToRenderer(
            LeafClient.Controls.SkinRendererControl renderer, string cosId, string category,
            LauncherSettings? settings)
        {
            switch (category)
            {
                case "capes":
                    renderer.ClearWings();
                    var capeBytes = TryLoadCosmeticAsset(cosId);
                    if (capeBytes != null) renderer.UpdateCapeTexture(capeBytes);
                    else renderer.ClearCape();
                    if (!string.IsNullOrEmpty(settings?.Equipped?.HatId))
                    {
                        var hb = TryLoadCosmeticAsset(settings.Equipped.HatId);
                        if (hb != null) renderer.UpdateHatTexture(hb, settings.Equipped.HatId.Contains("horns")); else renderer.ClearHat();
                    }
                    break;
                case "wings":
                    renderer.ClearCape();
                    var wingsBytes = TryLoadCosmeticAsset(cosId);
                    if (wingsBytes != null) renderer.UpdateWingsTexture(wingsBytes, cosId.Contains("angel"));
                    else renderer.ClearWings();
                    if (!string.IsNullOrEmpty(settings?.Equipped?.HatId))
                    {
                        var hb = TryLoadCosmeticAsset(settings.Equipped.HatId);
                        if (hb != null) renderer.UpdateHatTexture(hb, settings.Equipped.HatId.Contains("horns")); else renderer.ClearHat();
                    }
                    break;
                case "hats":
                    var hatBytes = TryLoadCosmeticAsset(cosId);
                    if (hatBytes != null) renderer.UpdateHatTexture(hatBytes, cosId.Contains("horns"));
                    else renderer.ClearHat();
                    if (!string.IsNullOrEmpty(settings?.Equipped?.CapeId))
                    {
                        var cb = TryLoadCosmeticAsset(settings.Equipped.CapeId);
                        if (cb != null) renderer.UpdateCapeTexture(cb); else renderer.ClearCape();
                    }
                    else if (!string.IsNullOrEmpty(settings?.Equipped?.WingsId))
                    {
                        var wb = TryLoadCosmeticAsset(settings.Equipped.WingsId);
                        if (wb != null) renderer.UpdateWingsTexture(wb, settings.Equipped.WingsId.Contains("angel")); else renderer.ClearWings();
                    }
                    break;
                case "auras":
                    renderer.SetAura(AuraIdToType(cosId));
                    break;
            }
        }
    }
}
