using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LeafClient.Services
{
    public sealed record CrashSuspect(
        string ModId,
        string DisplayName,
        string Reason,
        string? FilePath);

    public static class LaunchCrashAnalyzer
    {
        private static readonly Regex MixinApplyRegex = new(
            @"Mixin apply for mod ([A-Za-z0-9_\-]+) failed",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex InvalidInjectionRegex = new(
            @"InvalidInjectionException.*?from mod ([A-Za-z0-9_\-]+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ModResolutionRegex = new(
            @"requires (?:any version of )?'?([A-Za-z0-9_\-]+)'?,? but only the wrong version is present",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ClassNotFoundForModRegex = new(
            @"Error loading class:.*?for mod ([A-Za-z0-9_\-]+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static List<CrashSuspect> AnalyzeCrash(string minecraftFolder, string? recentLogText)
        {
            var suspects = new Dictionary<string, CrashSuspect>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var crashReportText = ReadLatestCrashReport(minecraftFolder);
                if (!string.IsNullOrEmpty(crashReportText))
                    Scan(crashReportText, suspects, minecraftFolder);
            }
            catch (Exception ex)
            {
                LeafLog.Error("CrashAnalyzer", $"Crash-report read failed: {ex.Message}");
            }

            if (!string.IsNullOrEmpty(recentLogText))
            {
                try { Scan(recentLogText, suspects, minecraftFolder); }
                catch (Exception ex) { LeafLog.Error("CrashAnalyzer", $"Log scan failed: {ex.Message}"); }
            }

            return suspects.Values.ToList();
        }

        private static string? ReadLatestCrashReport(string minecraftFolder)
        {
            try
            {
                var dir = Path.Combine(minecraftFolder, "crash-reports");
                if (!Directory.Exists(dir)) return null;
                var newest = Directory.EnumerateFiles(dir, "crash-*.txt")
                    .Select(p => new FileInfo(p))
                    .Where(fi => (DateTime.UtcNow - fi.LastWriteTimeUtc).TotalMinutes < 5)
                    .OrderByDescending(fi => fi.LastWriteTimeUtc)
                    .FirstOrDefault();
                if (newest == null) return null;
                return File.ReadAllText(newest.FullName);
            }
            catch { return null; }
        }

        private static void Scan(string text, Dictionary<string, CrashSuspect> dict, string minecraftFolder)
        {
            void Add(string id, string reason)
            {
                if (string.IsNullOrEmpty(id)) return;
                if (id.Equals("minecraft", StringComparison.OrdinalIgnoreCase)) return;
                if (id.Equals("fabricloader", StringComparison.OrdinalIgnoreCase)) return;
                if (id.Equals("fabric", StringComparison.OrdinalIgnoreCase)) return;
                if (id.Equals("java", StringComparison.OrdinalIgnoreCase)) return;
                if (id.Equals("leafclient", StringComparison.OrdinalIgnoreCase)) return;
                if (dict.ContainsKey(id)) return;
                var file = FindModFile(minecraftFolder, id);
                dict[id] = new CrashSuspect(id, PrettyName(id), reason, file);
            }

            foreach (Match m in MixinApplyRegex.Matches(text))
                Add(m.Groups[1].Value, "Mixin failed to apply on this Minecraft version (mod likely outdated for this MC version).");

            foreach (Match m in InvalidInjectionRegex.Matches(text))
                Add(m.Groups[1].Value, "Mod's mixin targets a method signature that no longer exists in this MC version.");

            foreach (Match m in ModResolutionRegex.Matches(text))
                Add(m.Groups[1].Value, "Required dependency version mismatch.");

            foreach (Match m in ClassNotFoundForModRegex.Matches(text))
                Add(m.Groups[1].Value, "Mod tried to load a class that doesn't exist in this MC version.");
        }

        private static string? FindModFile(string minecraftFolder, string modId)
        {
            try
            {
                var modsDir = Path.Combine(minecraftFolder, "mods");
                if (!Directory.Exists(modsDir)) return null;
                var lower = modId.ToLowerInvariant().Replace("-", "");
                foreach (var path in Directory.EnumerateFiles(modsDir, "*.jar", SearchOption.TopDirectoryOnly))
                {
                    var stem = Path.GetFileNameWithoutExtension(path).ToLowerInvariant().Replace("-", "");
                    if (stem.Contains(lower)) return path;
                }
            }
            catch { }
            return null;
        }

        public static string PrettyName(string modId)
        {
            if (string.IsNullOrEmpty(modId)) return modId;
            var parts = modId.Replace('_', '-').Split('-');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length == 0) continue;
                parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
            }
            return string.Join(" ", parts);
        }

        public static bool DisableMod(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return false;
                var disabled = filePath + ".disabled";
                if (File.Exists(disabled)) { try { File.Delete(disabled); } catch { } }
                File.Move(filePath, disabled);
                return true;
            }
            catch (Exception ex)
            {
                LeafLog.Error("CrashAnalyzer", $"DisableMod failed: {ex.Message}");
                return false;
            }
        }
    }
}
