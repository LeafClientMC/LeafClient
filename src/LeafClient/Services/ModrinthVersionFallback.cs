using System.Linq;

namespace LeafClient.Services;

public static class ModrinthVersionFallback
{
    private static readonly string[] FallbackVersions = ["1.21.4", "1.21.1", "1.21"];

    public static string? GetFallbackVersion(string requested) =>
        FallbackVersions.FirstOrDefault(v => v != requested);
}
