// Services/GameOptionsService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LeafClient.Services
{
    public class GameOptionsService
    {
        private readonly string _optionsPath;
        private readonly Dictionary<string, string> _kv = new(StringComparer.OrdinalIgnoreCase);

        public GameOptionsService(string? customPath = null)
        {
            _optionsPath = string.IsNullOrWhiteSpace(customPath) ? ResolveDefaultOptionsPath() : customPath!;
            Load();
        }

        private static string ResolveDefaultOptionsPath()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    return Path.Combine(appData, ".minecraft", "options.txt");
                }
                if (OperatingSystem.IsMacOS())
                {
                    var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    return Path.Combine(home, "Library", "Application Support", "minecraft", "options.txt");
                }
                // Linux
                var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(homeDir, ".minecraft", "options.txt");
            }
            catch
            {
                // Fallback to current dir
                return Path.Combine(Environment.CurrentDirectory, "options.txt");
            }
        }

        public void Load()
        {
            _kv.Clear();
            try
            {
                var dir = Path.GetDirectoryName(_optionsPath);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (!File.Exists(_optionsPath))
                {
                    File.WriteAllText(_optionsPath, "");
                    return;
                }

                foreach (var line in File.ReadAllLines(_optionsPath))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var idx = line.IndexOf(':');
                    if (idx <= 0) continue;

                    var key = line.Substring(0, idx).Trim();
                    var val = line.Substring(idx + 1).Trim();
                    if (!_kv.ContainsKey(key))
                        _kv.Add(key, val);
                    else
                        _kv[key] = val;
                }
            }
            catch { /* swallow */ }
        }

        public void Save()
        {
            try
            {
                var lines = _kv.Select(kv => $"{kv.Key}:{kv.Value}").ToArray();
                File.WriteAllLines(_optionsPath, lines);
            }
            catch { /* swallow */ }
        }

        public void SetRawValue(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            _kv[key] = value;
        }

        public string? GetRawValue(string key)
        {
            return _kv.TryGetValue(key, out var v) ? v : null;
        }

        public void SetBool(string key, bool value) => SetRawValue(key, value ? "true" : "false");
        public void SetInt(string key, int value) => SetRawValue(key, value.ToString());
        public void SetFloat(string key, double value) => SetRawValue(key, value.ToString("0.##"));
        public void SetEnum(string key, string value) => SetRawValue(key, value);
    }
}
