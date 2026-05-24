

using LeafClient.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LeafClient;

namespace LeafClient.Services
{
    public class ModCleanupService
    {
        private readonly string _cleanupFilePath;
        private List<ModCleanupEntry> _cleanupList = new List<ModCleanupEntry>();
        private static readonly object _fileLock = new object();

        public ModCleanupService(string minecraftFolder)
        {
            _cleanupFilePath = Path.Combine(minecraftFolder, "mod_cleanup_list.json");
            LoadCleanupListAsync().Wait();
        }

        public async Task LoadCleanupListAsync()
        {
            lock (_fileLock)
            {
                if (File.Exists(_cleanupFilePath))
                {
                    try
                    {
                        var json = File.ReadAllText(_cleanupFilePath);
                        _cleanupList = JsonSerializer.Deserialize(json, JsonContext.Default.ListModCleanupEntry) ?? new List<ModCleanupEntry>();
                        LeafLog.Info("ModCleanupService", $"Loaded {_cleanupList.Count} cleanup entries from {_cleanupFilePath}");
                    }
                    catch (JsonException ex)
                    {
                        LeafLog.Error("ModCleanupService ERROR", $"Failed to deserialize cleanup list: {ex.Message}. Creating new list.");
                        _cleanupList = new List<ModCleanupEntry>();
                        File.Move(_cleanupFilePath, _cleanupFilePath + ".corrupted." + DateTime.Now.ToFileTimeUtc(), true);
                    }
                    catch (Exception ex)
                    {
                        LeafLog.Error("ModCleanupService ERROR", $"Failed to load cleanup list: {ex.Message}. Creating new list.");
                        _cleanupList = new List<ModCleanupEntry>();
                    }
                }
                else
                {
                    _cleanupList = new List<ModCleanupEntry>();
                    LeafLog.Info("ModCleanupService", "No cleanup list file found. Initializing new list.");
                }
            }
        }

        public async Task SaveCleanupListAsync()
        {
            lock (_fileLock)
            {
                try
                {
                    var json = JsonSerializer.Serialize(_cleanupList, JsonContext.Default.ListModCleanupEntry);
                    File.WriteAllText(_cleanupFilePath, json);
                    LeafLog.Info("ModCleanupService", $"Saved {_cleanupList.Count} cleanup entries to {_cleanupFilePath}");
                }
                catch (Exception ex)
                {
                    LeafLog.Error("ModCleanupService ERROR", $"Failed to save cleanup list: {ex.Message}");
                }
            }
        }

        public void AddModToCleanup(InstalledMod mod)
        {
            if (!_cleanupList.Any(e => e.ModId == mod.ModId && e.FileName == mod.FileName && e.MinecraftVersion == mod.MinecraftVersion))
            {
                _cleanupList.Add(new ModCleanupEntry
                {
                    ModId = mod.ModId,
                    FileName = mod.FileName,
                    MinecraftVersion = mod.MinecraftVersion,
                    AddedDate = DateTime.Now
                });
                _ = SaveCleanupListAsync();
                LeafLog.Info("ModCleanupService", $"Added '{mod.Name}' to cleanup list.");
            }
        }

        public void RemoveModFromCleanup(ModCleanupEntry entry)
        {
            _cleanupList.RemoveAll(e => e.ModId == entry.ModId && e.FileName == entry.FileName && e.MinecraftVersion == entry.MinecraftVersion);
            _ = SaveCleanupListAsync();
            LeafLog.Info("ModCleanupService", $"Removed '{entry.FileName}' from cleanup list.");
        }

        public List<ModCleanupEntry> GetCleanupList()
        {
            return new List<ModCleanupEntry>(_cleanupList);
        }
    }
}
