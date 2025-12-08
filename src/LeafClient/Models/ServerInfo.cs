using Avalonia.Media;
using System;
using System.Text.Json.Serialization;

namespace LeafClient.Models
{
    public class ServerInfo
    {
        [JsonInclude]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonInclude]
        public string Name { get; set; } = "";

        [JsonInclude]
        public string Address { get; set; } = "";

        [JsonInclude]
        public int Port { get; set; } = 25565;

        [JsonInclude]
        public string IconBase64 { get; set; } = "";

        [JsonInclude]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [JsonInclude]
        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        // Runtime/UI-only properties (ignored by serializer)
        [JsonIgnore]
        public bool IsOnline { get; set; }

        [JsonIgnore]
        public int CurrentPlayers { get; set; }

        [JsonIgnore]
        public int MaxPlayers { get; set; }

        [JsonIgnore]
        public string Motd { get; set; } = "";

        [JsonIgnore]
        public string StatusText { get; set; } = "Checking...";

        [JsonIgnore]
        public IBrush StatusColor { get; set; } = Brushes.Gray;
    }
}