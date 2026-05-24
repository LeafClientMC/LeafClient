using LeafClient.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LeafClient.Services.ModFolderManagement
{
    public static class ModFolderTracker
    {
        public const string ArchiveFolderName = ".leaf-archived";
        public const string ProtectedFolderName = ".leaf-protected";

        public static string GetArchiveRoot(string modsFolder)
            => Path.Combine(modsFolder, ArchiveFolderName);

        public static string GetArchivePath(string modsFolder, string mcVersion)
            => Path.Combine(GetArchiveRoot(modsFolder), mcVersion);

        public static string GetProtectedPath(string modsFolder)
            => Path.Combine(modsFolder, ProtectedFolderName);

        public static async Task<List<TrackedJar>> ScanFolderAsync(
            string modsFolder,
            IReadOnlyList<InstalledMod> installedMods)
        {
            var result = new List<TrackedJar>();
            if (!Directory.Exists(modsFolder)) return result;

            var jarPaths = Directory.EnumerateFiles(modsFolder, "*.jar", SearchOption.TopDirectoryOnly).ToList();
            jarPaths.AddRange(Directory.EnumerateFiles(modsFolder, "*.disabled", SearchOption.TopDirectoryOnly));

            var hashTasks = jarPaths.Select(async p =>
            {
                var sha = await ModHasher.ComputeSha256Async(p);
                long size = 0;
                try { size = new FileInfo(p).Length; } catch { }
                return (path: p, sha, size);
            }).ToList();

            var hashed = await Task.WhenAll(hashTasks);

            var managedByHash = installedMods
                .Where(m => !string.IsNullOrEmpty(m.Sha256))
                .GroupBy(m => m.Sha256!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var managedByFileName = installedMods
                .Where(m => !string.IsNullOrEmpty(m.FileName))
                .GroupBy(m => m.FileName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var (path, sha, size) in hashed)
            {
                var fileName = Path.GetFileName(path);
                InstalledMod? managed = null;
                JarOwnership ownership;

                if (sha != null && managedByHash.TryGetValue(sha, out var byHash))
                {
                    managed = byHash;
                    ownership = JarOwnership.Managed;
                }
                else if (managedByFileName.TryGetValue(fileName, out var byName)
                         && string.IsNullOrEmpty(byName.Sha256))
                {
                    managed = byName;
                    ownership = JarOwnership.Managed;
                }
                else
                {
                    ownership = JarOwnership.User;
                }

                result.Add(new TrackedJar(path, fileName, sha, size, ownership, managed));
            }

            return result;
        }

        public static async Task<int> BackfillManagedHashesAsync(
            string modsFolder,
            List<InstalledMod> installedMods)
        {
            if (!Directory.Exists(modsFolder)) return 0;
            int updated = 0;

            foreach (var mod in installedMods)
            {
                if (!string.IsNullOrEmpty(mod.Sha256)) continue;
                if (string.IsNullOrEmpty(mod.FileName)) continue;
                var path = Path.Combine(modsFolder, mod.FileName);
                if (!File.Exists(path)) continue;
                var sha = await ModHasher.ComputeSha256Async(path);
                if (!string.IsNullOrEmpty(sha))
                {
                    mod.Sha256 = sha;
                    updated++;
                }
            }

            return updated;
        }

        public static int ArchiveJarsForOtherMcVersions(
            string modsFolder,
            string currentMcVersion,
            IReadOnlyList<TrackedJar> tracked)
        {
            int archived = 0;
            foreach (var jar in tracked)
            {
                if (jar.Ownership != JarOwnership.Managed) continue;
                if (jar.ManagedEntry == null) continue;
                var modMc = jar.ManagedEntry.MinecraftVersion;
                if (string.IsNullOrEmpty(modMc)) continue;
                if (string.Equals(modMc, currentMcVersion, StringComparison.OrdinalIgnoreCase)) continue;

                try
                {
                    var dest = Path.Combine(GetArchivePath(modsFolder, modMc), jar.FileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                    if (File.Exists(dest)) File.Delete(dest);
                    File.Move(jar.FullPath, dest);
                    archived++;
                }
                catch (Exception ex)
                {
                    LeafLog.Error("ModFolderTracker", $"Archive failed for {jar.FileName}: {ex.Message}");
                }
            }
            return archived;
        }

        public static int RestoreFromArchive(string modsFolder, string mcVersion)
        {
            int restored = 0;
            var archivePath = GetArchivePath(modsFolder, mcVersion);
            if (!Directory.Exists(archivePath)) return 0;

            foreach (var path in Directory.EnumerateFiles(archivePath, "*.jar", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var dest = Path.Combine(modsFolder, Path.GetFileName(path));
                    if (File.Exists(dest))
                    {
                        var existingSha = ModHasher.ComputeSha256(dest);
                        var archivedSha = ModHasher.ComputeSha256(path);
                        if (string.Equals(existingSha, archivedSha, StringComparison.OrdinalIgnoreCase))
                        {
                            File.Delete(path);
                            continue;
                        }
                        File.Delete(dest);
                    }
                    File.Move(path, dest);
                    restored++;
                }
                catch (Exception ex)
                {
                    LeafLog.Error("ModFolderTracker", $"Restore failed for {Path.GetFileName(path)}: {ex.Message}");
                }
            }

            try
            {
                if (Directory.Exists(archivePath) && !Directory.EnumerateFileSystemEntries(archivePath).Any())
                {
                    Directory.Delete(archivePath);
                }
            }
            catch { }

            return restored;
        }

        public static List<string> EnumerateOldArchivedFiles(string modsFolder, int olderThanDays)
        {
            var result = new List<string>();
            var archiveRoot = GetArchiveRoot(modsFolder);
            if (!Directory.Exists(archiveRoot)) return result;
            var cutoff = DateTime.UtcNow - TimeSpan.FromDays(olderThanDays);

            foreach (var path in Directory.EnumerateFiles(archiveRoot, "*", SearchOption.AllDirectories))
            {
                try
                {
                    if (File.GetLastWriteTimeUtc(path) < cutoff)
                    {
                        result.Add(path);
                    }
                }
                catch { }
            }
            return result;
        }
    }
}
