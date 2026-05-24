using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace LeafClient.Services.ModFolderManagement
{
    public static class LibrariesJijScanner
    {
        private static readonly string[] _candidateRelativePaths =
        {
            "libraries/net/fabricmc/sponge-mixin",
            "libraries/net/fabricmc/fabric-loader",
            "libraries/org/quiltmc/quilt-loader",
        };

        public static async Task<List<ModMetadata>> ScanAsync(string minecraftFolder)
        {
            var results = new List<ModMetadata>();
            if (string.IsNullOrEmpty(minecraftFolder)) return results;

            foreach (var rel in _candidateRelativePaths)
            {
                string root = Path.Combine(minecraftFolder, rel.Replace('/', Path.DirectorySeparatorChar));
                if (!Directory.Exists(root)) continue;

                try
                {
                    foreach (var jar in Directory.EnumerateFiles(root, "*.jar", SearchOption.AllDirectories))
                    {
                        try
                        {
                            var meta = await ModScanner.ReadJarAsync(jar, depth: 0, fastMode: true);
                            if (meta == null) continue;
                            if (meta.IsParsed && !string.IsNullOrEmpty(meta.ModId))
                                results.Add(meta);
                        }
                        catch (Exception ex)
                        {
                            LeafLog.Error("LibrariesJijScanner", $"Skipped {jar}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LeafLog.Error("LibrariesJijScanner", $"Walk {root} failed: {ex.Message}");
                }
            }

            return results;
        }
    }
}
