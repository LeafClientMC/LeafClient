using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace LeafClient.Services
{
    public enum DropTargetKind
    {
        Unknown,
        Mod,
        ResourcePack,
        Shader,
        Modpack,
        ModpackUnsupported
    }

    public sealed record DropImportItem(string SourcePath, DropTargetKind Kind, string DisplayName);

    public sealed record DropImportResult(
        int ModsInstalled,
        int ResourcePacksInstalled,
        int ShadersInstalled,
        int ModpacksInstalled,
        int ManagedModsOverwritten,
        int Skipped,
        IReadOnlyList<string> Errors,
        IReadOnlyList<DropTargetKind> InstalledKinds,
        IReadOnlyList<string> ManagedModsOverwrittenNames);

    public static class DragDropImportService
    {
        public static IReadOnlyList<string> ResolveLocalPaths(IEnumerable<IStorageItem> items)
        {
            var paths = new List<string>();
            if (items == null) return paths;
            foreach (var item in items)
            {
                try
                {
                    if (item?.Path == null) continue;
                    if (!item.Path.IsFile) continue;
                    var local = item.Path.LocalPath;
                    if (!string.IsNullOrEmpty(local)) paths.Add(local);
                }
                catch { }
            }
            return paths;
        }

        public static List<DropImportItem> Inspect(IEnumerable<string> paths)
        {
            var results = new List<DropImportItem>();
            foreach (var path in paths)
            {
                try
                {
                    if (Directory.Exists(path))
                    {
                        foreach (var inner in Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly))
                        {
                            var k = Classify(inner);
                            results.Add(new DropImportItem(inner, k, Path.GetFileName(inner)));
                        }
                        continue;
                    }
                    if (!File.Exists(path)) continue;
                    var kind = Classify(path);
                    results.Add(new DropImportItem(path, kind, Path.GetFileName(path)));
                }
                catch { }
            }
            return results;
        }

        public static DropTargetKind Classify(string path)
        {
            try
            {
                var ext = Path.GetExtension(path)?.ToLowerInvariant();
                if (ext == ".jar") return DropTargetKind.Mod;
                if (ext == ".mrpack") return DropTargetKind.Modpack;
                if (ext != ".zip") return DropTargetKind.Unknown;

                using var archive = ZipFile.OpenRead(path);

                bool hasMcMeta = false;
                bool hasShadersDir = false;
                bool hasModrinthIndex = false;
                bool hasCurseManifest = false;
                bool hasInstanceCfg = false;
                int rootJarCount = 0;
                int totalRootEntries = 0;

                foreach (var e in archive.Entries)
                {
                    var name = e.FullName.Replace('\\', '/');
                    if (string.Equals(name, "pack.mcmeta", StringComparison.OrdinalIgnoreCase)) hasMcMeta = true;
                    if (name.StartsWith("shaders/", StringComparison.OrdinalIgnoreCase)) hasShadersDir = true;
                    if (string.Equals(name, "modrinth.index.json", StringComparison.OrdinalIgnoreCase)) hasModrinthIndex = true;
                    if (string.Equals(name, "manifest.json", StringComparison.OrdinalIgnoreCase)) hasCurseManifest = true;
                    if (string.Equals(name, "instance.cfg", StringComparison.OrdinalIgnoreCase)) hasInstanceCfg = true;
                    if (!name.Contains('/'))
                    {
                        totalRootEntries++;
                        if (name.EndsWith(".jar", StringComparison.OrdinalIgnoreCase)) rootJarCount++;
                    }
                }

                if (hasModrinthIndex) return DropTargetKind.Modpack;
                if (hasCurseManifest)
                {
                    if (CurseForgeManifestIsModpack(archive)) return DropTargetKind.ModpackUnsupported;
                }
                if (hasInstanceCfg) return DropTargetKind.ModpackUnsupported;
                if (hasShadersDir) return DropTargetKind.Shader;
                if (hasMcMeta) return DropTargetKind.ResourcePack;
                if (rootJarCount > 0 && rootJarCount == totalRootEntries) return DropTargetKind.Mod;

                return DropTargetKind.Unknown;
            }
            catch
            {
                return DropTargetKind.Unknown;
            }
        }

        private static bool CurseForgeManifestIsModpack(ZipArchive archive)
        {
            try
            {
                var entry = archive.GetEntry("manifest.json");
                if (entry == null) return false;
                using var s = entry.Open();
                using var reader = new StreamReader(s);
                var json = reader.ReadToEnd();
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("manifestType", out var t)) return false;
                var v = t.GetString();
                return string.Equals(v, "minecraftModpack", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        public static async Task<DropImportResult> ImportAsync(
            IReadOnlyList<DropImportItem> items,
            string minecraftFolder,
            Func<string, string, Task<bool>>? installModpackAsync = null,
            IReadOnlyCollection<string>? launcherManagedModIds = null,
            Func<IReadOnlyList<string>, Task>? onManagedOverrideAsync = null)
        {
            string modsDir = Path.Combine(minecraftFolder, "mods");
            string rpDir = Path.Combine(minecraftFolder, "resourcepacks");
            string shaderDir = Path.Combine(minecraftFolder, "shaderpacks");

            int mods = 0, rps = 0, shaders = 0, modpacks = 0, managedOverwrites = 0, skipped = 0;
            var errors = new List<string>();
            var kinds = new HashSet<DropTargetKind>();
            var managedNames = new List<string>();
            var managedSet = launcherManagedModIds != null
                ? new HashSet<string>(launcherManagedModIds, StringComparer.OrdinalIgnoreCase)
                : null;

            Directory.CreateDirectory(modsDir);
            Directory.CreateDirectory(rpDir);
            Directory.CreateDirectory(shaderDir);

            if (managedSet != null && onManagedOverrideAsync != null)
            {
                var preDetected = new List<string>();
                foreach (var item in items)
                {
                    if (item.Kind != DropTargetKind.Mod) continue;
                    if (item.SourcePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) continue;
                    var fn = Path.GetFileName(item.SourcePath);
                    if (IsLauncherManagedFile(fn, managedSet, out var id))
                        preDetected.Add(id);
                }
                if (preDetected.Count > 0)
                    await onManagedOverrideAsync(preDetected.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
            }

            foreach (var item in items)
            {
                try
                {
                    switch (item.Kind)
                    {
                        case DropTargetKind.Mod:
                            if (item.SourcePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                                mods += ExtractJarsFromZip(item.SourcePath, modsDir);
                            else
                            {
                                var fileName = Path.GetFileName(item.SourcePath);
                                File.Copy(item.SourcePath, Path.Combine(modsDir, fileName), overwrite: true);
                                if (managedSet != null && IsLauncherManagedFile(fileName, managedSet, out var matchedId))
                                {
                                    managedOverwrites++;
                                    managedNames.Add(matchedId);
                                }
                                else
                                {
                                    mods++;
                                }
                            }
                            kinds.Add(DropTargetKind.Mod);
                            break;
                        case DropTargetKind.ResourcePack:
                            File.Copy(item.SourcePath, Path.Combine(rpDir, Path.GetFileName(item.SourcePath)), overwrite: true);
                            rps++;
                            kinds.Add(DropTargetKind.ResourcePack);
                            break;
                        case DropTargetKind.Shader:
                            File.Copy(item.SourcePath, Path.Combine(shaderDir, Path.GetFileName(item.SourcePath)), overwrite: true);
                            shaders++;
                            kinds.Add(DropTargetKind.Shader);
                            break;
                        case DropTargetKind.Modpack:
                            if (installModpackAsync != null)
                            {
                                var title = Path.GetFileNameWithoutExtension(item.SourcePath);
                                var ok = await installModpackAsync(item.SourcePath, title).ConfigureAwait(false);
                                if (ok) { modpacks++; kinds.Add(DropTargetKind.Modpack); }
                                else { errors.Add($"{item.DisplayName}: modpack install failed"); }
                            }
                            else { skipped++; }
                            break;
                        case DropTargetKind.ModpackUnsupported:
                            errors.Add($"{item.DisplayName}: CurseForge / MultiMC modpacks aren't supported (use Modrinth equivalent)");
                            break;
                        default:
                            skipped++;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"{item.DisplayName}: {ex.Message}");
                }
            }

            return new DropImportResult(mods, rps, shaders, modpacks, managedOverwrites, skipped, errors, kinds.ToList(), managedNames);
        }

        private static bool IsLauncherManagedFile(string fileName, HashSet<string> managedIds, out string matchedId)
        {
            matchedId = "";
            try
            {
                var stem = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
                if (stem.EndsWith(".jar")) stem = stem.Substring(0, stem.Length - 4);
                foreach (var id in managedIds)
                {
                    if (stem.Contains(id, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedId = id;
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private static int ExtractJarsFromZip(string zipPath, string modsDir)
        {
            int count = 0;
            using var archive = ZipFile.OpenRead(zipPath);
            foreach (var entry in archive.Entries)
            {
                var name = entry.FullName.Replace('\\', '/');
                if (name.Contains('/')) continue;
                if (!name.EndsWith(".jar", StringComparison.OrdinalIgnoreCase)) continue;
                var dest = Path.Combine(modsDir, entry.Name);
                entry.ExtractToFile(dest, overwrite: true);
                count++;
            }
            return count;
        }
    }
}
