using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LeafClient.Models;

namespace LeafClient.Services
{
    public static class ModrinthApi
    {
        private const string Base = "https://api.modrinth.com/v2";
        private const string UserAgent = "LeafClient/1.0 (+https://leafclient.com)";

        private static readonly HttpClient Http = CreateClient();

        private static HttpClient CreateClient()
        {
            var c = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            c.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return c;
        }

        public static async Task<List<ModrinthVersion>> GetProjectVersionsAsync(
            string projectIdOrSlug,
            string mcVersion,
            string loader,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(projectIdOrSlug)) return new List<ModrinthVersion>();
            string id = Uri.EscapeDataString(projectIdOrSlug);
            string gv = Uri.EscapeDataString($"[\"{mcVersion}\"]");
            string ld = Uri.EscapeDataString($"[\"{loader}\"]");
            string url = $"{Base}/project/{id}/version?game_versions={gv}&loaders={ld}";
            return await GetVersionsJsonAsync(url, projectIdOrSlug, ct);
        }

        public static async Task<List<ModrinthVersionDetailed>> GetProjectVersionsDetailedAsync(
            string projectIdOrSlug,
            string mcVersion,
            string loader,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(projectIdOrSlug)) return new List<ModrinthVersionDetailed>();
            string id = Uri.EscapeDataString(projectIdOrSlug);
            string gv = Uri.EscapeDataString($"[\"{mcVersion}\"]");
            string ld = Uri.EscapeDataString($"[\"{loader}\"]");
            string url = $"{Base}/project/{id}/version?game_versions={gv}&loaders={ld}";
            return await ParseDetailedAsync(url, projectIdOrSlug, ct);
        }

        public static async Task<List<ModrinthVersionDetailed>> GetAllProjectVersionsDetailedAsync(
            string projectIdOrSlug,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(projectIdOrSlug)) return new List<ModrinthVersionDetailed>();
            string id = Uri.EscapeDataString(projectIdOrSlug);
            string url = $"{Base}/project/{id}/version";
            return await ParseDetailedAsync(url, projectIdOrSlug, ct);
        }

        private static async Task<List<ModrinthVersionDetailed>> ParseDetailedAsync(string url, string projectIdOrSlug, CancellationToken ct)
        {
            string? body = await GetStringWithRetryAsync(url, projectIdOrSlug, ct);
            if (body == null) return new List<ModrinthVersionDetailed>();
            try
            {
                return JsonSerializer.Deserialize(body, JsonContext.Default.ListModrinthVersionDetailed) ?? new List<ModrinthVersionDetailed>();
            }
            catch (Exception ex)
            {
                LeafLog.Warn("ModrinthApi", $"Failed to parse detailed versions for '{projectIdOrSlug}': {ex.Message}");
                return new List<ModrinthVersionDetailed>();
            }
        }

        private static async Task<List<ModrinthVersion>> GetVersionsJsonAsync(string url, string projectIdOrSlug, CancellationToken ct)
        {
            string? body = await GetStringWithRetryAsync(url, projectIdOrSlug, ct);
            if (body == null) return new List<ModrinthVersion>();
            try
            {
                return JsonSerializer.Deserialize(body, JsonContext.Default.ListModrinthVersion) ?? new List<ModrinthVersion>();
            }
            catch (Exception ex)
            {
                LeafLog.Warn("ModrinthApi", $"Failed to parse versions for '{projectIdOrSlug}': {ex.Message}");
                return new List<ModrinthVersion>();
            }
        }

        public static async Task<string?> GetStringWithRetryAsync(string url, string context, CancellationToken ct)
        {
            int[] backoffMs = { 0, 500, 1500 };
            HttpResponseMessage? lastResp = null;
            Exception? lastEx = null;
            for (int attempt = 0; attempt < backoffMs.Length; attempt++)
            {
                if (backoffMs[attempt] > 0)
                {
                    try { await Task.Delay(backoffMs[attempt], ct); } catch (OperationCanceledException) { return null; }
                }
                try
                {
                    using var resp = await Http.GetAsync(url, ct);
                    lastResp = resp;
                    if (resp.StatusCode == HttpStatusCode.NotFound)
                    {
                        LeafLog.Info("ModrinthApi", $"[{context}] 404 (not found / no matching version) - {url}");
                        return null;
                    }
                    if (resp.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        int delay = 2000;
                        if (resp.Headers.TryGetValues("Retry-After", out var ra))
                        {
                            foreach (var v in ra)
                            {
                                if (int.TryParse(v, out var s)) { delay = Math.Max(delay, s * 1000); break; }
                            }
                        }
                        LeafLog.Warn("ModrinthApi", $"[{context}] 429 rate-limited, waiting {delay}ms then retrying");
                        try { await Task.Delay(delay, ct); } catch (OperationCanceledException) { return null; }
                        continue;
                    }
                    if ((int)resp.StatusCode >= 500)
                    {
                        LeafLog.Warn("ModrinthApi", $"[{context}] {(int)resp.StatusCode} server error on attempt {attempt + 1}");
                        continue;
                    }
                    if (!resp.IsSuccessStatusCode)
                    {
                        LeafLog.Warn("ModrinthApi", $"[{context}] non-success {(int)resp.StatusCode}");
                        return null;
                    }
                    return await resp.Content.ReadAsStringAsync(ct);
                }
                catch (TaskCanceledException) when (ct.IsCancellationRequested)
                {
                    return null;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    LeafLog.Warn("ModrinthApi", $"[{context}] transient error on attempt {attempt + 1}: {ex.GetType().Name}: {ex.Message}");
                }
            }
            if (lastEx != null)
                LeafLog.Warn("ModrinthApi", $"[{context}] giving up after retries: {lastEx.GetType().Name}: {lastEx.Message}");
            else if (lastResp != null)
                LeafLog.Warn("ModrinthApi", $"[{context}] giving up after retries (last status {(int)lastResp.StatusCode})");
            return null;
        }

        public static async Task<bool> DownloadFileWithRetryAsync(string url, string destPath, string context, CancellationToken ct = default)
        {
            int[] backoffMs = { 0, 500, 1500 };
            for (int attempt = 0; attempt < backoffMs.Length; attempt++)
            {
                if (backoffMs[attempt] > 0)
                {
                    try { await Task.Delay(backoffMs[attempt], ct); } catch (OperationCanceledException) { return false; }
                }
                try
                {
                    using var resp = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                    if (!resp.IsSuccessStatusCode)
                    {
                        LeafLog.Warn("ModrinthApi", $"[{context}] download HTTP {(int)resp.StatusCode} attempt {attempt + 1}");
                        if ((int)resp.StatusCode < 500 && resp.StatusCode != HttpStatusCode.TooManyRequests) return false;
                        continue;
                    }
                    string? dir = Path.GetDirectoryName(destPath);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    string tmp = destPath + ".part";
                    using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await resp.Content.CopyToAsync(fs, ct);
                    }
                    if (File.Exists(destPath)) File.Delete(destPath);
                    File.Move(tmp, destPath);
                    return true;
                }
                catch (Exception ex)
                {
                    LeafLog.Warn("ModrinthApi", $"[{context}] download error attempt {attempt + 1}: {ex.GetType().Name}: {ex.Message}");
                }
            }
            return false;
        }
    }
}
