using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LeafClient.Models;

namespace LeafClient.Services
{
    public static class CloudSyncService
    {
        private static long _lastPushUnixMs;
        private const int PushThrottleMs = 30_000;

        private static string McRoot => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");

        private static string ModConfigPath => Path.Combine(McRoot, "config", "leafclient", "settings.json");
        private static string ServersDatPath => Path.Combine(McRoot, "servers.dat");
        private static string OptionsTxtPath => Path.Combine(McRoot, "options.txt");

        private static string EscapeJsonString(string s)
        {
            var sb = new StringBuilder(s.Length + 8);
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"':  sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        private static JsonElement? SerializeLauncherSettings(LauncherSettings settings)
        {
            try
            {
                var clone = settings.Clone();
                clone.SessionAccessToken = null;
                clone.MicrosoftRefreshToken = null;
                clone.LeafApiJwt = null;
                clone.LeafApiRefreshToken = null;
                if (clone.SavedAccounts != null)
                {
                    foreach (var acc in clone.SavedAccounts)
                    {
                        acc.AccessToken = null;
                        acc.LeafApiJwt = null;
                        acc.LeafApiRefreshToken = null;
                    }
                }
                var json = JsonSerializer.Serialize(clone, JsonContext.Default.LauncherSettings);
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.Clone();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CloudSync] SerializeLauncherSettings failed: {ex.Message}");
                return null;
            }
        }

        private static JsonElement? ReadModSettings()
        {
            try
            {
                if (!File.Exists(ModConfigPath)) return null;
                var raw = File.ReadAllText(ModConfigPath);
                if (string.IsNullOrWhiteSpace(raw)) return null;
                using var doc = JsonDocument.Parse(raw);
                return doc.RootElement.Clone();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CloudSync] ReadModSettings failed: {ex.Message}");
                return null;
            }
        }

        private static JsonElement? ReadServerList()
        {
            try
            {
                if (!File.Exists(ServersDatPath)) return null;
                var bytes = File.ReadAllBytes(ServersDatPath);
                if (bytes.Length == 0 || bytes.Length > 524288) return null;
                var b64 = Convert.ToBase64String(bytes);
                var json = "{\"format\":\"nbt-base64\",\"data\":\"" + b64 + "\"}";
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.Clone();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CloudSync] ReadServerList failed: {ex.Message}");
                return null;
            }
        }

        private static JsonElement? ReadResourcePacks()
        {
            try
            {
                if (!File.Exists(OptionsTxtPath)) return null;
                var lines = File.ReadAllLines(OptionsTxtPath);
                string? rp = null;
                string? incompat = null;
                foreach (var line in lines)
                {
                    if (line.StartsWith("resourcePacks:", StringComparison.Ordinal))
                        rp = line.Substring("resourcePacks:".Length);
                    else if (line.StartsWith("incompatibleResourcePacks:", StringComparison.Ordinal))
                        incompat = line.Substring("incompatibleResourcePacks:".Length);
                }
                if (rp == null && incompat == null) return null;
                var sb = new StringBuilder();
                sb.Append('{');
                bool first = true;
                if (rp != null)
                {
                    sb.Append("\"resourcePacks\":");
                    sb.Append('"').Append(EscapeJsonString(rp)).Append('"');
                    first = false;
                }
                if (incompat != null)
                {
                    if (!first) sb.Append(',');
                    sb.Append("\"incompatibleResourcePacks\":");
                    sb.Append('"').Append(EscapeJsonString(incompat)).Append('"');
                }
                sb.Append('}');
                using var doc = JsonDocument.Parse(sb.ToString());
                return doc.RootElement.Clone();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CloudSync] ReadResourcePacks failed: {ex.Message}");
                return null;
            }
        }

        public static async Task<LeafApiSyncPullResult?> PullAsync(string accessToken, CancellationToken ct = default)
        {
            return await LeafApiService.GetCloudSyncAsync(accessToken, ct);
        }

        public static bool ShouldApplyRemote(string? localLastSyncedAt, string? remoteLastSyncedAt)
        {
            if (string.IsNullOrEmpty(remoteLastSyncedAt)) return false;
            if (!DateTimeOffset.TryParse(remoteLastSyncedAt, out var remote)) return false;
            if (string.IsNullOrEmpty(localLastSyncedAt)) return true;
            if (!DateTimeOffset.TryParse(localLastSyncedAt, out var local)) return true;
            return remote > local.AddSeconds(1);
        }

        public static void ApplyRemoteAuxiliary(LeafApiSyncPullResult remote)
        {
            try
            {
                if (remote.ModSettings.HasValue && remote.ModSettings.Value.ValueKind != JsonValueKind.Null)
                {
                    var dir = Path.GetDirectoryName(ModConfigPath);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir!);
                    File.WriteAllText(ModConfigPath, remote.ModSettings.Value.GetRawText(), new UTF8Encoding(false));
                }
            }
            catch (Exception ex) { Console.WriteLine($"[CloudSync] Apply mod_settings failed: {ex.Message}"); }

            try
            {
                if (remote.ServerList.HasValue && remote.ServerList.Value.ValueKind == JsonValueKind.Object)
                {
                    if (remote.ServerList.Value.TryGetProperty("data", out var dataEl) && dataEl.ValueKind == JsonValueKind.String)
                    {
                        var b64 = dataEl.GetString();
                        if (!string.IsNullOrEmpty(b64))
                        {
                            Directory.CreateDirectory(McRoot);
                            File.WriteAllBytes(ServersDatPath, Convert.FromBase64String(b64));
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"[CloudSync] Apply server_list failed: {ex.Message}"); }

            try
            {
                if (remote.ResourcePacks.HasValue && remote.ResourcePacks.Value.ValueKind == JsonValueKind.Object && File.Exists(OptionsTxtPath))
                {
                    string? rp = null;
                    string? incompat = null;
                    if (remote.ResourcePacks.Value.TryGetProperty("resourcePacks", out var rpEl) && rpEl.ValueKind == JsonValueKind.String)
                        rp = rpEl.GetString();
                    if (remote.ResourcePacks.Value.TryGetProperty("incompatibleResourcePacks", out var icEl) && icEl.ValueKind == JsonValueKind.String)
                        incompat = icEl.GetString();
                    if (rp != null || incompat != null)
                    {
                        var lines = File.ReadAllLines(OptionsTxtPath);
                        bool foundRp = false, foundIc = false;
                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (rp != null && lines[i].StartsWith("resourcePacks:", StringComparison.Ordinal)) { lines[i] = "resourcePacks:" + rp; foundRp = true; }
                            else if (incompat != null && lines[i].StartsWith("incompatibleResourcePacks:", StringComparison.Ordinal)) { lines[i] = "incompatibleResourcePacks:" + incompat; foundIc = true; }
                        }
                        var sb = new StringBuilder();
                        foreach (var line in lines) sb.AppendLine(line);
                        if (rp != null && !foundRp) sb.AppendLine("resourcePacks:" + rp);
                        if (incompat != null && !foundIc) sb.AppendLine("incompatibleResourcePacks:" + incompat);
                        File.WriteAllText(OptionsTxtPath, sb.ToString(), new UTF8Encoding(false));
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"[CloudSync] Apply resource_packs failed: {ex.Message}"); }
        }

        public static async Task<LeafApiSyncPushResult?> PushAsync(string accessToken, LauncherSettings settings, bool force, CancellationToken ct = default)
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (!force && (nowMs - Volatile.Read(ref _lastPushUnixMs)) < PushThrottleMs) return null;

            var body = new LeafApiSyncPushRequest(
                LauncherSettings: SerializeLauncherSettings(settings),
                ModSettings: ReadModSettings(),
                ServerList: ReadServerList(),
                ResourcePacks: ReadResourcePacks());

            var result = await LeafApiService.PushCloudSyncAsync(accessToken, body, ct);
            if (result?.Ok == true)
            {
                Volatile.Write(ref _lastPushUnixMs, nowMs);
                if (!string.IsNullOrEmpty(result.LastSyncedAt))
                    settings.LastCloudSyncAt = result.LastSyncedAt;
            }
            return result;
        }
    }
}
