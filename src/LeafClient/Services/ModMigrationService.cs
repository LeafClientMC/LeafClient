using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using LeafClient.Models;

#nullable enable
namespace LeafClient.Services
{
    public class ModrinthMigrationVersion
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("project_id")] public string? ProjectId { get; set; }
        [JsonPropertyName("version_number")] public string? VersionNumber { get; set; }
        [JsonPropertyName("version_type")] public string? VersionType { get; set; }
        [JsonPropertyName("game_versions")] public List<string>? GameVersions { get; set; }
        [JsonPropertyName("loaders")] public List<string>? Loaders { get; set; }
        [JsonPropertyName("date_published")] public DateTime? DatePublished { get; set; }
        [JsonPropertyName("files")] public List<ModrinthMigrationFile>? Files { get; set; }
    }

    public class ModrinthMigrationFile
    {
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("filename")] public string? Filename { get; set; }
        [JsonPropertyName("primary")] public bool Primary { get; set; }
        [JsonPropertyName("size")] public long Size { get; set; }
        [JsonPropertyName("hashes")] public ModrinthMigrationHashes? Hashes { get; set; }
    }

    public class ModrinthMigrationHashes
    {
        [JsonPropertyName("sha512")] public string? Sha512 { get; set; }
        [JsonPropertyName("sha1")] public string? Sha1 { get; set; }
    }

    public class ModrinthMigrationProjectStub
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("slug")] public string? Slug { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
    }

    public class ModrinthMigrationUpdateRequest
    {
        [JsonPropertyName("loaders")] public List<string> Loaders { get; set; } = new();
        [JsonPropertyName("game_versions")] public List<string> GameVersions { get; set; } = new();
    }
}
#nullable restore

namespace LeafClient.Services
{
    public class ModMigrationService
    {
        public enum ItemStatus
        {
            CanUpgrade,
            AlreadyOnTarget,
            NoCompatibleVersion,
            NotOnModrinth,
            UnknownOrigin,
        }

        public class PlanItem
        {
            public required InstalledMod Source { get; init; }
            public required ItemStatus Status { get; init; }
            public string? ResolvedProjectId { get; init; }
            public string? ResolvedProjectTitle { get; init; }
            public string? NewVersionId { get; init; }
            public string? NewVersionNumber { get; init; }
            public string? NewFileName { get; init; }
            public string? NewDownloadUrl { get; init; }
            public string? NewSha512 { get; init; }
            public string? NewSha1 { get; init; }
            public long NewFileSize { get; init; }
            public bool Selected { get; set; } = true;
            public string DisplayName
            {
                get
                {
                    if (!string.IsNullOrWhiteSpace(ResolvedProjectTitle)) return ResolvedProjectTitle!;
                    if (!string.IsNullOrWhiteSpace(Source.Name)) return Source.Name;
                    return Source.FileName;
                }
            }
        }

        public class MigrationPlan
        {
            public required string FromMcVersion { get; init; }
            public required string ToMcVersion { get; init; }
            public required string Loader { get; init; }
            public required List<PlanItem> Items { get; init; }

            public IEnumerable<PlanItem> Upgradable => Items.Where(i => i.Status == ItemStatus.CanUpgrade);
            public IEnumerable<PlanItem> Incompatible => Items.Where(i => i.Status == ItemStatus.NoCompatibleVersion);
            public IEnumerable<PlanItem> NotOnModrinth => Items.Where(i => i.Status == ItemStatus.NotOnModrinth);
            public bool NeedsAttention => Items.Any(i =>
                i.Status == ItemStatus.CanUpgrade ||
                i.Status == ItemStatus.NoCompatibleVersion ||
                i.Status == ItemStatus.NotOnModrinth);
        }

        public class MigrationResult
        {
            public int Updated { get; set; }
            public int Disabled { get; set; }
            public int Failed { get; set; }
            public string BackupFolder { get; set; } = "";
            public string FromMcVersion { get; set; } = "";
            public string ToMcVersion { get; set; } = "";
            public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;
            public List<string> FailureMessages { get; set; } = new();
            public List<string> UpdatedItems { get; set; } = new();
            public List<string> DisabledItems { get; set; } = new();
        }

        private readonly HttpClient _http;
        private readonly string _minecraftFolder;
        private readonly LauncherSettings _settings;
        private readonly SettingsService _settingsService;

        public ModMigrationService(HttpClient http, string minecraftFolder, LauncherSettings settings, SettingsService settingsService)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _minecraftFolder = minecraftFolder ?? throw new ArgumentNullException(nameof(minecraftFolder));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        }

        public async Task<MigrationPlan> BuildPlanAsync(string fromMcVersion, string toMcVersion, string loader, CancellationToken ct = default)
        {
            EnsureUserAgent();

            var items = new List<PlanItem>();
            if (string.IsNullOrWhiteSpace(toMcVersion))
            {
                return new MigrationPlan { FromMcVersion = fromMcVersion ?? "", ToMcVersion = toMcVersion ?? "", Loader = loader ?? "fabric", Items = items };
            }

            string loaderLower = string.IsNullOrWhiteSpace(loader) ? "fabric" : loader.ToLowerInvariant();
            string modsFolder = Path.Combine(_minecraftFolder, "mods");

            var modsForFrom = _settings.InstalledMods
                .Where(m => !m.IsAutoInstalled &&
                            string.Equals(m.MinecraftVersion, fromMcVersion, StringComparison.OrdinalIgnoreCase))
                .Where(m => !"leafclient".Equals(m.ModId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            LeafLog.Info("ModMigration", $"Building plan {fromMcVersion} -> {toMcVersion} ({loaderLower}), {modsForFrom.Count} mod(s) to inspect. ModsFolder={modsFolder}");

            foreach (var mod in modsForFrom)
            {
                ct.ThrowIfCancellationRequested();

                string? jarPath = !string.IsNullOrWhiteSpace(mod.FileName)
                    ? Path.Combine(modsFolder, mod.FileName)
                    : null;
                bool jarExists = jarPath != null && File.Exists(jarPath);
                string? jarSha1 = jarExists ? ComputeHashHex(jarPath!, () => SHA1.Create()) : null;

                LeafLog.Info("ModMigration", $"[{mod.FileName}] jarExists={jarExists} sha1={(jarSha1 ?? "(none)")} modId={(mod.ModId ?? "(none)")}");

                LookupOutcome outcome = await LookupAsync(mod, jarSha1, toMcVersion, loaderLower, ct);

                if (outcome.Status == ItemStatus.NotOnModrinth)
                {
                    LeafLog.Info("ModMigration", $"[{mod.FileName}] -> NotOnModrinth");
                    items.Add(new PlanItem { Source = mod, Status = ItemStatus.NotOnModrinth });
                    continue;
                }

                bool alreadyOnTarget = !string.IsNullOrWhiteSpace(outcome.ProjectId) &&
                    _settings.InstalledMods.Any(m =>
                        string.Equals(m.ModId, outcome.ProjectId, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(m.MinecraftVersion, toMcVersion, StringComparison.OrdinalIgnoreCase));
                if (alreadyOnTarget)
                {
                    LeafLog.Info("ModMigration", $"[{mod.FileName}] -> AlreadyOnTarget ({outcome.ProjectId})");
                    items.Add(new PlanItem
                    {
                        Source = mod,
                        Status = ItemStatus.AlreadyOnTarget,
                        ResolvedProjectId = outcome.ProjectId,
                        ResolvedProjectTitle = outcome.Title,
                    });
                    continue;
                }

                if (outcome.Status == ItemStatus.NoCompatibleVersion || outcome.CompatibleVersion == null)
                {
                    LeafLog.Info("ModMigration", $"[{mod.FileName}] -> NoCompatibleVersion ({outcome.ProjectId ?? "?"} on Modrinth, no {toMcVersion}/{loaderLower} release)");
                    items.Add(new PlanItem
                    {
                        Source = mod,
                        Status = ItemStatus.NoCompatibleVersion,
                        ResolvedProjectId = outcome.ProjectId,
                        ResolvedProjectTitle = outcome.Title,
                    });
                    continue;
                }

                var compatible = outcome.CompatibleVersion;
                var primary = compatible.Files?.FirstOrDefault(f => f.Primary) ?? compatible.Files?.FirstOrDefault();
                if (primary == null || string.IsNullOrWhiteSpace(primary.Url) || string.IsNullOrWhiteSpace(primary.Filename))
                {
                    LeafLog.Info("ModMigration", $"[{mod.FileName}] -> NoCompatibleVersion (compatible version had no file)");
                    items.Add(new PlanItem
                    {
                        Source = mod,
                        Status = ItemStatus.NoCompatibleVersion,
                        ResolvedProjectId = outcome.ProjectId,
                        ResolvedProjectTitle = outcome.Title,
                    });
                    continue;
                }

                LeafLog.Info("ModMigration", $"[{mod.FileName}] -> CanUpgrade {outcome.Title ?? outcome.ProjectId} {compatible.VersionNumber} ({primary.Filename})");
                items.Add(new PlanItem
                {
                    Source = mod,
                    Status = ItemStatus.CanUpgrade,
                    ResolvedProjectId = outcome.ProjectId,
                    ResolvedProjectTitle = outcome.Title,
                    NewVersionId = compatible.Id,
                    NewVersionNumber = compatible.VersionNumber,
                    NewFileName = primary.Filename,
                    NewDownloadUrl = primary.Url,
                    NewSha512 = primary.Hashes?.Sha512,
                    NewSha1 = primary.Hashes?.Sha1,
                    NewFileSize = primary.Size,
                });
            }

            return new MigrationPlan
            {
                FromMcVersion = fromMcVersion ?? "",
                ToMcVersion = toMcVersion,
                Loader = loaderLower,
                Items = items,
            };
        }

        private void EnsureUserAgent()
        {
            try
            {
                if (_http.DefaultRequestHeaders.UserAgent.Count == 0)
                {
                    _http.DefaultRequestHeaders.UserAgent.ParseAdd("LeafClient/1.0 (+https://leafclient.com)");
                }
            }
            catch (Exception ex)
            {
                LeafLog.Info("ModMigration", $"Could not set User-Agent: {ex.Message}");
            }
        }

        private class LookupOutcome
        {
            public ItemStatus Status { get; set; }
            public string? ProjectId { get; set; }
            public string? Title { get; set; }
            public ModrinthMigrationVersion? CompatibleVersion { get; set; }
        }

        private async Task<LookupOutcome> LookupAsync(InstalledMod mod, string? jarSha1, string toMcVersion, string loader, CancellationToken ct)
        {
            string? projectId = null;
            string? title = null;

            if (!string.IsNullOrWhiteSpace(jarSha1))
            {
                var (updateVer, updateProjectId, updateStatus) = await TryUpdateLookupAsync(jarSha1, toMcVersion, loader, ct);
                if (updateVer != null)
                {
                    string? resolvedTitle = !string.IsNullOrWhiteSpace(updateProjectId)
                        ? await ResolveProjectTitleAsync(updateProjectId!, ct)
                        : null;
                    return new LookupOutcome
                    {
                        Status = ItemStatus.CanUpgrade,
                        ProjectId = updateProjectId,
                        Title = resolvedTitle,
                        CompatibleVersion = updateVer,
                    };
                }

                if (updateStatus == 404 || updateStatus == 410)
                {
                    var (idVer, idStatus) = await TryIdentifyByHashAsync(jarSha1, ct);
                    if (idVer != null && !string.IsNullOrWhiteSpace(idVer.ProjectId))
                    {
                        projectId = idVer.ProjectId;
                        title = await ResolveProjectTitleAsync(projectId!, ct);
                    }
                    else
                    {
                        LeafLog.Info("ModMigration", $"[{mod.FileName}] hash {jarSha1.Substring(0, Math.Min(8, jarSha1.Length))} unknown to Modrinth (identify status={idStatus})");
                    }
                }
                else
                {
                    LeafLog.Info("ModMigration", $"[{mod.FileName}] /update returned status={updateStatus}, will try ModId fallback");
                }
            }

            if (string.IsNullOrWhiteSpace(projectId) && !string.IsNullOrWhiteSpace(mod.ModId)
                && !mod.ModId.StartsWith("imported:", StringComparison.OrdinalIgnoreCase)
                && !mod.ModId.Contains(' '))
            {
                var stub = await TryGetProjectAsync(mod.ModId!, ct);
                if (stub != null && !string.IsNullOrWhiteSpace(stub.Id))
                {
                    projectId = stub.Id;
                    title = stub.Title;
                }
            }

            if (string.IsNullOrWhiteSpace(projectId))
            {
                return new LookupOutcome { Status = ItemStatus.NotOnModrinth };
            }

            var compat = await FindCompatibleVersionAsync(projectId!, toMcVersion, loader, ct);
            if (compat == null)
            {
                return new LookupOutcome { Status = ItemStatus.NoCompatibleVersion, ProjectId = projectId, Title = title };
            }

            return new LookupOutcome
            {
                Status = ItemStatus.CanUpgrade,
                ProjectId = projectId,
                Title = title,
                CompatibleVersion = compat,
            };
        }

        private async Task<(ModrinthMigrationVersion? version, string? projectId, int status)> TryUpdateLookupAsync(string jarSha1, string toMcVersion, string loader, CancellationToken ct)
        {
            string url = $"https://api.modrinth.com/v2/version_file/{jarSha1}/update?algorithm=sha1";
            try
            {
                var body = new ModrinthMigrationUpdateRequest
                {
                    Loaders = new List<string> { loader },
                    GameVersions = new List<string> { toMcVersion },
                };
                string json = JsonSerializer.Serialize(body, JsonContext.Default.ModrinthMigrationUpdateRequest);
                using var req = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                };
                EnsureUserAgent();
                using var resp = await _http.SendAsync(req, ct);
                int status = (int)resp.StatusCode;
                LeafLog.Info("ModMigration", $"POST /version_file/{jarSha1.Substring(0, Math.Min(8, jarSha1.Length))}/update?algorithm=sha1 body={json} -> {status}");
                if (!resp.IsSuccessStatusCode)
                {
                    try
                    {
                        string respBody = await resp.Content.ReadAsStringAsync(ct);
                        if (!string.IsNullOrWhiteSpace(respBody))
                            LeafLog.Info("ModMigration", $"  body: {Truncate(respBody, 300)}");
                    }
                    catch { }
                    return (null, null, status);
                }
                await using var stream = await resp.Content.ReadAsStreamAsync(ct);
                var v = await JsonSerializer.DeserializeAsync(stream, JsonContext.Default.ModrinthMigrationVersion, ct);
                return (v, v?.ProjectId, status);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                LeafLog.Info("ModMigration", $"/update lookup threw: {ex.GetType().Name}: {ex.Message}");
                return (null, null, -1);
            }
        }

        private async Task<(ModrinthMigrationVersion? version, int status)> TryIdentifyByHashAsync(string jarSha1, CancellationToken ct)
        {
            string url = $"https://api.modrinth.com/v2/version_file/{jarSha1}?algorithm=sha1";
            try
            {
                EnsureUserAgent();
                using var resp = await _http.GetAsync(url, ct);
                int status = (int)resp.StatusCode;
                LeafLog.Info("ModMigration", $"GET /version_file/{jarSha1.Substring(0, Math.Min(8, jarSha1.Length))}?algorithm=sha1 -> {status}");
                if (!resp.IsSuccessStatusCode) return (null, status);
                await using var stream = await resp.Content.ReadAsStreamAsync(ct);
                var v = await JsonSerializer.DeserializeAsync(stream, JsonContext.Default.ModrinthMigrationVersion, ct);
                return (v, status);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                LeafLog.Info("ModMigration", $"/version_file identify threw: {ex.GetType().Name}: {ex.Message}");
                return (null, -1);
            }
        }

        private async Task<ModrinthMigrationProjectStub?> TryGetProjectAsync(string idOrSlug, CancellationToken ct)
        {
            string url = $"https://api.modrinth.com/v2/project/{Uri.EscapeDataString(idOrSlug)}";
            try
            {
                EnsureUserAgent();
                using var resp = await _http.GetAsync(url, ct);
                LeafLog.Info("ModMigration", $"GET /project/{idOrSlug} -> {(int)resp.StatusCode}");
                if (!resp.IsSuccessStatusCode) return null;
                await using var stream = await resp.Content.ReadAsStreamAsync(ct);
                return await JsonSerializer.DeserializeAsync(stream, JsonContext.Default.ModrinthMigrationProjectStub, ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                LeafLog.Info("ModMigration", $"/project/{idOrSlug} threw: {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        private async Task<string?> ResolveProjectTitleAsync(string projectId, CancellationToken ct)
        {
            try
            {
                EnsureUserAgent();
                string url = $"https://api.modrinth.com/v2/project/{Uri.EscapeDataString(projectId)}";
                using var resp = await _http.GetAsync(url, ct);
                if (!resp.IsSuccessStatusCode) return null;
                await using var stream = await resp.Content.ReadAsStreamAsync(ct);
                var proj = await JsonSerializer.DeserializeAsync(stream, JsonContext.Default.ModrinthMigrationProjectStub, ct);
                return proj?.Title;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                LeafLog.Info("ModMigration", $"title resolve threw: {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        private async Task<ModrinthMigrationVersion?> FindCompatibleVersionAsync(string projectId, string mcVersion, string loader, CancellationToken ct)
        {
            string encodedMc = Uri.EscapeDataString(mcVersion);
            string encodedLoader = Uri.EscapeDataString(loader);
            string url = $"https://api.modrinth.com/v2/project/{Uri.EscapeDataString(projectId)}/version?game_versions=%5B%22{encodedMc}%22%5D&loaders=%5B%22{encodedLoader}%22%5D";

            EnsureUserAgent();
            using var resp = await _http.GetAsync(url, ct);
            LeafLog.Info("ModMigration", $"GET /project/{projectId}/version?mc={mcVersion}&loader={loader} -> {(int)resp.StatusCode}");
            if (!resp.IsSuccessStatusCode) return null;
            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            var versions = await JsonSerializer.DeserializeAsync(stream, JsonContext.Default.ListModrinthMigrationVersion, ct);
            if (versions == null || versions.Count == 0) return null;

            var matches = versions
                .Where(v => v.GameVersions != null && v.GameVersions.Any(g => string.Equals(g, mcVersion, StringComparison.OrdinalIgnoreCase)))
                .Where(v => v.Loaders == null || v.Loaders.Count == 0 || v.Loaders.Any(l => string.Equals(l, loader, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(v => v.DatePublished)
                .ToList();
            if (matches.Count == 0) return null;

            var release = matches.FirstOrDefault(v => string.Equals(v.VersionType, "release", StringComparison.OrdinalIgnoreCase));
            return release ?? matches[0];
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s ?? "";
            return s.Substring(0, max) + "...";
        }

        public async Task<MigrationResult> ApplyAsync(MigrationPlan plan, IProgress<string>? progress, CancellationToken ct = default)
        {
            var result = new MigrationResult
            {
                FromMcVersion = plan.FromMcVersion,
                ToMcVersion = plan.ToMcVersion,
            };
            string modsFolder = Path.Combine(_minecraftFolder, "mods");
            string backupRoot = Path.Combine(modsFolder, ".leaf-backup");
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            string backupFolder = Path.Combine(backupRoot, $"{plan.FromMcVersion}-to-{plan.ToMcVersion}-{stamp}");
            string tmpFolder = Path.Combine(modsFolder, ".leaf-tmp");
            string disabledFolder = Path.Combine(modsFolder, ".disabled-incompatible", $"{plan.FromMcVersion}-{stamp}");

            try
            {
                Directory.CreateDirectory(modsFolder);
                Directory.CreateDirectory(backupFolder);
                Directory.CreateDirectory(tmpFolder);
                result.BackupFolder = backupFolder;
            }
            catch (Exception ex)
            {
                LeafLog.Error("ModMigration", $"Failed to create migration folders: {ex.Message}");
                result.FailureMessages.Add($"Could not prepare backup folder: {ex.Message}");
                return result;
            }

            foreach (var item in plan.Items.Where(i => i.Status == ItemStatus.CanUpgrade && i.Selected))
            {
                ct.ThrowIfCancellationRequested();
                progress?.Report($"Updating {item.DisplayName}...");

                if (string.IsNullOrWhiteSpace(item.NewDownloadUrl) || string.IsNullOrWhiteSpace(item.NewFileName))
                {
                    result.Failed++;
                    result.FailureMessages.Add($"{item.DisplayName}: missing download URL");
                    continue;
                }

                string tmpPath = Path.Combine(tmpFolder, item.NewFileName);
                try
                {
                    using (var req = new HttpRequestMessage(HttpMethod.Get, item.NewDownloadUrl))
                    using (var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct))
                    {
                        resp.EnsureSuccessStatusCode();
                        await using var fs = File.Create(tmpPath);
                        await resp.Content.CopyToAsync(fs, ct);
                    }
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.FailureMessages.Add($"{item.DisplayName}: download failed - {ex.Message}");
                    TryDelete(tmpPath);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(item.NewSha512))
                {
                    string actual = ComputeHashHex(tmpPath, () => SHA512.Create());
                    if (!string.Equals(actual, item.NewSha512, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Failed++;
                        result.FailureMessages.Add($"{item.DisplayName}: SHA-512 mismatch, skipped");
                        TryDelete(tmpPath);
                        continue;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(item.NewSha1))
                {
                    string actual = ComputeHashHex(tmpPath, () => SHA1.Create());
                    if (!string.Equals(actual, item.NewSha1, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Failed++;
                        result.FailureMessages.Add($"{item.DisplayName}: SHA-1 mismatch, skipped");
                        TryDelete(tmpPath);
                        continue;
                    }
                }

                string oldPath = Path.Combine(modsFolder, item.Source.FileName);
                if (File.Exists(oldPath))
                {
                    try
                    {
                        string backupDest = Path.Combine(backupFolder, item.Source.FileName);
                        File.Move(oldPath, backupDest, overwrite: true);
                    }
                    catch (Exception ex)
                    {
                        result.Failed++;
                        result.FailureMessages.Add($"{item.DisplayName}: backup move failed - {ex.Message}");
                        TryDelete(tmpPath);
                        continue;
                    }
                }

                string newPath = Path.Combine(modsFolder, item.NewFileName!);
                try
                {
                    if (File.Exists(newPath)) File.Delete(newPath);
                    File.Move(tmpPath, newPath);
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.FailureMessages.Add($"{item.DisplayName}: install move failed - {ex.Message}");
                    TryDelete(tmpPath);
                    string restored = Path.Combine(backupFolder, item.Source.FileName);
                    if (File.Exists(restored))
                    {
                        try { File.Move(restored, oldPath); } catch { }
                    }
                    continue;
                }

                _settings.InstalledMods.Add(new InstalledMod
                {
                    ModId = !string.IsNullOrWhiteSpace(item.ResolvedProjectId) ? item.ResolvedProjectId! : item.Source.ModId,
                    Name = !string.IsNullOrWhiteSpace(item.ResolvedProjectTitle) ? item.ResolvedProjectTitle! : item.Source.Name,
                    Description = item.Source.Description,
                    Version = item.NewVersionNumber ?? "",
                    MinecraftVersion = plan.ToMcVersion,
                    FileName = item.NewFileName!,
                    DownloadUrl = item.NewDownloadUrl!,
                    Enabled = true,
                    InstallDate = DateTime.Now,
                    IconUrl = item.Source.IconUrl,
                    IsAutoInstalled = false,
                });
                result.Updated++;
                result.UpdatedItems.Add($"{item.DisplayName} {item.Source.Version} → {item.NewVersionNumber ?? "?"}");
            }

            foreach (var item in plan.Items.Where(i => i.Status == ItemStatus.NoCompatibleVersion && i.Selected))
            {
                ct.ThrowIfCancellationRequested();
                string oldPath = Path.Combine(modsFolder, item.Source.FileName);
                if (!File.Exists(oldPath)) continue;
                try
                {
                    Directory.CreateDirectory(disabledFolder);
                    string dest = Path.Combine(disabledFolder, item.Source.FileName);
                    File.Move(oldPath, dest, overwrite: true);
                    result.Disabled++;
                    result.DisabledItems.Add($"{item.DisplayName} (was {item.Source.MinecraftVersion})");
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.FailureMessages.Add($"{item.DisplayName}: disable failed - {ex.Message}");
                }
            }

            try { if (Directory.Exists(tmpFolder)) Directory.Delete(tmpFolder, recursive: true); } catch { }

            try { await _settingsService.SaveSettingsAsync(_settings); }
            catch (Exception ex) { LeafLog.Error("ModMigration", $"Settings save failed after migration: {ex.Message}"); }

            result.CompletedAtUtc = DateTime.UtcNow;
            try { PersistLastResult(result); }
            catch (Exception ex) { LeafLog.Error("ModMigration", $"Result persist failed: {ex.Message}"); }

            return result;
        }

        public static string LastResultPath
        {
            get
            {
                string root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(root, "LeafClient", "last-migration.json");
            }
        }

        public static MigrationResult? ReadLastResult()
        {
            try
            {
                string path = LastResultPath;
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize(json, JsonContext.Default.MigrationResult);
            }
            catch (Exception ex)
            {
                LeafLog.Error("ModMigration", $"Read last result failed: {ex.Message}");
                return null;
            }
        }

        private static void PersistLastResult(MigrationResult result)
        {
            string path = LastResultPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string json = JsonSerializer.Serialize(result, JsonContext.Default.MigrationResult);
            File.WriteAllText(path, json);
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        private static string ComputeHashHex(string path, Func<HashAlgorithm> factory)
        {
            using var algo = factory();
            using var fs = File.OpenRead(path);
            var hash = algo.ComputeHash(fs);
            var sb = new System.Text.StringBuilder(hash.Length * 2);
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

    }
}
