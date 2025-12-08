using System.Text.Json.Serialization;

namespace LeafClient.Models
{
    public record ServerStatusResponse(
            [property: JsonPropertyName("online")] bool Online,
            [property: JsonPropertyName("players")] PlayersInfo? Players,
            [property: JsonPropertyName("motd")] MotdInfo? Motd,
            [property: JsonPropertyName("icon")] string? Icon
        );

    public record PlayersInfo(
        [property: JsonPropertyName("online")] int Online,
        [property: JsonPropertyName("max")] int Max
    );

    public record MotdInfo(
        [property: JsonPropertyName("clean")] string? Clean
    );
}