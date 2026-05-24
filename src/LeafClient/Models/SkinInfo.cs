using System;
using System.Text.Json.Serialization;

namespace LeafClient.Models
{
    public class SkinInfo
    {
        [JsonInclude]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonInclude]
        public string Name { get; set; } = "Untitled Skin";

        [JsonInclude]
        public string FilePath { get; set; } = string.Empty;

        [JsonInclude]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [JsonInclude]
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
    }
}