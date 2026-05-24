using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Identity.Client.Extensibility;

namespace LeafClient.Services
{
    public sealed class EmbeddedAuthBrokerException : Exception
    {
        public EmbeddedAuthBrokerException(string message, Exception inner) : base(message, inner) { }
    }

    public static class EmbeddedAuthUiHost
    {
        private static Func<TopLevel?>? _topLevelProvider;

        public static void SetHostProvider(Func<TopLevel?> provider) => _topLevelProvider = provider;
        public static void ClearHost() => _topLevelProvider = null;
        public static bool IsAvailable => _topLevelProvider != null;
        public static TopLevel? GetTopLevel() => _topLevelProvider?.Invoke();

        public static bool IsBrokerFailure(Exception? ex)
        {
            while (ex != null)
            {
                if (ex is EmbeddedAuthBrokerException) return true;
                ex = ex.InnerException;
            }
            return false;
        }
    }

    internal sealed class EmbeddedMicrosoftAuthUi : ICustomWebUi
    {
        public async Task<Uri> AcquireAuthorizationCodeAsync(Uri authorizationUri, Uri redirectUri, CancellationToken cancellationToken)
        {
            var topLevel = await Dispatcher.UIThread.InvokeAsync(() => EmbeddedAuthUiHost.GetTopLevel());
            if (topLevel == null)
                throw new EmbeddedAuthBrokerException("No top-level window registered for embedded Microsoft auth.", new InvalidOperationException());

            Avalonia.Controls.WebAuthenticationResult? result;
            try
            {
                result = await Dispatcher.UIThread.InvokeAsync(async () =>
                    await Avalonia.Controls.WebAuthenticationBroker.AuthenticateAsync(
                        topLevel,
                        new Avalonia.Controls.WebAuthenticatorOptions(authorizationUri, redirectUri)));
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                throw new EmbeddedAuthBrokerException("Embedded Microsoft auth window failed: " + ex.Message, ex);
            }

            if (result?.CallbackUri == null)
                throw new OperationCanceledException("Microsoft login was cancelled.");

            return result.CallbackUri;
        }
    }
}
