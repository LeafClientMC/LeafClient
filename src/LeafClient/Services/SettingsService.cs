using LeafClient.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LeafClient.Services
{
    public class SettingsService
    {
        private static readonly SemaphoreSlim _fileLock = new(1, 1);
        private readonly string _settingsFilePath;

        public SettingsService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appDirectory = Path.Combine(appDataPath, "LeafClient");
            Directory.CreateDirectory(appDirectory);
            _settingsFilePath = Path.Combine(appDirectory, "settings.json");
        }

        public async Task SaveSettingsAsync(LauncherSettings settings)
        {
            await _fileLock.WaitAsync();
            try
            {
                var jsonString = JsonSerializer.Serialize(settings, JsonContext.Default.LauncherSettings);
                await File.WriteAllTextAsync(_settingsFilePath, jsonString);
                Console.WriteLine("Settings saved successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
            finally
            {
                _fileLock.Release();
            }
        }

        public async Task<LauncherSettings> LoadSettingsAsync()
        {
            if (!File.Exists(_settingsFilePath))
            {
                Console.WriteLine("Settings file not found. Loading default settings.");
                return new LauncherSettings();
            }

            await _fileLock.WaitAsync();
            try
            {
                var jsonString = await File.ReadAllTextAsync(_settingsFilePath);
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
            finally
            {
                _fileLock.Release();
            }
        }
    }
}
