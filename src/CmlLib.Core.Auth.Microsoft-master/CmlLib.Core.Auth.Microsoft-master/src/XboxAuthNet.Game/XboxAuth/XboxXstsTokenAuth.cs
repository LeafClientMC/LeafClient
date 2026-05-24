// File: C:\Users\ziadf\source\repos\CmlLib.Core.Auth.Microsoft-master\CmlLib.Core.Auth.Microsoft-master\src\XboxAuthNet.Game\XboxAuth\XboxXstsTokenAuth.cs
using System; // Required for Console.WriteLine
using XboxAuthNet.Game.Authenticators;
using XboxAuthNet.Game.SessionStorages;
using XboxAuthNet.XboxLive;
using XboxAuthNet.XboxLive.Requests;

namespace XboxAuthNet.Game.XboxAuth;

public class XboxXstsTokenAuth : SessionAuthenticator<XboxAuthTokens>
{
    private readonly string _relyingParty;

    public XboxXstsTokenAuth(
        string relyingParty,
        ISessionSource<XboxAuthTokens> sessionSource)
        : base(sessionSource) =>
        _relyingParty = relyingParty;

    protected override async ValueTask<XboxAuthTokens?> Authenticate(AuthenticateContext context)
    {
        context.Logger.LogXboxXstsTokenAuth(_relyingParty);

        var xboxTokens = GetSessionFromStorage() ?? new XboxAuthTokens();

        // --- NEW DIAGNOSTIC LOGGING ---
        Console.WriteLine($"[XboxXstsTokenAuth] UserToken before XSTS request: {xboxTokens.UserToken?.Token ?? "NULL"}");
        Console.WriteLine($"[XboxXstsTokenAuth] DeviceToken before XSTS request: {xboxTokens.DeviceToken?.Token ?? "NULL"}");
        Console.WriteLine($"[XboxXstsTokenAuth] TitleToken before XSTS request: {xboxTokens.TitleToken?.Token ?? "NULL"}");
        Console.WriteLine($"[XboxXstsTokenAuth] RelyingParty for XSTS request: {_relyingParty}");
        // --- END NEW DIAGNOSTIC LOGGING ---

        var xboxAuthClient = new XboxAuthClient(context.HttpClient);
        var xsts = await xboxAuthClient.RequestXsts(new XboxXstsRequest
        {
            UserToken = xboxTokens.UserToken?.Token,
            DeviceToken = xboxTokens.DeviceToken?.Token,
            TitleToken = xboxTokens.TitleToken?.Token,
            RelyingParty = _relyingParty,
        });

        xboxTokens.XstsToken = xsts;
        return xboxTokens;
    }
}
