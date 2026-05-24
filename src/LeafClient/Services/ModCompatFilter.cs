using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeafClient.Services
{
    public static class ModCompatFilter
    {
        public const string SuffixPrefix = ".leafclient-mismatch.";

        public sealed class FilterReport
        {
            public List<DisabledMod> Disabled { get; } = new();
            public List<string>      Restored { get; } = new();
        }

        public sealed class DisabledMod
        {
            public string OriginalFileName { get; init; } = "";
            public string ModId            { get; init; } = "";
            public string Reason           { get; init; } = "";
        }

        public static async Task<HashSet<string>> PreviewDisablesAsync(
            string modsFolder,
            string targetMcVersion,
            int    activeJavaMajor)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(modsFolder) || !Directory.Exists(modsFolder)) return result;
            if (string.IsNullOrEmpty(targetMcVersion)) return result;

            await Task.Run(() =>
            {
                var present = new List<(string Path, FabricMeta Meta)>();
                foreach (var path in Directory.EnumerateFiles(modsFolder, "*.jar", SearchOption.TopDirectoryOnly))
                {
                    var meta = TryReadFabricMeta(path);
                    if (meta != null && !string.IsNullOrEmpty(meta.ModId))
                        present.Add((path, meta));
                }

                var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                versions["minecraft"] = targetMcVersion;
                if (activeJavaMajor > 0) versions["java"] = activeJavaMajor.ToString();
                foreach (var (_, m) in present)
                {
                    if (!string.IsNullOrEmpty(m.ModId) && !string.IsNullOrEmpty(m.Version))
                        versions[m.ModId!] = m.Version!;
                    foreach (var p in m.Provides)
                    {
                        if (string.IsNullOrWhiteSpace(p)) continue;
                        if (!versions.ContainsKey(p)) versions[p] = m.Version ?? "0";
                    }
                }

                bool changed;
                do
                {
                    changed = false;
                    for (int i = present.Count - 1; i >= 0; i--)
                    {
                        var (path, meta) = present[i];
                        if (FindIncompatibilityReason(meta, versions, activeJavaMajor) == null) continue;
                        result.Add(path);
                        if (!string.IsNullOrEmpty(meta.ModId)) versions.Remove(meta.ModId!);
                        foreach (var p in meta.Provides)
                            if (!string.IsNullOrWhiteSpace(p)) versions.Remove(p);
                        present.RemoveAt(i);
                        changed = true;
                    }
                } while (changed);
            });

            return result;
        }

        private sealed record KnownConflict(string ModId, string AgainstModId, string[] McPrefixes, string Reason);

        private static readonly KnownConflict[] KnownConflicts = new[]
        {
            new KnownConflict(
                "enhancedblockentities",
                "vulkanmod",
                new[] { "1.20." },
                "incompatible with VulkanMod on 1.20.x (overwrites WorldRenderer.setupTerrain)"),
        };

        private static bool McMatchesAnyPrefix(string mcVer, string[] prefixes)
        {
            foreach (var p in prefixes)
            {
                if (string.IsNullOrEmpty(p)) continue;
                if (mcVer.Equals(p.TrimEnd('.'), StringComparison.OrdinalIgnoreCase)) return true;
                if (mcVer.StartsWith(p, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static string? FindIncompatibilityReason(
            FabricMeta meta,
            Dictionary<string, string> versions,
            int activeJavaMajor)
        {
            foreach (var dep in meta.Depends)
            {
                string depKey = dep.Key;
                string constraint = dep.Value;
                if (depKey.Equals("fabricloader", StringComparison.OrdinalIgnoreCase)) continue;

                if (!versions.TryGetValue(depKey, out var have))
                {
                    if (depKey.Equals("java", StringComparison.OrdinalIgnoreCase) && activeJavaMajor <= 0) continue;
                    return $"depends on '{depKey}' {constraint} but it's not present";
                }
                if (!VersionConstraintMatches(constraint, have))
                    return $"requires '{depKey}' {constraint} (have {have})";
            }
            foreach (var br in meta.Breaks)
            {
                string breakKey = br.Key;
                string breakConstraint = br.Value;
                if (!versions.TryGetValue(breakKey, out var have)) continue;
                if (VersionConstraintMatches(breakConstraint, have))
                    return $"breaks on '{breakKey}' {breakConstraint} (have {have})";
            }
            if (!string.IsNullOrEmpty(meta.ClassTweakerNamespace))
            {
                string ns = meta.ClassTweakerNamespace!;
                if (!ns.Equals("intermediary", StringComparison.OrdinalIgnoreCase))
                    return $"classTweaker/accessWidener namespace is '{ns}' (runtime needs 'intermediary')";
            }
            if (!string.IsNullOrEmpty(meta.ModId)
                && versions.TryGetValue("minecraft", out var mcVer)
                && !string.IsNullOrEmpty(mcVer))
            {
                foreach (var k in KnownConflicts)
                {
                    if (!meta.ModId!.Equals(k.ModId, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!versions.ContainsKey(k.AgainstModId)) continue;
                    if (!McMatchesAnyPrefix(mcVer, k.McPrefixes)) continue;
                    return k.Reason;
                }
            }
            return null;
        }

        public static async Task<FilterReport> FilterAndRenameAsync(
            string modsFolder,
            string targetMcVersion,
            int    activeJavaMajor)
        {
            var report = new FilterReport();
            if (string.IsNullOrEmpty(modsFolder) || !Directory.Exists(modsFolder)) return report;
            if (string.IsNullOrEmpty(targetMcVersion)) return report;

            await Task.Run(() => RestoreAll(modsFolder, report.Restored));

            await Task.Run(() =>
            {
                var present = new List<(string Path, FabricMeta Meta)>();
                foreach (var path in Directory.EnumerateFiles(modsFolder, "*.jar", SearchOption.TopDirectoryOnly))
                {
                    var meta = TryReadFabricMeta(path);
                    if (meta != null && !string.IsNullOrEmpty(meta.ModId))
                        present.Add((path, meta));
                }

                var versions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                versions["minecraft"] = targetMcVersion;
                if (activeJavaMajor > 0) versions["java"] = activeJavaMajor.ToString();
                foreach (var (_, m) in present)
                {
                    if (!string.IsNullOrEmpty(m.ModId) && !string.IsNullOrEmpty(m.Version))
                        versions[m.ModId!] = m.Version!;
                    foreach (var p in m.Provides)
                    {
                        if (string.IsNullOrWhiteSpace(p)) continue;
                        if (!versions.ContainsKey(p)) versions[p] = m.Version ?? "0";
                    }
                }

                bool changed;
                do
                {
                    changed = false;
                    for (int i = present.Count - 1; i >= 0; i--)
                    {
                        var (path, meta) = present[i];
                        string? reason = FindIncompatibilityReason(meta, versions, activeJavaMajor);
                        if (reason == null) continue;

                        try
                        {
                            string disabledName = Path.GetFileName(path) + SuffixPrefix + SanitizeTag(targetMcVersion);
                            string disabledPath = Path.Combine(modsFolder, disabledName);
                            if (File.Exists(disabledPath)) File.Delete(disabledPath);
                            File.Move(path, disabledPath);

                            report.Disabled.Add(new DisabledMod
                            {
                                OriginalFileName = Path.GetFileName(path),
                                ModId            = meta.ModId ?? "",
                                Reason           = reason,
                            });

                            if (!string.IsNullOrEmpty(meta.ModId)) versions.Remove(meta.ModId!);
                            foreach (var p in meta.Provides)
                                if (!string.IsNullOrWhiteSpace(p)) versions.Remove(p);
                            present.RemoveAt(i);
                            changed = true;
                        }
                        catch (Exception ex)
                        {
                            try { LeafLog.Warn("ModCompatFilter", $"Skipped '{Path.GetFileName(path)}': {ex.Message}"); } catch { }
                        }
                    }
                } while (changed);
            });

            return report;
        }

        public static int RestoreAll(string modsFolder, List<string>? restoredOut = null)
        {
            if (!Directory.Exists(modsFolder)) return 0;
            int count = 0;
            foreach (var path in Directory.EnumerateFiles(modsFolder, "*" + SuffixPrefix + "*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    string fileName = Path.GetFileName(path);
                    int idx = fileName.IndexOf(SuffixPrefix, StringComparison.Ordinal);
                    if (idx <= 0) continue;
                    string restoredName = fileName.Substring(0, idx);
                    string restoredPath = Path.Combine(modsFolder, restoredName);
                    if (File.Exists(restoredPath))
                    {
                        File.Delete(path);
                    }
                    else
                    {
                        File.Move(path, restoredPath);
                    }
                    restoredOut?.Add(restoredName);
                    count++;
                }
                catch (Exception ex)
                {
                    try { LeafLog.Warn("ModCompatFilter", $"Restore failed for '{Path.GetFileName(path)}': {ex.Message}"); } catch { }
                }
            }
            return count;
        }

        private sealed class FabricMeta
        {
            public string? ModId   { get; init; }
            public string? Version { get; init; }
            public List<string> Provides { get; init; } = new();
            public Dictionary<string, string> Depends { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, string> Breaks  { get; init; } = new(StringComparer.OrdinalIgnoreCase);
            public string? ClassTweakerNamespace { get; init; }
            public string? ClassTweakerPath      { get; init; }
        }

        private static FabricMeta? TryReadFabricMeta(string jarPath)
        {
            try
            {
                using var fs  = File.OpenRead(jarPath);
                using var zip = new ZipArchive(fs, ZipArchiveMode.Read, false);
                var entry = zip.GetEntry("fabric.mod.json");
                if (entry == null) return null;

                using var stream = entry.Open();
                using var reader = new StreamReader(stream);
                string json = SanitizeLenientJson(reader.ReadToEnd());

                using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling     = JsonCommentHandling.Skip,
                });
                var root = doc.RootElement;

                string? modId = null;
                if (root.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String)
                    modId = idEl.GetString();

                string? version = null;
                if (root.TryGetProperty("version", out var vEl) && vEl.ValueKind == JsonValueKind.String)
                    version = vEl.GetString();

                var provides = new List<string>();
                if (root.TryGetProperty("provides", out var pvEl) && pvEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in pvEl.EnumerateArray())
                    {
                        if (el.ValueKind == JsonValueKind.String)
                        {
                            var s = el.GetString();
                            if (!string.IsNullOrWhiteSpace(s)) provides.Add(s!);
                        }
                    }
                }

                var depends = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (root.TryGetProperty("depends", out var dEl) && dEl.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in dEl.EnumerateObject())
                    {
                        var c = ExtractConstraint(dEl, prop.Name);
                        if (!string.IsNullOrEmpty(c)) depends[prop.Name] = c;
                    }
                }
                if (depends.Count == 0 && root.TryGetProperty("requires", out var rEl) && rEl.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in rEl.EnumerateObject())
                    {
                        var c = ExtractConstraint(rEl, prop.Name);
                        if (!string.IsNullOrEmpty(c)) depends[prop.Name] = c;
                    }
                }

                var breaks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (root.TryGetProperty("breaks", out var bEl) && bEl.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in bEl.EnumerateObject())
                    {
                        var c = ExtractConstraint(bEl, prop.Name);
                        if (!string.IsNullOrEmpty(c)) breaks[prop.Name] = c;
                    }
                }
                if (root.TryGetProperty("conflicts", out var cEl) && cEl.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in cEl.EnumerateObject())
                    {
                        var c = ExtractConstraint(cEl, prop.Name);
                        if (!string.IsNullOrEmpty(c) && !breaks.ContainsKey(prop.Name)) breaks[prop.Name] = c;
                    }
                }

                string? ctPath = null;
                if (root.TryGetProperty("accessWidener", out var awEl) && awEl.ValueKind == JsonValueKind.String)
                    ctPath = awEl.GetString();
                else if (root.TryGetProperty("classTweaker", out var ctEl) && ctEl.ValueKind == JsonValueKind.String)
                    ctPath = ctEl.GetString();

                string? ctNamespace = null;
                if (!string.IsNullOrEmpty(ctPath))
                {
                    var ctEntry = zip.GetEntry(ctPath);
                    if (ctEntry != null)
                    {
                        try
                        {
                            using var ctStream = ctEntry.Open();
                            using var ctReader = new StreamReader(ctStream);
                            string firstLine = ctReader.ReadLine() ?? "";
                            var tokens = firstLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (tokens.Length >= 3
                                && (tokens[0].Equals("accessWidener", StringComparison.OrdinalIgnoreCase)
                                 || tokens[0].Equals("classTweaker",  StringComparison.OrdinalIgnoreCase)))
                            {
                                ctNamespace = tokens[2];
                            }
                        }
                        catch { }
                    }
                }

                return new FabricMeta
                {
                    ModId    = modId,
                    Version  = version,
                    Provides = provides,
                    Depends  = depends,
                    Breaks   = breaks,
                    ClassTweakerPath      = ctPath,
                    ClassTweakerNamespace = ctNamespace,
                };
            }
            catch (Exception ex)
            {
                try { LeafLog.Warn("ModCompatFilter", $"Failed to read fabric.mod.json from '{Path.GetFileName(jarPath)}': {ex.GetType().Name}: {ex.Message}"); } catch { }
                return null;
            }
        }

        private static string? ExtractConstraint(JsonElement depends, string key)
        {
            if (!depends.TryGetProperty(key, out var v)) return null;
            if (v.ValueKind == JsonValueKind.String) return v.GetString();
            if (v.ValueKind == JsonValueKind.Array && v.GetArrayLength() > 0)
            {
                var parts = new List<string>();
                foreach (var el in v.EnumerateArray())
                    if (el.ValueKind == JsonValueKind.String) parts.Add(el.GetString() ?? "");
                if (parts.Count > 0) return string.Join("||", parts);
            }
            return null;
        }

        private static int? ParseJavaMajor(string? constraint)
        {
            if (string.IsNullOrWhiteSpace(constraint)) return null;
            int best = 0;
            foreach (var token in constraint.Split(new[] { '|', '&', ',', '[', ']', '(', ')', '~', '^', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string t = token.TrimStart('>', '=', '<', '*');
                if (string.IsNullOrEmpty(t)) continue;
                int dotIdx = t.IndexOf('.');
                string head = dotIdx >= 0 ? t.Substring(0, dotIdx) : t;
                if (int.TryParse(head, out int n) && n > best) best = n;
            }
            return best > 0 ? best : (int?)null;
        }

        public static bool VersionConstraintMatches(string constraint, string actualVersion)
        {
            if (string.IsNullOrWhiteSpace(constraint)) return true;
            constraint = constraint.Trim();
            if (constraint == "*") return true;
            actualVersion = actualVersion.Trim();

            if (constraint.Contains("||"))
            {
                foreach (var alt in constraint.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries))
                    if (VersionConstraintMatches(alt.Trim(), actualVersion)) return true;
                return false;
            }

            if (constraint.Contains(',') && (constraint.StartsWith("[") || constraint.StartsWith("(")))
            {
                return MavenRangeMatches(constraint, actualVersion);
            }

            if (constraint.IndexOf(' ') > 0 || constraint.IndexOf('\t') > 0)
            {
                foreach (var part in constraint.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
                    if (!VersionConstraintMatches(part, actualVersion)) return false;
                return true;
            }

            string sign = "";
            int p = 0;
            while (p < constraint.Length && (constraint[p] == '>' || constraint[p] == '<' || constraint[p] == '=' || constraint[p] == '~' || constraint[p] == '^'))
            {
                sign += constraint[p];
                p++;
            }
            string rhs = constraint.Substring(p).Trim();

            if (rhs.EndsWith(".x", StringComparison.OrdinalIgnoreCase))
            {
                string prefix = rhs.Substring(0, rhs.Length - 2);
                return actualVersion.StartsWith(prefix + ".", StringComparison.Ordinal)
                    || actualVersion == prefix;
            }
            if (rhs.Contains("*"))
            {
                string prefix = rhs.TrimEnd('*').TrimEnd('.');
                return actualVersion.StartsWith(prefix, StringComparison.Ordinal);
            }

            int cmp = CompareVersions(actualVersion, rhs);
            return sign switch
            {
                ""   => cmp >= 0,
                "="  => cmp == 0,
                "==" => cmp == 0,
                ">"  => cmp >  0,
                ">=" => cmp >= 0,
                "<"  => cmp <  0,
                "<=" => cmp <= 0,
                "~"  => cmp >= 0 && SameMinor(actualVersion, rhs),
                "^"  => cmp >= 0 && SameMajor(actualVersion, rhs),
                _    => cmp >= 0,
            };
        }

        private static bool MavenRangeMatches(string range, string version)
        {
            try
            {
                bool inLow  = range.StartsWith("[");
                bool inHigh = range.EndsWith("]");
                string inner = range.Substring(1, range.Length - 2);
                var parts = inner.Split(',');
                string lo = parts[0].Trim();
                string hi = parts.Length > 1 ? parts[1].Trim() : "";

                if (!string.IsNullOrEmpty(lo))
                {
                    int c = CompareVersions(version, lo);
                    if (inLow ? c < 0 : c <= 0) return false;
                }
                if (!string.IsNullOrEmpty(hi))
                {
                    int c = CompareVersions(version, hi);
                    if (inHigh ? c > 0 : c >= 0) return false;
                }
                return true;
            }
            catch { return true; }
        }

        private static int CompareVersions(string a, string b)
        {
            a = StripBuildMeta(a);
            b = StripBuildMeta(b);
            var pa = a.Split('.').Select(ParseLeadingInt).ToList();
            var pb = b.Split('.').Select(ParseLeadingInt).ToList();

            int n = Math.Max(pa.Count, pb.Count);
            for (int i = 0; i < n; i++)
            {
                int ai = i < pa.Count ? pa[i] : 0;
                int bi = i < pb.Count ? pb[i] : 0;
                if (ai != bi) return ai.CompareTo(bi);
            }
            return 0;
        }

        private static string StripBuildMeta(string v)
        {
            int plus = v.IndexOf('+');
            if (plus >= 0) v = v.Substring(0, plus);
            int dash = v.IndexOf('-');
            if (dash >= 0) v = v.Substring(0, dash);
            return v;
        }

        private static int ParseLeadingInt(string s)
        {
            int i = 0;
            while (i < s.Length && char.IsDigit(s[i])) i++;
            return i == 0 ? 0 : int.Parse(s.Substring(0, i));
        }

        private static bool SameMinor(string a, string b)
        {
            var pa = StripBuildMeta(a).Split('.');
            var pb = StripBuildMeta(b).Split('.');
            return pa.Length >= 2 && pb.Length >= 2 && pa[0] == pb[0] && pa[1] == pb[1];
        }

        private static bool SameMajor(string a, string b)
        {
            var pa = StripBuildMeta(a).Split('.');
            var pb = StripBuildMeta(b).Split('.');
            return pa.Length >= 1 && pb.Length >= 1 && pa[0] == pb[0];
        }

        private static string SanitizeTag(string s)
        {
            var chars = s.Where(c => char.IsLetterOrDigit(c) || c == '.' || c == '-').ToArray();
            return new string(chars);
        }

        private static string SanitizeLenientJson(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return raw;
            var sb = new System.Text.StringBuilder(raw.Length + 64);
            bool inString = false;
            bool escape   = false;
            foreach (char c in raw)
            {
                if (inString)
                {
                    if (escape)        { sb.Append(c); escape = false; continue; }
                    if (c == '\\')     { sb.Append(c); escape = true;  continue; }
                    if (c == '"')      { sb.Append(c); inString = false; continue; }
                    if (c == '\n')     { sb.Append("\\n"); continue; }
                    if (c == '\r')     { sb.Append("\\r"); continue; }
                    if (c == '\t')     { sb.Append("\\t"); continue; }
                    if (c < 0x20)      { sb.Append($"\\u{(int)c:X4}"); continue; }
                    sb.Append(c);
                }
                else
                {
                    sb.Append(c);
                    if (c == '"') inString = true;
                }
            }
            return sb.ToString();
        }
    }
}
