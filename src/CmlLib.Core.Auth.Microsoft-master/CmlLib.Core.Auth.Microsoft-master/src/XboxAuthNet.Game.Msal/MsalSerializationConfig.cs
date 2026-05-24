// File: XboxAuthNet.Game.Msal/MsalSerializationConfig.cs
using System.Text.Json;

namespace XboxAuthNet.Game.Msal
{
    /// <summary>
    /// Provides a static mechanism for the consuming application to inject
    /// AOT-compatible JsonSerializerOptions for System.Text.Json operations
    /// within XboxAuthNet.Game.Msal.
    /// </summary>
    public static class MsalSerializationConfig
    {
        // This static property will hold the JsonSerializerOptions that includes the TypeInfoResolver.
        // It must be set by the consuming application (LeafClient) before any JSON serialization occurs.
        public static JsonSerializerOptions? DefaultSerializerOptions { get; set; }
    }
}
