using XboxAuthNet.Game.Authenticators;
using XboxAuthNet.Game.SessionStorages;
using XboxAuthNet.XboxLive;
using XboxAuthNet.XboxLive.Crypto;

namespace XboxAuthNet.Game.XboxAuth;

public class XboxDeviceTokenAuth : SessionAuthenticator<XboxAuthTokens>
{
    private readonly IXboxRequestSigner _signer;
    private readonly string _deviceType;
    private readonly string _deviceVersion;

    public XboxDeviceTokenAuth(
        string deviceType,
        string deviceVersion,
        IXboxRequestSigner signer,
        ISessionSource<XboxAuthTokens> sessionSource)
        : base(sessionSource) => 
        (_deviceType, _deviceVersion, _signer) = (deviceType, deviceVersion, signer);

    protected override async ValueTask<XboxAuthTokens?> Authenticate(AuthenticateContext context)
    {
        context.Logger.LogXboxDeviceToken();

        // ADD DIAGNOSTIC LOGGING
        Console.WriteLine($"[XboxDeviceTokenAuth] Starting device token authentication...");
        Console.WriteLine($"[XboxDeviceTokenAuth] DeviceType: {_deviceType}, DeviceVersion: {_deviceVersion}");

        var xboxTokens = GetSessionFromStorage() ?? new XboxAuthTokens();

        try
        {
            var xboxAuthClient = new XboxSignedClient(_signer, context.HttpClient);
            xboxTokens.DeviceToken = await xboxAuthClient.RequestDeviceToken(
                _deviceType, _deviceVersion);

            // ADD DIAGNOSTIC LOGGING
            Console.WriteLine($"[XboxDeviceTokenAuth] Device token obtained: {xboxTokens.DeviceToken?.Token ?? "NULL"}");
            Console.WriteLine($"[XboxDeviceTokenAuth] Device token expires: {xboxTokens.DeviceToken?.ExpireOn}");
        }
        catch (Exception ex)
        {
            // ADD DIAGNOSTIC LOGGING
            Console.WriteLine($"[XboxDeviceTokenAuth] ERROR: {ex.Message}");
            Console.WriteLine($"[XboxDeviceTokenAuth] Stack trace: {ex.StackTrace}");
            throw;
        }

        return xboxTokens;
    }
}