using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeafClient.Services;

public sealed class FirstLaunchDefaultsService
{
    public static FirstLaunchDefaultsService Instance { get; } = new();

    public async Task ApplyDefaultsIfNeededAsync(string minecraftFolder)
    {
        try
        {
            string optionsPath = Path.Combine(minecraftFolder, "options.txt");
            string sodiumPath = Path.Combine(minecraftFolder, "config", "sodium-options.json");
            string sodiumExtraPath = Path.Combine(minecraftFolder, "config", "sodium-extra-options.json");

            Directory.CreateDirectory(Path.Combine(minecraftFolder, "config"));

            await ApplyDefaultOptionsAsync(optionsPath);
            await ApplyDefaultSodiumAsync(sodiumPath);
            await ApplyDefaultSodiumExtraAsync(sodiumExtraPath);

            LeafLog.Error("FirstLaunchDefaults", "Applied default Minecraft & Sodium settings");
        }
        catch (Exception ex)
        {
            LeafLog.Error("FirstLaunchDefaults", $"Error applying defaults: {ex.Message}");
        }
    }

    private async Task ApplyDefaultOptionsAsync(string optionsPath)
    {
        var defaults = new[]
        {
            ("renderDistance", "10"),
            ("simulationDistance", "8"),
            ("maxFps", "260"),
            ("enableVsync", "false"),
            ("graphicsMode", "1"),
            ("particles", "1"),
            ("entityShadows", "false"),
            ("mipmapLevels", "4"),
            ("biomeBlendRadius", "1"),
            ("ao", "1"),
            ("entityDistanceScaling", "0.75"),
            ("renderClouds", "fast"),
            ("fov", "0.0625"),
        };

        var lines = File.Exists(optionsPath) ? File.ReadAllLines(optionsPath) : Array.Empty<string>();
        var settings = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
            var parts = line.Split(':');
            if (parts.Length == 2)
                settings[parts[0].Trim()] = parts[1].Trim();
        }

        foreach (var (key, value) in defaults)
        {
            if (!settings.ContainsKey(key))
                settings[key] = value;
        }

        var output = new System.Text.StringBuilder();
        foreach (var kvp in settings)
            output.AppendLine($"{kvp.Key}:{kvp.Value}");

        await File.WriteAllTextAsync(optionsPath, output.ToString());
    }

    private async Task ApplyDefaultSodiumAsync(string sodiumPath)
    {
        if (File.Exists(sodiumPath)) return;

        var options = new
        {
            quality = new
            {
                weather_quality = "FANCY",
                leaves_quality = "FANCY",
                particle_quality = "DECREASED",
                smooth_lighting = "SMOOTH",
                clouds_quality = "FAST",
                fog_quality = "SIMPLE",
                entity_distance = 0.75
            },
            performance = new
            {
                chunk_builder_threads = 0,
                always_defer_chunk_updates = true,
                use_chunk_multidraw = true,
                use_entity_culling = true,
                use_fog_occlusion = true,
                use_block_face_culling = true,
                animate_only_visible_textures = true
            },
            advanced = new
            {
                use_persistent_mapping = true,
                cpu_render_ahead_limit = 3
            }
        };

        var json = JsonSerializer.Serialize(options, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(sodiumPath, json);
    }

    private async Task ApplyDefaultSodiumExtraAsync(string sodiumExtraPath)
    {
        if (File.Exists(sodiumExtraPath)) return;

        var options = new
        {
            extras = new
            {
                cloud_height = 192,
                rain_splash = false
            }
        };

        var json = JsonSerializer.Serialize(options, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(sodiumExtraPath, json);
    }
}
