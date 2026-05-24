using System.Text.Json;

namespace XboxAuthNet
{
    public static class JsonConfig
    {
        // Set by the consuming application (LeafClient) before any JSON is used.
        public static JsonSerializerOptions? DefaultOptions { get; set; }
    }
}
