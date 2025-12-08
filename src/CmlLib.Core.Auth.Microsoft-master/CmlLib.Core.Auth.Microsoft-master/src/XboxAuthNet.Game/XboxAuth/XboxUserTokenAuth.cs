// File: C:\Users\ziadf\source\repos\CmlLib.Core.Auth.Microsoft-master\CmlLib.Core.Auth.Microsoft-master\src\XboxAuthNet.Game\XboxAuth\XboxUserTokenAuth.cs
using System; // Required for Console.WriteLine
using XboxAuthNet.Game.Authenticators;
using XboxAuthNet.Game.SessionStorages;
using XboxAuthNet.OAuth;
using XboxAuthNet.XboxLive;

namespace XboxAuthNet.Game.XboxAuth;

public class XboxUserTokenAuth : SessionAuthenticator<XboxAuthTokens>
{
    private readonly ISessionSource<MicrosoftOAuthResponse> _oauthSessionSource;

    public XboxUserTokenAuth(
        ISessionSource<MicrosoftOAuthResponse> oAuthSessionSource,
        ISessionSource<XboxAuthTokens> sessionSource)
        : base(sessionSource) =>
        _oauthSessionSource = oAuthSessionSource;

    protected override async ValueTask<XboxAuthTokens?> Authenticate(AuthenticateContext context)
    {
        context.Logger.LogXboxUserTokenAuth();

        var oAuthResponse = _oauthSessionSource.Get(context.SessionStorage);
        var oAuthAccessToken = oAuthResponse?.AccessToken;

        // --- NEW DIAGNOSTIC LOGGING ---
        Console.WriteLine($"[XboxUserTokenAuth] Microsoft OAuth AccessToken: {oAuthAccessToken ?? "NULL"}");
        if (oAuthResponse != null)
        {
            Console.WriteLine($"[XboxUserTokenAuth] Microsoft OAuth ExpiresOn: {oAuthResponse.ExpiresOn}");
            Console.WriteLine($"[XboxUserTokenAuth] Current UTC time: {DateTimeOffset.UtcNow}");
            Console.WriteLine($"[XboxUserTokenAuth] Is OAuth AccessToken expired? {DateTimeOffset.UtcNow > oAuthResponse.ExpiresOn}");
        }
        else
        {
            Console.WriteLine("[XboxUserTokenAuth] Microsoft OAuth Response was NULL.");
        }
        // --- END NEW DIAGNOSTIC LOGGING ---

        if (string.IsNullOrEmpty(oAuthAccessToken))
            throw new XboxAuthException("OAuth access token was empty. Microsoft OAuth is required.", 0);

        var xboxAuthClient = new XboxAuthClient(context.HttpClient);
        var userToken = await xboxAuthClient.RequestUserToken(oAuthAccessToken);

        // --- NEW DIAGNOSTIC LOGGING ---
        Console.WriteLine($"[XboxUserTokenAuth] Xbox User Token obtained: {userToken?.Token ?? "NULL"}");
        Console.WriteLine($"[XboxUserTokenAuth] Xbox User Token ExpiresOn: {userToken?.ExpireOn}");
        // --- END NEW DIAGNOSTIC LOGGING ---

        var tokens = GetSessionFromStorage() ?? new XboxAuthTokens();
        tokens.UserToken = userToken;
        return tokens;
    }
}
