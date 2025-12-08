// File: C:\Users\ziadf\source\repos\XboxAuthNet-main\XboxAuthNet-main\src\XboxAuthNet\XboxLive\Responses\XboxAuthResponseModels.cs
using System.Text.Json.Serialization;

namespace XboxAuthNet.XboxLive.Responses // <--- THIS IS THE CORRECT NAMESPACE
{
    // Matches the structure of an Xbox Live error response
    public record XboxErrorResponse(
        [property: JsonPropertyName("XErr")] string? XErr,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("redirect")] string? Redirect
    );
}
