using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using LeafClient.Services;

namespace LeafClient.PrivateServices
{
    public class OnlineCountService : IDisposable
    {
        private readonly string _launcherId;
        private Timer? _heartbeatTimer;
        private string? _minecraftUsername;
        private string? _accessToken;
        private Func<Task<string?>>? _tokenRefreshCallback;

        private LeafApiConfig? _cachedConfig;
        private DateTime _cacheExpiry = DateTime.MinValue;
        private readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);

        public OnlineCountService()
        {
            _launcherId = LoadOrCreateLauncherId();
        }

        public void SetTokenRefreshCallback(Func<Task<string?>> callback)
        {
            _tokenRefreshCallback = callback;
        }

        public void Start(string minecraftUsername, string? accessToken)
        {
            if (string.IsNullOrWhiteSpace(minecraftUsername))
                return;

            _minecraftUsername = minecraftUsername;
            _accessToken = accessToken;
            _ = SendHeartbeatAsync();
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = new Timer(
                _ => _ = SendHeartbeatAsync(),
                null,
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(5));
        }

        public void UpdateAccessToken(string? accessToken)
        {
            _accessToken = accessToken;
        }

        public async Task<int> GetOnlineCount()
        {
            if (_cachedConfig == null || DateTime.UtcNow > _cacheExpiry)
                await RefreshConfigAsync();
            return _cachedConfig?.OnlineCount ?? int.MinValue;
        }

        public string? GetMotd()
        {
            return _cachedConfig?.Motd;
        }

        public string? GetMotdColor()
        {
            return _cachedConfig?.MotdColor;
        }

        public async Task UpdateCount(bool isJoin, CancellationToken ct = default)
        {
            var username = _minecraftUsername;
            var token = await GetValidTokenAsync();
            if (isJoin)
            {
                if (!string.IsNullOrWhiteSpace(username))
                {
                    try { await LeafApiService.SendHeartbeatAsync(username!, "launcher", token); } catch { }
                }
                await RefreshConfigAsync();
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(username))
                {
                    try { await LeafApiService.DeleteHeartbeatAsync(username!, token, ct); } catch { }
                }
            }
        }

        public async Task DeleteHeartbeatAsync()
        {
            var username = _minecraftUsername;
            var token = await GetValidTokenAsync();
            if (string.IsNullOrWhiteSpace(username))
                return;
            try { await LeafApiService.DeleteHeartbeatAsync(username!, token); } catch { }
        }

        private async Task SendHeartbeatAsync()
        {
            var username = _minecraftUsername;
            var token = await GetValidTokenAsync();
            if (!string.IsNullOrWhiteSpace(username))
            {
                try { await LeafApiService.SendHeartbeatAsync(username!, "launcher", token); } catch { }
            }
            await RefreshConfigAsync();
        }

        private async Task<string?> GetValidTokenAsync()
        {
            if (_tokenRefreshCallback != null)
            {
                try
                {
                    var fresh = await _tokenRefreshCallback();
                    if (fresh != null) _accessToken = fresh;
                }
                catch { }
            }
            return _accessToken;
        }

        private async Task RefreshConfigAsync()
        {
            await _refreshLock.WaitAsync();
            try
            {
                if (_cachedConfig != null && DateTime.UtcNow <= _cacheExpiry)
                    return;
                var config = await LeafApiService.GetConfigAsync();
                if (config != null)
                {
                    _cachedConfig = config;
                    _cacheExpiry = DateTime.UtcNow.AddSeconds(30);
                }
                else
                {
                    Console.WriteLine("[OnlineCountService] WARN: GetConfigAsync returned null");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OnlineCountService] RefreshConfigAsync error: {ex.Message}");
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        private static string LoadOrCreateLauncherId()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LeafClient");
            Directory.CreateDirectory(dir);
            var idFile = Path.Combine(dir, "launcher_id.txt");
            if (File.Exists(idFile))
            {
                var id = File.ReadAllText(idFile).Trim();
                if (!string.IsNullOrWhiteSpace(id)) return id;
            }
            var newId = Guid.NewGuid().ToString("N");
            File.WriteAllText(idFile, newId);
            return newId;
        }

        public void Dispose()
        {
            _heartbeatTimer?.Dispose();
        }
    }
}
