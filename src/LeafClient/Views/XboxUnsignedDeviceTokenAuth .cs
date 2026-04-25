using System;
using System.Net.Http;
using System.Threading.Tasks;
using XboxAuthNet.Game.Authenticators;
using XboxAuthNet.Game.SessionStorages;
using XboxAuthNet.Game.XboxAuth;
using XboxAuthNet.XboxLive;
using XboxAuthNet.XboxLive.Responses;

namespace LeafClient.Views;

/// <summary>Named request body for XboxUnsignedDeviceTokenAuth — replaces anonymous type for AOT compatibility.</summary>
public sealed class XboxDeviceTokenRequestBody
{
    public string RelyingParty { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
    public XboxDeviceTokenRequestProperties Properties { get; set; } = new();
}

public sealed class XboxDeviceTokenRequestProperties
{
    public string AuthMethod { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public object ProofKey { get; set; } = new();
}

public class XboxUnsignedDeviceTokenAuth : SessionAuthenticator<XboxAuthTokens>
{
    private readonly string _deviceType;
    private readonly string _deviceVersion;

    public XboxUnsignedDeviceTokenAuth(
        string deviceType,
        string deviceVersion,
        ISessionSource<XboxAuthTokens> sessionSource)
        : base(sessionSource) =>
        (_deviceType, _deviceVersion) = (deviceType, deviceVersion);

    protected override async ValueTask<XboxAuthTokens?> Authenticate(AuthenticateContext context)
    {
        Console.WriteLine($"[XboxUnsignedDeviceTokenAuth] Starting unsigned device token authentication...");

        var xboxTokens = GetSessionFromStorage() ?? new XboxAuthTokens();

        try
        {
            // Create a custom device token request without signed authentication
            var request = new HttpRequestMessage(HttpMethod.Post, "https://device.auth.xboxlive.com/device/authenticate");

            var requestBody = new XboxDeviceTokenRequestBody
            {
                RelyingParty = "http://auth.xboxlive.com",
                TokenType = "JWT",
                Properties = new XboxDeviceTokenRequestProperties
                {
                    AuthMethod = "ProofOfPossession",
                    Id = Guid.NewGuid().ToString(),
                    DeviceType = _deviceType,
                    Version = _deviceVersion,
                    ProofKey = new { }
                }
            };

            var json = System.Text.Json.JsonSerializer.Serialize(requestBody, JsonContext.Default.XboxDeviceTokenRequestBody);
            request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await context.HttpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var deviceTokenResponse = System.Text.Json.JsonSerializer.Deserialize(responseBody, JsonContext.Default.XboxAuthResponse);
                xboxTokens.DeviceToken = deviceTokenResponse;

                Console.WriteLine($"[XboxUnsignedDeviceTokenAuth] Device token obtained successfully");
            }
            else
            {
                Console.WriteLine($"[XboxUnsignedDeviceTokenAuth] ERROR: {response.StatusCode} - {responseBody}");
                throw new Exception($"Device token request failed: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[XboxUnsignedDeviceTokenAuth] ERROR: {ex.Message}");
            throw;
        }

        return xboxTokens;
    }
}