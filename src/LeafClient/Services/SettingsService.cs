using LeafClient.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeafClient.Services
{
    public class SettingsService
    {
        private readonly string _settingsFilePath;

        // AOT-FIX: Removed the reflection-based JsonSerializerOptions.
        // We will now use the source-generated JsonContext.

        public SettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appDirectory = Path.Combine(appDataPath, "LeafClient");
            Directory.CreateDirectory(appDirectory);
            _settingsFilePath = Path.Combine(appDirectory, "settings.json");
        }

        public async Task SaveSettingsAsync(LauncherSettings settings)
        {
            try
            {
                // AOT-CHANGE: Use the source-generated context for serialization.
                var jsonString = JsonSerializer.Serialize(settings, JsonContext.Default.LauncherSettings);
                await File.WriteAllTextAsync(_settingsFilePath, jsonString);
                Console.WriteLine("Settings saved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public async Task<LauncherSettings> LoadSettingsAsync()
        {
            if (!File.Exists(_settingsFilePath))
            {
                Console.WriteLine("Settings file not found. Loading default settings.");
                return new LauncherSettings();
            }

            try
            {
                var jsonString = await File.ReadAllTextAsync(_settingsFilePath);
                // AOT-CHANGE: Use the source-generated context for deserialization.
                var settings = JsonSerializer.Deserialize(jsonString, JsonContext.Default.LauncherSettings);

                if (settings != null)
                {
                    settings.SelectedAddonByVersion ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }

                Console.WriteLine("Settings loaded successfully.");
                return settings ?? new LauncherSettings();
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error deserializing settings file: {ex.Message}. Loading default settings.");
                return new LauncherSettings();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}. Loading default settings.");
                return new LauncherSettings();
            }
        }
    }
}
