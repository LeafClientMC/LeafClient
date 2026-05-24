// File: XboxAuthNet.Game/JsonConfig.cs
using System.Text.Json;

namespace XboxAuthNet.Game
{
    public static class JsonConfig
    {
        // This static property will hold the JsonSerializerOptions that includes the TypeInfoResolver.
        // It must be set by the consuming application (LeafClient) before any JSON serialization occurs.
        public static JsonSerializerOptions? DefaultOptions { get; set; }
    }
}
