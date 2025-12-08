using System.Text.Json.Serialization;

namespace XboxAuthNet.XboxLive.Requests
{
    public class XboxXstsRequestPayload
    {
        [JsonPropertyName("RelyingParty")]
        public string RelyingParty { get; set; } = null!;

        [JsonPropertyName("TokenType")]
        public string TokenType { get; set; } = "JWT";

        [JsonPropertyName("Properties")]
        public XboxXstsRequestProperties Properties { get; set; } = null!;
    }
}
