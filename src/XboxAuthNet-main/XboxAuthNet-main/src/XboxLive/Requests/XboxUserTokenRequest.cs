// File: XboxAuthNet/XboxLive/Requests/XboxUserTokenRequest.cs (in base XboxAuthNet project)
using System;
using System.Net.Http;
using System.Threading.Tasks;
using XboxAuthNet; // Added for HttpHelper
using XboxAuthNet.XboxLive.Responses; // For XboxAuthResponse

namespace XboxAuthNet.XboxLive.Requests
{
    public class XboxUserTokenRequest : AbstractXboxAuthRequest
    {
        public const string UserAuthenticateUrl = "https://user.auth.xboxlive.com/user/authenticate";

        public XboxUserTokenRequest()
        {
            ContractVersion = "0";
            RelyingParty = XboxAuthConstants.XboxAuthRelyingParty;
        }

        public string? AccessToken { get; set; }
        public string? RelyingParty { get; set; }

        protected override HttpRequestMessage BuildRequest()
        {
            if (string.IsNullOrEmpty(AccessToken))
                throw new InvalidOperationException("AccessToken was null");

            // CRITICAL: Use concrete, AOT-serializable types defined within XboxAuthNet
            var properties = new XboxUserTokenRequestProperties(
                AuthMethod: "RPS",
                SiteName: "user.auth.xboxlive.com",
                RpsTicket: AccessToken! // AccessToken is non-null from check above
            );

            var payload = new XboxUserTokenRequestPayload(
                RelyingParty: RelyingParty!, // RelyingParty is non-null from constructor
                TokenType: "JWT",
                Properties: properties
            );

            var req = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(UserAuthenticateUrl),
                Content = HttpHelper.CreateJsonContent(payload) // Pass the concrete payload
            };

            CommonRequestHeaders.AddDefaultHeaders(req);
            return req;
        }

        public Task<XboxAuthResponse> Send(HttpClient httpClient)
        {
            return Send<XboxAuthResponse>(httpClient);
        }
    }
}
