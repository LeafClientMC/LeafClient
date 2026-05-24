using System;
using System.Threading;
using System.Threading.Tasks;

namespace LeafClient.Services
{
    public sealed class IngameHeartbeatBeacon : IDisposable
    {
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

        private readonly Func<Task<string?>> _tokenProvider;
        private readonly object _gate = new();
        private CancellationTokenSource? _cts;
        private string? _username;

        public IngameHeartbeatBeacon(Func<Task<string?>> tokenProvider)
        {
            _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
        }

        public void Start(string minecraftUsername)
        {
            if (string.IsNullOrWhiteSpace(minecraftUsername)) return;

            CancellationTokenSource cts;
            lock (_gate)
            {
                StopUnlocked();
                _username = minecraftUsername;
                _cts = cts = new CancellationTokenSource();
            }

            _ = RunLoopAsync(minecraftUsername, cts.Token);
        }

        public void Stop()
        {
            string? username;
            lock (_gate)
            {
                username = _username;
                StopUnlocked();
            }
            if (!string.IsNullOrWhiteSpace(username))
            {
                _ = DeleteAsync(username!);
            }
        }

        public void Dispose() => Stop();

        private void StopUnlocked()
        {
            try { _cts?.Cancel(); } catch { }
            try { _cts?.Dispose(); } catch { }
            _cts = null;
            _username = null;
        }

        private async Task RunLoopAsync(string username, CancellationToken ct)
        {
            await SendAsync(username).ConfigureAwait(false);
            while (!ct.IsCancellationRequested)
            {
                try { await Task.Delay(Interval, ct).ConfigureAwait(false); }
                catch (OperationCanceledException) { return; }
                if (ct.IsCancellationRequested) return;
                await SendAsync(username).ConfigureAwait(false);
            }
        }

        private async Task SendAsync(string username)
        {
            string? token = null;
            try { token = await _tokenProvider().ConfigureAwait(false); } catch { }
            try { await LeafApiService.SendHeartbeatAsync(username, "ingame", token).ConfigureAwait(false); } catch { }
        }

        private async Task DeleteAsync(string username)
        {
            string? token = null;
            try { token = await _tokenProvider().ConfigureAwait(false); } catch { }
            try { await LeafApiService.DeleteHeartbeatAsync(username, token).ConfigureAwait(false); } catch { }
        }
    }
}
