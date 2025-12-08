// File: LeafClient/Services/ModCleanupService.cs

using LeafClient.Models; // Ensure this using statement is present
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeafClient.Services
{
    public class ModCleanupService
    {
        private readonly string _cleanupFilePath;
        private List<ModCleanupEntry> _cleanupList = new List<ModCleanupEntry>();
        private static readonly object _fileLock = new object(); // For thread-safe file access

        public ModCleanupService(string minecraftFolder)
        {
            _cleanupFilePath = Path.Combine(minecraftFolder, "mod_cleanup_list.json");
            // Load cleanup list synchronously during initialization to ensure it's ready
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
                        _cleanupList = JsonSerializer.Deserialize<List<ModCleanupEntry>>(json, Json.Options) ?? new List<ModCleanupEntry>();
                        Console.WriteLine($"[ModCleanupService] Loaded {_cleanupList.Count} cleanup entries from {_cleanupFilePath}");
                    }
                    catch (JsonException ex)
                    {
                        Console.Error.WriteLine($"[ModCleanupService ERROR] Failed to deserialize cleanup list: {ex.Message}. Creating new list.");
                        _cleanupList = new List<ModCleanupEntry>();
                        // Optionally, backup the corrupted file for debugging
                        File.Move(_cleanupFilePath, _cleanupFilePath + ".corrupted." + DateTime.Now.ToFileTimeUtc(), true);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[ModCleanupService ERROR] Failed to load cleanup list: {ex.Message}. Creating new list.");
                        _cleanupList = new List<ModCleanupEntry>();
                    }
                }
                else
                {
                    _cleanupList = new List<ModCleanupEntry>();
                    Console.WriteLine("[ModCleanupService] No cleanup list file found. Initializing new list.");
                }
            }
        }

        public async Task SaveCleanupListAsync()
        {
            lock (_fileLock)
            {
                try
                {
                    var json = JsonSerializer.Serialize(_cleanupList, Json.Options);
                    File.WriteAllText(_cleanupFilePath, json);
                    Console.WriteLine($"[ModCleanupService] Saved {_cleanupList.Count} cleanup entries to {_cleanupFilePath}");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ModCleanupService ERROR] Failed to save cleanup list: {ex.Message}");
                }
            }
        }

        public void AddModToCleanup(InstalledMod mod)
        {
            // Only add if it's not already in the cleanup list
            if (!_cleanupList.Any(e => e.ModId == mod.ModId && e.FileName == mod.FileName && e.MinecraftVersion == mod.MinecraftVersion))
            {
                _cleanupList.Add(new ModCleanupEntry
                {
                    ModId = mod.ModId,
                    FileName = mod.FileName,
                    MinecraftVersion = mod.MinecraftVersion,
                    AddedDate = DateTime.Now
                });
                _ = SaveCleanupListAsync(); // Fire and forget save
                Console.WriteLine($"[ModCleanupService] Added '{mod.Name}' to cleanup list.");
            }
        }

        public void RemoveModFromCleanup(ModCleanupEntry entry)
        {
            _cleanupList.RemoveAll(e => e.ModId == entry.ModId && e.FileName == entry.FileName && e.MinecraftVersion == entry.MinecraftVersion);
            _ = SaveCleanupListAsync(); // Fire and forget save
            Console.WriteLine($"[ModCleanupService] Removed '{entry.FileName}' from cleanup list.");
        }

        public List<ModCleanupEntry> GetCleanupList()
        {
            return new List<ModCleanupEntry>(_cleanupList); // Return a copy to prevent external modification
        }
    }
}
