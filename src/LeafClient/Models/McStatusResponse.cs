using System.Text.Json.Serialization;

namespace LeafClient.Models
{
    // The top-level class matching the entire JSON object
    public class McStatusResponse
    {
        [JsonPropertyName("online")]
        public bool Online { get; set; }

        [JsonPropertyName("host")]
        public string? Host { get; set; }

        [JsonPropertyName("port")]
        public int Port { get; set; }

        [JsonPropertyName("players")]
        public PlayersData? Players { get; set; }

        [JsonPropertyName("motd")]
        public MotdData? Motd { get; set; }

        [JsonPropertyName("icon")]
        public string? Icon { get; set; }
    }

    public class PlayersData
    {
        [JsonPropertyName("online")]
        public int Online { get; set; }

        [JsonPropertyName("max")]
        public int Max { get; set; }
    }

    public class MotdData
    {
        // This is the clean, unformatted MOTD from the API
        [JsonPropertyName("clean")]
        public string? Clean { get; set; }
    }
}