using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;
using System;
using System.IO;
using XboxAuthNet.Game.Msal;

namespace LeafClient.Services
{
    internal static class AuthConfig
    {
        public const string MicrosoftClientId = "86486490-6c13-46b0-a33e-d5717acb6281";

        public static SystemWebViewOptions BrowserOptions => new SystemWebViewOptions
        {
            BrowserRedirectSuccess = new System.Uri("https://leafclient.com/auth/success"),
            BrowserRedirectError = new System.Uri("https://leafclient.com/auth/error"),
        };

        public static void ApplyInteractiveOptions(AcquireTokenInteractiveParameterBuilder b)
        {
            if (EmbeddedAuthUiHost.IsAvailable)
                b.WithCustomWebUi(new EmbeddedMicrosoftAuthUi());
            else
                b.WithSystemWebViewOptions(BrowserOptions);
        }

        public static MsalCacheSettings BuildMsalCacheSettings()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string cacheDir = Path.Combine(appData, "LeafClient", "msal");
            try { Directory.CreateDirectory(cacheDir); } catch { }
            return new MsalCacheSettings
            {
                CacheFileName = "msal_token_cache.bin",
                CacheDir = cacheDir,
                KeyChainServiceName = "leafclient_msal_service",
                KeyChainAccountName = "leafclient_msal_account",
                LinuxKeyRingSchema = "com.leafclient.tokencache",
                LinuxKeyRingLabel = "MSAL token cache for Leaf Client launcher",
            };
        }
    }
}
