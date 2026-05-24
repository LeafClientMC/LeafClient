using System;
using System.Threading;
using DiscordRPC;
using DiscordRPC.Message;

namespace LeafClient.Services
{
    public class DiscordRichPresenceService : IDisposable
    {
        private DiscordRpcClient? _client;
        private bool _initialized;
        private bool _connected;
        private string? _clientId;
        private RichPresence? _lastPresence;
        private Timer? _watchdog;
        private readonly object _lock = new();

        public bool IsInitialized => _initialized;
        public bool IsConnected => _connected && (_client?.IsInitialized ?? false);

        public void Initialize(string clientId)
        {
            lock (_lock)
            {
                _clientId = clientId;
                if (_initialized && _client != null)
                {
                    return;
                }
                TrySpinUpClient();
                _watchdog ??= new Timer(WatchdogTick, null, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(20));
                _initialized = true;
            }
        }

        private void TrySpinUpClient()
        {
            try
            {
                if (_clientId == null) return;
                if (_client != null)
                {
                    try { _client.Dispose(); } catch { }
                    _client = null;
                }

                _connected = false;
                var c = new DiscordRpcClient(_clientId, autoEvents: true);

                c.OnReady += (_, e) =>
                {
                    _connected = true;
                    LeafLog.Info("DRP", $"Connected as {e.User.Username}");
                    try
                    {
                        RichPresence? toSend;
                        lock (_lock) { toSend = _lastPresence; }
                        if (toSend != null) c.SetPresence(toSend);
                    }
                    catch (Exception ex) { LeafLog.Info("DRP", $"OnReady republish failed: {ex.Message}"); }
                };

                c.OnConnectionFailed += (_, e) =>
                {
                    _connected = false;
                    LeafLog.Error("DRP", $"Connection failed on pipe {e.FailedPipe}");
                };

                c.OnClose += (_, e) =>
                {
                    _connected = false;
                    LeafLog.Info("DRP", $"Closed: {e.Code} - {e.Reason}");
                };

                c.OnError += (_, e) =>
                    LeafLog.Info("DRP", $"Error: {e.Code} - {e.Message}");

                c.Initialize();
                _client = c;
            }
            catch (Exception ex)
            {
                LeafLog.Info("DRP", $"Spin-up failed: {ex.Message}");
                _connected = false;
            }
        }

        private void WatchdogTick(object? _)
        {
            try
            {
                if (!_initialized || _clientId == null) return;
                bool alive = _client != null && (_client.IsInitialized) && _connected;
                if (alive) return;
                lock (_lock)
                {
                    LeafLog.Info("DRP", "Watchdog: client not connected; recycling.");
                    TrySpinUpClient();
                }
            }
            catch (Exception ex) { LeafLog.Info("DRP", $"Watchdog error: {ex.Message}"); }
        }

        public void SetPresence(
            string details,
            string state,
            string largeImageKey,
            string largeImageText,
            string? smallImageKey = null,
            string? smallImageText = null,
            DateTime? start = null,
            Button[]? buttons = null)
        {
            var presence = new RichPresence
            {
                Details = details,
                State = state,
                Timestamps = start.HasValue ? new Timestamps(start.Value) : null,
                Assets = new Assets
                {
                    LargeImageKey = largeImageKey,
                    LargeImageText = largeImageText,
                    SmallImageKey = smallImageKey,
                    SmallImageText = smallImageText
                },
                Buttons = buttons
            };

            lock (_lock) { _lastPresence = presence; }

            try { _client?.SetPresence(presence); }
            catch (Exception ex) { LeafLog.Error("DRP", $"SetPresence failed (cached for retry): {ex.Message}"); }
        }

        public void ClearPresence()
        {
            lock (_lock) { _lastPresence = null; }
            try { _client?.ClearPresence(); } catch { }
        }

        public void Shutdown()
        {
            lock (_lock)
            {
                try { _watchdog?.Dispose(); } catch { }
                _watchdog = null;

                if (_client != null)
                {
                    try { _client.ClearPresence(); } catch { }
                    try { _client.Invoke(); } catch { }
                    try { Thread.Sleep(200); } catch { }
                    try { _client.Invoke(); } catch { }
                    try { _client.Deinitialize(); } catch { }
                    try { _client.Dispose(); } catch { }
                    _client = null;
                }
                _initialized = false;
                _connected = false;
                _lastPresence = null;
            }
        }

        public void Dispose() => Shutdown();
    }
}
