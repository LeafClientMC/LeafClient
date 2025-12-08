using System.Text.Json.Serialization;

namespace XboxAuthNet.XboxLive.Requests
{
    public class XboxXstsRequestProperties
    {
        [JsonPropertyName("UserTokens")]
        public string[] UserTokens { get; set; } = null!;

        [JsonPropertyName("DeviceToken")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DeviceToken { get; set; }

        [JsonPropertyName("TitleToken")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TitleToken { get; set; }

        [JsonPropertyName("SandboxId")]
        public string SandboxId { get; set; } = "RETAIL";
    }
}
