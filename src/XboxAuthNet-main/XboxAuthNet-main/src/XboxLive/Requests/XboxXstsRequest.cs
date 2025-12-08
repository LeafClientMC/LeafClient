using System;
using System.Net.Http;
using System.Threading.Tasks;
using XboxAuthNet;
using XboxAuthNet.XboxLive.Responses;

namespace XboxAuthNet.XboxLive.Requests
{
    public class XboxXstsRequest : AbstractXboxAuthRequest
    {
        public const string XstsAuthorizeUrl = "https://xsts.auth.xboxlive.com/xsts/authorize";

        public XboxXstsRequest()
        {
            RelyingParty = XboxAuthConstants.XboxLiveRelyingParty;
            ContractVersion = "1";
        }

        public string? UserToken { get; set; }
        public string? RelyingParty { get; set; }
        public string? DeviceToken { get; set; }
        public string? TitleToken { get; set; }

        protected override HttpRequestMessage BuildRequest()
        {
            if (string.IsNullOrWhiteSpace(UserToken))
                throw new InvalidOperationException("UserToken was null");

            if (string.IsNullOrWhiteSpace(RelyingParty))
                throw new InvalidOperationException("RelyingParty was null");

            var props = new XboxXstsRequestProperties
            {
                UserTokens = new[] { UserToken },
                SandboxId = "RETAIL"
            };

            // ✅ Only include if present
            if (!string.IsNullOrWhiteSpace(DeviceToken))
                props.DeviceToken = DeviceToken;

            if (!string.IsNullOrWhiteSpace(TitleToken))
                props.TitleToken = TitleToken;

            var payload = new XboxXstsRequestPayload
            {
                RelyingParty = RelyingParty,
                TokenType = "JWT",
                Properties = props
            };

            // Optional debug
            // Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(payload, JsonConfig.DefaultOptions));

            var req = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(XstsAuthorizeUrl),
                Content = HttpHelper.CreateJsonContent(payload)
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
