using System;
using System.Collections.Generic;

namespace LeafClient.Services;

public static class ModrinthVersionFallback
{
    private static readonly Dictionary<string, string> FallbackMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["1.21.11"] = "1.21.10",
        ["1.21.10"] = "1.21.9",
        ["1.21.9"] = "1.21.8",
        ["1.21.8"] = "1.21.7",
        ["1.21.7"] = "1.21.6",
        ["1.21.6"] = "1.21.5",
        ["1.21.5"] = "1.21.4",
        ["1.21.4"] = "1.21.3",
        ["1.21.3"] = "1.21.2",
        ["1.21.2"] = "1.21.1",
        ["1.21.1"] = "1.21",
        ["1.21"] = "1.20.1",
        ["1.20.2"] = "1.20.1",
    };

    public static string? GetFallbackVersion(string requested)
    {
        return FallbackMap.TryGetValue(requested, out var fallback) ? fallback : null;
    }
}
