// Services/DiscordRichPresenceService.cs
using System;
using DiscordRPC;
using DiscordRPC.Message; // ensure this import is present

namespace LeafClient.Services
{
    public class DiscordRichPresenceService : IDisposable
    {
        private DiscordRpcClient? _client;
        private bool _initialized;
        public bool IsInitialized => _initialized;

        public void Initialize(string clientId)
        {
            if (_initialized) return;

            _client = new DiscordRpcClient(clientId, autoEvents: true);

            _client.OnReady += (_, e) =>
                Console.WriteLine($"[DRP] Connected as {e.User.Username}");

            // ConnectionFailedMessage provides FailedPipe; remove ErrorMessage/Exception (they don't exist)
            _client.OnConnectionFailed += (_, e) =>
                Console.WriteLine($"[DRP] Connection failed on pipe {e.FailedPipe}");

            _client.OnClose += (_, e) =>
                Console.WriteLine($"[DRP] Closed: {e.Code} - {e.Reason}");

            _client.OnError += (_, e) =>
                Console.WriteLine($"[DRP] Error: {e.Code} - {e.Message}");

            _client.Initialize();
            _initialized = true;
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
            if (!_initialized || _client == null) return;

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

            _client.SetPresence(presence);
        }

        public void ClearPresence()
        {
            if (_initialized && _client != null)
                _client.ClearPresence();
        }

        public void Shutdown()
        {
            if (_client != null)
            {
                try { _client.ClearPresence(); } catch { }
                _client.Dispose();
                _client = null;
            }
            _initialized = false;
        }

        public void Dispose() => Shutdown();
    }
}
