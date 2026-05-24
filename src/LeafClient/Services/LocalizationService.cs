using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace LeafClient.Services
{
    public sealed class LocalizationService : INotifyPropertyChanged
    {
        public static LocalizationService Instance { get; } = new LocalizationService();

        public sealed class LanguageOption
        {
            public string Code { get; }
            public string NativeName { get; }
            public string EnglishName { get; }
            public LanguageOption(string code, string nativeName, string englishName)
            {
                Code = code;
                NativeName = nativeName;
                EnglishName = englishName;
            }
            public override string ToString() => NativeName;
        }

        public static readonly IReadOnlyList<LanguageOption> SupportedLanguages = new[]
        {
            new LanguageOption("en", "English",    "English"),
            new LanguageOption("es", "Español",    "Spanish"),
            new LanguageOption("fr", "Français",   "French"),
            new LanguageOption("de", "Deutsch",    "German"),
            new LanguageOption("pt", "Português",  "Portuguese"),
            new LanguageOption("ru", "Русский",    "Russian"),
            new LanguageOption("zh", "中文",       "Chinese"),
            new LanguageOption("ja", "日本語",     "Japanese"),
            new LanguageOption("it", "Italiano",   "Italian"),
            new LanguageOption("pl", "Polski",     "Polish"),
            new LanguageOption("nl", "Nederlands", "Dutch"),
        };

        private readonly object _lock = new object();
        private Dictionary<string, string> _strings;
        private Dictionary<string, string> _englishFallback;
        private string _activeLocale = "en";

        private LocalizationService()
        {
            _englishFallback = LoadStrings("en");
            _strings = _englishFallback;
        }

        public string ActiveLocale
        {
            get { lock (_lock) return _activeLocale; }
        }

        public string this[string key]
        {
            get
            {
                if (string.IsNullOrEmpty(key)) return string.Empty;
                lock (_lock)
                {
                    if (_strings.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v))
                        return v;
                    if (_englishFallback.TryGetValue(key, out var en) && !string.IsNullOrEmpty(en))
                        return en;
                }
                return key;
            }
        }

        public string T(string key, string fallback)
        {
            if (string.IsNullOrEmpty(key)) return fallback;
            lock (_lock)
            {
                if (_strings.TryGetValue(key, out var v) && !string.IsNullOrEmpty(v))
                    return v;
                if (_englishFallback.TryGetValue(key, out var en) && !string.IsNullOrEmpty(en))
                    return en;
            }
            return fallback;
        }

        public void SetLocale(string locale)
        {
            if (string.IsNullOrWhiteSpace(locale)) return;
            var code = locale.Trim().ToLowerInvariant();
            if (!SupportedLanguages.Any(l => l.Code == code)) code = "en";
            lock (_lock)
            {
                if (_activeLocale == code && _strings.Count > 0) return;
                _activeLocale = code;
                _strings = LoadStrings(code);
                if (_englishFallback.Count == 0)
                    _englishFallback = LoadStrings("en");
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActiveLocale)));
        }

        private static Dictionary<string, string> LoadStrings(string locale)
        {
            string? json = TryReadFromDisk(locale) ?? TryReadFromEmbedded(locale);
            if (string.IsNullOrEmpty(json))
            {
                LeafLog.Info("Localization", $"No strings found for '{locale}' (disk + embedded both missed). Falling back to raw keys.");
                return new Dictionary<string, string>(StringComparer.Ordinal);
            }
            try
            {
                var parsed = JsonSerializer.Deserialize(json, LeafClient.JsonContext.Default.DictionaryStringString);
                return parsed != null
                    ? new Dictionary<string, string>(parsed, StringComparer.Ordinal)
                    : new Dictionary<string, string>(StringComparer.Ordinal);
            }
            catch (Exception ex)
            {
                LeafLog.Info("Localization", $"Parse failure for '{locale}': {ex.Message}");
                return new Dictionary<string, string>(StringComparer.Ordinal);
            }
        }

        private static string? TryReadFromDisk(string locale)
        {
            var baseDir = AppContext.BaseDirectory ?? Environment.CurrentDirectory;
            var candidates = new[]
            {
                Path.Combine(baseDir, "Resources", "Strings", $"{locale}.json"),
                Path.Combine(baseDir, "..", "Resources", "Strings", $"{locale}.json"),
                Path.Combine(baseDir, "..", "Resources", $"{locale}.json"),
            };
            foreach (var path in candidates)
            {
                try
                {
                    if (File.Exists(path)) return File.ReadAllText(path);
                }
                catch { }
            }
            return null;
        }

        private static string? TryReadFromEmbedded(string locale)
        {
            try
            {
                var asm = typeof(LocalizationService).Assembly;
                var resName = $"LeafClient.Resources.Strings.{locale}.json";
                using var s = asm.GetManifestResourceStream(resName);
                if (s == null) return null;
                using var reader = new StreamReader(s);
                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                LeafLog.Error("Localization", $"Embedded read failed for '{locale}': {ex.Message}");
                return null;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
