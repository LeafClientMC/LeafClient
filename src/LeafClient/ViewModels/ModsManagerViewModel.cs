using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Threading;
using LeafClient.Models;
using LeafClient.Services;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LeafClient.ViewModels
{
    public class ModsManagerViewModel : ViewModelBase
    {
        private ObservableCollection<ModItem> _allMods;
        private ObservableCollection<ModItem> _filteredMods;
        private string _searchText;
        private readonly ModSettingsService _settingsService;

        // --- GENERAL SETTINGS ---
        private GeneralConfig _generalSettings = new GeneralConfig();

        public GeneralConfig GeneralSettings
        {
            get => _generalSettings;
            set { if (_generalSettings != value) { _generalSettings = value; OnPropertyChanged(); } }
        }

        public ICommand SaveGeneralSettingsCommand { get; }

        // --- OPTIONS MODAL LOGIC ---
        private bool _isOptionsOpen = false;
        private string _selectedModName = "";
        private object _currentModSettings;

        public bool IsOptionsOpen
        {
            get => _isOptionsOpen;
            set { if (_isOptionsOpen != value) { _isOptionsOpen = value; OnPropertyChanged(); } }
        }

        public string SelectedModName
        {
            get => _selectedModName;
            set { if (_selectedModName != value) { _selectedModName = value; OnPropertyChanged(); } }
        }

        public object CurrentModSettings
        {
            get => _currentModSettings;
            set { if (_currentModSettings != value) { _currentModSettings = value; OnPropertyChanged(); } }
        }

        // --- WAYPOINTS LOGIC ---
        private readonly WaypointsService _waypointsService;
        private ObservableCollection<Waypoint> _allWaypoints;
        private ObservableCollection<Waypoint> _filteredWaypoints;
        private string _waypointSearchText = "";
        private string _selectedDimensionFilter = "ALL";
        private bool _isAddWaypointModalVisible = false;

        // Server & Player Context
        private string _currentServer = "127.0.0.1";
        private int _playerX = 0;
        private int _playerY = 64;
        private int _playerZ = 0;

        private DispatcherTimer _stateTimer;

        // Add Modal Fields
        private string _newWpName = "";
        private string _newWpX = "0";
        private string _newWpY = "64";
        private string _newWpZ = "0";
        private string _newWpDimension = "minecraft:overworld";
        private int _newWpColor = 0x3B82F6; // Default Blue

        // Display Options
        private bool _newWpShowText = true;
        private bool _newWpHighlightBlock = true;
        private bool _newWpShowBeam = true;
        private bool _newWpShowDistance = true;

        // Color Palette
        private readonly int[] _colorPalette = new[]
        {
            0x3B82F6, 0xEF4444, 0x10B981, 0xF59E0B,
            0x8B5CF6, 0xEC4899, 0x6366F1, 0x14B8A6
        };
        private int _colorIndex = 0;

        // --- Properties ---
        public ObservableCollection<Waypoint> FilteredWaypoints
        {
            get => _filteredWaypoints;
            set { if (_filteredWaypoints != value) { _filteredWaypoints = value; OnPropertyChanged(); } }
        }

        public string WaypointSearchText
        {
            get => _waypointSearchText;
            set { if (_waypointSearchText != value) { _waypointSearchText = value; OnPropertyChanged(); FilterWaypoints(); } }
        }

        public string SelectedDimensionFilter
        {
            get => _selectedDimensionFilter;
            set { if (_selectedDimensionFilter != value) { _selectedDimensionFilter = value; OnPropertyChanged(); FilterWaypoints(); } }
        }

        public bool IsAddWaypointModalVisible
        {
            get => _isAddWaypointModalVisible;
            set { if (_isAddWaypointModalVisible != value) { _isAddWaypointModalVisible = value; OnPropertyChanged(); } }
        }

        // Add Modal Bindings
        public string NewWpName { get => _newWpName; set { _newWpName = value; OnPropertyChanged(); } }
        public string NewWpX { get => _newWpX; set { _newWpX = value; OnPropertyChanged(); } }
        public string NewWpY { get => _newWpY; set { _newWpY = value; OnPropertyChanged(); } }
        public string NewWpZ { get => _newWpZ; set { _newWpZ = value; OnPropertyChanged(); } }
        public string NewWpDimension { get => _newWpDimension; set { _newWpDimension = value; OnPropertyChanged(); } }
        public int NewWpColor { get => _newWpColor; set { _newWpColor = value; OnPropertyChanged(); } }

        public bool NewWpShowText { get => _newWpShowText; set { _newWpShowText = value; OnPropertyChanged(); } }
        public bool NewWpHighlightBlock { get => _newWpHighlightBlock; set { _newWpHighlightBlock = value; OnPropertyChanged(); } }
        public bool NewWpShowBeam { get => _newWpShowBeam; set { _newWpShowBeam = value; OnPropertyChanged(); } }
        public bool NewWpShowDistance { get => _newWpShowDistance; set { _newWpShowDistance = value; OnPropertyChanged(); } }

        public ObservableCollection<string> AvailableDimensions { get; } = new()
        {
            "minecraft:overworld",
            "minecraft:the_nether",
            "minecraft:the_end"
        };

        // Commands
        public ICommand OpenAddModalCommand { get; }
        public ICommand CloseAddModalCommand { get; }
        public ICommand SaveNewWaypointCommand { get; }
        public ICommand DeleteWaypointCommand { get; }
        public ICommand SetDimensionFilterCommand { get; }
        public ICommand ChangeColorCommand { get; }
        public ICommand SetToCurrentCoordsCommand { get; }
        public ICommand OpenOptionsCommand { get; }
        public ICommand CloseOptionsCommand { get; }
        public ICommand SaveOptionsCommand { get; }

        // --- MODS LOGIC ---
        public ObservableCollection<ModItem> FilteredMods
        {
            get => _filteredMods;
            set { if (_filteredMods != value) { _filteredMods = value; OnPropertyChanged(); } }
        }

        public string SearchText
        {
            get => _searchText;
            set { if (_searchText != value) { _searchText = value; OnPropertyChanged(); FilterMods(); } }
        }

        public ModsManagerViewModel()
        {
            _settingsService = new ModSettingsService();
            _waypointsService = new WaypointsService();

            // Load General Settings
            _ = LoadGeneralSettingsAsync();

            SaveGeneralSettingsCommand = new RelayCommand(async () =>
            {
                await _settingsService.SaveModSettingsAsync("General", GeneralSettings);
                // FIX: Notify UI that GeneralSettings might have changed (to refresh bindings like IsVisible)
                OnPropertyChanged(nameof(GeneralSettings));
            });

            // Initialize Mods
            _allMods = new ObservableCollection<ModItem>();
            var modList = new[] {
                new { Name = "Armor HUD", Icon = "🛡️" }, new { Name = "Chat Macros", Icon = "💬" },
                new { Name = "Coordinates", Icon = "📍" }, new { Name = "CPS", Icon = "🖱️" },
                new { Name = "Crosshair", Icon = "➕" }, new { Name = "FPS", Icon = "🎞️" },
                new { Name = "Freelook", Icon = "👀" }, new { Name = "FullBright", Icon = "☀️" },
                new { Name = "HUD Themes", Icon = "🎨" }, new { Name = "Item Counter", Icon = "🔢" },
                new { Name = "Keystrokes", Icon = "⌨️" }, new { Name = "Minimap", Icon = "🗺️" },
                new { Name = "Performance", Icon = "🚀" }, new { Name = "Ping", Icon = "📶" },
                new { Name = "Server Info", Icon = "ℹ️" }, new { Name = "Toggle Sprint", Icon = "🏃" },
                new { Name = "Waypoints", Icon = "🚩" }, new { Name = "Zoom", Icon = "🔍" }
            };

            foreach (var m in modList)
                _allMods.Add(new ModItem(m.Name, m.Icon, false, OnModStateChanged));
            FilteredMods = new ObservableCollection<ModItem>(_allMods);

            _allWaypoints = new ObservableCollection<Waypoint>();
            FilteredWaypoints = new ObservableCollection<Waypoint>();

            CheckServerState(null, null);

            _stateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _stateTimer.Tick += CheckServerState;
            _stateTimer.Start();

            // --- COMMANDS ---
            OpenAddModalCommand = new RelayCommand(() =>
            {
                NewWpX = _playerX.ToString();
                NewWpY = _playerY.ToString();
                NewWpZ = _playerZ.ToString();
                NewWpName = "";
                _colorIndex = 0;
                NewWpColor = _colorPalette[0];
                IsAddWaypointModalVisible = true;
            });

            CloseAddModalCommand = new RelayCommand(() => { IsAddWaypointModalVisible = false; });

            ChangeColorCommand = new RelayCommand(() =>
            {
                _colorIndex = (_colorIndex + 1) % _colorPalette.Length;
                NewWpColor = _colorPalette[_colorIndex];
            });

            SetToCurrentCoordsCommand = new RelayCommand(() =>
            {
                NewWpX = _playerX.ToString();
                NewWpY = _playerY.ToString();
                NewWpZ = _playerZ.ToString();
            });

            SaveNewWaypointCommand = new RelayCommand(async () =>
            {
                int.TryParse(NewWpX, out int x);
                int.TryParse(NewWpY, out int y);
                int.TryParse(NewWpZ, out int z);

                var wp = new Waypoint
                {
                    Name = NewWpName,
                    X = x,
                    Y = y,
                    Z = z,
                    Server = _currentServer,
                    Dimension = NewWpDimension,
                    Color = NewWpColor,
                    ShowText = NewWpShowText,
                    HighlightBlock = NewWpHighlightBlock,
                    ShowBeam = NewWpShowBeam,
                    ShowDistance = NewWpShowDistance
                };

                _allWaypoints.Add(wp);
                await _waypointsService.SaveWaypointsAsync(_allWaypoints.ToList());
                FilterWaypoints();
                IsAddWaypointModalVisible = false;
            });

            DeleteWaypointCommand = new RelayCommand<Waypoint>(async (wp) =>
            {
                if (wp == null) return;
                _allWaypoints.Remove(wp);
                await _waypointsService.SaveWaypointsAsync(_allWaypoints.ToList());
                FilterWaypoints();
            });

            SetDimensionFilterCommand = new RelayCommand<string>((dim) =>
            {
                if (dim != null) SelectedDimensionFilter = dim;
            });

            // Options Logic
            OpenOptionsCommand = new RelayCommand<ModItem>(async (mod) =>
            {
                if (mod == null) return;
                SelectedModName = mod.Name;

                switch (mod.Name)
                {
                    case "Performance": CurrentModSettings = await _settingsService.GetModSettingsAsync<PerformanceConfig>(mod.Name); break;
                    case "Keystrokes": CurrentModSettings = await _settingsService.GetModSettingsAsync<KeystrokesConfig>(mod.Name); break;
                    case "Item Counter": CurrentModSettings = await _settingsService.GetModSettingsAsync<ItemCounterConfig>(mod.Name); break;
                    case "Minimap": CurrentModSettings = await _settingsService.GetModSettingsAsync<MinimapConfig>(mod.Name); break;
                    case "Toggle Sprint": CurrentModSettings = await _settingsService.GetModSettingsAsync<ToggleSprintConfig>(mod.Name); break;
                    default: CurrentModSettings = await _settingsService.GetModSettingsAsync<GenericModConfig>(mod.Name); break;
                }
                IsOptionsOpen = true;
            });

            CloseOptionsCommand = new RelayCommand(() => IsOptionsOpen = false);

            SaveOptionsCommand = new RelayCommand(async () =>
            {
                if (CurrentModSettings == null) return;
                if (CurrentModSettings is PerformanceConfig perf) await _settingsService.SaveModSettingsAsync(SelectedModName, perf);
                else if (CurrentModSettings is KeystrokesConfig key) await _settingsService.SaveModSettingsAsync(SelectedModName, key);
                else if (CurrentModSettings is ItemCounterConfig item) await _settingsService.SaveModSettingsAsync(SelectedModName, item);
                else if (CurrentModSettings is MinimapConfig mini) await _settingsService.SaveModSettingsAsync(SelectedModName, mini);
                else if (CurrentModSettings is ToggleSprintConfig sprint) await _settingsService.SaveModSettingsAsync(SelectedModName, sprint);
                else if (CurrentModSettings is GenericModConfig gen) await _settingsService.SaveModSettingsAsync(SelectedModName, gen);
                IsOptionsOpen = false;
            });

            _ = LoadModStatesAsync();
            _ = LoadWaypointsAsync();
        }

        private async Task LoadGeneralSettingsAsync()
        {
            GeneralSettings = await _settingsService.GetModSettingsAsync<GeneralConfig>("General");
        }

        private void CheckServerState(object? sender, EventArgs e)
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LeafClient", "leaf_client_state.json");
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        var root = doc.RootElement;
                        if (root.TryGetProperty("server", out var serverElem))
                        {
                            string server = serverElem.GetString();
                            if (!string.IsNullOrEmpty(server) && server != _currentServer)
                            {
                                _currentServer = server;
                                FilterWaypoints();
                            }
                        }
                        if (root.TryGetProperty("x", out var xEl)) _playerX = xEl.GetInt32();
                        if (root.TryGetProperty("y", out var yEl)) _playerY = yEl.GetInt32();
                        if (root.TryGetProperty("z", out var zEl)) _playerZ = zEl.GetInt32();
                    }
                }
            }
            catch { }
        }

        private async Task LoadModStatesAsync()
        {
            await Task.Delay(50);
            foreach (var mod in _allMods)
            {
                bool isEnabled = await _settingsService.GetModStateAsync(mod.Name);
                mod.SetIsEnabledSilent(isEnabled);
            }
        }

        private async void OnModStateChanged(ModItem mod)
        {
            await _settingsService.SaveModStateAsync(mod.Name, mod.IsEnabled);
        }

        private void FilterMods()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                FilteredMods = new ObservableCollection<ModItem>(_allMods);
            else
                FilteredMods = new ObservableCollection<ModItem>(_allMods.Where(m => m.Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private async Task LoadWaypointsAsync()
        {
            var list = await _waypointsService.LoadWaypointsAsync();
            _allWaypoints = new ObservableCollection<Waypoint>(list);
            FilterWaypoints();
        }

        private void FilterWaypoints()
        {
            var query = _allWaypoints.AsEnumerable();
            query = query.Where(w => string.Equals(w.Server, _currentServer, StringComparison.OrdinalIgnoreCase));
            if (SelectedDimensionFilter != "ALL") query = query.Where(w => w.Dimension == SelectedDimensionFilter);
            if (!string.IsNullOrWhiteSpace(WaypointSearchText)) query = query.Where(w => w.Name.Contains(WaypointSearchText, StringComparison.OrdinalIgnoreCase));
            FilteredWaypoints = new ObservableCollection<Waypoint>(query);
        }
    }

    public class ModItem : ViewModelBase
    {
        private bool _isEnabled;
        private readonly Action<ModItem>? _onStateChangedCallback;
        public string Name { get; set; }
        public string Icon { get; set; }
        public ICommand ToggleCommand { get; }
        public bool IsEnabled { get => _isEnabled; set { if (_isEnabled != value) { _isEnabled = value; OnPropertyChanged(); _onStateChangedCallback?.Invoke(this); } } }
        public ModItem(string name, string icon, bool isEnabled, Action<ModItem> onStateChanged) { Name = name; Icon = icon; _isEnabled = isEnabled; _onStateChangedCallback = onStateChanged; ToggleCommand = new RelayCommand(Toggle); }
        private void Toggle() { IsEnabled = !IsEnabled; }
        public void SetIsEnabledSilent(bool value) { if (_isEnabled != value) { _isEnabled = value; OnPropertyChanged(nameof(IsEnabled)); } }
    }

    public class BoolToColorConverter : IValueConverter
    {
        public static readonly BoolToColorConverter Instance = new();
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => (value is bool b && b) ? Brush.Parse("#10B981") : Brush.Parse("#EF4444");
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BoolToTextConverter : IValueConverter
    {
        public static readonly BoolToTextConverter Instance = new();
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => (value is bool b && b) ? "ENABLED" : "DISABLED";
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class IntStringColorConverter : IValueConverter
    {
        public static readonly IntStringColorConverter Instance = new();
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int colorInt) return new SolidColorBrush(Color.FromUInt32((uint)(0xFF000000 | colorInt)));
            if (value is string strVal && int.TryParse(strVal, out int c)) return new SolidColorBrush(Color.FromUInt32((uint)(0xFF000000 | c)));
            return Brushes.White;
        }
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute; private readonly Func<bool>? _canExecute;
        public RelayCommand(Action execute, Func<bool>? canExecute = null) { _execute = execute; _canExecute = canExecute; }
        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute();
        public void Execute(object? parameter) => _execute();
        public event EventHandler? CanExecuteChanged;
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute; private readonly Predicate<T>? _canExecute;
        public RelayCommand(Action<T> execute, Predicate<T>? canExecute = null) { _execute = execute; _canExecute = canExecute; }
        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute((T)parameter!);
        public void Execute(object? parameter) => _execute((T)parameter!);
        public event EventHandler? CanExecuteChanged;
    }
}
