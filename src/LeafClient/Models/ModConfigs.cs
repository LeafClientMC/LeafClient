using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace LeafClient.Models
{
    // Base class implements notification so UI updates instantly
    public class ModConfigBase : INotifyPropertyChanged
    {
        private bool _enabled = false;

        [JsonPropertyName("enabled")]
        public bool Enabled
        {
            get => _enabled;
            set { if (_enabled != value) { _enabled = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class GeneralConfig : ModConfigBase
    {
        private bool _hudSnapping = true;
        private int _hudSnapRange = 4;
        private bool _showHudGrid = false;
        private bool _menuParticles = true;

        [JsonPropertyName("hudSnapping")]
        public bool HudSnapping
        {
            get => _hudSnapping;
            set { if (_hudSnapping != value) { _hudSnapping = value; OnPropertyChanged(); } }
        }

        [JsonPropertyName("hudSnapRange")]
        public int HudSnapRange
        {
            get => _hudSnapRange;
            set { if (_hudSnapRange != value) { _hudSnapRange = value; OnPropertyChanged(); } }
        }

        [JsonPropertyName("showHudGrid")]
        public bool ShowHudGrid
        {
            get => _showHudGrid;
            set { if (_showHudGrid != value) { _showHudGrid = value; OnPropertyChanged(); } }
        }

        [JsonPropertyName("menuParticles")]
        public bool MenuParticles
        {
            get => _menuParticles;
            set { if (_menuParticles != value) { _menuParticles = value; OnPropertyChanged(); } }
        }
    }

    public class PerformanceConfig : ModConfigBase
    {
        [JsonPropertyName("showMemory")]
        public bool ShowMemory { get; set; } = true;
        [JsonPropertyName("showCpu")]
        public bool ShowCpu { get; set; } = true;
    }

    public class KeystrokesConfig : ModConfigBase
    {
        [JsonPropertyName("scale")]
        public double Scale { get; set; } = 1.0;
        [JsonPropertyName("opacity")]
        public double Opacity { get; set; } = 0.9;
        [JsonPropertyName("textColor")]
        public int TextColor { get; set; } = 16777215;
        [JsonPropertyName("backgroundColor")]
        public int BackgroundColor { get; set; } = -1879048192;
        [JsonPropertyName("pressedColor")]
        public int PressedColor { get; set; } = -1862270976;
        [JsonPropertyName("showAnimations")]
        public bool ShowAnimations { get; set; } = true;
        [JsonPropertyName("chromaMode")]
        public bool ChromaMode { get; set; } = false;
        [JsonPropertyName("showBorder")]
        public bool ShowBorder { get; set; } = true;
    }

    public class ItemCounterConfig : ModConfigBase
    {
        [JsonPropertyName("autoDetect")]
        public bool AutoDetect { get; set; } = false;
        [JsonPropertyName("showIcons")]
        public bool ShowIcons { get; set; } = true;
        [JsonPropertyName("showAnimations")]
        public bool ShowAnimations { get; set; } = true;
        [JsonPropertyName("horizontalLayout")]
        public bool HorizontalLayout { get; set; } = true;
        [JsonPropertyName("scale")]
        public double Scale { get; set; } = 1.0;
        [JsonPropertyName("iconSize")]
        public double IconSize { get; set; } = 16.0;
        [JsonPropertyName("spacing")]
        public double Spacing { get; set; } = 4.0;
    }

    public class MinimapConfig : ModConfigBase
    {
        [JsonPropertyName("mapSize")]
        public int MapSize { get; set; } = 256;
        [JsonPropertyName("displaySize")]
        public int DisplaySize { get; set; } = 180;
        [JsonPropertyName("zoom")]
        public double Zoom { get; set; } = 1.0;
        [JsonPropertyName("showWaypoints")]
        public bool ShowWaypoints { get; set; } = true;
        [JsonPropertyName("showCoordinates")]
        public bool ShowCoordinates { get; set; } = true;
    }

    public class ToggleSprintConfig : ModConfigBase
    {
        [JsonPropertyName("flyBoost")]
        public bool FlyBoost { get; set; } = true;
    }

    public class GenericModConfig : ModConfigBase { }
}
