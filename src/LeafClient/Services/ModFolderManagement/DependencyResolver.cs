using System;
using System.Collections.Generic;
using System.Linq;

namespace LeafClient.Services.ModFolderManagement
{
    public static class DependencyResolver
    {
        public static List<ConflictReport> FindConflicts(
            IReadOnlyList<ModMetadata> mods,
            string mcVersion,
            int javaMajorVersion,
            string fabricLoaderVersion,
            IReadOnlyCollection<string> ignoredKeys,
            IReadOnlyCollection<string>? permissiveManagedIds = null)
        {
            var permissive = new HashSet<string>(
                permissiveManagedIds ?? Array.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);
            permissive.Add("minecraft");
            permissive.Add("java");
            permissive.Add("fabricloader");
            permissive.Add("fabric-loader");

            var byId = new Dictionary<string, ModMetadata>(StringComparer.OrdinalIgnoreCase);
            foreach (var m in mods)
            {
                if (!string.IsNullOrEmpty(m.ModId)) byId[m.ModId] = m;
                foreach (var jij in m.JarInJar)
                {
                    if (!string.IsNullOrEmpty(jij.ModId)) byId[jij.ModId] = jij;
                }
            }

            byId["minecraft"] = SyntheticMod("minecraft", mcVersion);
            byId["java"] = SyntheticMod("java", javaMajorVersion.ToString());
            byId["fabricloader"] = SyntheticMod("fabricloader", fabricLoaderVersion);
            byId["fabric-loader"] = byId["fabricloader"];

            foreach (var managedId in permissive)
            {
                if (!byId.ContainsKey(managedId))
                    byId[managedId] = SyntheticMod(managedId, "999.0.0");
            }

            var allMods = mods.SelectMany(m => new[] { m }.Concat(m.JarInJar)).ToList();
            var conflicts = new List<ConflictReport>();

            foreach (var mod in allMods)
            {
                if (!mod.IsParsed) continue;

                foreach (var dep in mod.Depends)
                {
                    if (!byId.TryGetValue(dep.TargetId, out var target))
                    {
                        var reportMissing = new ConflictReport(
                            Source: mod,
                            TargetId: dep.TargetId,
                            Reason: $"{mod.DisplayName} requires '{dep.TargetId}' but it is not installed",
                            RequiredRange: dep.VersionRange,
                            InstalledVersion: null,
                            Severity: ConflictSeverity.Blocker);
                        if (!ignoredKeys.Contains(MakeKey(reportMissing)))
                            conflicts.Add(reportMissing);
                        continue;
                    }

                    if (permissive.Contains(dep.TargetId)) continue;

                    if (!VersionRange.Matches(dep.VersionRange, target.Version))
                    {
                        var report = new ConflictReport(
                            Source: mod,
                            TargetId: dep.TargetId,
                            Reason: $"{mod.DisplayName} requires {dep.TargetId} {dep.VersionRange}",
                            RequiredRange: dep.VersionRange,
                            InstalledVersion: target.Version,
                            Severity: ConflictSeverity.Blocker);
                        if (!ignoredKeys.Contains(MakeKey(report)))
                            conflicts.Add(report);
                    }
                }

                foreach (var brk in mod.Breaks)
                {
                    if (byId.TryGetValue(brk.TargetId, out var target)
                        && VersionRange.Matches(brk.VersionRange, target.Version))
                    {
                        var report = new ConflictReport(
                            Source: mod,
                            TargetId: brk.TargetId,
                            Reason: $"{mod.DisplayName} declares incompatibility with {brk.TargetId} {brk.VersionRange}",
                            RequiredRange: brk.VersionRange,
                            InstalledVersion: target.Version,
                            Severity: ConflictSeverity.Warning);
                        if (!ignoredKeys.Contains(MakeKey(report)))
                            conflicts.Add(report);
                    }
                }

                foreach (var rec in mod.Recommends)
                {
                    if (!byId.ContainsKey(rec.TargetId))
                    {
                        var report = new ConflictReport(
                            Source: mod,
                            TargetId: rec.TargetId,
                            Reason: $"{mod.DisplayName} recommends installing {rec.TargetId}",
                            RequiredRange: rec.VersionRange,
                            InstalledVersion: null,
                            Severity: ConflictSeverity.Notice);
                        if (!ignoredKeys.Contains(MakeKey(report)))
                            conflicts.Add(report);
                    }
                }
            }

            var idGroups = mods
                .Where(m => m.IsParsed && !string.IsNullOrEmpty(m.ModId))
                .GroupBy(m => m.ModId, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1);
            foreach (var group in idGroups)
            {
                var first = group.First();
                var others = string.Join(", ", group.Skip(1).Select(m => m.FileName));
                var report = new ConflictReport(
                    Source: first,
                    TargetId: first.ModId,
                    Reason: $"Duplicate mod ID '{first.ModId}' (also in: {others})",
                    RequiredRange: "",
                    InstalledVersion: first.Version,
                    Severity: ConflictSeverity.Blocker);
                if (!ignoredKeys.Contains(MakeKey(report)))
                    conflicts.Add(report);
            }

            return conflicts;
        }

        public static string MakeKey(ConflictReport r)
        {
            return $"{r.Source.ModId}|{r.Source.Version}|{r.TargetId}|{r.RequiredRange}|{r.InstalledVersion ?? ""}";
        }

        private static ModMetadata SyntheticMod(string id, string version) =>
            new(id, id, version, "", "", null,
                Array.Empty<ModDependency>(),
                Array.Empty<ModDependency>(),
                Array.Empty<ModDependency>(),
                Array.Empty<ModDependency>(),
                Array.Empty<ModMetadata>(),
                IsParsed: true,
                ParseError: null);
    }
}
