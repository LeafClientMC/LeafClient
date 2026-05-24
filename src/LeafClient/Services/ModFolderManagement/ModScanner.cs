using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeafClient.Services.ModFolderManagement
{
    public static class ModScanner
    {
        private const int MaxJijDepth = 3;

        public static async Task<List<ModMetadata>> ScanFolderAsync(string modsFolder, bool fastMode = false)
        {
            var results = new List<ModMetadata>();
            if (!Directory.Exists(modsFolder)) return results;

            foreach (var path in Directory.EnumerateFiles(modsFolder, "*.jar", SearchOption.TopDirectoryOnly))
            {
                var meta = await ReadJarAsync(path, depth: 0, fastMode: fastMode);
                if (meta != null) results.Add(meta);
            }
            return results;
        }

        public static async Task<ModMetadata?> ReadJarAsync(string jarPath, int depth, bool fastMode = false)
        {
            if (depth > MaxJijDepth) return null;
            try
            {
                if (!File.Exists(jarPath)) return null;

                string? sha = fastMode ? null : await ModHasher.ComputeSha256Async(jarPath);
                using var archive = ZipFile.OpenRead(jarPath);

                var fabricEntry = archive.GetEntry("fabric.mod.json");
                var quiltEntry = archive.GetEntry("quilt.mod.json");

                if (fabricEntry != null)
                {
                    return ParseFabricMeta(fabricEntry, archive, jarPath, sha, depth, fastMode);
                }
                if (quiltEntry != null)
                {
                    return ParseQuiltMeta(quiltEntry, archive, jarPath, sha, depth);
                }

                return new ModMetadata(
                    ModId: System.IO.Path.GetFileNameWithoutExtension(jarPath),
                    DisplayName: System.IO.Path.GetFileName(jarPath),
                    Version: "",
                    FilePath: jarPath,
                    FileName: System.IO.Path.GetFileName(jarPath),
                    Sha256: sha,
                    Depends: Array.Empty<ModDependency>(),
                    Breaks: Array.Empty<ModDependency>(),
                    Recommends: Array.Empty<ModDependency>(),
                    Suggests: Array.Empty<ModDependency>(),
                    JarInJar: Array.Empty<ModMetadata>(),
                    IsParsed: false,
                    ParseError: "no fabric.mod.json or quilt.mod.json"
                );
            }
            catch (Exception ex)
            {
                return new ModMetadata(
                    ModId: System.IO.Path.GetFileNameWithoutExtension(jarPath),
                    DisplayName: System.IO.Path.GetFileName(jarPath),
                    Version: "",
                    FilePath: jarPath,
                    FileName: System.IO.Path.GetFileName(jarPath),
                    Sha256: null,
                    Depends: Array.Empty<ModDependency>(),
                    Breaks: Array.Empty<ModDependency>(),
                    Recommends: Array.Empty<ModDependency>(),
                    Suggests: Array.Empty<ModDependency>(),
                    JarInJar: Array.Empty<ModMetadata>(),
                    IsParsed: false,
                    ParseError: ex.Message
                );
            }
        }

        private static ModMetadata ParseFabricMeta(
            ZipArchiveEntry fabricEntry, ZipArchive archive, string jarPath, string? sha, int depth, bool fastMode = false)
        {
            using var stream = fabricEntry.Open();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var json = reader.ReadToEnd();
            using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            var root = doc.RootElement;

            string id = GetString(root, "id") ?? System.IO.Path.GetFileNameWithoutExtension(jarPath);
            string name = GetString(root, "name") ?? id;
            string version = GetString(root, "version") ?? "";

            var depends = ExtractDeps(root, "depends");
            var breaks = ExtractDeps(root, "breaks");
            var conflicts = ExtractDeps(root, "conflicts");
            if (conflicts.Count > 0) breaks.AddRange(conflicts);
            var recommends = ExtractDeps(root, "recommends");
            var suggests = ExtractDeps(root, "suggests");

            var jij = new List<ModMetadata>();
            int jijDepthLimit = fastMode ? 1 : MaxJijDepth;
            if (depth < jijDepthLimit && root.TryGetProperty("jars", out var jarsEl) && jarsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in jarsEl.EnumerateArray())
                {
                    var file = GetString(entry, "file");
                    if (string.IsNullOrEmpty(file)) continue;
                    var nestedEntry = archive.GetEntry(file);
                    if (nestedEntry == null) continue;
                    try
                    {
                        if (fastMode)
                        {
                            var nested = ReadNestedJarFromStream(nestedEntry, depth + 1);
                            if (nested != null) jij.Add(nested);
                        }
                        else
                        {
                            var tmp = System.IO.Path.GetTempFileName() + ".jar";
                            try
                            {
                                using (var src = nestedEntry.Open())
                                using (var dst = File.Create(tmp))
                                {
                                    src.CopyTo(dst);
                                }
                                var nested = ReadJarAsync(tmp, depth + 1).GetAwaiter().GetResult();
                                if (nested != null) jij.Add(nested);
                            }
                            finally
                            {
                                try { File.Delete(tmp); } catch { }
                            }
                        }
                    }
                    catch { }
                }
            }

            return new ModMetadata(
                ModId: id,
                DisplayName: name,
                Version: version,
                FilePath: jarPath,
                FileName: System.IO.Path.GetFileName(jarPath),
                Sha256: sha,
                Depends: depends,
                Breaks: breaks,
                Recommends: recommends,
                Suggests: suggests,
                JarInJar: jij,
                IsParsed: true,
                ParseError: null
            );
        }

        private static ModMetadata? ReadNestedJarFromStream(ZipArchiveEntry nestedEntry, int depth)
        {
            try
            {
                using var src = nestedEntry.Open();
                using var ms = new MemoryStream();
                src.CopyTo(ms);
                ms.Position = 0;
                using var nestedArchive = new ZipArchive(ms, ZipArchiveMode.Read);

                var fabricEntry = nestedArchive.GetEntry("fabric.mod.json");
                if (fabricEntry != null)
                {
                    return ParseFabricMeta(fabricEntry, nestedArchive, nestedEntry.FullName, sha: null, depth: depth, fastMode: true);
                }
                var quiltEntry = nestedArchive.GetEntry("quilt.mod.json");
                if (quiltEntry != null)
                {
                    return ParseQuiltMeta(quiltEntry, nestedArchive, nestedEntry.FullName, sha: null, depth: depth);
                }
            }
            catch { }
            return null;
        }

        private static ModMetadata ParseQuiltMeta(
            ZipArchiveEntry quiltEntry, ZipArchive archive, string jarPath, string? sha, int depth)
        {
            using var stream = quiltEntry.Open();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var json = reader.ReadToEnd();
            using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            var root = doc.RootElement;

            JsonElement loader = root;
            if (root.TryGetProperty("quilt_loader", out var ql)) loader = ql;

            string id = GetString(loader, "id") ?? System.IO.Path.GetFileNameWithoutExtension(jarPath);
            string version = GetString(loader, "version") ?? "";
            string name = id;
            if (loader.TryGetProperty("metadata", out var meta) && meta.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String)
            {
                name = nameEl.GetString() ?? id;
            }

            var depends = ExtractQuiltDeps(loader, "depends");
            var breaks = ExtractQuiltDeps(loader, "breaks");

            return new ModMetadata(
                ModId: id,
                DisplayName: name,
                Version: version,
                FilePath: jarPath,
                FileName: System.IO.Path.GetFileName(jarPath),
                Sha256: sha,
                Depends: depends,
                Breaks: breaks,
                Recommends: Array.Empty<ModDependency>(),
                Suggests: Array.Empty<ModDependency>(),
                JarInJar: Array.Empty<ModMetadata>(),
                IsParsed: true,
                ParseError: null
            );
        }

        private static List<ModDependency> ExtractDeps(JsonElement root, string fieldName)
        {
            var result = new List<ModDependency>();
            if (!root.TryGetProperty(fieldName, out var el)) return result;
            if (el.ValueKind != JsonValueKind.Object) return result;

            foreach (var prop in el.EnumerateObject())
            {
                string id = prop.Name;
                string range = "*";
                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    range = prop.Value.GetString() ?? "*";
                }
                else if (prop.Value.ValueKind == JsonValueKind.Array)
                {
                    var alts = new List<string>();
                    foreach (var item in prop.Value.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String) alts.Add(item.GetString() ?? "");
                    }
                    range = string.Join(" || ", alts);
                }
                result.Add(new ModDependency(id, range));
            }
            return result;
        }

        private static List<ModDependency> ExtractQuiltDeps(JsonElement loader, string fieldName)
        {
            var result = new List<ModDependency>();
            if (!loader.TryGetProperty(fieldName, out var el)) return result;
            if (el.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in el.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        result.Add(new ModDependency(item.GetString() ?? "", "*"));
                    }
                    else if (item.ValueKind == JsonValueKind.Object)
                    {
                        var id = GetString(item, "id");
                        if (string.IsNullOrEmpty(id)) continue;
                        var version = GetString(item, "versions") ?? GetString(item, "version") ?? "*";
                        result.Add(new ModDependency(id, version));
                    }
                }
            }
            return result;
        }

        private static string? GetString(JsonElement el, string key)
        {
            if (el.ValueKind != JsonValueKind.Object) return null;
            if (!el.TryGetProperty(key, out var v)) return null;
            if (v.ValueKind == JsonValueKind.String) return v.GetString();
            return null;
        }
    }
}
