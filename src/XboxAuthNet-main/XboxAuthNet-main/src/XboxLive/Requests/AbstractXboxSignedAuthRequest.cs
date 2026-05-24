// File: XboxAuthNet/XboxLive/Requests/AbstractXboxSignedAuthRequest.cs (in base XboxAuthNet project)
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json; // Added for JsonSerializer
using System.Threading.Tasks;
using XboxAuthNet.XboxLive.Crypto;
using XboxAuthNet.XboxLive.Responses;

namespace XboxAuthNet.XboxLive.Requests;

public abstract class AbstractXboxSignedAuthRequest
{
    public XboxAuthResponseHandler ResponseHandler { get; set; } = new();
    protected abstract string RequestUrl { get; }
    protected virtual string Token { get; } = "";

    public async Task<T> Send<T>(HttpClient httpClient, IXboxRequestSigner signer)
    {
        if (ResponseHandler == null)
            throw new InvalidOperationException("ResponseHandler was null");

        var request = buildRequest(signer);
        var response = await httpClient.SendAsync(request);
        return await ResponseHandler.HandleResponse<T>(response);
    }

    private HttpRequestMessage buildRequest(IXboxRequestSigner signer)
    {
        var body = BuildBody(signer.ProofKey);

        // CRITICAL: Use the AOT-safe options for serialization
        var options = XboxAuthNet.JsonConfig.DefaultOptions ?? new JsonSerializerOptions();
        var bodyStr = JsonSerializer.Serialize(body, options);

        var req = new HttpRequestMessage
        {
            RequestUri = new Uri(RequestUrl),
            Method = HttpMethod.Post,
            Content = new StringContent(bodyStr, Encoding.UTF8, "application/json")
        };

        var signature = signer.SignRequest(RequestUrl, Token, bodyStr);
        req.Headers.Add("Signature", signature);
        CommonRequestHeaders.AddDefaultHeaders(req);
        return req;
    }

    protected abstract object BuildBody(object proofKey);
}
