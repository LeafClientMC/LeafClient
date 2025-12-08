using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using LeafClient.Models;

namespace LeafClient.Services
{
    public class ModSettingsService
    {
        private readonly string _settingsFilePath;
        private readonly JsonSerializerOptions _jsonOptions;

        public ModSettingsService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _settingsFilePath = Path.Combine(appData, "LeafClient", "leafclient_mod_settings.txt");
            _jsonOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true };
        }

        private string GetModKey(string modName)
        {
            return modName.Replace(" ", "").ToUpperInvariant();
        }

        private async Task<JsonNode?> LoadJsonRootAsync()
        {
            if (!File.Exists(_settingsFilePath)) return null;
            try
            {
                using (var stream = new FileStream(_settingsFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    string json = await reader.ReadToEndAsync();
                    if (string.IsNullOrWhiteSpace(json)) return null;
                    return JsonNode.Parse(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ModSettings] Read Error: {ex.Message}");
                return null;
            }
        }

        private async Task SaveJsonRootAsync(JsonNode root)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath)!);
                var options = new JsonSerializerOptions { WriteIndented = true };
                await File.WriteAllTextAsync(_settingsFilePath, root.ToJsonString(options));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ModSettings] Save Error: {ex.Message}");
            }
        }

        // --- 1. SIMPLE BOOLEAN READER ---
        public async Task<bool> GetModStateAsync(string modName)
        {
            var root = await LoadJsonRootAsync();
            string key = GetModKey(modName);

            if (root?[key] is JsonNode modNode && modNode["enabled"] != null)
            {
                return modNode["enabled"].GetValue<bool>();
            }
            return false;
        }

        // --- 2. SIMPLE BOOLEAN WRITER ---
        public async Task SaveModStateAsync(string modName, bool isEnabled)
        {
            var root = await LoadJsonRootAsync() ?? new JsonObject();
            string key = GetModKey(modName);

            if (root[key] == null) root[key] = new JsonObject();
            root[key]!["enabled"] = isEnabled;

            await SaveJsonRootAsync(root);
        }

        // --- 3. COMPLEX SETTINGS READER (Manual Mapping for AOT) ---
        public async Task<T> GetModSettingsAsync<T>(string modName) where T : ModConfigBase, new()
        {
            var root = await LoadJsonRootAsync();
            string key = GetModKey(modName);
            var result = new T();

            if (root?[key] is JsonNode n)
            {
                // Base
                if (n["enabled"] != null) result.Enabled = (bool)n["enabled"];

                // Manual Mapping based on Type
                if (result is PerformanceConfig perf)
                {
                    if (n["showMemory"] != null) perf.ShowMemory = (bool)n["showMemory"];
                    if (n["showCpu"] != null) perf.ShowCpu = (bool)n["showCpu"];
                }
                else if (result is KeystrokesConfig ks)
                {
                    if (n["scale"] != null) ks.Scale = (double)n["scale"];
                    if (n["opacity"] != null) ks.Opacity = (double)n["opacity"];

                    if (n["textColor"] != null) ks.TextColor = (int)n["textColor"].GetValue<double>();
                    if (n["backgroundColor"] != null) ks.BackgroundColor = (int)n["backgroundColor"].GetValue<double>();

                    if (n["showAnimations"] != null) ks.ShowAnimations = (bool)n["showAnimations"];
                    if (n["chromaMode"] != null) ks.ChromaMode = (bool)n["chromaMode"];
                    if (n["showBorder"] != null) ks.ShowBorder = (bool)n["showBorder"];
                }
                else if (result is ItemCounterConfig ic)
                {
                    if (n["autoDetect"] != null) ic.AutoDetect = (bool)n["autoDetect"];
                    if (n["showIcons"] != null) ic.ShowIcons = (bool)n["showIcons"];
                    if (n["showAnimations"] != null) ic.ShowAnimations = (bool)n["showAnimations"];
                    if (n["horizontalLayout"] != null) ic.HorizontalLayout = (bool)n["horizontalLayout"];
                    if (n["scale"] != null) ic.Scale = (double)n["scale"];
                }
                else if (result is MinimapConfig mm)
                {
                    if (n["mapSize"] != null) mm.MapSize = (int)n["mapSize"].GetValue<double>();
                    if (n["displaySize"] != null) mm.DisplaySize = (int)n["displaySize"].GetValue<double>();
                    if (n["zoom"] != null) mm.Zoom = (double)n["zoom"];
                    if (n["showWaypoints"] != null) mm.ShowWaypoints = (bool)n["showWaypoints"];
                    if (n["showCoordinates"] != null) mm.ShowCoordinates = (bool)n["showCoordinates"];
                }
                else if (result is ToggleSprintConfig ts)
                {
                    if (n["flyBoost"] != null) ts.FlyBoost = (bool)n["flyBoost"];
                }
                // --- FIX: Added GeneralConfig Mapping ---
                else if (result is GeneralConfig gen)
                {
                    if (n["hudSnapping"] != null) gen.HudSnapping = (bool)n["hudSnapping"];
                    if (n["hudSnapRange"] != null) gen.HudSnapRange = (int)n["hudSnapRange"].GetValue<double>();
                    if (n["showHudGrid"] != null) gen.ShowHudGrid = (bool)n["showHudGrid"];
                    if (n["menuParticles"] != null) gen.MenuParticles = (bool)n["menuParticles"];
                }
            }
            return result;
        }

        // --- 4. COMPLEX SETTINGS WRITER (Manual Mapping for AOT) ---
        public async Task SaveModSettingsAsync<T>(string modName, T settings) where T : ModConfigBase
        {
            var root = await LoadJsonRootAsync() ?? new JsonObject();
            string key = GetModKey(modName);

            if (root[key] == null) root[key] = new JsonObject();
            var n = root[key];

            // Base
            n["enabled"] = settings.Enabled;

            // Manual Mapping
            if (settings is PerformanceConfig perf)
            {
                n["showMemory"] = perf.ShowMemory;
                n["showCpu"] = perf.ShowCpu;
            }
            else if (settings is KeystrokesConfig ks)
            {
                n["scale"] = ks.Scale;
                n["opacity"] = ks.Opacity;
                n["textColor"] = ks.TextColor;
                n["backgroundColor"] = ks.BackgroundColor;
                n["showAnimations"] = ks.ShowAnimations;
                n["chromaMode"] = ks.ChromaMode;
                n["showBorder"] = ks.ShowBorder;
            }
            else if (settings is ItemCounterConfig ic)
            {
                n["autoDetect"] = ic.AutoDetect;
                n["showIcons"] = ic.ShowIcons;
                n["showAnimations"] = ic.ShowAnimations;
                n["horizontalLayout"] = ic.HorizontalLayout;
                n["scale"] = ic.Scale;
            }
            else if (settings is MinimapConfig mm)
            {
                n["mapSize"] = mm.MapSize;
                n["displaySize"] = mm.DisplaySize;
                n["zoom"] = mm.Zoom;
                n["showWaypoints"] = mm.ShowWaypoints;
                n["showCoordinates"] = mm.ShowCoordinates;
            }
            else if (settings is ToggleSprintConfig ts)
            {
                n["flyBoost"] = ts.FlyBoost;
            }
            // --- FIX: Added GeneralConfig Mapping ---
            else if (settings is GeneralConfig gen)
            {
                n["hudSnapping"] = gen.HudSnapping;
                n["hudSnapRange"] = gen.HudSnapRange;
                n["showHudGrid"] = gen.ShowHudGrid;
                n["menuParticles"] = gen.MenuParticles;
            }

            await SaveJsonRootAsync(root);
        }
    }
}
