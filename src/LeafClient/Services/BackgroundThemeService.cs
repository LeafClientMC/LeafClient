using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace LeafClient.Services
{
    public sealed class BackgroundThemeService
    {
        public sealed class Theme
        {
            public string Slug { get; init; } = "";
            public string DisplayName { get; init; } = "";
            public string AssetPath { get; init; } = "";
        }

        public static readonly Theme[] Themes = new[]
        {
            new Theme { Slug = "aurora", DisplayName = "Aurora", AssetPath = "avares://LeafClient/Assets/bg.jpg" },
            new Theme { Slug = "forest", DisplayName = "Forest", AssetPath = "avares://LeafClient/Assets/bg2.png" },
            new Theme { Slug = "twilight", DisplayName = "Twilight", AssetPath = "avares://LeafClient/Assets/bg3.png" },
        };

        public static BackgroundThemeService Instance { get; } = new();

        public event EventHandler<Theme>? ThemeChanged;

        public Theme CurrentTheme { get; private set; } = Themes[0];

        private readonly Dictionary<string, Bitmap> _cache = new();

        public Theme GetThemeOrDefault(string? slug)
        {
            if (string.IsNullOrEmpty(slug)) return Themes[0];
            return Themes.FirstOrDefault(t => string.Equals(t.Slug, slug, StringComparison.OrdinalIgnoreCase)) ?? Themes[0];
        }

        public Bitmap? GetBitmap(string slug)
        {
            var theme = GetThemeOrDefault(slug);
            if (_cache.TryGetValue(theme.Slug, out var existing)) return existing;
            try
            {
                using var stream = AssetLoader.Open(new Uri(theme.AssetPath));
                var bmp = new Bitmap(stream);
                _cache[theme.Slug] = bmp;
                return bmp;
            }
            catch (Exception ex)
            {
                LeafLog.Error("BgTheme", $"Failed to load {theme.AssetPath}: {ex.Message}");
                return null;
            }
        }

        public Bitmap? GetCurrentBitmap() => GetBitmap(CurrentTheme.Slug);

        public void SetTheme(string slug)
        {
            var t = GetThemeOrDefault(slug);
            if (ReferenceEquals(t, CurrentTheme)) return;
            CurrentTheme = t;
            ThemeChanged?.Invoke(this, t);
        }
    }
}
