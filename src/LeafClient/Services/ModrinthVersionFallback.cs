using System;

namespace LeafClient.Services;

public static class ModrinthVersionFallback
{
    private static readonly string[] FallbackVersions = ["1.21.4", "1.21.1", "1.21"];

    public static string? GetFallbackVersion(string requested)
    {
        int requestedIndex = Array.FindIndex(FallbackVersions,
            v => string.Equals(v, requested, StringComparison.OrdinalIgnoreCase));

        if (requestedIndex == -1)
            return FallbackVersions.Length > 0 ? FallbackVersions[0] : null;

        if (requestedIndex + 1 < FallbackVersions.Length)
            return FallbackVersions[requestedIndex + 1];

        if (requestedIndex > 0)
            return FallbackVersions[0];

        return null;
    }
}
