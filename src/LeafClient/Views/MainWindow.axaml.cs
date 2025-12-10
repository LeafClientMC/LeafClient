using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.Files;
using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ProcessBuilder;
using CmlLib.Core.VersionLoader;
using LeafClient;
using LeafClient.Models;
using LeafClient.PrivateServices;
using LeafClient.Services;
using LeafClient.ViewModels;
using Microsoft.Extensions.Logging;
using Mojang;
using MojangAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO; // Explicitly use System.IO for Path, Directory, File, etc.
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using XboxAuthNet.Game.Msal;
using static LeafClient.Models.LauncherSettings;
using Avalonia.Controls.Templates;
using System.Runtime.InteropServices; // Required for DataTemplate

namespace LeafClient.Views
{
    public partial class MainWindow : Window
    {

        #region Defs/Vars

        private Border? _selectionIndicator;
        private Border? _settingsIndicator;
        private int _currentSelectedIndex = 0;
        // Launch background zoom
        private Image? _launchBgImage;
        private ScaleTransform? _launchBgScale;
        private CancellationTokenSource? _launchBgCts;
        private CancellationTokenSource? _newsItem1Cts;
        private CancellationTokenSource? _newsItem2Cts;
        private CancellationTokenSource? _zoomCts;
        // Particle layer
        private Canvas? _particleLayer;
        private Border? _launchSection;
        private CancellationTokenSource? _particleCts;
        private readonly Random _rand = new();
        private readonly List<Particle> _particles = new();
        // Account panel
        private Grid? _accountPanelOverlay;
        private Border? _accountPanel;
        private TextBlock? _accountUsernameDisplay;
        private TextBlock? _accountUuidDisplay;
        private TextBlock? _playingAsUsername;
        // Friends
        private StackPanel? _friendsLoadingPanel;
        private TextBlock? _noFriendsMessage;
        // News banners
        private Image? _newsItem1Image;
        private ScaleTransform? _newsItem1Scale;
        private Image? _newsItem2Image;
        private ScaleTransform? _newsItem2Scale;
        // Navigation pages
        private Grid? _gamePage;
        private Grid? _versionsPage;
        private Grid? _serversPage;
        private Grid? _modsPage;
        private Grid? _settingsPage;
        // Version details
        private Border? _versionDetailsSidebar;
        private TextBlock? _versionTitle;
        private TextBlock? _versionType;
        private TextBlock? _versionLoader;
        private TextBlock? _versionDate;
        private TextBlock? _versionDescription;
        private ComboBox? _versionDropdown;
        private Border? _versionBannerContainer;
        private StackPanel? _majorVersionsStackPanel;
        private TextBlock? _launchVersionText;
        // Services and Data
        private SettingsService _settingsService = new SettingsService();
        private SessionService _sessionService = new SessionService();
        private SuggestionsService? _suggestionsService;
        private LauncherSettings _currentSettings = new LauncherSettings();
        private List<VersionInfo> _allVersions = new List<VersionInfo>();
        // Settings Controls
        private ToggleSwitch? _launchOnStartupToggle;
        private ToggleSwitch? _minimizeToTrayToggle;
        private ToggleSwitch? _discordRichPresenceToggle;
        private TextBox? _minRamAllocationTextBox;
        private ComboBox? _maxRamAllocationComboBox;
        private TextBox? _quickJoinServerAddressTextBox;
        private TextBox? _quickJoinServerPortTextBox;
        private ToggleSwitch? _quickLaunchEnabledToggle;
        private Slider? _mouseSensitivitySlider;
        private TextBlock? _mouseSensitivityValueText;
        private Slider? _scrollSensitivitySlider;
        private TextBlock? _scrollSensitivityValueText;
        private ToggleSwitch? _autoJumpToggle;
        private ToggleSwitch? _touchscreenToggle;
        private ToggleSwitch? _toggleSprintToggle;
        private ToggleSwitch? _toggleCrouchToggle;
        private ToggleSwitch? _subtitlesToggle;
        private Slider? _renderDistanceSlider;
        private TextBlock? _renderDistanceValueText;
        private Slider? _simulationDistanceSlider;
        private TextBlock? _simulationDistanceValueText;
        private Slider? _entityDistanceSlider;
        private TextBlock? _entityDistanceValueText;
        private Slider? _maxFpsSlider;
        private TextBlock? _maxFpsValueText;
        private ToggleSwitch? _vSyncToggle;
        private ToggleSwitch? _fullscreenToggle;
        private ToggleSwitch? _entityShadowsToggle;
        private ToggleSwitch? _highContrastToggle;
        private ComboBox? _renderCloudsComboBox;
        private ToggleSwitch? _playerHatToggle;
        private ToggleSwitch? _playerCapeToggle;
        private ToggleSwitch? _playerJacketToggle;
        private ToggleSwitch? _playerLeftSleeveToggle;
        private ToggleSwitch? _playerRightSleeveToggle;
        private ToggleSwitch? _playerLeftPantToggle;
        private ToggleSwitch? _playerRightPantToggle;
        private ComboBox? _playerMainHandComboBox;
        private ComboBox? _themeComboBox;
        private ToggleSwitch? _animationsEnabledToggle;
        // Logout flag
        private bool _isLoggingOut = false;
        // Skin render
        private readonly SkinRenderService? _skinRenderService;
        // Game Options
        private GameOptionsService _optionsService = new GameOptionsService();
        // Character preview images
        private Image? _playingAsImage;
        private Image? _accountCharacterImage;
        private readonly DiscordRichPresenceService _drp = new DiscordRichPresenceService();
        private const string DiscordClientId = "1440389324528156908";
        private DateTime _drpSessionStart = DateTime.UtcNow;
        private string? _currentUsername;
        private bool _loggedIn;
        private string? _lastSmallPose;

        private MSession? _session;
        private MinecraftLauncher? _launcher;
        private Process? _gameProcess;
        private bool _isLaunching = false;
        private readonly string _minecraftFolder = GetMinecraftPath();

        private static string GetMinecraftPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Application Support", "minecraft");
            }
            else
            {
                return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".minecraft");
            }
        }


        // Progress bar for launch operations
        private StackPanel? _launchProgressPanel;
        private ProgressBar? _launchProgressBar;
        private TextBlock? _launchProgressText;

        // Versions Addon Buttons
        private Button? _addonFabricButton;
        private Button? _addonVanillaButton;
        private TextBlock? _versionOptiFineSupport;

        // Sidebar Tooltip Controls
        private Border? _sidebarHoverTooltip;
        private TextBlock? _sidebarHoverTooltipText;
        private CancellationTokenSource? _tooltipHideCts;
        private IList<ITransition>? _savedTooltipTransitions;
        private bool _tooltipHasShown = false;
        private int? _currentHoverIndex;
        private const double TooltipLeft = 86;
        private const double GapRightOfSidebar = 6;    // small gap between sidebar and tooltip
        private const double TooltipHeight = 26;
        private const double SidebarWidth = 90;

        // Skins Page
        private Grid? _skinsPage;
        private WrapPanel? _skinsWrapPanel;
        private Border? _noSkinsMessage;
        private Border? _currentlySelectedSkinCard;

        private Border? _skinStatusBanner;
        private TextBlock? _skinStatusBannerText;
        private Button? _skinStatusBannerButton;
        private bool _isProgrammaticallySelectingSkin = false;

        // Exit Handler
        private TrayIcon? _trayIcon;
        private bool _isExitingApp = false;

        // Mods Page
        private TextBlock? _modsInfoText;

        // Installs check
        private readonly SemaphoreSlim _installGate = new(1, 1);
        private volatile bool _isInstalling = false;
        private string _currentOperationText = "LAUNCH GAME"; // New field to track current state for button text
        private string _currentOperationColor = "SeaGreen"; // New field to track current state for button color
        private CancellationTokenSource? _launchCancellationTokenSource;

        private Border? _launchErrorBanner;
        private TextBlock? _launchErrorBannerText;
        private Button? _launchErrorBannerCloseButton;

        private Border? _settingsSaveBanner;
        private Button? _settingsSaveBannerSaveButton;
        private bool _isApplyingSettings = false;
        private bool _settingsDirty = false;

        // Quick Play Tooltip Controls
        private Border? _quickPlayTooltip;
        private TextBlock? _quickPlayTooltipText;
        private CancellationTokenSource? _quickPlayTooltipHideCts;
        private IList<ITransition>? _savedQuickPlayTooltipTransitions;
        private bool _quickPlayTooltipHasShown = false;

        // Server Status and Quick Play
        private MinecraftServerChecker _serverChecker = new MinecraftServerChecker();
        private DispatcherTimer? _serverStatusRefreshTimer;

        // UI elements for Servers Page
        private StackPanel? _serversWrapPanel;
        private Border? _noServersMessage;
        private StackPanel? _quickPlayServersContainer;

        private Border? _promoBanner;
        private Grid? _newsSectionGrid;

        private IList<ITransition>? _savedSelectionIndicatorTransitions;
        private IList<ITransition>? _savedSettingsIndicatorTransitions;
        private IList<ITransition>? _savedPromoBannerTransitions;
        private IList<ITransition>? _savedLaunchSectionTransitions;
        private IList<ITransition>? _savedNewsSectionGridTransitions;
        private IList<ITransition>? _savedLaunchErrorBannerTransitions;
        private IList<ITransition>? _savedSettingsSaveBannerTransitions;
        private IList<ITransition>? _savedAccountPanelTransitions;

        // For sidebar buttons (Border.SidebarIcon style)
        private Border? _gameButton;
        private Border? _versionsButton;
        private Border? _serversButton;
        private Border? _modsButton;
        private Border? _settingsButton;
        private IList<ITransition>? _savedGameButtonTransitions;
        private IList<ITransition>? _savedVersionsButtonTransitions;
        private IList<ITransition>? _savedServersButtonTransitions;
        private IList<ITransition>? _savedModsButtonTransitions;
        private IList<ITransition>? _savedSettingsButtonTransitions;

        private Button? _jvmArgumentsEditButton;

        private Grid? _aboutLeafClientOverlay;
        private Border? _aboutLeafClientPanel;

        private string _logFolderPath = "";
        private string _logFilePath = "";
        private static StreamWriter? _logStreamWriter;
        private static TextWriter? _originalConsoleOut; // To store the original Console.Out
        private static TextWriter? _originalConsoleError; // To store the original Console.Error

        private Grid? _commonQuestionsOverlay;
        private Border? _commonQuestionsPanel;

        private static readonly HttpClient _httpClient = new HttpClient();

        private Version GetCurrentAppVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version ?? new Version("0.0.0.0");
        }

        private MojangApiService? _mojangApiService;

        private ToggleSwitch? _lithiumToggle;
        private ToggleSwitch? _optiFineToggle;
        private ToggleSwitch? _sodiumToggle;


        private Grid? _modBrowserOverlay;
        private Border? _modBrowserPanel;
        private WrapPanel? _modsResultsPanel;
        private StackPanel? _modsLoadingPanel;
        private StackPanel? _modsEmptyPanel;
        private TextBox? _modSearchBox;
        private Border? _modDetailsSidebar;
        private Image? _modDetailsIcon;
        private TextBlock? _modDetailsTitle;
        private TextBlock? _modDetailsDescription;
        private TextBlock? _modDetailsStats;
        private Button? _modDetailsDownloadButton;
        private ModrinthProject? _selectedMod;
        private StackPanel? _userModsPanel;
        private Border? _noUserModsMessage;
        private CancellationTokenSource? _modSearchCts;
        private readonly HttpClient _modrinthClient = new HttpClient();

        private OnlineCountService? _onlineCountService;
        private TextBlock? _onlineCountTextBlock;
        private Ellipse? _onlineStatusDot;

        private DispatcherTimer? _networkMonitorTimer;

        private Border? _gameStartingBanner;
        private TextBlock? _gameStartingBannerText;
        private Button? _gameStartingBannerCloseButton;
        private CancellationTokenSource? _gameStartingBannerCts;

        private bool _gameStartingBannerShownForCurrentLaunch = false;

        private Border? _launchFailureBanner;
        private TextBlock? _launchFailureBannerText;
        private Button? _launchFailureBannerCloseButton;
        private CancellationTokenSource? _launchFailureBannerCts;
        private bool _launchFailureBannerShownForCurrentLaunch = false;

        // Add these fields near your other banner/overlay CTS fields
        private bool _isAboutLeafClientAnimating = false;
        private bool _isCommonQuestionsAnimating = false;

        // Game Resolution
        private TextBox? _gameResolutionWidthTextBox;
        private TextBox? _gameResolutionHeightTextBox;
        private Button? _selectResolutionPresetButton;
        private Button? _visualiseResolutionButton;
        private ToggleSwitch? _useCustomGameResolutionToggle;
        private ToggleSwitch? _lockGameAspectRatioToggle;

        // Launcher Visibility on Game Launch
        private RadioButton? _launcherVisibilityKeepOpenRadio;
        private RadioButton? _launcherVisibilityHideRadio;

        // Game Update Delivery
        private RadioButton? _updateDeliveryNormalRadio;
        private RadioButton? _updateDeliveryEarlyRadio;
        private RadioButton? _updateDeliveryLateRadio;

        // Discord Rich Presence Username Visibility
        private ToggleSwitch? _showUsernameInDiscordRichPresenceToggle;

        // Closing Notifications, Update Notifications, New Content Indicators
        private RadioButton? _closingNotificationsAlwaysRadio;
        private RadioButton? _closingNotificationsJustOnceRadio;
        private RadioButton? _closingNotificationsNeverRadio;
        private ToggleSwitch? _enableUpdateNotificationsToggle;
        private ToggleSwitch? _enableNewContentIndicatorsToggle;

        // Directory Overview, Logs, Clear Cache
        private Border? _logsUsageBar;
        private Border? _profilesUsageBar;
        private Border? _assetsUsageBar;
        private Border? _cacheUsageBar;
        private Border? _otherUsageBar;
        private TextBlock? _logsUsageText;
        private TextBlock? _profilesUsageText;
        private TextBlock? _assetsUsageText;
        private TextBlock? _cacheUsageText;
        private TextBlock? _otherUsageText;
        private TextBlock? _totalUsageText;
        private Button? _clearLogsButton;
        private Button? _clearCacheButton;

        // Resolution Preset & Visualizer overlays
        private Grid? _resolutionPresetOverlay;
        private Border? _resolutionPresetPanel;

        private Grid? _resolutionVisualOverlay;
        private Border? _resolutionVisualPanel;
        private TextBlock? _resolutionVisualText;

        private Border? _resolutionBox;
        private TextBlock? _resolutionScaleNote;

        private Grid? _feedbackOverlay;
        private Border? _feedbackPanel;
        private ComboBox? _feedbackTypeComboBox;
        private StackPanel? _featureSuggestionPanel;
        private TextBox? _suggestionTextBox;
        private StackPanel? _bugReportPanel;
        private TextBox? _expectedBehaviorTextBox;
        private TextBox? _actualBehaviorTextBox;
        private TextBox? _stepsToReproduceTextBox;
        private TextBlock? _attachedLogFileName;
        private TextBox? _osVersionTextBox;
        private TextBox? _leafClientVersionTextBox;
        private TextBlock? _statusMessageTextBlock;
        private bool _isFeedbackAnimating = false; // Flag to prevent re-opening during animation
        private string _feedbackLogFolderPath = ""; // To store the path for log attachment
        private string? _attachedLogFileContent;

        // Planned updates
        private Button? _plannedUpdatesButton;
        private Grid? _plannedUpdatesOverlay;
        private Border? _plannedUpdatesPanel;
        private CancellationTokenSource? _plannedUpdatesCts;

        // Prayer Time Reminder
        private ToggleSwitch? _enablePrayerTimeReminderToggle;
        private ComboBox? _prayerTimeCountryComboBox;
        private TextBox? _prayerTimeCityTextBox;
        private ComboBox? _prayerCalculationMethodComboBox;
        private Slider? _prayerReminderMinutesBeforeSlider;
        private TextBlock? _prayerReminderMinutesBeforeValueText;

        private DispatcherTimer? _prayerTimeCheckTimer;
        private DateTime? _nextPrayerTimeReminder;
        private string? _nextPrayerName;

        private AladhanPrayerTimesResponse? _lastFetchedPrayerTimes;

        // Mod Cleanup
        private ModCleanupService? _modCleanupService;

        // Signals watcher
        private FileSystemWatcher? _externalSignalWatcher;

        #endregion

        // File: MainWindow.cs (within the MainWindow() constructor)

        public MainWindow()
        {
            try
            {
                // === NEW LOGGING SETUP: START ===
                // Only initialize logging ONCE for the entire application
                if (_logStreamWriter == null)
                {
                    // Store original console streams BEFORE redirecting
                    _originalConsoleOut = Console.Out;
                    _originalConsoleError = Console.Error;

                    // Determine the application's base directory
                    var appDirectory = AppContext.BaseDirectory;
                    _logFolderPath = System.IO.Path.Combine(appDirectory, "Logs");

                    // Ensure the Logs directory exists
                    System.IO.Directory.CreateDirectory(_logFolderPath);

                    // Create a unique log file name with a timestamp
                    var logFileName = $"launcher_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                    _logFilePath = System.IO.Path.Combine(_logFolderPath, logFileName);

                    // Create a StreamWriter and redirect Console.Out and Console.Error
                    _logStreamWriter = new StreamWriter(new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    {
                        AutoFlush = true
                    };
                    Console.SetOut(_logStreamWriter);
                    Console.SetError(_logStreamWriter);

                    Console.WriteLine($"[LOGGING] Started logging to: {_logFilePath}");
                }
                else
                {
                    Console.WriteLine("[LOGGING] Using existing log writer (window recreation)");
                }
                // === NEW LOGGING SETUP: END ===
            }
            catch (Exception ex)
            {
                // Fallback to console if file logging fails
                Console.Error.WriteLine($"[ERROR] Failed to set up file logging: {ex.Message}");
                Console.SetOut(_originalConsoleOut ?? new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
                Console.SetError(_originalConsoleError ?? new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
            }

            InitializeComponent();
            DataContext = new MainWindowViewModel();

            _skinRenderService = new SkinRenderService();
            _mojangApiService = new MojangApiService(_httpClient);
            _settingsService = new SettingsService(); // Ensure this line exists and is before SuggestionsService init
            _suggestionsService = new SuggestionsService(_settingsService);

            _modCleanupService = new ModCleanupService(_minecraftFolder); // This line is new

            var topBorder = this.FindControl<Border>("TopHeaderBorder");
            if (topBorder != null)
            {
                topBorder.PointerPressed += (sender, e) =>
                {
                    if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                        BeginMoveDrag(e);
                };
            }
            InitializeTrayIcon();
            InitializeControls(); // Ensure _quickPlayServersContainer is initialized here
            InitializeLauncher();
            PopulateAllVersionsData();
            LoadFriendsAsync();
            AnimateBannersOnLoad();
            LoadAndApplySettings();
            LoadUserInfoAsync();
            LoadServerData();
            InitializeSignalWatcher();

            // Initialize OnlineCountService with a try-catch for robustness
            try
            {
                _onlineCountService = new OnlineCountService();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[MainWindow ERROR] Failed to initialize OnlineCountService: {ex.Message}");
                // Ensure _onlineCountService is explicitly null if its constructor fails.
                // The functionality relying on it will be skipped due to subsequent null checks.
                _onlineCountService = null;
            }

            this.Opened += async (_, __) =>
            {
                StartRichPresenceIfEnabled();

                // Ensure defaults exist before drawing UI, then build the UI
                await InitializeDefaultServersAsync();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    LoadServers();
                    RefreshQuickPlayBar();
                });

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await RefreshAllServerStatusesAsync();
                        await WarmupServerIconsAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Startup Server Refresh] Error: {ex.Message}");
                    }
                });

                UpdateLaunchButton("LAUNCH GAME", "SeaGreen");

                if (_currentSettings.IsFirstLaunch)
                {
                    await Task.Delay(500); // Small delay to ensure main window is fully settled before pop-up animation
                    OpenAboutLeafClient(null, new RoutedEventArgs()); // Open the About page

                    _currentSettings.IsFirstLaunch = false; // Mark as no longer first launch
                    await _settingsService.SaveSettingsAsync(_currentSettings); // Save this change
                }

                CheckForUpdates(null, new RoutedEventArgs());

                if (_onlineCountService != null)
                {
                    // before fetching the count for display.
                    await _onlineCountService.UpdateCount(true);

                    // Now fetch and display the initial online count, which will now be correct
                    await UpdateOnlineCountDisplay();
                }
                else
                {
                    Console.Error.WriteLine("[MainWindow ERROR] OnlineCountService was not initialized. Skipping online count updates.");
                    // Optionally, update UI to reflect that online count is unavailable.
                    if (_onlineCountTextBlock != null)
                    {
                        _onlineCountTextBlock.Text = "You're offline";
                    }
                    if (_onlineStatusDot != null)
                    {
                        _onlineStatusDot.Fill = new SolidColorBrush(Colors.Red); // Indicate error
                    }
                }
                _ = PerformModCleanup(); // Fire and forget cleanup on startup
            };


            this.Closing += OnWindowClosing;

            AppDomain.CurrentDomain.FirstChanceException += (sender, e) =>
            {
                if (e.Exception is JsonException jsonEx)
                {
                    Console.WriteLine($"[JSON ERROR] {jsonEx.Message}");
                    // Don't access StackTrace here as it can cause issues
                }
            };

            _networkMonitorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)   // how often to re-check
            };
            _networkMonitorTimer.Tick += (_, __) => CheckNetworkConnectivity();
            _networkMonitorTimer.Start();
        }
        private void InitializeSignalWatcher()
        {
            try
            {
                _externalSignalWatcher = new FileSystemWatcher(_minecraftFolder);
                _externalSignalWatcher.Filter = "leaf_open_mods.signal";

                // Watch for creation or writing
                _externalSignalWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;

                _externalSignalWatcher.Created += OnExternalSignalDetected;
                _externalSignalWatcher.Changed += OnExternalSignalDetected;

                _externalSignalWatcher.EnableRaisingEvents = true;

                Console.WriteLine("[Signal Watcher] Listening for in-game mod menu trigger...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Signal Watcher] Failed to start: {ex.Message}");
            }
        }

        private void OnExternalSignalDetected(object sender, FileSystemEventArgs e)
        {
            // Use Dispatcher to ensure UI operations happen on the main thread
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                try
                {
                    // 1. Consume the signal (delete the file so it can be triggered again)
                    if (System.IO.File.Exists(e.FullPath))
                    {
                        await Task.Delay(50); // Tiny delay to ensure Java released the handle
                        System.IO.File.Delete(e.FullPath);
                    }

                    // 2. Open the Mods Manager
                    OpenModsManager();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Signal Watcher] Error processing signal: {ex.Message}");
                }
            });
        }

        private void OpenModsManager()
        {
            // Check if it's already open to avoid duplicates
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var existingManager = desktop.Windows.FirstOrDefault(w => w is ModsManager);

                if (existingManager != null)
                {
                    // Just Show() and ensure Topmost. 
                    // AVOID Activate() if possible, as it forces focus stealing which kills Exclusive Fullscreen.
                    existingManager.Show();
                    existingManager.Topmost = true;
                    existingManager.WindowState = WindowState.Normal;
                }
                else
                {
                    var modsManager = new ModsManager();
                    modsManager.Show();
                    // Ensure the new window is Topmost so it floats over the game (requires Borderless Windowed game)
                    modsManager.Topmost = true;
                }
            }
        }

        private async Task PerformModCleanup()
        {
            if (_modCleanupService == null) return;

            Console.WriteLine("[Mod Cleanup] Starting background cleanup process...");
            var cleanupList = _modCleanupService.GetCleanupList(); // Get a copy of the list
            var modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");

            List<ModCleanupEntry> successfullyCleanedMods = new List<ModCleanupEntry>();

            foreach (var entry in cleanupList)
            {
                // Construct InstalledMod for GetModFilePath consistency
                var tempInstalledMod = new InstalledMod
                {
                    ModId = entry.ModId,
                    FileName = entry.FileName,
                    MinecraftVersion = entry.MinecraftVersion
                };

                string jarPath = GetModFilePath(modsFolder, tempInstalledMod, isDisabled: false);
                string disabledPath = GetModFilePath(modsFolder, tempInstalledMod, isDisabled: true);

                bool deletedAnyFile = false;
                if (File.Exists(jarPath))
                {
                    try
                    {
                        File.Delete(jarPath);
                        Console.WriteLine($"[Mod Cleanup] Successfully deleted orphaned mod file: {System.IO.Path.GetFileName(jarPath)}");
                        deletedAnyFile = true;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[Mod Cleanup ERROR] Failed to delete orphaned mod file '{System.IO.Path.GetFileName(jarPath)}': {ex.Message}");
                    }
                }
                if (File.Exists(disabledPath))
                {
                    try
                    {
                        File.Delete(disabledPath);
                        Console.WriteLine($"[Mod Cleanup] Successfully deleted orphaned disabled mod file: {System.IO.Path.GetFileName(disabledPath)}");
                        deletedAnyFile = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Mod Cleanup ERROR] Failed to delete orphaned disabled mod file '{System.IO.Path.GetFileName(disabledPath)}': {ex.Message}");
                    }
                }

                if (deletedAnyFile)
                {
                    successfullyCleanedMods.Add(entry);
                }
            }

            // Remove successfully cleaned up mods from the service's list
            foreach (var entry in successfullyCleanedMods)
            {
                _modCleanupService.RemoveModFromCleanup(entry);
            }

            if (successfullyCleanedMods.Any())
            {
                Console.WriteLine($"[Mod Cleanup] Completed. Cleaned up {successfullyCleanedMods.Count} mods.");
            }
            else
            {
                Console.WriteLine("[Mod Cleanup] Completed. No mods needed cleanup.");
            }
        }

        private void CheckNetworkConnectivity()
        {
            // The isOnline check is implicitly handled by ApplyLaunchButtonState now.
            // Just trigger a re-evaluation of the button state.
            Dispatcher.UIThread.Post(() =>
            {
                ApplyLaunchButtonState();
            });
        }

        /// <summary>
        /// Fetches the current online count and updates the UI TextBlock and status dot.
        /// </summary>
        private async Task UpdateOnlineCountDisplay()
        {
            // Always perform a null check before using _onlineCountService
            if (_onlineCountService == null)
            {
                Console.WriteLine("[OnlineCountService] Service not available. Cannot update UI.");
                if (_onlineCountTextBlock != null) _onlineCountTextBlock.Text = "You're offline";
                if (_onlineStatusDot != null) _onlineStatusDot.Fill = new SolidColorBrush(Colors.Red);
                return;
            }

            if (_onlineCountTextBlock == null || _onlineStatusDot == null)
            {
                // Try to find them again in case they weren't initialized on first attempt
                _onlineCountTextBlock = this.FindControl<TextBlock>("OnlineCountTextBlock");
                _onlineStatusDot = this.FindControl<Ellipse>("OnlineStatusDot");

                if (_onlineCountTextBlock == null || _onlineStatusDot == null)
                {
                    Console.WriteLine("[OnlineCountService] Online count TextBlock or Ellipse not found in UI.");
                    return; // Cannot update UI if controls are not found
                }
            }

            int count = await _onlineCountService.GetOnlineCount();
            if (count >= 0)
            {
                // Update UI on the UI thread
                Dispatcher.UIThread.Post(() =>
                {
                    _onlineCountTextBlock.Text = $"{count} Online";
                    if (count > 0)
                    {
                        _onlineStatusDot.Fill = new SolidColorBrush(Colors.Green); // Green for online
                    }
                    else
                    {
                        _onlineStatusDot.Fill = new SolidColorBrush(Colors.Gray); // Gray for offline or 0 users
                    }
                });
            }
            else
            {
                // Indicate error in fetching count
                Dispatcher.UIThread.Post(() =>
                {
                    _onlineCountTextBlock.Text = "You're offline";
                    _onlineStatusDot.Fill = new SolidColorBrush(Colors.Red); // Red for error
                });
            }
        }

        private void InitializeModBrowserControls()
        {
            _modBrowserOverlay = this.FindControl<Grid>("ModBrowserOverlay");
            _modBrowserPanel = this.FindControl<Border>("ModBrowserPanel");
            _modsResultsPanel = this.FindControl<WrapPanel>("ModsResultsPanel");
            _modsLoadingPanel = this.FindControl<StackPanel>("ModsLoadingPanel");
            _modsEmptyPanel = this.FindControl<StackPanel>("ModsEmptyPanel");
            _modSearchBox = this.FindControl<TextBox>("ModSearchBox");

            _modDetailsSidebar = this.FindControl<Border>("ModDetailsSidebar");
            _modDetailsIcon = this.FindControl<Image>("ModDetailsIcon");
            _modDetailsTitle = this.FindControl<TextBlock>("ModDetailsTitle");
            _modDetailsDescription = this.FindControl<TextBlock>("ModDetailsDescription");
            _modDetailsStats = this.FindControl<TextBlock>("ModDetailsStats");
            _modDetailsDownloadButton = this.FindControl<Button>("ModDetailsDownloadButton");

            if (_modSearchBox != null)
                _modSearchBox.TextChanged += OnModSearchTextChanged;

            _modrinthClient.DefaultRequestHeaders.Remove("User-Agent");
            _modrinthClient.DefaultRequestHeaders.Add("User-Agent", "LeafClient/1.1.0 (contact@leafclient.net)");
        }
        #region Updater Important Variables

        string updaterDownloadUrl = "https://github.com/LeafClientMC/LeafClient/raw/refs/heads/main/latestexe/LeafClientUpdater.exe";
        string newExeDownloadUrl = "https://github.com/LeafClientMC/LeafClient/raw/refs/heads/main/latestexe/LeafClient.exe";
        string versionFileUrl = "https://raw.githubusercontent.com/LeafClientMC/LeafClient/main/latestversion.txt";

        #endregion

        private async Task DownloadLeafRuntimeDependencies(string version, bool isFabric)
        {
            if (version != "1.20.1" || !isFabric)
                return;

            string leafRuntimeDir = System.IO.Path.Combine(_minecraftFolder, "leaf-runtime");
            System.IO.Directory.CreateDirectory(leafRuntimeDir);

            string bootstrapUrl = "https://github.com/LeafClientMC/LeafClient/raw/refs/heads/main/latestjars/LeafBootstrap-1.0.0.jar";
            string runtimeUrl = "https://github.com/LeafClientMC/LeafClient/raw/refs/heads/main/latestjars/LeafRuntime-1.0.0.jar";

            string bootstrapPath = System.IO.Path.Combine(leafRuntimeDir, "LeafBootstrap-1.0.0.jar");
            string runtimePath = System.IO.Path.Combine(leafRuntimeDir, "LeafRuntime-1.0.0.jar");

            ShowProgress(true, "Downloading Leaf Client Runtime...");

            try
            {
                await DownloadFileAsync(bootstrapUrl, bootstrapPath);
                await DownloadFileAsync(runtimeUrl, runtimePath);
                Console.WriteLine("[Leaf Runtime] Runtime dependencies downloaded successfully.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Leaf Runtime ERROR] Failed to download dependencies: {ex.Message}");
            }
            finally
            {
                ShowProgress(false);
            }
        }



        private async void CheckForUpdates(object? sender, RoutedEventArgs e, bool isManualCheck = false)
        {
            Console.WriteLine("[Updater] Checking for updates...");

            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                if (_currentSettings.EnableUpdateNotifications || isManualCheck)
                    await ShowUpdateErrorDialog("No internet connection.");
                return;
            }

            var currentVersion = GetCurrentAppVersion();

            try
            {
                string latestVersionString = await _httpClient.GetStringAsync(versionFileUrl);
                var latestVersion = Version.Parse(latestVersionString.Trim());

                if (latestVersion > currentVersion)
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        if (_currentSettings.EnableUpdateNotifications || isManualCheck)
                        {
                            bool updateConfirmed = await ShowUpdateAvailableDialog(latestVersion.ToString());
                            if (updateConfirmed) await InitiateSelfUpdate(newExeDownloadUrl);
                        }
                    }
                    else
                    {
                        if (_currentSettings.EnableUpdateNotifications || isManualCheck)
                        {
                            await ShowManualUpdateDialog(latestVersion.ToString());
                        }
                    }
                }
                else if (isManualCheck)
                {
                    await ShowNoUpdateAvailableDialog();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Updater ERROR] {ex.Message}");
                if (isManualCheck) await ShowUpdateErrorDialog(ex.Message);
            }
        }

        private async Task ShowManualUpdateDialog(string newVersion)
        {
            var dialog = new Window
            {
                Title = "Update Available",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 15,
                    Children =
            {
                new TextBlock
                {
                    Text = $"Version {newVersion} is available!",
                    Foreground = GetBrush("PrimaryForegroundBrush"),
                    FontWeight = FontWeight.Bold,
                    FontSize = 16
                },
                new TextBlock
                {
                    Text = "Auto-update is only supported on Windows. Please download the latest version manually.",
                    Foreground = GetBrush("SecondaryForegroundBrush"),
                    TextWrapping = TextWrapping.Wrap
                },
                new Button
                {
                    Content = "Open Download Page",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Background = GetBrush("PrimaryAccentBrush"),
                    Foreground = GetBrush("AccentButtonForegroundBrush")
                }
            }
                }
            };

            if ((dialog.Content as StackPanel)?.Children.OfType<Button>().Last() is Button downloadBtn)
            {
                downloadBtn.Click += (_, __) =>
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://github.com/LeafClientMC/LeafClient/releases/latest",
                        UseShellExecute = true
                    });
                    dialog.Close();
                };
            }

            await dialog.ShowDialog(this);
        }



        private void CheckForUpdatesManual(object? sender, RoutedEventArgs e)
        {
            CheckForUpdates(sender, e, true); // Call the main method with isManualCheck = true
        }

        private async Task InitiateSelfUpdate(string newExeDownloadUrl)
        {
            Console.WriteLine("[Updater] Preparing self-update process...");

            try
            {
                // FIX: Use Process.MainModule.FileName to get the path in single-file/AOT builds
                string? currentExePath = null;
                try
                {
                    currentExePath = Process.GetCurrentProcess().MainModule?.FileName;
                }
                catch { /* Ignore permission errors */ }

                // Fallback if MainModule fails
                if (string.IsNullOrEmpty(currentExePath))
                {
                    currentExePath = Environment.ProcessPath;
                }

                // Final fallback
                if (string.IsNullOrEmpty(currentExePath))
                {
                    currentExePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                }

                if (string.IsNullOrEmpty(currentExePath))
                {
                    throw new Exception("Could not determine executable path.");
                }

                string appDirectory = System.IO.Path.GetDirectoryName(currentExePath)!;

                string updaterDir = System.IO.Path.Combine(appDirectory, "Updater");
                System.IO.Directory.CreateDirectory(updaterDir);

                string updaterExeName = "LeafClientUpdater.exe";
                string updaterLocalPath = System.IO.Path.Combine(updaterDir, updaterExeName);

                Console.WriteLine($"[Updater] Downloading updater from {updaterDownloadUrl} to {updaterLocalPath}");
                using (var client = new HttpClient())
                {
                    byte[] updaterBytes = await client.GetByteArrayAsync(updaterDownloadUrl);
                    await System.IO.File.WriteAllBytesAsync(updaterLocalPath, updaterBytes);
                }
                Console.WriteLine("[Updater] Updater downloaded successfully.");

                Process.Start(new ProcessStartInfo
                {
                    FileName = updaterLocalPath,
                    Arguments = $"\"{currentExePath}\" \"{newExeDownloadUrl}\"",
                    UseShellExecute = false
                });

                Console.WriteLine("[Updater] Launched updater and exiting main application.");

                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.Shutdown();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Updater ERROR] Failed to initiate self-update: {ex.Message}");
                await ShowUpdateErrorDialog($"Failed to start update: {ex.Message}");
            }
        }



        // --- Helper Dialogs for Update Feature ---

        private async Task<bool> ShowUpdateAvailableDialog(string newVersion)
        {
            var dialog = new Window
            {
                Title = "Update Available!",
                Width = 400,
                Height = 220,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 15,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"A new version of Leaf Client ({newVersion}) is available!",
                            Foreground = GetBrush("PrimaryForegroundBrush"),
                            FontWeight = FontWeight.Bold,
                            FontSize = 16,
                            TextWrapping = TextWrapping.Wrap
                        },
                        new TextBlock
                        {
                            Text = "Do you want to download and install the update now?",
                            Foreground = GetBrush("SecondaryForegroundBrush"),
                            FontSize = 13,
                            TextWrapping = TextWrapping.Wrap
                        },
                        new StackPanel
                        {
                            Orientation = Avalonia.Layout.Orientation.Horizontal,
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                            Spacing = 10,
                            Margin = new Thickness(0, 10, 0, 0),
                            Children =
                            {
                                new Button { Content = "Not Now", Padding = new Thickness(15, 8), Background = GetBrush("HoverBackgroundBrush"), Foreground = GetBrush("PrimaryForegroundBrush"), CornerRadius = new CornerRadius(8) },
                                new Button { Content = "Update Now", Padding = new Thickness(15, 8), Background = GetBrush("SuccessBrush"), Foreground = GetBrush("AccentButtonForegroundBrush"), FontWeight = FontWeight.Bold, CornerRadius = new CornerRadius(8) }
                            }
                        }
                    }
                }
            };

            var tcs = new TaskCompletionSource<bool>();
            if ((dialog.Content as StackPanel)?.Children.OfType<StackPanel>().Last() is StackPanel buttonPanel)
            {
                if (buttonPanel.Children.OfType<Button>().FirstOrDefault() is Button notNowButton)
                {
                    notNowButton.Click += (_, __) => { tcs.SetResult(false); dialog.Close(); };
                }
                if (buttonPanel.Children.OfType<Button>().LastOrDefault() is Button updateNowButton)
                {
                    updateNowButton.Click += (_, __) => { tcs.SetResult(true); dialog.Close(); };
                }
            }
            await dialog.ShowDialog(this);
            return await tcs.Task;
        }

        private async Task ShowNoUpdateAvailableDialog()
        {
            var dialog = new Window
            {
                Title = "No Updates",
                Width = 350,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 15,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "You are running the latest version of Leaf Client!",
                            Foreground = GetBrush("PrimaryForegroundBrush"),
                            FontWeight = FontWeight.Bold,
                            FontSize = 16,
                            TextWrapping = TextWrapping.Wrap
                        },
                        new Button
                        {
                            Content = "OK",
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                            Padding = new Thickness(15, 8),
                            Background = GetBrush("PrimaryAccentBrush"),
                            Foreground = GetBrush("AccentButtonForegroundBrush"),
                            CornerRadius = new CornerRadius(8)
                        }
                    }
                }
            };
            if ((dialog.Content as StackPanel)?.Children.OfType<Button>().Last() is Button okButton)
            {
                okButton.Click += (s, e) => dialog.Close();
            }
            await dialog.ShowDialog(this);
        }

        private async Task ShowUpdateErrorDialog(string message)
        {
            var dialog = new Window
            {
                Title = "Update Error",
                Width = 450,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 15,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "An error occurred while checking for updates:",
                            Foreground = GetBrush("ErrorBrush"),
                            FontWeight = FontWeight.Bold,
                            FontSize = 16,
                            TextWrapping = TextWrapping.Wrap
                        },
                        new TextBlock
                        {
                            Text = message,
                            Foreground = GetBrush("PrimaryForegroundBrush"),
                            FontSize = 13,
                            TextWrapping = TextWrapping.Wrap
                        },
                        new Button
                        {
                            Content = "OK",
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                            Padding = new Thickness(15, 8),
                            Background = GetBrush("PrimaryAccentBrush"),
                            Foreground = GetBrush("AccentButtonForegroundBrush"),
                            CornerRadius = new CornerRadius(8)
                        }
                    }
                }
            };
            if ((dialog.Content as StackPanel)?.Children.OfType<Button>().Last() is Button okButton)
            {
                okButton.Click += (s, e) => dialog.Close();
            }
            await dialog.ShowDialog(this);
        }

        private void LoadUserMods()
        {
            if (_userModsPanel == null || _noUserModsMessage == null) return;

            Console.WriteLine($"[User Mods] Loading {_currentSettings.InstalledMods.Count} mods from settings");

            _userModsPanel.Children.Clear();

            // Only show mods for the currently selected Minecraft version
            string currentMcVersion = _currentSettings.SelectedSubVersion;
            var modsForCurrentVersion = _currentSettings.InstalledMods
                .Where(m => m.MinecraftVersion.Equals(currentMcVersion, StringComparison.OrdinalIgnoreCase))
                .OrderBy(m => m.Name)
                .ToList();

            if (!modsForCurrentVersion.Any())
            {
                Console.WriteLine("[User Mods] No mods found in settings for current MC version.");
                _noUserModsMessage.IsVisible = true;
                return;
            }

            _noSkinsMessage.IsVisible = false; // This line seems to be a copy-paste error from LoadSkins, should be _noUserModsMessage
            _noUserModsMessage.IsVisible = false;

            foreach (var mod in modsForCurrentVersion)
            {
                Console.WriteLine($"[User Mods] Creating card for: {mod.Name} (MC {mod.MinecraftVersion}, Enabled: {mod.Enabled})");
                var modCard = CreateUserModCard(mod);
                _userModsPanel.Children.Add(modCard);
            }

            Console.WriteLine($"[User Mods] Loaded {_userModsPanel.Children.Count} mod cards for MC {currentMcVersion}");
        }

        private Border CreateUserModCard(InstalledMod mod)
        {
            var card = new Border
            {
                Background = GetBrush("CardBackgroundColor"),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 5)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Mod icon
            var iconBorder = new Border
            {
                Width = 40,
                Height = 40,
                Margin = new Thickness(0, 0, 10, 0),
                CornerRadius = new CornerRadius(6),
                ClipToBounds = true,
                Background = GetBrush("HoverBackgroundBrush")
            };

            var modIcon = new Image
            {
                Width = 32,
                Height = 32,
                Stretch = Avalonia.Media.Stretch.Uniform,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            if (!string.IsNullOrEmpty(mod.IconUrl))
            {
                _ = LoadModIcon(modIcon, mod.IconUrl);
            }
            else
            {
                modIcon.Source = new Avalonia.Media.Imaging.Bitmap(
                    AssetLoader.Open(new Uri("avares://LeafClient/Assets/minecraft.png")));
            }

            iconBorder.Child = modIcon;
            Grid.SetColumn(iconBorder, 0);
            grid.Children.Add(iconBorder);

            // Mod info
            var infoStack = new StackPanel
            {
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            var nameText = new TextBlock
            {
                Text = mod.Name,
                Foreground = GetBrush("PrimaryForegroundBrush"),
                FontWeight = FontWeight.Bold
            };

            var versionText = new TextBlock
            {
                Text = $"v{mod.Version} • MC {mod.MinecraftVersion}",
                Foreground = GetBrush("SecondaryForegroundBrush"),
                FontSize = 11
            };

            infoStack.Children.Add(nameText);
            infoStack.Children.Add(versionText);
            Grid.SetColumn(infoStack, 1);
            grid.Children.Add(infoStack);

            // Toggle switch
            var toggle = new ToggleSwitch
            {
                IsChecked = mod.Enabled,
                OffContent = "Disabled",
                OnContent = "Enabled",
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 10, 0)
            };

            toggle.Checked += async (s, e) => await ToggleMod(mod, true);
            toggle.Unchecked += async (s, e) => await ToggleMod(mod, false);

            Grid.SetColumn(toggle, 2);
            grid.Children.Add(toggle);

            // Delete button
            var deleteButton = new Button
            {
                Content = "×",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Width = 30,
                Height = 30,
                Background = GetBrush("ErrorBrush"),
                Foreground = GetBrush("AccentButtonForegroundBrush"),
                CornerRadius = new CornerRadius(15),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            deleteButton.Click += async (s, e) => await DeleteUserMod(mod);

            Grid.SetColumn(deleteButton, 3);
            grid.Children.Add(deleteButton);

            card.Child = grid;
            return card;
        }

        private async Task ToggleMod(InstalledMod mod, bool enabled)
        {
            mod.Enabled = enabled; // Update the in-memory state

            string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
            string currentJarPath = GetModFilePath(modsFolder, mod, isDisabled: false); // Path if it's currently .jar
            string currentDisabledPath = GetModFilePath(modsFolder, mod, isDisabled: true); // Path if it's currently .jar.disabled

            string targetPath = GetModFilePath(modsFolder, mod, isDisabled: !enabled); // The path we want it to be

            if (enabled) // User wants to ENABLE the mod
            {
                if (File.Exists(currentDisabledPath)) // It's currently disabled on disk
                {
                    try
                    {
                        File.Move(currentDisabledPath, currentJarPath); // Rename .jar.disabled to .jar
                        Console.WriteLine($"[Mod Manager] Enabled mod '{mod.Name}'.");
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[Mod Manager] Failed to enable mod '{mod.Name}': {ex.Message}");
                        ShowLaunchErrorBanner($"Failed to enable mod '{mod.Name}'. It might be in use.");
                        mod.Enabled = !enabled; // Revert state in UI if failed
                    }
                }
                else if (!File.Exists(currentJarPath))
                {
                    // Mod file (.jar or .jar.disabled) is completely missing.
                    // It will be re-downloaded by InstallUserModsAsync during launch.
                    Console.WriteLine($"[Mod Manager] Mod '{mod.Name}' file missing. Will be re-downloaded on next launch if still enabled.");
                }
            }
            else // User wants to DISABLE the mod
            {
                if (File.Exists(currentJarPath)) // It's currently enabled on disk
                {
                    try
                    {
                        File.Move(currentJarPath, currentDisabledPath); // Rename .jar to .jar.disabled
                        Console.WriteLine($"[Mod Manager] Disabled mod '{mod.Name}'.");
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[Mod Manager] Failed to disable mod '{mod.Name}': {ex.Message}");
                        ShowLaunchErrorBanner($"Failed to disable mod '{mod.Name}'. It might be in use.");
                        mod.Enabled = !enabled; // Revert state in UI if failed
                    }
                }
            }

            await _settingsService.SaveSettingsAsync(_currentSettings);
            LoadUserMods(); // Reload UI to reflect potential state changes
        }

        // File: MainWindow.cs (within the MainWindow class)

        private async Task DeleteUserMod(InstalledMod mod)
        {
            string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
            string jarPath = GetModFilePath(modsFolder, mod, isDisabled: false);
            string disabledPath = GetModFilePath(modsFolder, mod, isDisabled: true);

            // Remove from settings immediately
            _currentSettings.InstalledMods.Remove(mod);
            await _settingsService.SaveSettingsAsync(_currentSettings);
            Console.WriteLine($"[Mod Manager] Removed '{mod.Name}' from settings.");

            bool fileDeletedSuccessfully = false;
            // Attempt to delete the physical files
            if (File.Exists(jarPath))
            {
                try
                {
                    File.Delete(jarPath);
                    Console.WriteLine($"[Mod Manager] Deleted mod file: {System.IO.Path.GetFileName(jarPath)}");
                    fileDeletedSuccessfully = true;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Mod Manager] Failed to delete mod file '{System.IO.Path.GetFileName(jarPath)}': {ex.Message}");
                    // Add to cleanup list if deletion failed
                    _modCleanupService?.AddModToCleanup(mod);
                    ShowLaunchErrorBanner($"Failed to delete mod '{mod.Name}'. It might be in use. It will be retried later.");
                }
            }
            if (File.Exists(disabledPath))
            {
                try
                {
                    File.Delete(disabledPath);
                    Console.WriteLine($"[Mod Manager] Deleted disabled mod file: {System.IO.Path.GetFileName(disabledPath)}");
                    fileDeletedSuccessfully = true; // Mark as true if at least one file was deleted
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Mod Manager] Failed to delete disabled mod file '{System.IO.Path.GetFileName(disabledPath)}': {ex.Message}");
                    // Add to cleanup list if deletion failed
                    _modCleanupService?.AddModToCleanup(mod);
                    ShowLaunchErrorBanner($"Failed to delete mod '{mod.Name}'. It might be in use. It will be retried later.");
                }
            }

            // If both files were not found or could not be deleted, and it was removed from settings,
            // then it's effectively "cleaned up" from the user's perspective, so remove from cleanup list.
            if (!fileDeletedSuccessfully && _modCleanupService != null)
            {
                _modCleanupService.RemoveModFromCleanup(new ModCleanupEntry
                {
                    ModId = mod.ModId,
                    FileName = mod.FileName,
                    MinecraftVersion = mod.MinecraftVersion
                });
            }

            // If the deleted mod was the currently selected one, clear selection
            if (_currentSettings.SelectedSkinId == mod.ModId)
            {
                _currentSettings.SelectedSkinId = null;
                _currentlySelectedSkinCard = null;
            }

            // Reload UI to reflect changes (mod removed from list)
            LoadUserMods();
        }


        private void InitializeModManagementControls()
        {
            _userModsPanel = this.FindControl<StackPanel>("UserModsPanel");
            _noUserModsMessage = this.FindControl<Border>("NoUserModsMessage");

            if (_userModsPanel == null)
            {
                Console.WriteLine("[User Mods] ERROR: UserModsPanel not found!");
                return;
            }

            if (_noUserModsMessage == null)
            {
                Console.WriteLine("[User Mods] ERROR: NoUserModsMessage not found!");
                return;
            }

            Console.WriteLine("[User Mods] Controls initialized successfully");
            LoadUserMods();
        }

        private void InitializeControls()
        {

            InitializeModBrowserControls();
            InitializeModManagementControls();
            InitializePlannedUpdatesControls();

            if (this.FindControl<TextBlock>("AppVersionTextBlock") is { } appVersionTextBlock)
            {
                Version currentVersion = GetCurrentAppVersion();
                string versionString = $"Version {currentVersion.Major}.{currentVersion.Minor}.{currentVersion.Build} Beta"; // Format as "Version 1.1.0 Beta"
                appVersionTextBlock.Text = versionString;
            }

            _feedbackOverlay = this.FindControl<Grid>("FeedbackOverlay");
            _feedbackPanel = this.FindControl<Border>("FeedbackPanel");

            if (_feedbackPanel != null) // Only try to find nested controls if the panel exists
            {
                _feedbackTypeComboBox = _feedbackPanel.FindControl<ComboBox>("FeedbackTypeComboBox");
                _featureSuggestionPanel = _feedbackPanel.FindControl<StackPanel>("FeatureSuggestionPanel");
                _suggestionTextBox = _feedbackPanel.FindControl<TextBox>("SuggestionTextBox");
                _bugReportPanel = _feedbackPanel.FindControl<StackPanel>("BugReportPanel");
                _expectedBehaviorTextBox = _feedbackPanel.FindControl<TextBox>("ExpectedBehaviorTextBox");
                _actualBehaviorTextBox = _feedbackPanel.FindControl<TextBox>("ActualBehaviorTextBox");
                _stepsToReproduceTextBox = _feedbackPanel.FindControl<TextBox>("StepsToReproduceTextBox");
                _attachedLogFileName = _feedbackPanel.FindControl<TextBlock>("AttachedLogFileName");
                _osVersionTextBox = _feedbackPanel.FindControl<TextBox>("OsVersionTextBox");
                _leafClientVersionTextBox = _feedbackPanel.FindControl<TextBox>("LeafClientVersionTextBox");
                _statusMessageTextBlock = _feedbackPanel.FindControl<TextBlock>("StatusMessageTextBlock");

                // Get references to buttons for event wiring. Perform null checks.
                var attachLogButton = _feedbackPanel.FindControl<Button>("AttachLogButton");
                var sendFeedbackButton = _feedbackPanel.FindControl<Button>("SendFeedbackButton");
                var cancelFeedbackButton = _feedbackPanel.FindControl<Button>("CancelFeedbackButton");

                // Wire up events only if controls are successfully found
                if (attachLogButton != null) attachLogButton.Click += AttachLogButton_Click;
                if (sendFeedbackButton != null) sendFeedbackButton.Click += SendFeedbackButton_Click;
                if (cancelFeedbackButton != null) cancelFeedbackButton.Click += CancelFeedbackButton_Click;
                if (_feedbackTypeComboBox != null) _feedbackTypeComboBox.SelectionChanged += FeedbackTypeComboBox_SelectionChanged;

                // Pre-fill system information once here, as controls are now initialized
                if (_osVersionTextBox != null)
                    _osVersionTextBox.Text = Environment.OSVersion.ToString();

                if (_leafClientVersionTextBox != null)
                {
                    Version? currentAppVersion = GetCurrentAppVersion();
                    _leafClientVersionTextBox.Text = currentAppVersion != null ?
                                                    $"Launcher v{currentAppVersion.Major}.{currentAppVersion.Minor}.{currentAppVersion.Build}" :
                                                    "N/A";
                }
            }
            else
            {
                Console.Error.WriteLine("[CRITICAL ERROR] FeedbackPanel not found during InitializeControls. Feedback system will be non-functional.");
            }


            if (this.FindControl<StackPanel>("SettingsFeedbackSection") is { } feedbackSection)
            {
                if (feedbackSection.FindControl<Button>("SuggestFeatureButton") is { } suggestButton)
                {
                    suggestButton.Click += OpenFeedbackOverlay;
                }
                if (feedbackSection.FindControl<Button>("ReportBugButton") is { } reportButton)
                {
                    reportButton.Click += OpenFeedbackOverlay;
                }
            }

            _enablePrayerTimeReminderToggle = this.FindControl<ToggleSwitch>("EnablePrayerTimeReminderToggle");
            _prayerTimeCountryComboBox = this.FindControl<ComboBox>("PrayerTimeCountryComboBox");
            _prayerTimeCityTextBox = this.FindControl<TextBox>("PrayerTimeCityTextBox");
            _prayerCalculationMethodComboBox = this.FindControl<ComboBox>("PrayerCalculationMethodComboBox");
            _prayerReminderMinutesBeforeSlider = this.FindControl<Slider>("PrayerReminderMinutesBeforeSlider");
            _prayerReminderMinutesBeforeValueText = this.FindControl<TextBlock>("PrayerReminderMinutesBeforeValueText");

            if (_prayerReminderMinutesBeforeSlider != null)
            {
                _prayerReminderMinutesBeforeSlider.ValueChanged += (s, e) =>
                {
                    if (_prayerReminderMinutesBeforeValueText != null)
                    {
                        _prayerReminderMinutesBeforeValueText.Text = $"{e.NewValue:F0} minutes";
                    }
                };
            }

            _resolutionScaleNote = this.FindControl<TextBlock>("ResolutionScaleNote");
            _resolutionBox = this.FindControl<Border>("ResolutionBox");
            _resolutionPresetOverlay = this.FindControl<Grid>("ResolutionPresetOverlay");
            _resolutionPresetPanel = this.FindControl<Border>("ResolutionPresetPanel");
            if (this.FindControl<Button>("ResolutionPresetCloseButton") is { } presetCloseBtn)
                presetCloseBtn.Click += CloseResolutionPreset;

            _resolutionVisualOverlay = this.FindControl<Grid>("ResolutionVisualOverlay");
            _resolutionVisualPanel = this.FindControl<Border>("ResolutionVisualPanel");
            _resolutionVisualText = this.FindControl<TextBlock>("ResolutionVisualText");
            if (this.FindControl<Button>("ResolutionVisualCloseButton") is { } visualCloseBtn)
                visualCloseBtn.Click += CloseResolutionVisualOverlay;

            // Keep existing wire-up but it will now open the overlay
            if (_selectResolutionPresetButton != null)
                _selectResolutionPresetButton.Click += SelectGameResolutionPreset;
            if (_visualiseResolutionButton != null)
                _visualiseResolutionButton.Click += VisualiseGameResolution;

            _gameResolutionWidthTextBox = this.FindControl<TextBox>("GameResolutionWidthTextBox");
            _gameResolutionHeightTextBox = this.FindControl<TextBox>("GameResolutionHeightTextBox");
            if (_gameResolutionWidthTextBox != null)
            {
                _gameResolutionWidthTextBox.LostFocus += OnResolutionWidthChanged;
            }

            if (_gameResolutionHeightTextBox != null)
            {
                _gameResolutionHeightTextBox.LostFocus += OnResolutionHeightChanged;
            }
            _selectResolutionPresetButton = this.FindControl<Button>("SelectResolutionPresetButton");
            _visualiseResolutionButton = this.FindControl<Button>("VisualiseResolutionButton");
            _useCustomGameResolutionToggle = this.FindControl<ToggleSwitch>("UseCustomGameResolutionToggle");
            _lockGameAspectRatioToggle = this.FindControl<ToggleSwitch>("LockGameAspectRatioToggle");

            if (_selectResolutionPresetButton != null) _selectResolutionPresetButton.Click += SelectGameResolutionPreset;
            if (_visualiseResolutionButton != null) _visualiseResolutionButton.Click += VisualiseGameResolution;

            _launcherVisibilityKeepOpenRadio = this.FindControl<RadioButton>("LauncherVisibilityKeepOpenRadio");
            _launcherVisibilityHideRadio = this.FindControl<RadioButton>("LauncherVisibilityHideRadio");

            _updateDeliveryNormalRadio = this.FindControl<RadioButton>("UpdateDeliveryNormalRadio");
            _updateDeliveryEarlyRadio = this.FindControl<RadioButton>("UpdateDeliveryEarlyRadio");
            _updateDeliveryLateRadio = this.FindControl<RadioButton>("UpdateDeliveryLateRadio");

            _showUsernameInDiscordRichPresenceToggle = this.FindControl<ToggleSwitch>("ShowUsernameInDiscordRichPresenceToggle");
            if (_showUsernameInDiscordRichPresenceToggle != null)
            {
                _showUsernameInDiscordRichPresenceToggle.Checked += async (_, __) =>
                {
                    if (_isApplyingSettings) return;
                    _currentSettings.ShowUsernameInDiscordRichPresence = true;
                    await _settingsService.SaveSettingsAsync(_currentSettings);
                    UpdateRichPresenceFromState(); // Immediate update
                    MarkSettingsDirty();
                };
                _showUsernameInDiscordRichPresenceToggle.Unchecked += async (_, __) =>
                {
                    if (_isApplyingSettings) return;
                    _currentSettings.ShowUsernameInDiscordRichPresence = false;
                    await _settingsService.SaveSettingsAsync(_currentSettings);
                    UpdateRichPresenceFromState(); // Immediate update
                    MarkSettingsDirty();
                };
            }

            _closingNotificationsAlwaysRadio = this.FindControl<RadioButton>("ClosingNotificationsAlwaysRadio");
            _closingNotificationsJustOnceRadio = this.FindControl<RadioButton>("ClosingNotificationsJustOnceRadio");
            _closingNotificationsNeverRadio = this.FindControl<RadioButton>("ClosingNotificationsNeverRadio");
            _enableUpdateNotificationsToggle = this.FindControl<ToggleSwitch>("EnableUpdateNotificationsToggle");
            _enableNewContentIndicatorsToggle = this.FindControl<ToggleSwitch>("EnableNewContentIndicatorsToggle");

            _logsUsageBar = this.FindControl<Border>("LogsUsageBar");
            _profilesUsageBar = this.FindControl<Border>("ProfilesUsageBar");
            _assetsUsageBar = this.FindControl<Border>("AssetsUsageBar");
            _cacheUsageBar = this.FindControl<Border>("CacheUsageBar");
            _otherUsageBar = this.FindControl<Border>("OtherUsageBar");
            _logsUsageText = this.FindControl<TextBlock>("LogsUsageText");
            _profilesUsageText = this.FindControl<TextBlock>("ProfilesUsageText");
            _assetsUsageText = this.FindControl<TextBlock>("AssetsUsageText");
            _cacheUsageText = this.FindControl<TextBlock>("CacheUsageText");
            _otherUsageText = this.FindControl<TextBlock>("OtherUsageText");
            _totalUsageText = this.FindControl<TextBlock>("TotalUsageText");
            _clearLogsButton = this.FindControl<Button>("ClearLogsButton");
            _clearCacheButton = this.FindControl<Button>("ClearCacheButton");

            if (_clearLogsButton != null) _clearLogsButton.Click += ClearLogsButton_Click;
            if (_clearCacheButton != null) _clearCacheButton.Click += ClearCacheButton_Click;

            _gameStartingBanner = this.FindControl<Border>("GameStartingBanner");
            _gameStartingBannerText = this.FindControl<TextBlock>("GameStartingBannerText");
            _gameStartingBannerCloseButton = this.FindControl<Button>("GameStartingBannerCloseButton");

            if (_gameStartingBannerCloseButton != null)
            {
                _gameStartingBannerCloseButton.Click += HideGameStartingBanner_Click;
            }

            _launchFailureBanner = this.FindControl<Border>("LaunchFailureBanner");
            _launchFailureBannerText = this.FindControl<TextBlock>("LaunchFailureBannerText");
            _launchFailureBannerCloseButton = this.FindControl<Button>("LaunchFailureBannerCloseButton");
            _launchFailureBannerCts = null;
            _launchFailureBannerShownForCurrentLaunch = false;

            if (_launchFailureBannerCloseButton != null)
            {
                _launchFailureBannerCloseButton.Click += (_, __) => HideLaunchFailureBanner();
            }

            _onlineCountTextBlock = this.FindControl<TextBlock>("OnlineCountTextBlock");
            _onlineStatusDot = this.FindControl<Ellipse>("OnlineStatusDot");

            _optiFineToggle = this.FindControl<ToggleSwitch>("OptiFineToggle");
            _lithiumToggle = this.FindControl<ToggleSwitch>("LithiumToggle");
            _sodiumToggle = this.FindControl<ToggleSwitch>("SodiumToggle");

            _commonQuestionsOverlay = this.FindControl<Grid>("CommonQuestionsOverlay");
            _commonQuestionsPanel = this.FindControl<Border>("CommonQuestionsPanel");

            _aboutLeafClientOverlay = this.FindControl<Grid>("AboutLeafClientOverlay");
            _aboutLeafClientPanel = this.FindControl<Border>("AboutLeafClientPanel");

            _jvmArgumentsEditButton = this.FindControl<Button>("JvmArgumentsEditButton");
            if (_jvmArgumentsEditButton != null)
            {
                _jvmArgumentsEditButton.Click += OpenJvmArgumentsEditor;
            }

            _selectionIndicator = this.FindControl<Border>("SelectionIndicator");
            _savedSelectionIndicatorTransitions = _selectionIndicator?.Transitions?.ToList();

            _settingsIndicator = this.FindControl<Border>("SettingsIndicator");
            _savedSettingsIndicatorTransitions = _settingsIndicator?.Transitions?.ToList();

            // Ensure these class fields are assigned correctly
            _promoBanner = this.FindControl<Border>("PromoBanner");
            _savedPromoBannerTransitions = _promoBanner?.Transitions?.ToList();

            _launchSection = this.FindControl<Border>("LaunchSection");
            _savedLaunchSectionTransitions = _launchSection?.Transitions?.ToList();

            _newsSectionGrid = this.FindControl<Grid>("NewsSectionGrid"); // This was likely causing the 'not exist' error
            _savedNewsSectionGridTransitions = _newsSectionGrid?.Transitions?.ToList();

            _launchErrorBanner = this.FindControl<Border>("LaunchErrorBanner");
            _savedLaunchErrorBannerTransitions = _launchErrorBanner?.Transitions?.ToList();

            _launchErrorBannerText = this.FindControl<TextBlock>("LaunchErrorBannerText");
            _launchErrorBannerCloseButton = this.FindControl<Button>("LaunchErrorBannerCloseButton");

            _settingsSaveBanner = this.FindControl<Border>("SettingsSaveBanner");
            _savedSettingsSaveBannerTransitions = _settingsSaveBanner?.Transitions?.ToList();

            _accountPanel = this.FindControl<Border>("AccountPanel");
            // AccountPanel's transitions are on its RenderTransform
            // We need to safely get the TranslateTransform first, then its transitions
            if (_accountPanel?.RenderTransform is TranslateTransform accountPanelTt)
            {
                _savedAccountPanelTransitions = accountPanelTt.Transitions?.ToList();
            }
            else
            {
                // If it's not a TranslateTransform or not set, create one to ensure it exists
                var newTt = new TranslateTransform();
                _accountPanel!.RenderTransform = newTt;
                _savedAccountPanelTransitions = newTt.Transitions?.ToList();
            }


            // Find and save transitions for sidebar buttons
            _gameButton = this.FindControl<Border>("GameButton");
            _savedGameButtonTransitions = _gameButton?.Transitions?.ToList();
            _versionsButton = this.FindControl<Border>("VersionsButton");
            _savedVersionsButtonTransitions = _versionsButton?.Transitions?.ToList();
            _serversButton = this.FindControl<Border>("ServersButton");
            _savedServersButtonTransitions = _serversButton?.Transitions?.ToList();
            _modsButton = this.FindControl<Border>("ModsButton");
            _savedModsButtonTransitions = _modsButton?.Transitions?.ToList();
            _settingsButton = this.FindControl<Border>("SettingsButton");
            _savedSettingsButtonTransitions = _settingsButton?.Transitions?.ToList();

            _versionOptiFineSupport = this.FindControl<TextBlock>("VersionOptiFineSupport");
            _serversWrapPanel = this.FindControl<StackPanel>("ServersWrapPanel");
            _quickPlayServersContainer = this.FindControl<StackPanel>("QuickPlayServersContainer");
            _noServersMessage = this.FindControl<Border>("NoServersMessage");
            _quickPlayTooltip = this.FindControl<Border>("QuickPlayTooltip");
            _quickPlayTooltipText = this.FindControl<TextBlock>("QuickPlayTooltipText");

            _savedQuickPlayTooltipTransitions = _quickPlayTooltip?.Transitions?.ToList();

            if (_quickPlayTooltip != null)
            {
                _quickPlayTooltip.IsVisible = false;
                _quickPlayTooltip.Opacity = 0;
            }
            _quickPlayServersContainer = this.FindControl<StackPanel>("QuickPlayServersContainer"); // Re-assignment, fine if intended
            _playingAsImage = this.FindControl<Image>("PlayingAsImage");
            _accountCharacterImage = this.FindControl<Image>("Image");
            _launchProgressPanel = this.FindControl<StackPanel>("LaunchProgressPanel");
            _launchProgressBar = this.FindControl<ProgressBar>("LaunchProgressBar");
            _launchProgressText = this.FindControl<TextBlock>("LaunchProgressText");
            // _selectionIndicator, _settingsIndicator already assigned above
            _modsInfoText = this.FindControl<TextBlock>("ModsInfoText");
            _launchBgImage = this.FindControl<Image>("LaunchBgImage");
            if (_launchBgImage != null)
            {
                _launchBgScale = new ScaleTransform(1, 1);
                _launchBgImage.RenderTransform = _launchBgScale;
                _launchBgImage.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            }
            _particleLayer = this.FindControl<Canvas>("ParticleLayer");
            // _launchSection already assigned above
            SetupParticles();
            _accountPanelOverlay = this.FindControl<Grid>("AccountPanelOverlay");
            // _accountPanel already assigned above
            _accountUsernameDisplay = this.FindControl<TextBlock>("AccountUsernameDisplay");
            _accountUuidDisplay = this.FindControl<TextBlock>("AccountUuidDisplay");
            _playingAsUsername = this.FindControl<TextBlock>("PlayingAsUsername");
            _friendsLoadingPanel = this.FindControl<StackPanel>("FriendsLoadingPanel");
            _noFriendsMessage = this.FindControl<TextBlock>("NoFriendsMessage");
            _newsItem1Image = this.FindControl<Image>("NewsItem1Image");
            if (_newsItem1Image != null)
            {
                _newsItem1Scale = new ScaleTransform(1, 1);
                _newsItem1Image.RenderTransform = _newsItem1Scale;
                _newsItem1Image.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            }
            _newsItem2Image = this.FindControl<Image>("NewsItem2Image");
            if (_newsItem2Image != null)
            {
                _newsItem2Scale = new ScaleTransform(1, 1);
                _newsItem2Image.RenderTransform = _newsItem2Scale;
                _newsItem2Image.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            }
            _gamePage = this.FindControl<Grid>("GamePage");
            _versionsPage = this.FindControl<Grid>("VersionsPage");
            _serversPage = this.FindControl<Grid>("ServersPage");
            _modsPage = this.FindControl<Grid>("ModsPage");
            _settingsPage = this.FindControl<Grid>("SettingsPage");
            _versionDetailsSidebar = this.FindControl<Border>("VersionDetailsSidebar");
            _versionTitle = this.FindControl<TextBlock>("VersionTitle");
            _versionType = this.FindControl<TextBlock>("VersionType");
            _versionLoader = this.FindControl<TextBlock>("VersionLoader");
            _versionDate = this.FindControl<TextBlock>("VersionDate");
            _versionDescription = this.FindControl<TextBlock>("VersionDescription");
            _versionDropdown = this.FindControl<ComboBox>("VersionDropdown");
            _versionBannerContainer = this.FindControl<Border>("VersionBannerContainer");
            _majorVersionsStackPanel = this.FindControl<StackPanel>("MajorVersionsStackPanel");
            _launchVersionText = this.FindControl<TextBlock>("LaunchVersionText");
            _addonFabricButton = this.FindControl<Button>("AddonFabricButton");
            _addonVanillaButton = this.FindControl<Button>("AddonVanillaButton");
            _sidebarHoverTooltip = this.FindControl<Border>("SidebarHoverTooltip");
            _sidebarHoverTooltipText = this.FindControl<TextBlock>("SidebarHoverTooltipText");

            _savedTooltipTransitions = _sidebarHoverTooltip?.Transitions?.ToList();

            if (_sidebarHoverTooltip != null)
            {
                _sidebarHoverTooltip.IsVisible = false;
                _sidebarHoverTooltip.Opacity = 0;
            }

            InitializeSettingsControls();
            WireGameOptionsHandlers();
            InitializeSkinsControls();
            InitializeSkinStatusBanner();

            if (this.FindControl<Button>("LaunchGameButton") is { } launchBtn)
            {
                launchBtn.Click += async (s, e) =>
                {
                    // Case 1: A game is currently running. Terminate it.
                    if (_gameProcess != null && !_gameProcess.HasExited)
                    {
                        try
                        {
                            Console.WriteLine("[Launcher] User clicked to terminate running game process.");
                            _gameProcess.Kill(); // Terminate the running game process
                            _gameProcess.WaitForExit(5000); // Wait a bit for it to exit cleanly
                            Console.WriteLine("[Launcher] Running game process terminated successfully.");
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"[Launcher ERROR] Failed to terminate game process on click: {ex.Message}");
                            ShowLaunchErrorBanner($"Failed to terminate game: {ex.Message}");
                        }
                        finally
                        {
                            // Always reset flags and button state after trying to kill the process
                            _isLaunching = false;
                            _isInstalling = false;
                            _gameProcess = null; // Clear the reference
                            UpdateLaunchButton("LAUNCH GAME", "SeaGreen"); // Reset button to default
                            await UpdateServerButtonStates(); // Re-enable server buttons if they were disabled
                        }
                        return; // Operation handled, exit click handler
                    }

                    // Case 2: An installation or pre-launch task is in progress. Cancel it.
                    if (_isLaunching || _isInstalling)
                    {
                        Console.WriteLine("[Launcher] User clicked to cancel ongoing launch/install operation.");
                        _launchCancellationTokenSource?.Cancel(); // Cancel any ongoing async operations
                        _isLaunching = false;
                        _isInstalling = false;
                        UpdateLaunchButton("LAUNCH CANCELLED", "Orange"); // Indicate cancellation
                        await UpdateServerButtonStates(); // Re-enable server buttons if they were disabled
                        return; // Operation handled, exit click handler
                    }

                    // Case 3: No task in progress and no game running. Proceed with a new launch.
                    var selectedVersionInfo = _allVersions.FirstOrDefault(v => v.FullVersion == _currentSettings.SelectedSubVersion);
                    if (selectedVersionInfo == null)
                    {
                        UpdateLaunchButton("SELECT VERSION", "OrangeRed"); // This will disable the button via ApplyLaunchButtonState
                        ShowLaunchErrorBanner("Please select a Minecraft version to launch.");
                        return;
                    }

                    // Ensure user is logged in (unless it's an offline session explicitly chosen)
                    if (_session == null) // This check needs to be smarter for offline mode
                    {
                        // If offline username is set, it's an offline session, allow.
                        // Otherwise, require login.
                        if (_currentSettings.AccountType != "offline" || string.IsNullOrWhiteSpace(_currentSettings.OfflineUsername))
                        {
                            UpdateLaunchButton("LOGIN REQUIRED", "OrangeRed"); // This will disable the button via ApplyLaunchButtonState
                            ShowLaunchErrorBanner("LOGIN REQUIRED: Please log in to your Minecraft account to launch.");
                            return;
                        }
                    }

                    bool isFabric = selectedVersionInfo.Loader.Equals("Fabric", StringComparison.OrdinalIgnoreCase);
                    await LaunchGameAsync(selectedVersionInfo.FullVersion, isFabric);
                };

                // Add PointerEntered and PointerExited handlers for hover effect
                launchBtn.PointerEntered += (s, e) =>
                {
                    // Check if an operation is in progress OR a game is running
                    if (_isLaunching || _isInstalling || (_gameProcess != null && !_gameProcess.HasExited))
                    {
                        string hoverText;
                        Color tempBaseColor;

                        if (_gameProcess != null && !_gameProcess.HasExited)
                        {
                            hoverText = "TERMINATE GAME"; // Specific text for running game
                            tempBaseColor = Colors.DarkRed; // More aggressive red for termination
                        }
                        else
                        {
                            hoverText = "CANCEL OPERATION"; // Generic for launching/installing
                            tempBaseColor = Colors.Firebrick; // Red for cancellation
                        }

                        // Temporarily change button appearance for hover
                        // Do NOT update _currentOperationText or _currentOperationColor here
                        if (s is Button hoveredBtn)
                        {
                            hoveredBtn.Background = new SolidColorBrush(tempBaseColor);
                            if (this.FindControl<Border>("LaunchButtonOuterBorder") is { } outerBorder)
                            {
                                Color tempBorderColor = DarkenColor(tempBaseColor, 0.2f);
                                outerBorder.BorderBrush = new SolidColorBrush(tempBorderColor);
                            }
                            if (hoveredBtn.Content is StackPanel contentStack && contentStack.Children.OfType<TextBlock>().FirstOrDefault() is TextBlock mainTextBlock)
                            {
                                mainTextBlock.Text = hoverText;
                            }
                        }
                    }
                    // If no operation is active, the button should retain its current (non-hover) appearance,
                    // which is already set by ApplyLaunchButtonState. No action needed here.
                };

                launchBtn.PointerExited += (s, e) =>
                {
                    // Revert to the actual state by reapplying it, which will pick up the stored
                    // _currentOperationText and _currentOperationColor (or default LAUNCH GAME)
                    ApplyLaunchButtonState();
                };
                _settingsSaveBanner = this.FindControl<Border>("SettingsSaveBanner");
                _settingsSaveBannerSaveButton = this.FindControl<Button>("SettingsSaveBannerSaveButton");
                if (_settingsSaveBannerSaveButton != null)
                {
                    _settingsSaveBannerSaveButton.Click += (s, e) =>
                    {
                        SaveSettingsFromUi();
                        HideSettingsSaveBanner();
                    };
                }
                WireSettingsDirtyHandlers();
            }
        }

        private void InitializePlannedUpdatesControls()
        {
            _plannedUpdatesButton = this.FindControl<Button>("PlannedUpdatesButton");
            _plannedUpdatesOverlay = this.FindControl<Grid>("PlannedUpdatesOverlay");
            _plannedUpdatesPanel = this.FindControl<Border>("PlannedUpdatesPanel");
        }

        private async void OpenPlannedUpdates(object? sender, PointerEventArgs e)
        {
            _plannedUpdatesCts?.Cancel(); // Cancel any pending close operation
            _plannedUpdatesCts = new CancellationTokenSource(); // Create a new CTS for the current animation
            var token = _plannedUpdatesCts.Token;

            if (_plannedUpdatesOverlay == null || _plannedUpdatesPanel == null) return;

            _plannedUpdatesOverlay.IsVisible = true;

            // Only reset position/blur if it was fully hidden to ensure smooth re-entry
            if (_plannedUpdatesPanel.Opacity == 0)
            {
                if (_plannedUpdatesPanel.RenderTransform is TranslateTransform tt) tt.Y = 20;
                if (_plannedUpdatesPanel.Effect is BlurEffect blur) blur.Radius = 10;
            }

            if (!AreAnimationsEnabled())
            {
                _plannedUpdatesPanel.Opacity = 1;
                if (_plannedUpdatesPanel.RenderTransform is TranslateTransform t) t.Y = 0;
                if (_plannedUpdatesPanel.Effect is BlurEffect b) b.Radius = 0;
                return;
            }

            // Animate In: Fade In + Slide Up + Unblur
            const int steps = 20;
            for (int i = 0; i <= steps; i++)
            {
                // Stop if a close was requested mid-animation (which cancels this token)
                if (token.IsCancellationRequested) return;

                double t = (double)i / steps;
                double easeOut = 1 - Math.Pow(1 - t, 3); // Cubic Ease Out

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_plannedUpdatesPanel == null) return;
                    _plannedUpdatesPanel.Opacity = t;
                    if (_plannedUpdatesPanel.RenderTransform is TranslateTransform trans)
                        trans.Y = 20 - (20 * easeOut);
                    if (_plannedUpdatesPanel.Effect is BlurEffect b)
                        b.Radius = 10 - (10 * t);
                });
                await Task.Delay(10);
            }
        }

        private void ClosePlannedUpdates(object? sender, PointerEventArgs e)
        {
            _plannedUpdatesCts?.Cancel();
            _plannedUpdatesCts = new CancellationTokenSource();
            var token = _plannedUpdatesCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        if (_plannedUpdatesOverlay == null || _plannedUpdatesPanel == null) return;

                        if (!AreAnimationsEnabled())
                        {
                            _plannedUpdatesOverlay.IsVisible = false;
                            return;
                        }

                        // Animate Out: Fade Out + Slide Down + Blur
                        const int steps = 15;
                        for (int i = 0; i <= steps; i++)
                        {
                            if (token.IsCancellationRequested) return;

                            double t = (double)i / steps;
                            double easeIn = t * t;

                            _plannedUpdatesPanel.Opacity = 1 - t;
                            if (_plannedUpdatesPanel.RenderTransform is TranslateTransform trans)
                                trans.Y = 0 + (20 * easeIn);
                            if (_plannedUpdatesPanel.Effect is BlurEffect b)
                                b.Radius = 0 + (10 * t);

                            await Task.Delay(10);
                        }

                        if (!token.IsCancellationRequested)
                            _plannedUpdatesOverlay.IsVisible = false;
                    });
                }
                catch (TaskCanceledException) { /* Ignore */ }
            });
        }


        private string MaskUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length <= 2)
                return username; // Don't mask very short usernames

            int visibleChars = 1; // Show first and last char
            int maskedLength = username.Length - (visibleChars * 2);

            if (maskedLength <= 0)
                return username; // Username too short to mask meaningfully

            string masked = username[0] + new string('*', maskedLength) + username[username.Length - 1];
            return masked;
        }

        private double _lastAspectRatio = 16.0 / 9.0; // Default aspect ratio

        private void OnResolutionWidthChanged(object? sender, RoutedEventArgs e)
        {
            if (!_currentSettings.LockGameAspectRatio || _isApplyingSettings)
                return;

            if (_gameResolutionWidthTextBox != null && _gameResolutionHeightTextBox != null)
            {
                if (int.TryParse(_gameResolutionWidthTextBox.Text, out int newWidth) && newWidth > 0)
                {
                    // Calculate aspect ratio from current values if both are valid
                    if (int.TryParse(_gameResolutionHeightTextBox.Text, out int currentHeight) && currentHeight > 0)
                    {
                        _lastAspectRatio = (double)currentHeight / newWidth;
                    }

                    // Apply aspect ratio
                    int newHeight = (int)Math.Round(newWidth * _lastAspectRatio);
                    _gameResolutionHeightTextBox.Text = newHeight.ToString();

                    Console.WriteLine($"[Aspect Ratio Lock] Width changed to {newWidth}, adjusted height to {newHeight} (ratio: {_lastAspectRatio:F4})");
                }
            }
        }

        private void OnResolutionHeightChanged(object? sender, RoutedEventArgs e)
        {
            if (!_currentSettings.LockGameAspectRatio || _isApplyingSettings)
                return;

            if (_gameResolutionWidthTextBox != null && _gameResolutionHeightTextBox != null)
            {
                if (int.TryParse(_gameResolutionHeightTextBox.Text, out int newHeight) && newHeight > 0)
                {
                    // Calculate aspect ratio from current values if both are valid
                    if (int.TryParse(_gameResolutionWidthTextBox.Text, out int currentWidth) && currentWidth > 0)
                    {
                        _lastAspectRatio = (double)newHeight / currentWidth;
                    }

                    // Apply aspect ratio
                    int newWidth = (int)Math.Round(newHeight / _lastAspectRatio);
                    _gameResolutionWidthTextBox.Text = newWidth.ToString();

                    Console.WriteLine($"[Aspect Ratio Lock] Height changed to {newHeight}, adjusted width to {newWidth} (ratio: {_lastAspectRatio:F4})");
                }
            }
        }

        private async void ClearLogsButton_Click(object? sender, RoutedEventArgs e)
        {
            var confirmed = await ShowConfirmationDialog("Clear Logs", "Are you sure you want to delete all launcher log files? This action cannot be undone.");
            if (!confirmed) return;

            try
            {
                if (System.IO.Directory.Exists(_logFolderPath))
                {
                    foreach (var file in System.IO.Directory.GetFiles(_logFolderPath, "launcher_log_*.txt"))
                    {
                        try
                        {
                            System.IO.File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"[Clear Logs] Failed to delete log file {System.IO.Path.GetFileName(file)}: {ex.Message}");
                        }
                    }
                    Console.WriteLine("[Clear Logs] All launcher log files cleared.");
                    await ShowAccountActionSuccessDialog("All launcher log files have been cleared.");
                }
                else
                {
                    Console.WriteLine("[Clear Logs] Log folder does not exist.");
                    await ShowAccountActionErrorDialog("Log folder does not exist.");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Clear Logs ERROR] {ex.Message}");
                await ShowAccountActionErrorDialog($"Failed to clear logs: {ex.Message}");
            }
            finally
            {
                await CalculateDiskUsageAsync(); // Recalculate and update UI
            }
        }

        private async void ClearCacheButton_Click(object? sender, RoutedEventArgs e)
        {
            var confirmed = await ShowConfirmationDialog("Clear Cache", "Are you sure you want to delete all launcher cache files? This includes downloaded assets and temporary files. This may temporarily increase game launch times.");
            if (!confirmed) return;

            try
            {
                string minecraftCachePath = System.IO.Path.Combine(_minecraftFolder, "assets", "objects");
                string cmlLibCachePath = System.IO.Path.Combine(_minecraftFolder, "libraries"); // CMLib caches libraries here
                // We should be careful with versions folder, as it contains installed versions.
                // For a full "clear cache", we might want to clean up *old* or *corrupted* versions,
                // but not all of them unless explicitly requested.
                // For now, let's just ensure the core paths exist, but don't aggressively delete versions.
                // If a user wants to truly remove versions, they should do it from the Versions page.

                if (System.IO.Directory.Exists(minecraftCachePath))
                {
                    System.IO.Directory.Delete(minecraftCachePath, true);
                    Console.WriteLine($"[Clear Cache] Deleted Minecraft assets cache: {minecraftCachePath}");
                }
                if (System.IO.Directory.Exists(cmlLibCachePath))
                {
                    System.IO.Directory.Delete(cmlLibCachePath, true);
                    Console.WriteLine($"[Clear Cache] Deleted CMLib libraries cache: {cmlLibCachePath}");
                }

                // Recreate essential directories if they were deleted
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(_minecraftFolder, "assets", "objects"));
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine(_minecraftFolder, "libraries"));

                await ShowAccountActionSuccessDialog("Launcher cache has been cleared.");
                Console.WriteLine("[Clear Cache] Launcher cache cleared successfully.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Clear Cache ERROR] {ex.Message}");
                await ShowAccountActionErrorDialog($"Failed to clear cache: {ex.Message}");
            }
            finally
            {
                await CalculateDiskUsageAsync(); // Recalculate and update UI
            }
        }

        private async Task CalculateDiskUsageAsync()
        {
            if (_logsUsageBar == null || _profilesUsageBar == null || _assetsUsageBar == null || _cacheUsageBar == null ||
                _otherUsageBar == null || _logsUsageText == null || _profilesUsageText == null || _assetsUsageText == null ||
                _cacheUsageText == null || _otherUsageText == null || _totalUsageText == null) return;

            // Run disk calculation on a background thread to avoid blocking UI
            await Task.Run(async () =>
            {
                double logsSize = await GetDirectorySizeAsync(_logFolderPath);
                double profilesSize = await GetDirectorySizeAsync(System.IO.Path.Combine(_minecraftFolder, "versions"));
                double assetsSize = await GetDirectorySizeAsync(System.IO.Path.Combine(_minecraftFolder, "assets"));
                double librariesSize = await GetDirectorySizeAsync(System.IO.Path.Combine(_minecraftFolder, "libraries")); // Use libraries as "cache" for now

                double totalKnownSize = logsSize + profilesSize + assetsSize + librariesSize;

                // Attempt to get the total .minecraft folder size and calculate "Other"
                double minecraftRootSize = await GetDirectorySizeAsync(_minecraftFolder);
                double otherSize = minecraftRootSize - totalKnownSize;
                if (otherSize < 0) otherSize = 0; // Prevent negative if known parts exceed root (e.g., due to symlinks or incomplete scan)

                double totalActualDisplayedSize = totalKnownSize + otherSize; // Sum of all displayed categories

                // Update UI elements on the UI thread
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    // Update TextBlocks
                    _logsUsageText.Text = $"Logs - {FormatBytes(logsSize)}";
                    _profilesUsageText.Text = $"Profiles - {FormatBytes(profilesSize)}";
                    _assetsUsageText.Text = $"Assets - {FormatBytes(assetsSize)}";
                    _cacheUsageText.Text = $"Cache - {FormatBytes(librariesSize)}"; // Display libraries as cache
                    _otherUsageText.Text = $"Other - {FormatBytes(otherSize)}";
                    _totalUsageText.Text = FormatBytes(totalActualDisplayedSize);

                    // Update Usage Bars
                    // The XAML for usage bars is a Grid with 5 columns. We'll set the Width of each Border
                    // within its respective column. For a true continuous bar, a different XAML structure
                    // (e.g., a single Border with a LinearGradientBrush) would be needed.
                    // For now, these are just illustrative widths for their respective columns.

                    // Get the total width of the parent container for the bars
                    double parentWidth = 0;
                    if (_logsUsageBar.Parent is Grid parentGrid)
                    {
                        parentWidth = parentGrid.Bounds.Width;
                    }

                    if (parentWidth > 0 && totalActualDisplayedSize > 0)
                    {
                        _logsUsageBar.Width = (logsSize / totalActualDisplayedSize) * parentWidth;
                        _profilesUsageBar.Width = (profilesSize / totalActualDisplayedSize) * parentWidth;
                        _assetsUsageBar.Width = (assetsSize / totalActualDisplayedSize) * parentWidth;
                        _cacheUsageBar.Width = (librariesSize / totalActualDisplayedSize) * parentWidth;
                        _otherUsageBar.Width = (otherSize / totalActualDisplayedSize) * parentWidth;
                    }
                    else
                    {
                        _logsUsageBar.Width = 0;
                        _profilesUsageBar.Width = 0;
                        _assetsUsageBar.Width = 0;
                        _cacheUsageBar.Width = 0;
                        _otherUsageBar.Width = 0;
                    }
                });
            });
        }
        private async Task<double> GetDirectorySizeAsync(string path)
        {
            if (!System.IO.Directory.Exists(path)) return 0;

            double size = 0;
            try
            {
                await Task.Run(() =>
                {
                    foreach (string file in System.IO.Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            size += new System.IO.FileInfo(file).Length;
                        }
                        catch (UnauthorizedAccessException) { /* Ignore access denied files */ }
                        catch (System.IO.IOException) { /* Ignore other IO errors */ }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Disk Usage] Error calculating size for {path}: {ex.Message}");
            }
            return size;
        }

        private string FormatBytes(double bytes)
        {
            string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024;
            }
            return $"{dblSByte:0.##} {Suffix[i]}";
        }

        private async void SelectGameResolutionPreset(object? sender, RoutedEventArgs e)
        {
            if (_resolutionPresetOverlay == null || _resolutionPresetPanel == null) return;

            if (_resolutionPresetOverlay.IsVisible) return;

            _resolutionPresetOverlay.IsVisible = true;

            var tt = _resolutionPresetPanel.RenderTransform as TranslateTransform ?? new TranslateTransform();
            _resolutionPresetPanel.RenderTransform = tt;

            if (!AreAnimationsEnabled())
            {
                tt.Y = 0;
                _resolutionPresetPanel.Opacity = 1;
                return;
            }

            const int durationMs = 500;
            const int steps = 30;
            int delayMs = durationMs / steps;

            tt.Y = -700;
            _resolutionPresetPanel.Opacity = 0;

            for (int i = 0; i <= steps; i++)
            {
                double t = (double)i / steps;
                double eased = 1 - Math.Pow(1 - t, 3);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    tt.Y = -700 + (700 * eased);
                    _resolutionPresetPanel.Opacity = eased;
                });
                if (i < steps) await Task.Delay(delayMs);
            }
        }


        private void CloseResolutionPreset(object? sender, RoutedEventArgs e)
        {
            if (_resolutionPresetOverlay == null || _resolutionPresetPanel == null) return;

            var tt = _resolutionPresetPanel.RenderTransform as TranslateTransform ?? new TranslateTransform();
            _resolutionPresetPanel.RenderTransform = tt;

            if (!AreAnimationsEnabled())
            {
                tt.Y = -700;
                _resolutionPresetPanel.Opacity = 0;
                _resolutionPresetOverlay.IsVisible = false;
                return;
            }

            _ = Task.Run(async () =>
            {
                const int durationMs = 500;
                const int steps = 30;
                int delayMs = durationMs / steps;

                for (int i = 0; i <= steps; i++)
                {
                    double t = (double)i / steps;
                    double eased = 1 - Math.Pow(1 - t, 3);
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        tt.Y = 0 - (700 * eased);
                        _resolutionPresetPanel.Opacity = 1 - eased;
                    });
                    if (i < steps) await Task.Delay(delayMs);
                }
                await Dispatcher.UIThread.InvokeAsync(() => _resolutionPresetOverlay.IsVisible = false);
            });
        }

        private void OnPresetClicked(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                var parts = tag.Split('x');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int w) &&
                    int.TryParse(parts[1], out int h))
                {
                    if (_gameResolutionWidthTextBox != null) _gameResolutionWidthTextBox.Text = w.ToString();
                    if (_gameResolutionHeightTextBox != null) _gameResolutionHeightTextBox.Text = h.ToString();
                    MarkSettingsDirty();
                }
            }
            CloseResolutionPreset(null, new RoutedEventArgs());
        }


        private void VisualiseGameResolution(object? sender, RoutedEventArgs e)
        {
            if (!int.TryParse(_gameResolutionWidthTextBox?.Text, out int width) || width <= 0 ||
                !int.TryParse(_gameResolutionHeightTextBox?.Text, out int height) || height <= 0)
            {
                _ = ShowAccountActionErrorDialog("Invalid resolution. Enter positive integers for width and height.");
                return;
            }

            // Divide physical target by screen scaling to get the correct window size.
            double scaling = this.RenderScaling;
            double logicalWidth = width / scaling;
            double logicalHeight = height / scaling;

            // Create a separate, borderless window for the preview
            var previewWindow = new Window
            {
                Title = "Resolution Preview",
                SystemDecorations = SystemDecorations.None,
                Width = logicalWidth,
                Height = logicalHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                CanResize = false,
                Topmost = true,
                // Semi-transparent background
                Background = new SolidColorBrush(Color.Parse("#CC101010")),
                TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent },
                ExtendClientAreaToDecorationsHint = true,
                ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome
            };

            // Content showing the dimensions
            var container = new Border
            {
                // Use the app's accent color for the border
                BorderBrush = GetBrush("PrimaryAccentBrush", Brushes.Lime),
                BorderThickness = new Thickness(4),
                Child = new StackPanel
                {
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Spacing = 5,
                    Children =
            {
                new TextBlock
                {
                    Text = $"{width} × {height}",
                    Foreground = Brushes.White,
                    FontWeight = FontWeight.ExtraBold,
                    FontSize = 32,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                },
                new TextBlock
                {
                    Text = $"Physical Pixels (Scale: {scaling:0.##}x)",
                    Foreground = Brushes.LightGray,
                    FontSize = 14,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                },
                new TextBlock
                {
                    Text = "Click anywhere to close",
                    Foreground = Brushes.Gray,
                    FontSize = 12,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 0)
                }
            }
                }
            };

            previewWindow.Content = container;

            // Close if clicked inside the window
            previewWindow.PointerPressed += (_, __) => previewWindow.Close();

            // Close if clicked outside (window loses focus)
            previewWindow.Deactivated += (_, __) => previewWindow.Close();

            previewWindow.Show();
        }



        // ADD close handler
        private void CloseResolutionVisualOverlay(object? sender, RoutedEventArgs e)
        {
            if (_resolutionVisualOverlay == null || _resolutionVisualPanel == null) return;

            var tt = _resolutionVisualPanel.RenderTransform as TranslateTransform ?? new TranslateTransform();
            _resolutionVisualPanel.RenderTransform = tt;

            if (!AreAnimationsEnabled())
            {
                tt.Y = -700;
                _resolutionVisualPanel.Opacity = 0;
                _resolutionVisualOverlay.IsVisible = false;
                return;
            }

            _ = Task.Run(async () =>
            {
                const int durationMs = 500;
                const int steps = 30;
                int delayMs = durationMs / steps;

                for (int i = 0; i <= steps; i++)
                {
                    double t = (double)i / steps;
                    double eased = 1 - Math.Pow(1 - t, 3);
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        tt.Y = 0 - (700 * eased);
                        _resolutionVisualPanel.Opacity = 1 - eased;
                    });
                    if (i < steps) await Task.Delay(delayMs);
                }
                await Dispatcher.UIThread.InvokeAsync(() => _resolutionVisualOverlay.IsVisible = false);
            });
        }




        private async void ShowLaunchFailureBanner(string message)
        {
            if (_launchFailureBanner == null || _launchFailureBannerText == null) return;

            if (_launchFailureBannerShownForCurrentLaunch) return;

            _launchFailureBannerText.Text = message;
            _launchFailureBanner.IsVisible = true;
            _launchFailureBannerShownForCurrentLaunch = true;

            _launchFailureBannerCts?.Cancel();
            _launchFailureBannerCts = new CancellationTokenSource();
            var ct = _launchFailureBannerCts.Token;

            TranslateTransform? transform = _launchFailureBanner.RenderTransform as TranslateTransform;
            if (transform == null)
            {
                transform = new TranslateTransform();
                _launchFailureBanner.RenderTransform = transform;
            }

            if (!AreAnimationsEnabled())
            {
                transform.Y = 0; // Fully visible position
                _launchFailureBanner.Opacity = 1;
                return;
            }

            // Animate slide-down and fade-in
            transform.Y = -100; // Start off-screen top
            _launchFailureBanner.Opacity = 0;

            const int durationMs = 500;
            const int steps = 30;
            const int delayMs = durationMs / steps;

            try
            {
                for (int i = 0; i <= steps; i++)
                {
                    if (ct.IsCancellationRequested) break;

                    double progress = (double)i / steps;
                    double eased = 1 - Math.Pow(1 - progress, 3); // Cubic ease out

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_launchFailureBanner == null || ct.IsCancellationRequested) return;
                        transform.Y = -100 + (100 * eased); // -100 -> 0 (slides into view)
                        _launchFailureBanner.Opacity = eased; // Fade to full opacity
                    });

                    if (i < steps)
                        await Task.Delay(delayMs, ct);
                }

                // Auto-hide after a few seconds
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(5000, ct); // Show for 5 seconds
                        if (!ct.IsCancellationRequested)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() => HideLaunchFailureBanner());
                        }
                    }
                    catch (OperationCanceledException) { /* ignore if cancelled */ }
                }, ct);
            }
            catch (OperationCanceledException)
            {
                // Animation was cancelled (e.g. by HideLaunchFailureBanner), simply return.
            }
        }

        private async void HideLaunchFailureBanner()
        {
            if (_launchFailureBanner == null) return;

            _launchFailureBannerCts?.Cancel(); // Cancel any ongoing auto-hide or show animation
            _launchFailureBannerShownForCurrentLaunch = false; // Reset flag

            TranslateTransform? transform = _launchFailureBanner.RenderTransform as TranslateTransform;
            if (transform == null) return;

            if (!AreAnimationsEnabled())
            {
                transform.Y = -100; // Instantly move off-screen
                _launchFailureBanner.Opacity = 0;
                _launchFailureBanner.IsVisible = false;
                return;
            }

            // Animate slide-up and fade-out
            const int durationMs = 500;
            const int steps = 30;
            const int delayMs = durationMs / steps;

            for (int i = 0; i <= steps; i++)
            {
                double progress = (double)i / steps;
                double eased = 1 - Math.Pow(1 - progress, 3); // Cubic ease out

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_launchFailureBanner == null) return;
                    transform.Y = 0 - (100 * eased); // 0 -> -100 (slides out of view)
                    _launchFailureBanner.Opacity = 1 - eased; // Fade out
                });

                if (i < steps)
                    await Task.Delay(delayMs);
            }

            _launchFailureBanner.IsVisible = false;
        }

        private async void ShowGameStartingBanner(string message)
        {
            if (_gameStartingBanner == null || _gameStartingBannerText == null) return;

            if (_gameStartingBannerShownForCurrentLaunch) return;

            _gameStartingBannerText.Text = message;
            _gameStartingBanner.IsVisible = true;
            _gameStartingBannerShownForCurrentLaunch = true; // Mark as shown for this launch

            _gameStartingBannerCts?.Cancel(); // Cancel any existing auto-hide
            _gameStartingBannerCts = new CancellationTokenSource();
            var ct = _gameStartingBannerCts.Token;

            TranslateTransform? transform = _gameStartingBanner.RenderTransform as TranslateTransform;
            if (transform == null)
            {
                transform = new TranslateTransform();
                _gameStartingBanner.RenderTransform = transform;
            }

            if (!AreAnimationsEnabled())
            {
                transform.Y = 0; // Fully visible position
                _gameStartingBanner.Opacity = 1;
                return;
            }

            // Animate slide-down and fade-in
            transform.Y = -100; // Start off-screen top
            _gameStartingBanner.Opacity = 0;

            const int durationMs = 500;
            const int steps = 30;
            const int delayMs = durationMs / steps;

            for (int i = 0; i <= steps; i++)
            {
                if (ct.IsCancellationRequested) break;

                double progress = (double)i / steps;
                double eased = 1 - Math.Pow(1 - progress, 3); // Cubic ease out

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_gameStartingBanner == null || ct.IsCancellationRequested) return;
                    transform.Y = -100 + (100 * eased); // -100 -> 0 (slides into view)
                    _gameStartingBanner.Opacity = eased * 0.8; // Fade to semi-transparent (80% opacity)
                });

                if (i < steps)
                    await Task.Delay(delayMs, ct);
            }

            // Auto-hide after a few seconds
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(5000, ct); // Show for 5 seconds
                    if (!ct.IsCancellationRequested)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() => HideGameStartingBanner());
                    }
                }
                catch (OperationCanceledException) { /* ignore if cancelled */ }
            }, ct);
        }


        private void HideGameStartingBanner_Click(object? sender, RoutedEventArgs e)
        {
            HideGameStartingBanner();
        }

        private async void HideGameStartingBanner()
        {
            if (_gameStartingBanner == null) return;

            _gameStartingBannerCts?.Cancel(); // Cancel any ongoing auto-hide or show animation

            TranslateTransform? transform = _gameStartingBanner.RenderTransform as TranslateTransform;
            if (transform == null) return;

            if (!AreAnimationsEnabled())
            {
                transform.Y = -100; // Instantly move off-screen
                _gameStartingBanner.Opacity = 0;
                _gameStartingBanner.IsVisible = false;
                return;
            }

            // Animate slide-up and fade-out
            const int durationMs = 500;
            const int steps = 30;
            const int delayMs = durationMs / steps;

            for (int i = 0; i <= steps; i++)
            {
                double progress = (double)i / steps;
                double eased = 1 - Math.Pow(1 - progress, 3); // Cubic ease out

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_gameStartingBanner == null) return;
                    transform.Y = 0 - (100 * eased); // 0 -> -100 (slides out of view)
                    _gameStartingBanner.Opacity = (1 - eased) * 0.8; // Fade out from current opacity
                });

                if (i < steps)
                    await Task.Delay(delayMs);
            }

            _gameStartingBanner.IsVisible = false;
        }

        private async void OpenModBrowser(object? sender, RoutedEventArgs e)
        {
            if (_modBrowserOverlay == null || _modBrowserPanel == null) return;

            _modBrowserOverlay.IsVisible = true;

            if (_modSearchBox != null) _modSearchBox.Text = "";
            await SearchMods("");

            TranslateTransform? tt = _modBrowserPanel.RenderTransform as TranslateTransform;
            if (tt == null)
            {
                tt = new TranslateTransform();
                _modBrowserPanel.RenderTransform = tt;
            }

            if (!AreAnimationsEnabled())
            {
                tt.Y = 0;
                return;
            }

            int durationMs = 500;    // Remove const
            int steps = 30;          // Remove const  
            int delayMs = durationMs / steps; // Now this works fine

            for (int i = 0; i <= steps; i++)
            {
                double progress = (double)i / steps;
                double eased = 1 - Math.Pow(1 - progress, 3);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (tt != null) tt.Y = -800 + (800 * eased);
                });

                if (i < steps)
                    await Task.Delay(delayMs);
            }
        }

        private async void CloseModBrowser(object? sender, RoutedEventArgs e)
        {
            if (_modBrowserOverlay == null || _modBrowserPanel == null) return;

            TranslateTransform? tt = _modBrowserPanel.RenderTransform as TranslateTransform;
            if (tt == null) return;

            if (!AreAnimationsEnabled())
            {
                tt.Y = -800;
                _modBrowserOverlay.IsVisible = false;
                return;
            }

            int durationMs = 500;    // Remove const
            int steps = 30;          // Remove const
            int delayMs = durationMs / steps; // Now this works fine

            for (int i = 0; i <= steps; i++)
            {
                double progress = (double)i / steps;
                double eased = 1 - Math.Pow(1 - progress, 3);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (tt != null) tt.Y = 0 - (800 * eased);
                });

                if (i < steps)
                    await Task.Delay(delayMs);
            }

            _modBrowserOverlay.IsVisible = false;
        }

        // Search functionality with debouncing
        private async void OnModSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            _modSearchCts?.Cancel();
            _modSearchCts = new CancellationTokenSource();

            var searchText = _modSearchBox?.Text ?? "";

            try
            {
                await Task.Delay(500, _modSearchCts.Token); // Debounce 500ms
                await SearchMods(searchText);
            }
            catch (OperationCanceledException)
            {
                // Search was cancelled by new input
            }
        }

        private async Task SearchMods(string query)
        {
            if (_modsResultsPanel == null || _modsLoadingPanel == null || _modsEmptyPanel == null) return;

            // Show loading state
            _modsLoadingPanel.IsVisible = true;
            _modsEmptyPanel.IsVisible = false;
            _modsResultsPanel.IsVisible = false;
            _modsResultsPanel.Children.Clear();

            try
            {
                string apiUrl;
                if (string.IsNullOrWhiteSpace(query))
                {
                    // Get popular mods when no query
                    apiUrl = "https://api.modrinth.com/v2/search?limit=20&index=downloads";
                }
                else
                {
                    // Search with query
                    apiUrl = $"https://api.modrinth.com/v2/search?query={Uri.EscapeDataString(query)}&limit=30";
                }

                var response = await _modrinthClient.GetStringAsync(apiUrl);
                try
                {

                    var searchResponse = JsonSerializer.Deserialize<ModrinthSearchResponse>(response, Json.Options);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _modsResultsPanel.Children.Clear();

                        if (searchResponse?.hits == null || searchResponse.hits.Count == 0)
                        {
                            _modsEmptyPanel.IsVisible = true;
                            _modsLoadingPanel.IsVisible = false;
                            return;
                        }

                        foreach (var mod in searchResponse.hits)
                        {
                            var modCard = CreateModCard(mod);
                            _modsResultsPanel.Children.Add(modCard);
                        }

                        _modsLoadingPanel.IsVisible = false;
                        _modsResultsPanel.IsVisible = true;
                    });

                }
                catch (NotSupportedException ex)
                {
                    Console.WriteLine($"[JSON ERROR in SearchMods] {ex.Message}");
                    Console.WriteLine($"[JSON ERROR] Stack trace: {ex.StackTrace}");
                    throw;
                }


            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _modsLoadingPanel.IsVisible = false;
                    _modsEmptyPanel.IsVisible = true;
                    Console.WriteLine($"[Mod Browser] Error searching mods: {ex.Message}");
                });
            }
        }

        private Border CreateModCard(ModrinthProject mod)
        {
            var card = new Border
            {
                Width = 200,
                // Height removed to avoid big empty space; let content size the card
                Background = GetBrush("CardBackgroundColor"),
                CornerRadius = new CornerRadius(12),
                Cursor = new Cursor(StandardCursorType.Hand),
                Margin = new Thickness(0, 0, 15, 15)
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(120) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // info
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // stats

            // Mod Icon
            var iconBorder = new Border
            {
                Background = GetBrush("HoverBackgroundBrush"),
                CornerRadius = new CornerRadius(12),
                ClipToBounds = true,
                Margin = new Thickness(15, 15, 15, 5)
            };

            var modIcon = new Image
            {
                Width = 80,
                Height = 80,
                Stretch = Avalonia.Media.Stretch.Uniform,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            if (!string.IsNullOrEmpty(mod.icon_url)) _ = LoadModIcon(modIcon, mod.icon_url);
            else
                modIcon.Source = new Avalonia.Media.Imaging.Bitmap(
                    AssetLoader.Open(new Uri("avares://LeafClient/Assets/minecraft.png")));

            iconBorder.Child = modIcon;
            Grid.SetRow(iconBorder, 0);
            grid.Children.Add(iconBorder);

            // Info
            var infoStack = new StackPanel { Spacing = 5, Margin = new Thickness(15, 0, 15, 0) };
            var titleText = new TextBlock
            {
                Text = mod.title,
                Foreground = GetBrush("PrimaryForegroundBrush"),
                FontWeight = FontWeight.Bold,
                FontSize = 14,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                MaxHeight = 40,
                TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
            };
            var descText = new TextBlock
            {
                Text = string.IsNullOrEmpty(mod.description)
                    ? "No description available"
                    : mod.description.Length > 100 ? mod.description.Substring(0, 100) + "..." : mod.description,
                Foreground = GetBrush("SecondaryForegroundBrush"),
                FontSize = 11,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            };
            infoStack.Children.Add(titleText);
            infoStack.Children.Add(descText);
            Grid.SetRow(infoStack, 1);
            grid.Children.Add(infoStack);

            // Stats (no giant gap because both rows are Auto and card height is not fixed)
            var statsStack = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 10,
                Margin = new Thickness(15, 8, 15, 12),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };
            var downloadsText = new TextBlock
            {
                Text = $"⬇️ {FormatCount(mod.downloads)}",
                Foreground = GetBrush("SecondaryForegroundBrush"),
                FontSize = 10
            };
            var followsText = new TextBlock
            {
                Text = $"❤️ {FormatCount(mod.follows)}",
                Foreground = GetBrush("SecondaryForegroundBrush"),
                FontSize = 10
            };
            statsStack.Children.Add(downloadsText);
            statsStack.Children.Add(followsText);
            Grid.SetRow(statsStack, 2);
            grid.Children.Add(statsStack);

            card.Child = grid;

            // Click -> open details sidebar instead of installing
            card.PointerPressed += (s, e) => ShowModDetails(mod, modIcon.Source);

            return card;
        }

        private void ShowModDetails(ModrinthProject mod, IImage? iconSource)
        {
            _selectedMod = mod;

            if (_modDetailsSidebar != null) _modDetailsSidebar.IsVisible = true;
            if (_modDetailsTitle != null) _modDetailsTitle.Text = mod.title;
            if (_modDetailsDescription != null)
                _modDetailsDescription.Text = string.IsNullOrWhiteSpace(mod.description) ? "No description available." : mod.description;

            if (_modDetailsStats != null)
                _modDetailsStats.Text = $"⬇️ {FormatCount(mod.downloads)}    ❤️ {FormatCount(mod.follows)}";

            if (_modDetailsIcon != null)
            {
                if (iconSource != null) _modDetailsIcon.Source = iconSource;
                else if (!string.IsNullOrEmpty(mod.icon_url)) _ = LoadModIcon(_modDetailsIcon, mod.icon_url);
            }
        }

        private string FormatCount(long count)
        {
            if (count >= 1000000) return (count / 1000000.0).ToString("0.0") + "M";
            if (count >= 1000) return (count / 1000.0).ToString("0.0") + "K";
            return count.ToString();
        }


        private async void OnModDetailsDownloadClicked(object? sender, RoutedEventArgs e)
        {
            if (_selectedMod == null) return;
            await InstallMod(_selectedMod);
        }

        private async Task LoadModIcon(Image image, string iconUrl)
        {
            try
            {
                var imageBytes = await _modrinthClient.GetByteArrayAsync(iconUrl);
                using var stream = new MemoryStream(imageBytes);
                image.Source = new Avalonia.Media.Imaging.Bitmap(stream);
            }
            catch
            {
                // Fallback to default icon
                image.Source = new Avalonia.Media.Imaging.Bitmap(
                    AssetLoader.Open(new Uri("avares://LeafClient/Assets/minecraft.png")));
            }
        }

        private string FormatCount(int count)
        {
            if (count >= 1000000)
                return (count / 1000000.0).ToString("0.0") + "M";
            if (count >= 1000)
                return (count / 1000.0).ToString("0.0") + "K";
            return count.ToString();
        }

        private async Task InstallMod(ModrinthProject mod)
        {
            try
            {
                // Get the latest compatible version
                var versionsUrl = $"https://api.modrinth.com/v2/project/{mod.project_id}/version";
                var versionsResponse = await _modrinthClient.GetStringAsync(versionsUrl);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                try
                {

                    var versions = JsonSerializer.Deserialize<List<ModrinthVersionDetailed>>(versionsResponse, Json.Options);


                    // Find a version compatible with current Minecraft version and Fabric
                    var currentVersion = _currentSettings.SelectedSubVersion;
                    var compatibleVersion = versions?.FirstOrDefault(v =>
                        v.GameVersions.Contains(currentVersion) &&
                        v.loaders.Contains("fabric"));

                    if (compatibleVersion == null)
                    {
                        await ShowSimpleDialog("Install Failed",
                            $"No compatible version found for Minecraft {currentVersion} with Fabric.");
                        return;
                    }

                    // Download and install the mod
                    var modFile = compatibleVersion.files.FirstOrDefault(f =>
                        f.filename.EndsWith(".jar", StringComparison.OrdinalIgnoreCase));

                    if (modFile == null)
                    {
                        await ShowSimpleDialog("Install Failed", "No valid mod file found.");
                        return;
                    }

                    // Create installed mod record
                    var installedMod = new InstalledMod
                    {
                        ModId = mod.project_id,
                        Name = mod.title,
                        Description = mod.description ?? "No description available",
                        Version = compatibleVersion.version_number,
                        MinecraftVersion = currentVersion, // Ensure this is correctly set here
                        FileName = modFile.filename,
                        DownloadUrl = modFile.url,
                        Enabled = true, // Enabled by default after install
                        InstallDate = DateTime.Now,
                        IconUrl = mod.icon_url ?? ""
                    };

                    // Download and install the mod file
                    await DownloadAndInstallMod(modFile.url, modFile.filename, mod.title, installedMod);

                    // Add to settings and save (only if not already present by ModId and MC Version)
                    // This prevents duplicates if the user installs the same mod multiple times
                    if (!_currentSettings.InstalledMods.Any(m => m.ModId == installedMod.ModId && m.MinecraftVersion == installedMod.MinecraftVersion))
                    {
                        _currentSettings.InstalledMods.Add(installedMod);
                        Console.WriteLine($"[User Mods] Added '{mod.title}' to settings, now {_currentSettings.InstalledMods.Count} mods total");
                        await _settingsService.SaveSettingsAsync(_currentSettings);
                        Console.WriteLine($"[User Mods] Settings saved successfully");
                    }
                    else
                    {
                        // Update existing mod details (e.g., download URL, enable state)
                        Console.WriteLine($"[User Mods] Mod '{mod.title}' for MC {installedMod.MinecraftVersion} already exists in settings. Updating existing entry.");
                        var existingMod = _currentSettings.InstalledMods.First(m => m.ModId == installedMod.ModId && m.MinecraftVersion == installedMod.MinecraftVersion);
                        existingMod.Name = installedMod.Name;
                        existingMod.Description = installedMod.Description;
                        existingMod.Version = installedMod.Version;
                        existingMod.FileName = installedMod.FileName;
                        existingMod.DownloadUrl = installedMod.DownloadUrl;
                        existingMod.Enabled = true; // Always enable if re-installed/updated
                        existingMod.InstallDate = DateTime.Now;
                        existingMod.IconUrl = installedMod.IconUrl;
                        await _settingsService.SaveSettingsAsync(_currentSettings);
                        Console.WriteLine($"[User Mods] Settings updated for '{mod.title}'.");
                    }


                    // REFRESH THE UI
                    await Dispatcher.UIThread.InvokeAsync(() => { LoadUserMods(); });

                    await ShowSimpleDialog("Success",
                        $"'{mod.title}' has been installed successfully!");

                    CloseModBrowser(null, new RoutedEventArgs());

                }
                catch (NotSupportedException ex)
                {
                    Console.WriteLine($"[JSON ERROR in InstallMod] {ex.Message}");
                    Console.WriteLine($"[JSON ERROR] Stack trace: {ex.StackTrace}");
                    throw;
                }

            }
            catch (JsonException jsonEx)
            {
                await ShowSimpleDialog("Install Failed",
                    $"JSON parsing error: {jsonEx.Message}. The mod data format may have changed.");
            }
            catch (Exception ex)
            {
                await ShowSimpleDialog("Install Failed",
                    $"Failed to install '{mod.title}': {ex.Message}");
            }
        }

        private async Task DownloadAndInstallMod(string downloadUrl, string fileName, string modName, InstalledMod installedMod)
        {
            var modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
            Directory.CreateDirectory(modsFolder);

            var filePath = System.IO.Path.Combine(modsFolder, fileName);

            // Show progress
            ShowProgress(true, $"Downloading {modName}...");

            try
            {
                var modData = await _modrinthClient.GetByteArrayAsync(downloadUrl);
                await File.WriteAllBytesAsync(filePath, modData);

                // Update the installed mod with the actual file path
                installedMod.FileName = fileName;

                Console.WriteLine($"[Mod Install] Successfully installed {modName}");
            }
            finally
            {
                ShowProgress(false);
            }
        }

        private async Task InstallUserModsAsync(string mcVersion)
        {
            string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
            Directory.CreateDirectory(modsFolder);

            // Get only the ENABLED launcher-managed mods for the current MC version
            var modsToEnsurePresent = _currentSettings.InstalledMods
                .Where(m => m.Enabled && m.MinecraftVersion.Equals(mcVersion, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!modsToEnsurePresent.Any())
            {
                Console.WriteLine($"[User Mods] No enabled launcher-managed mods to install for Minecraft {mcVersion}.");
                return;
            }

            foreach (var mod in modsToEnsurePresent)
            {
                try
                {
                    ShowProgress(true, $"Ensuring '{mod.Name}' is installed...");

                    string targetFilePath = GetModFilePath(modsFolder, mod, isDisabled: false); // Should be .jar
                    string disabledFilePath = GetModFilePath(modsFolder, mod, isDisabled: true); // Should be .jar.disabled

                    // If the disabled version exists, but should be enabled, try to move it
                    if (File.Exists(disabledFilePath))
                    {
                        try
                        {
                            File.Move(disabledFilePath, targetFilePath);
                            Console.WriteLine($"[User Mods] Re-enabled '{mod.Name}' from .disabled state.");
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"[User Mods] Failed to re-enable '{mod.Name}': {ex.Message}");
                            ShowLaunchErrorBanner($"Failed to re-enable '{mod.Name}'. It might be in use.");
                            continue; // Skip to next mod
                        }
                    }

                    // Check if the mod (as .jar) already exists
                    if (File.Exists(targetFilePath))
                    {
                        Console.WriteLine($"[User Mods] '{mod.Name}' already exists, skipping download.");
                        continue; // Mod is already present and enabled
                    }

                    // If it's not present (and not disabled), download it
                    var modData = await _modrinthClient.GetByteArrayAsync(mod.DownloadUrl);
                    await File.WriteAllBytesAsync(targetFilePath, modData);

                    Console.WriteLine($"[User Mods] Successfully installed '{mod.Name}'");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[User Mods] Failed to install '{mod.Name}': {ex.Message}");
                    ShowLaunchErrorBanner($"Failed to install '{mod.Name}'. It may not be available for this version.");
                }
                finally
                {
                    ShowProgress(false);
                }
            }
        }

        private async Task ShowSimpleDialog(string title, string message)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var panel = new StackPanel { Margin = new Thickness(20), Spacing = 15 };

            panel.Children.Add(new TextBlock
            {
                Text = message,
                Foreground = GetBrush("PrimaryForegroundBrush"),
                FontWeight = FontWeight.Bold,
                FontSize = 16,
                TextWrapping = TextWrapping.Wrap
            });

            var okButton = new Button
            {
                Content = "OK",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Padding = new Thickness(15, 8),
                Background = GetBrush("PrimaryAccentBrush"),
                Foreground = GetBrush("AccentButtonForegroundBrush"),
                CornerRadius = new CornerRadius(8)
            };

            okButton.Click += (s, e) => dialog.Close();
            panel.Children.Add(okButton);

            dialog.Content = panel;
            await dialog.ShowDialog(this);
        }

        /// <summary>
        /// Opens the feedback overlay for feature suggestions or bug reports.
        /// </summary>
        /// <param name="sender">The UI element that triggered the action.</param>
        /// <param name="e">Event arguments.</param>
        private async void OpenFeedbackOverlay(object? sender, RoutedEventArgs e)
        {
            // First, ensure the overlay container and panel themselves are found.
            // These should have been initialized in InitializeControls.
            if (_feedbackOverlay == null || _feedbackPanel == null)
            {
                // This indicates a critical XAML loading error for the overlay itself.
                Console.Error.WriteLine("[CRITICAL ERROR] FeedbackOverlay or FeedbackPanel were not initialized. Check MainWindow.axaml and InitializeControls.");
                return;
            }

            // If already visible or currently animating, ignore
            if (_feedbackOverlay.IsVisible || _isFeedbackAnimating)
            {
                // If the overlay is already visible but perhaps hidden behind another overlay,
                // ensure it comes to front and its state is correctly set.
                if (_feedbackOverlay.IsVisible)
                {
                    // Bring to front (Avalonia does this automatically with ZIndex, but can re-trigger animations)
                    // For now, just ensure animation is not stuck.
                }
                return;
            }

            // At this point, all feedback controls should be non-null and events wired from InitializeControls.
            // We can now safely access them.
            if (_feedbackTypeComboBox == null || _featureSuggestionPanel == null || _suggestionTextBox == null ||
                _bugReportPanel == null || _expectedBehaviorTextBox == null || _actualBehaviorTextBox == null ||
                _stepsToReproduceTextBox == null || _attachedLogFileName == null || _osVersionTextBox == null ||
                _leafClientVersionTextBox == null || _statusMessageTextBlock == null)
            {
                Console.Error.WriteLine("[CRITICAL ERROR] One or more feedback overlay controls are null after InitializeControls. Feedback system cannot function.");
                _feedbackOverlay.IsVisible = false;
                return;
            }


            _isFeedbackAnimating = true;
            _feedbackOverlay.IsVisible = true;


            // Pre-fill system information (these controls are now guaranteed non-null)
            _osVersionTextBox.Text = Environment.OSVersion.ToString();

            Version? currentAppVersion = GetCurrentAppVersion();
            _leafClientVersionTextBox.Text = currentAppVersion != null ?
                                            $"Launcher v{currentAppVersion.Major}.{currentAppVersion.Minor}.{currentAppVersion.Build}" :
                                            "N/A";

            // Determine initial feedback type based on sender's Tag
            SuggestionType initialType = SuggestionType.Feature; // Default
            if (sender is Button button && button.Tag is string buttonTag)
            {
                if (Enum.TryParse(buttonTag, out SuggestionType parsedType))
                {
                    initialType = parsedType;
                }
            }
            else if (sender is MenuItem menuItem && menuItem.Tag is string menuTag)
            {
                if (Enum.TryParse(menuTag, out SuggestionType parsedType))
                {
                    initialType = parsedType;
                }
            }
            SetInitialFeedbackType(initialType); // Set the ComboBox selection

            // Reset fields
            _suggestionTextBox.Text = string.Empty;
            _expectedBehaviorTextBox.Text = string.Empty;
            _actualBehaviorTextBox.Text = string.Empty;
            _stepsToReproduceTextBox.Text = string.Empty;
            _attachedLogFileContent = null;
            _attachedLogFileName.Text = "No file attached";
            _statusMessageTextBlock.Text = string.Empty;

            _feedbackLogFolderPath = _logFolderPath; // Pass the log folder path from MainWindow

            var tt = _feedbackPanel.RenderTransform as TranslateTransform;
            if (tt == null)
            {
                tt = new TranslateTransform();
                _feedbackPanel.RenderTransform = tt;
            }

            if (!AreAnimationsEnabled())
            {
                tt.Y = 0;
                _feedbackPanel.Opacity = 1;
                _isFeedbackAnimating = false;
                return;
            }

            const int durationMs = 500;
            const int steps = 30;
            const int delayMs = durationMs / steps;

            tt.Y = -700;
            _feedbackPanel.Opacity = 0;

            try
            {
                for (int i = 0; i <= steps; i++)
                {
                    double progress = (double)i / steps;
                    double eased = 1 - Math.Pow(1 - progress, 3);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_feedbackPanel == null) return; // Re-check _feedbackPanel in case it became null
                        tt.Y = -700 + (700 * eased);
                        _feedbackPanel.Opacity = eased;
                    });

                    if (i < steps)
                        await Task.Delay(delayMs);
                }
            }
            finally
            {
                _isFeedbackAnimating = false;
            }
        }

        /// <summary>
        /// Closes the feedback overlay.
        /// </summary>
        /// <param name="sender">The UI element that triggered the action.</param>
        /// <param name="e">Event arguments.</param>
        private async void CloseFeedbackOverlay(object? sender, RoutedEventArgs e)
        {
            if (_feedbackOverlay == null || _feedbackPanel == null) return;

            // Only allow closing if overlay is visible and not already animating
            if (!_feedbackOverlay.IsVisible || _isFeedbackAnimating)
                return;

            _isFeedbackAnimating = true;

            var tt = _feedbackPanel.RenderTransform as TranslateTransform;
            if (tt == null)
            {
                tt = new TranslateTransform();
                _feedbackPanel.RenderTransform = tt;
            }

            if (!AreAnimationsEnabled())
            {
                tt.Y = -700;
                _feedbackPanel.Opacity = 0;
                _feedbackOverlay.IsVisible = false;
                _isFeedbackAnimating = false;
                return;
            }

            const int durationMs = 500;
            const int steps = 30;
            const int delayMs = durationMs / steps;

            try
            {
                for (int i = 0; i <= steps; i++)
                {
                    double progress = (double)i / steps;
                    double eased = 1 - Math.Pow(1 - progress, 3);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_feedbackPanel == null) return;
                        tt.Y = 0 - (700 * eased);
                        _feedbackPanel.Opacity = 1 - eased;
                    });

                    if (i < steps)
                        await Task.Delay(delayMs);
                }

                _feedbackOverlay.IsVisible = false;
            }
            finally
            {
                _isFeedbackAnimating = false;
            }
        }

        /// <summary>
        /// Handles the selection change in the feedback type ComboBox.
        /// </summary>
        private void FeedbackTypeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_feedbackTypeComboBox?.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string tag)
            {
                if (_featureSuggestionPanel != null) _featureSuggestionPanel.IsVisible = (tag == "Feature");
                if (_bugReportPanel != null) _bugReportPanel.IsVisible = (tag == "Bug");
                if (_statusMessageTextBlock != null) _statusMessageTextBlock.Text = ""; // Clear status message on type change
            }
        }

        /// <summary>
        /// Sets the initial selected feedback type in the ComboBox when opening the overlay.
        /// </summary>
        /// <param name="type">The SuggestionType to pre-select.</param>
        private void SetInitialFeedbackType(SuggestionType type)
        {
            if (_feedbackTypeComboBox?.Items is { } items)
            {
                var itemToSelect = items.OfType<ComboBoxItem>()
                                        .FirstOrDefault(item => (item.Tag as string) == type.ToString());
                if (itemToSelect != null)
                {
                    _feedbackTypeComboBox.SelectedItem = itemToSelect;
                }
            }
        }

        /// <summary>
        /// Handles attaching the latest log file to the bug report.
        /// Opens the log file with FileShare.ReadWrite to avoid locking issues.
        /// </summary>
        private async void AttachLogButton_Click(object? sender, RoutedEventArgs e)
        {
            // All these controls should be non-null after InitializeControls
            if (_statusMessageTextBlock == null || _attachedLogFileName == null || _osVersionTextBox == null || _leafClientVersionTextBox == null) return;

            _statusMessageTextBlock.Text = "Loading log file...";
            _statusMessageTextBlock.Foreground = GetBrush("SecondaryForegroundBrush");

            try
            {
                if (!Directory.Exists(_feedbackLogFolderPath))
                {
                    _statusMessageTextBlock.Text = "Logs folder not found.";
                    _statusMessageTextBlock.Foreground = GetBrush("ErrorBrush");
                    return;
                }

                var latestLogFile = Directory.GetFiles(_feedbackLogFolderPath, "launcher_log_*.txt")
                                             .OrderByDescending(f => File.GetCreationTime(f))
                                             .FirstOrDefault();

                if (latestLogFile != null)
                {

                    using (var fs = new FileStream(latestLogFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fs))
                    {
                        _attachedLogFileContent = await reader.ReadToEndAsync();
                    }

                    _attachedLogFileName.Text = System.IO.Path.GetFileName(latestLogFile);
                    _statusMessageTextBlock.Text = "Latest log file attached.";
                    _statusMessageTextBlock.Foreground = GetBrush("SuccessBrush");
                }
                else
                {
                    _attachedLogFileContent = null;
                    _attachedLogFileName.Text = "No log files found.";
                    _statusMessageTextBlock.Text = "No log files found in the logs folder.";
                    _statusMessageTextBlock.Foreground = GetBrush("ErrorBrush");
                }
            }
            catch (Exception ex)
            {
                _attachedLogFileContent = null;
                _attachedLogFileName.Text = "Error attaching file.";
                _statusMessageTextBlock.Text = $"Error attaching log: {ex.Message}";
                _statusMessageTextBlock.Foreground = GetBrush("ErrorBrush");
                Console.Error.WriteLine($"[FeedbackOverlay ERROR] Failed to attach log: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles sending the feedback (suggestion or bug report).
        /// </summary>
        private async void SendFeedbackButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_suggestionsService == null || _statusMessageTextBlock == null) return;

            _statusMessageTextBlock.Text = "Sending feedback...";
            _statusMessageTextBlock.Foreground = GetBrush("PrimaryAccentBrush");

            (bool Success, string Message) result;

            if (_featureSuggestionPanel?.IsVisible == true)
            {
                result = await _suggestionsService.SendFeatureSuggestionAsync(_suggestionTextBox?.Text ?? string.Empty);
            }
            else // BugReportPanel.IsVisible == true
            {
                result = await _suggestionsService.SendBugReportAsync(
                    _expectedBehaviorTextBox?.Text ?? string.Empty,
                    _actualBehaviorTextBox?.Text ?? string.Empty,
                    _stepsToReproduceTextBox?.Text ?? string.Empty,
                    _attachedLogFileContent,
                    _osVersionTextBox?.Text,
                    _leafClientVersionTextBox?.Text
                );
            }

            _statusMessageTextBlock.Text = result.Message;
            _statusMessageTextBlock.Foreground = result.Success ? GetBrush("SuccessBrush") : GetBrush("ErrorBrush");

            if (result.Success)
            {
                await Task.Delay(2000); // Show message for a bit
                CloseFeedbackOverlay(null, new RoutedEventArgs()); // Close the overlay on success
            }
        }

        /// <summary>
        /// Handles the Cancel button click in the feedback overlay.
        /// </summary>
        private void CancelFeedbackButton_Click(object? sender, RoutedEventArgs e)
        {
            CloseFeedbackOverlay(null, new RoutedEventArgs());
        }


        private async void OpenAboutLeafClient(object? sender, RoutedEventArgs e)
        {
            if (_aboutLeafClientOverlay == null || _aboutLeafClientPanel == null) return;

            // If already visible or currently animating, ignore
            if (_aboutLeafClientOverlay.IsVisible || _isAboutLeafClientAnimating)
                return;

            _isAboutLeafClientAnimating = true;
            _aboutLeafClientOverlay.IsVisible = true;

            var tt = _aboutLeafClientPanel.RenderTransform as TranslateTransform;
            if (tt == null)
            {
                tt = new TranslateTransform();
                _aboutLeafClientPanel.RenderTransform = tt;
            }

            if (!AreAnimationsEnabled())
            {
                tt.Y = 0;
                _aboutLeafClientPanel.Opacity = 1;
                _isAboutLeafClientAnimating = false;
                return;
            }

            const int durationMs = 500;
            const int steps = 30;
            const int delayMs = durationMs / steps;

            tt.Y = -700;
            _aboutLeafClientPanel.Opacity = 0;

            try
            {
                for (int i = 0; i <= steps; i++)
                {
                    double progress = (double)i / steps;
                    double eased = 1 - Math.Pow(1 - progress, 3);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_aboutLeafClientPanel == null) return;
                        tt.Y = -700 + (700 * eased);
                        _aboutLeafClientPanel.Opacity = eased;
                    });

                    if (i < steps)
                        await Task.Delay(delayMs);
                }
            }
            finally
            {
                _isAboutLeafClientAnimating = false;
            }
        }

        // 3) REPLACE your existing CloseAboutLeafClient with this

        private async void CloseAboutLeafClient(object? sender, RoutedEventArgs e)
        {
            if (_aboutLeafClientOverlay == null || _aboutLeafClientPanel == null) return;

            // Only allow closing if overlay is visible and not already animating
            if (!_aboutLeafClientOverlay.IsVisible || _isAboutLeafClientAnimating)
                return;

            _isAboutLeafClientAnimating = true;

            var tt = _aboutLeafClientPanel.RenderTransform as TranslateTransform;
            if (tt == null)
            {
                tt = new TranslateTransform();
                _aboutLeafClientPanel.RenderTransform = tt;
            }

            if (!AreAnimationsEnabled())
            {
                tt.Y = -700;
                _aboutLeafClientPanel.Opacity = 0;
                _aboutLeafClientOverlay.IsVisible = false;
                _isAboutLeafClientAnimating = false;
                return;
            }

            const int durationMs = 500;
            const int steps = 30;
            const int delayMs = durationMs / steps;

            try
            {
                for (int i = 0; i <= steps; i++)
                {
                    double progress = (double)i / steps;
                    double eased = 1 - Math.Pow(1 - progress, 3);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_aboutLeafClientPanel == null) return;
                        tt.Y = 0 - (700 * eased);
                        _aboutLeafClientPanel.Opacity = 1 - eased;
                    });

                    if (i < steps)
                        await Task.Delay(delayMs);
                }

                _aboutLeafClientOverlay.IsVisible = false;
            }
            finally
            {
                _isAboutLeafClientAnimating = false;
            }
        }



        // 4) REPLACE your existing OpenCommonQuestions with this

        private async void OpenCommonQuestions(object? sender, RoutedEventArgs e)
        {
            if (_commonQuestionsOverlay == null || _commonQuestionsPanel == null) return;

            // If already visible or currently animating, ignore
            if (_commonQuestionsOverlay.IsVisible || _isCommonQuestionsAnimating)
                return;

            _isCommonQuestionsAnimating = true;
            _commonQuestionsOverlay.IsVisible = true;

            var tt = _commonQuestionsPanel.RenderTransform as TranslateTransform;
            if (tt == null)
            {
                tt = new TranslateTransform();
                _commonQuestionsPanel.RenderTransform = tt;
            }

            if (!AreAnimationsEnabled())
            {
                tt.Y = 0;
                _commonQuestionsPanel.Opacity = 1;
                _isCommonQuestionsAnimating = false;
                return;
            }

            const int durationMs = 500;
            const int steps = 30;
            const int delayMs = durationMs / steps;

            tt.Y = -700;
            _commonQuestionsPanel.Opacity = 0;

            try
            {
                for (int i = 0; i <= steps; i++)
                {
                    double progress = (double)i / steps;
                    double eased = 1 - Math.Pow(1 - progress, 3);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_commonQuestionsPanel == null) return;
                        tt.Y = -700 + (700 * eased);
                        _commonQuestionsPanel.Opacity = eased;
                    });

                    if (i < steps)
                        await Task.Delay(delayMs);
                }
            }
            finally
            {
                _isCommonQuestionsAnimating = false;
            }
        }

        // 5) REPLACE your existing CloseCommonQuestions with this

        private async void CloseCommonQuestions(object? sender, RoutedEventArgs e)
        {
            if (_commonQuestionsOverlay == null || _commonQuestionsPanel == null) return;

            // Only allow closing if overlay is visible and not already animating
            if (!_commonQuestionsOverlay.IsVisible || _isCommonQuestionsAnimating)
                return;

            _isCommonQuestionsAnimating = true;

            var tt = _commonQuestionsPanel.RenderTransform as TranslateTransform;
            if (tt == null)
            {
                tt = new TranslateTransform();
                _commonQuestionsPanel.RenderTransform = tt;
            }

            if (!AreAnimationsEnabled())
            {
                tt.Y = -700;
                _commonQuestionsPanel.Opacity = 0;
                _commonQuestionsOverlay.IsVisible = false;
                _isCommonQuestionsAnimating = false;
                return;
            }

            const int durationMs = 500;
            const int steps = 30;
            const int delayMs = durationMs / steps;

            try
            {
                for (int i = 0; i <= steps; i++)
                {
                    double progress = (double)i / steps;
                    double eased = 1 - Math.Pow(1 - progress, 3);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_commonQuestionsPanel == null) return;
                        tt.Y = 0 - (700 * eased);
                        _commonQuestionsPanel.Opacity = 1 - eased;
                    });

                    if (i < steps)
                        await Task.Delay(delayMs);
                }

                _commonQuestionsOverlay.IsVisible = false;
            }
            finally
            {
                _isCommonQuestionsAnimating = false;
            }
        }




        private void OpenLauncherLogsFolder(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (System.IO.Directory.Exists(_logFolderPath))
                {
                    // Use ShellExecute to open the folder with the default file explorer
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _logFolderPath,
                        UseShellExecute = true,
                        Verb = "open" // Explicitly request to open
                    });
                }
                else
                {
                    Console.WriteLine($"[Logs] Log folder not found: {_logFolderPath}");
                    // Optionally, show a user-friendly message here
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] Failed to open logs folder: {ex.Message}");
                // Optionally, show an error message to the user
            }
        }


        private async Task WarmupServerIconsAsync()
        {
            var serversNeedingIcons = _currentSettings.CustomServers
                .Where(s => string.IsNullOrEmpty(s.IconBase64))
                .ToList();

            using var throttler = new SemaphoreSlim(4);
            var tasks = serversNeedingIcons.Select(async s =>
            {
                await throttler.WaitAsync();
                try
                {
                    // Uses default 5000ms timeout from MinecraftServerChecker
                    var status = await _serverChecker.GetServerStatusAsync(s.Address, s.Port);
                    if (!string.IsNullOrEmpty(status.IconData))
                    {
                        s.IconBase64 = status.IconData;
                    }
                }
                catch { /* ignore */ }
                finally
                {
                    throttler.Release();
                }
            });

            await Task.WhenAll(tasks); // This waits for all icon warmups to finish before saving settings
            await _settingsService.SaveSettingsAsync(_currentSettings);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                LoadServers();
                RefreshQuickPlayBar();
            });
        }

        private async Task RefreshSingleServerAsync(ServerInfo server)
        {
            try
            {
                // Uses default 5000ms timeout from MinecraftServerChecker
                var status = await _serverChecker.GetServerStatusAsync(server.Address, server.Port);

                server.IsOnline = status.IsOnline;
                server.CurrentPlayers = status.CurrentPlayers;
                server.MaxPlayers = status.MaxPlayers;
                server.Motd = status.Motd;

                if (!string.IsNullOrEmpty(status.IconData))
                {
                    server.IconBase64 = status.IconData;
                    await _settingsService.SaveSettingsAsync(_currentSettings);
                }

                // Update the UI on the UI thread immediately after this server's status is known
                await Dispatcher.UIThread.InvokeAsync(() => { UpdateServerCardUI(server); });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server Status Refresh Error] {server.Name}: {ex.Message}");
                server.IsOnline = false;
                server.Motd = "Refresh Failed (No Response)";
                server.CurrentPlayers = 0;
                server.MaxPlayers = 0;
                await Dispatcher.UIThread.InvokeAsync(() => { UpdateServerCardUI(server); });
            }
        }

        private async Task RefreshAllServerStatusesAsync()
        {
            var tasks = _currentSettings.CustomServers.Select(async server =>
            {
                try
                {
                    var status = await _serverChecker.GetServerStatusAsync(server.Address, server.Port);

                    // Update ALL ServerInfo properties BEFORE UI update
                    server.IsOnline = status.IsOnline;
                    server.CurrentPlayers = status.CurrentPlayers;
                    server.MaxPlayers = status.MaxPlayers;
                    server.Motd = status.Motd;

                    // Update status text and color based on online status
                    server.StatusText = status.IsOnline ? "Online" : "Offline";
                    server.StatusColor = status.IsOnline ? Brushes.Green : Brushes.Red;

                    // Update icon if available
                    if (!string.IsNullOrEmpty(status.IconData))
                    {
                        server.IconBase64 = status.IconData;
                    }

                    // Update UI on the UI thread
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        // Access UI element _serversPage.IsVisible on the UI thread
                        if (_serversPage?.IsVisible == true)
                        {
                            UpdateServerCardUI(server);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Server Status] Error checking {server.Name}: {ex.Message}");

                    // Set offline status on error
                    server.IsOnline = false;
                    server.StatusText = "Offline";
                    server.StatusColor = Brushes.Red;
                    server.Motd = "Connection Failed";
                    server.CurrentPlayers = 0;
                    server.MaxPlayers = 0;

                    // Update UI on the UI thread
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        // Access UI element _serversPage.IsVisible on the UI thread
                        if (_serversPage?.IsVisible == true)
                        {
                            UpdateServerCardUI(server);
                        }
                    });
                }
            });

            await Task.WhenAll(tasks);

            // Save updated server data (icons, etc.)
            await _settingsService.SaveSettingsAsync(_currentSettings);

            // CRITICAL: Always update the quick play bar after refreshing server statuses
            await Dispatcher.UIThread.InvokeAsync(() => RefreshQuickPlayBar());

            // After statuses are refreshed, update the enabled state of all server buttons
            await UpdateServerButtonStates();
        }


        private async Task ShowAccountActionSuccessDialog(string message)
        {
            var dialog = new Window
            {
                Title = "Success!",
                Width = 450,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 15,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Operation successful:",
                            Foreground = GetBrush("SuccessBrush"), // Assuming you have a "SuccessBrush"
                            FontWeight = FontWeight.Bold,
                            FontSize = 16,
                            TextWrapping = TextWrapping.Wrap
                        },
                        new TextBlock
                        {
                            Text = message,
                            Foreground = GetBrush("PrimaryForegroundBrush"),
                            FontSize = 13,
                            TextWrapping = TextWrapping.Wrap
                        },
                        new Button
                        {
                            Content = "OK",
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                            Padding = new Thickness(15, 8),
                            Background = GetBrush("PrimaryAccentBrush"),
                            Foreground = GetBrush("AccentButtonForegroundBrush"),
                            CornerRadius = new CornerRadius(8)
                        }
                    }
                }
            };
            if ((dialog.Content as StackPanel)?.Children.OfType<Button>().Last() is Button okButton)
            {
                okButton.Click += (s, e) => dialog.Close();
            }
            await dialog.ShowDialog(this);
        }

        private async Task ShowAccountActionErrorDialog(string message)
        {
            var dialog = new Window
            {
                Title = "Action Failed",
                Width = 450,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 15,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "An error occurred:",
                            Foreground = GetBrush("ErrorBrush"), // Assumes "ErrorBrush" is defined in your styles
                            FontWeight = FontWeight.Bold,
                            FontSize = 16,
                            TextWrapping = TextWrapping.Wrap
                        },
                        new TextBlock
                        {
                            Text = message,
                            Foreground = GetBrush("PrimaryForegroundBrush"),
                            FontSize = 13,
                            TextWrapping = TextWrapping.Wrap
                        },
                        new Button
                        {
                            Content = "OK",
                            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                            Padding = new Thickness(15, 8),
                            Background = GetBrush("PrimaryAccentBrush"),
                            Foreground = GetBrush("AccentButtonForegroundBrush"),
                            CornerRadius = new CornerRadius(8)
                        }
                    }
                }
            };
            // Set the button's click event to close the dialog
            if ((dialog.Content as StackPanel)?.Children.OfType<Button>().Last() is Button okButton)
            {
                okButton.Click += (s, e) => dialog.Close();
            }
            await dialog.ShowDialog(this);
        }

        private async Task<bool> ShowConfirmationDialog(string title, string message)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 450,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var tcs = new TaskCompletionSource<bool>();
            var panel = new StackPanel { Margin = new Thickness(20), Spacing = 15 };

            panel.Children.Add(new TextBlock
            {
                Text = message,
                Foreground = GetBrush("PrimaryForegroundBrush"),
                FontWeight = FontWeight.Bold,
                FontSize = 16,
                TextWrapping = TextWrapping.Wrap
            });

            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 10,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };

            var noButton = new Button { Content = "No", Padding = new Thickness(15, 8), Background = GetBrush("HoverBackgroundBrush"), Foreground = GetBrush("PrimaryForegroundBrush"), CornerRadius = new CornerRadius(8) };
            noButton.Click += (_, __) => { tcs.SetResult(false); dialog.Close(); };

            var yesButton = new Button { Content = "Yes", Padding = new Thickness(15, 8), Background = GetBrush("ErrorBrush"), Foreground = GetBrush("AccentButtonForegroundBrush"), FontWeight = FontWeight.Bold, CornerRadius = new CornerRadius(8) };
            yesButton.Click += (_, __) => { tcs.SetResult(true); dialog.Close(); };

            buttonPanel.Children.Add(noButton);
            buttonPanel.Children.Add(yesButton);
            panel.Children.Add(buttonPanel);

            dialog.Content = panel;
            await dialog.ShowDialog(this);
            return await tcs.Task;
        }

        private async Task<string?> ShowTextInputDialog(string title, string message, string? initialText = null)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 450,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var tcs = new TaskCompletionSource<string?>();
            var panel = new StackPanel { Margin = new Thickness(20), Spacing = 15 };

            panel.Children.Add(new TextBlock
            {
                Text = message,
                Foreground = GetBrush("PrimaryForegroundBrush"),
                FontWeight = FontWeight.Bold,
                FontSize = 16,
                TextWrapping = TextWrapping.Wrap
            });

            var inputTextBox = new TextBox
            {
                Text = initialText ?? "",
                Watermark = "Enter text here...",
                MinWidth = 300,
                Margin = new Thickness(0, 10, 0, 0)
            };
            panel.Children.Add(inputTextBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 10,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };

            var cancelButton = new Button { Content = "Cancel", Padding = new Thickness(15, 8), Background = GetBrush("HoverBackgroundBrush"), Foreground = GetBrush("PrimaryForegroundBrush"), CornerRadius = new CornerRadius(8) };
            cancelButton.Click += (_, __) => { tcs.SetResult(null); dialog.Close(); };

            var okButton = new Button { Content = "OK", Padding = new Thickness(15, 8), Background = GetBrush("PrimaryAccentBrush"), Foreground = GetBrush("AccentButtonForegroundBrush"), FontWeight = FontWeight.Bold, CornerRadius = new CornerRadius(8) };
            okButton.Click += (_, __) => { tcs.SetResult(inputTextBox.Text); dialog.Close(); };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(okButton);
            panel.Children.Add(buttonPanel);

            dialog.Content = panel;
            await dialog.ShowDialog(this);
            return await tcs.Task;
        }

        private async void OnChangeNameClick(object? sender, RoutedEventArgs e)
        {
            Console.WriteLine("[Account] Change Name clicked.");

            // Ensure session is fresh and valid right before action
            await LoadSessionAsync();

            Console.WriteLine($"[Account Debug] OnChangeNameClick - _session is {(_session == null ? "NULL" : "NOT NULL")}");
            if (_session != null) Console.WriteLine($"[Account Debug] OnChangeNameClick - _session.CheckIsValid(): {_session.CheckIsValid()}");
            if (_session != null) Console.WriteLine($"[Account Debug] OnChangeNameClick - _session.AccessToken is {(string.IsNullOrEmpty(_session.AccessToken) ? "NULL/EMPTY" : "NOT NULL/EMPTY")}");
            Console.WriteLine($"[Account Debug] OnChangeNameClick - _currentSettings.IsLoggedIn: {_currentSettings.IsLoggedIn}");


            if (_session == null || !_session.CheckIsValid() || string.IsNullOrEmpty(_session.AccessToken))
            {
                await ShowAccountActionErrorDialog("You must be logged in to change your name.");
                return;
            }

            if (_mojangApiService == null)
            {
                await ShowAccountActionErrorDialog("Mojang API service not initialized.");
                return;
            }

            try
            {
                Console.WriteLine("[Account] Checking name change cooldown status...");
                MojangApiService.NameChangeStatusResponse nameChangeStatus = await _mojangApiService.GetNameChangeStatus(_session.AccessToken);

                if (!nameChangeStatus.ProfileChangeAllowed)
                {
                    string cooldownMessage = $"You cannot change your name yet. Next change available on {nameChangeStatus.GetNextChangeDateTimeFormatted()}.";
                    await ShowAccountActionErrorDialog(cooldownMessage);
                    Console.WriteLine($"[Account] Name change forbidden due to cooldown. {cooldownMessage}");
                    return; // Exit if name change is not allowed
                }

                // Show input dialog for new username
                string? newUsername = await ShowTextInputDialog("Change Username", "Enter your new Minecraft username:", _currentSettings.SessionUsername);

                if (string.IsNullOrWhiteSpace(newUsername) || newUsername == _currentSettings.SessionUsername)
                {
                    Console.WriteLine("[Account] Username change cancelled or same name entered.");
                    return;
                }

                // Confirmation dialog
                bool confirmed = await ShowConfirmationDialog("Confirm Name Change", $"Are you sure you want to change your username to '{newUsername}'? This can only be done once every 30 days.");
                if (!confirmed)
                {
                    Console.WriteLine("[Account] Username change cancelled by user.");
                    return;
                }

                Console.WriteLine($"[Account] Attempting to change username to: {newUsername}");
                // Use our new service method
                MojangApiService.PlayerProfileResponse profile = await _mojangApiService.ChangeName(_session.AccessToken, newUsername);

                // Update session and settings with new username
                _session.Username = profile.Name ?? newUsername; // Use newUsername as fallback
                _currentSettings.SessionUsername = profile.Name ?? newUsername;
                _currentSettings.SessionUuid = profile.Id ?? _session.UUID; // Update UUID if API provides it, else keep old
                await _settingsService.SaveSettingsAsync(_currentSettings);

                Console.WriteLine($"[Account] Username changed successfully to: {profile.Name ?? newUsername}");
                await ShowAccountActionSuccessDialog($"Your username has been changed to '{profile.Name ?? newUsername}'.");

                // Refresh UI elements
                await LoadUserInfoAsync(); // This will refresh all account-related UI including name and skin preview
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Account ERROR] Failed to change username: {ex.Message}");
                await ShowAccountActionErrorDialog($"Failed to change username: {ex.Message}");
            }
        }


        private async Task InitializeDefaultServersAsync()
        {
            _currentSettings.CustomServers ??= new List<ServerInfo>();

            bool changesMade = false;
            var defaults = new List<ServerInfo>
    {
        new ServerInfo { Id = Guid.NewGuid().ToString(), Name = "Hypixel",   Address = "mc.hypixel.net",     Port = 25565 },
        new ServerInfo { Id = Guid.NewGuid().ToString(), Name = "2b2t",      Address = "2b2t.org",           Port = 25565 },
        new ServerInfo { Id = Guid.NewGuid().ToString(), Name = "PvPlegacy", Address = "play.pvplegacy.net", Port = 25565 }
    };

            foreach (var def in defaults)
            {
                if (!_currentSettings.CustomServers.Any(s =>
                    s.Address.Equals(def.Address, StringComparison.OrdinalIgnoreCase)))
                {
                    _currentSettings.CustomServers.Add(def);
                    changesMade = true;
                }
            }

            if (changesMade)
                await _settingsService.SaveSettingsAsync(_currentSettings);
        }



        private void LoadServerData()
        {
            // This method now only sets up the periodic server status refresh timer.
            // Server data loading and default initialization are handled by InitializeDefaultServers() and LoadServers().

            _serverStatusRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };

            _serverStatusRefreshTimer.Tick += async (s, e) =>
            {
                try
                {
                    await RefreshAllServerStatusesAsync(); // Now refreshes statuses for the CustomServers
                }
                catch (Exception ex)
                {
                    // ABSOLUTE SILENCE - NO CONSOLE, NO LOGGING
                    try
                    {
                        // Even the console write might be triggering the ding in some cases
                        // This is the nuclear option
                    }
                    catch { }
                }
            };
        }


        private void ShowOfflineMode()
        {
            Dispatcher.UIThread.Post(() =>
            {
                // Show banner or notification
                var banner = this.FindControl<Border>("OfflineModeBanner");
                if (banner != null)
                {
                    banner.IsVisible = true;
                }
            });
        }

        private Dictionary<string, (ServerStatusResult result, DateTime timestamp)> _statusCache = new();

        private async Task RefreshServerStatusAsync()
        {
            Console.WriteLine("[Server Status] Refreshing server statuses…");

            var accentBrush = GetBrush("PrimaryAccentBrush");
            var hoverBackgroundBrush = GetBrush("HoverBackgroundBrush");
            var accentButtonForegroundBrush = GetBrush("AccentButtonForegroundBrush");
            var disabledForegroundBrush = GetBrush("DisabledForegroundBrush");

            bool isNetworkAvailable = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
            if (!isNetworkAvailable)
            {
                Console.WriteLine("[Server Status] No network, skipping status refresh.");
                return;
            }

            // Use a degree of parallelism to avoid being blocked by very slow servers
            int maxParallel = 3; // or number of servers / 2 — tune this
            await Parallel.ForEachAsync(_currentSettings.CustomServers, new ParallelOptions { MaxDegreeOfParallelism = maxParallel }, async (server, ct) =>
            {
                ServerStatusResult result;
                try
                {
                    result = await _serverChecker.GetServerStatusAsync(server.Address, server.Port);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Server Status] Error checking {server.Name}: {ex}");
                    result = new ServerStatusResult { IsOnline = false, CurrentPlayers = 0, MaxPlayers = 0, Motd = "Error", IconData = "" };
                }

                // Now marshal back to UI thread to update UI elements / controls
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    server.IsOnline = result.IsOnline;
                    server.CurrentPlayers = result.CurrentPlayers;
                    server.MaxPlayers = result.MaxPlayers;
                    server.Motd = result.Motd;
                    server.IconBase64 = result.IconData; // Assign result.IconData (from MineStat) to IconBase64 (in your model)
                                                         // If you had 'server.IconData = result.IconData;' here, delete it.

                    _ = UpdateServerStatusUIAsync(server, accentBrush, hoverBackgroundBrush, accentButtonForegroundBrush, disabledForegroundBrush);
                });
            });
        }



        private async Task UpdateServerStatusUIAsync(
            LeafClient.Models.ServerInfo server, // Ensure this uses LeafClient.Models.ServerInfo
            IBrush accentBrush,
            IBrush hoverBackgroundBrush,
            IBrush accentButtonForegroundBrush,
            IBrush disabledForegroundBrush)
        {
            // This method now directly calls UpdateServerCardUI, which handles the UI updates for dynamic cards.
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateServerCardUI(server);
            });
        }


        private void UpdateStatusDot(Ellipse? statusDot, IBrush color)
        {
            if (statusDot == null) return;

            try
            {
                if (statusDot.Fill != color)
                    statusDot.Fill = color;
            }
            catch { /* Silent failure - don't trigger ding */ }
        }

        private void UpdateStatusText(TextBlock? statusText, string text)
        {
            if (statusText == null) return;

            try
            {
                if (statusText.Text != text)
                    statusText.Text = text;
            }
            catch { /* Silent failure */ }
        }

        private void UpdateMotdText(TextBlock? motdText, string text)
        {
            if (motdText == null) return;

            try
            {
                if (motdText.Text != text)
                    motdText.Text = text;
            }
            catch { /* Silent failure */ }
        }

        private void UpdateJoinButton(Button? joinButton, bool isEnabled, IBrush accentBrush, IBrush hoverBackgroundBrush,
            IBrush accentButtonForegroundBrush, IBrush disabledForegroundBrush)
        {
            if (joinButton == null) return;

            try
            {
                if (joinButton.IsEnabled != isEnabled)
                    joinButton.IsEnabled = isEnabled;

                IBrush targetBackground = isEnabled ? accentBrush : hoverBackgroundBrush;
                if (joinButton.Background != targetBackground)
                    joinButton.Background = targetBackground;

                IBrush targetForeground = isEnabled ? accentButtonForegroundBrush : disabledForegroundBrush;
                if (joinButton.Foreground != targetForeground)
                    joinButton.Foreground = targetForeground;
            }
            catch { /* Silent failure */ }
        }


        private async void ShowSettingsSaveBanner()
        {
            if (_settingsSaveBanner == null) return;

            // Unconditionally make the banner visible first
            _settingsSaveBanner.IsVisible = true;

            // Declare transform once, handling potential null or incorrect type
            TranslateTransform? transform = _settingsSaveBanner.RenderTransform as TranslateTransform;
            if (transform == null)
            {
                transform = new TranslateTransform();
                _settingsSaveBanner.RenderTransform = transform;
            }

            if (!AreAnimationsEnabled())
            {
                // If animations are disabled, set to final visible state instantly
                transform.Y = 0; // Final visible position
                _settingsSaveBanner.Opacity = 1; // Fully opaque
                return; // No animation needed
            }

            // If animations are enabled, proceed with animation
            // Set initial state for animation (hidden below, transparent)
            transform.Y = 80;
            _settingsSaveBanner.Opacity = 0;

            const int durationMs = 300;
            const int steps = 20;
            const int delayMs = durationMs / steps;

            for (int i = 0; i <= steps; i++)
            {
                double progress = (double)i / steps;
                double eased = 1 - Math.Pow(1 - progress, 3); // Cubic ease out

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_settingsSaveBanner == null) return;
                    transform.Y = 80 - (80 * eased); // 80 -> 0
                    _settingsSaveBanner.Opacity = eased; // 0 -> 1
                });

                if (i < steps)
                    await Task.Delay(delayMs);
            }
        }

        private async void HideSettingsSaveBanner()
        {
            if (_settingsSaveBanner == null) return;
            TranslateTransform? transform = _settingsSaveBanner.RenderTransform as TranslateTransform;
            if (transform == null) return;

            // Immediately mark as not dirty, even if animation is still playing.
            // This is crucial for the UI to correctly detect subsequent changes.
            _settingsDirty = false;

            if (!AreAnimationsEnabled())
            {
                // Jump to initial (hidden) state immediately
                transform.Y = 80;
                _settingsSaveBanner.Opacity = 0;
                _settingsSaveBanner.IsVisible = false;
                return;
            }

            // Animate manually
            const int durationMs = 300;
            const int steps = 20;
            const int delayMs = durationMs / steps;

            for (int i = 0; i <= steps; i++)
            {
                double progress = (double)i / steps;
                double eased = 1 - Math.Pow(1 - progress, 3); // Cubic ease out

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_settingsSaveBanner == null) return;
                    transform.Y = 0 + (80 * eased); // 0 -> 80
                    _settingsSaveBanner.Opacity = 1 - eased; // 1 -> 0
                });

                if (i < steps)
                    await Task.Delay(delayMs);
            }

            _settingsSaveBanner.IsVisible = false;
        }

        private void MarkSettingsDirty()
        {
            if (_isApplyingSettings) return;   // ignore while loading/applying
            if (!_settingsDirty)
            {
                _settingsDirty = true;
                ShowSettingsSaveBanner();
            }
        }


        protected override void OnClosing(WindowClosingEventArgs e)
        {
            Console.WriteLine($"[MainWindow] OnClosing called - IsSwapToLogin: {(Application.Current as App)?.IsSwapToLogin}");
            base.OnClosing(e);
        }


        private bool _isShuttingDown = false; // Add this flag at the top of your class with other fields
        private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
        {
            // If we are already in the process of shutting down, just let it happen.
            if (_isShuttingDown)
            {
                e.Cancel = false;
                return;
            }

            // --- Handle Minimize to Tray ---
            if (_currentSettings.MinimizeToTray && !_isExitingApp)
            {
                e.Cancel = true;
                MinimizeToTray();

                if (_currentSettings.ClosingNotificationsPreference == NotificationPreference.Always ||
                    (_currentSettings.ClosingNotificationsPreference == NotificationPreference.JustOnce))
                {
                    Console.WriteLine("[Notification] Launcher is running in the background. Click the tray icon to restore.");
                }

                return;
            }

            // --- If this is a logout scenario, don't do full shutdown ---
            if (_isLoggingOut)
            {
                Console.WriteLine("[MainWindow] Closing during logout - skipping cleanup");
                e.Cancel = false;
                return;
            }

            // --- Start Graceful Shutdown (only for true app exit) ---
            _isShuttingDown = true;
            e.Cancel = true;

            this.Hide();
            Console.WriteLine("[MainWindow] Window hidden. Starting graceful shutdown...");

            // Perform the asynchronous cleanup
            if (_onlineCountService != null)
            {
                try
                {
                    Console.WriteLine("[MainWindow] Attempting to decrement online count before shutdown...");
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                    await _onlineCountService.UpdateCount(false, cts.Token);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[MainWindow ERROR] Failed to decrement online count during shutdown: {ex.Message}");
                }
            }

            // Perform other synchronous cleanup
            StopRichPresence();
            KillMinecraftProcess();
            if (_trayIcon != null)
            {
                _trayIcon.IsVisible = false;
                _trayIcon.Dispose();
            }

            // Close the log writer ONLY during true shutdown
            if (_logStreamWriter != null)
            {
                try
                {
                    Console.WriteLine("[MainWindow] Closing log writer...");
                    Console.SetOut(_originalConsoleOut ?? new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
                    Console.SetError(_originalConsoleError ?? new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
                    _logStreamWriter.Close();
                    _logStreamWriter.Dispose();
                    _logStreamWriter = null;
                }
                catch (Exception ex)
                {
                    _originalConsoleOut?.WriteLine($"[MainWindow ERROR] Failed to close log writer: {ex.Message}");
                }
            }

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                _originalConsoleOut?.WriteLine("[MainWindow] Graceful shutdown complete. Terminating process.");
                desktop.Shutdown();
            }
        }

        // Add this helper in MainWindow
        private async Task<T> RunExclusiveInstall<T>(Func<Task<T>> installAction)
        {
            await _installGate.WaitAsync();
            try
            {
                return await installAction();
            }
            finally
            {
                _installGate.Release();
            }
        }

        private void InitializeTrayIcon()
        {
            try
            {
                _trayIcon = new TrayIcon();

                // Set the icon
                var iconStream = AssetLoader.Open(new Uri("avares://LeafClient/Assets/logo_icon.png"));
                _trayIcon.Icon = new WindowIcon(iconStream);
                _trayIcon.ToolTipText = "Leaf Client";
                _trayIcon.IsVisible = false;

                // Create menu
                var trayMenu = new NativeMenu();

                var homeItem = new NativeMenuItem("Home");
                homeItem.Click += (s, e) => TrayMenuItem_Home();
                trayMenu.Add(homeItem);

                var versionsItem = new NativeMenuItem("Versions");
                versionsItem.Click += (s, e) => TrayMenuItem_Versions();
                trayMenu.Add(versionsItem);

                var serversItem = new NativeMenuItem("Servers");
                serversItem.Click += (s, e) => TrayMenuItem_Servers();
                trayMenu.Add(serversItem);

                var modsItem = new NativeMenuItem("Mods");
                modsItem.Click += (s, e) => TrayMenuItem_Mods();
                trayMenu.Add(modsItem);

                var settingsItem = new NativeMenuItem("Settings");
                settingsItem.Click += (s, e) => TrayMenuItem_Settings();
                trayMenu.Add(settingsItem);

                var skinsItem = new NativeMenuItem("Skins");
                skinsItem.Click += (s, e) => TrayMenuItem_Skins();
                trayMenu.Add(skinsItem);

                trayMenu.Add(new NativeMenuItemSeparator());

                var logoutItem = new NativeMenuItem("Logout");
                logoutItem.Click += (s, e) => TrayMenuItem_Logout();
                trayMenu.Add(logoutItem);

                var exitItem = new NativeMenuItem("Close Launcher");
                exitItem.Click += (s, e) => TrayMenuItem_Exit();
                trayMenu.Add(exitItem);

                _trayIcon.Menu = trayMenu;

                // Double-click to restore window
                _trayIcon.Clicked += (s, e) => RestoreFromTray();

                var trayIconsCollection = new Avalonia.Controls.TrayIcons();
                trayIconsCollection.Add(_trayIcon); // Add the TrayIcon to the collection

                // Pass Application.Current and the populated TrayIcons collection
                TrayIcon.SetIcons(Application.Current, trayIconsCollection);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TrayIcon] Failed to initialize: {ex.Message}");
            }
        }


        // Tray menu item handlers
        private void TrayMenuItem_Home()
        {
            RestoreFromTray();
            SwitchToPage(0);
        }

        private void TrayMenuItem_Versions()
        {
            RestoreFromTray();
            SwitchToPage(1);
        }

        private void TrayMenuItem_Servers()
        {
            RestoreFromTray();
            SwitchToPage(2);
        }

        private void TrayMenuItem_Mods()
        {
            RestoreFromTray();
            SwitchToPage(3);
        }

        private void TrayMenuItem_Settings()
        {
            RestoreFromTray();
            SwitchToPage(4);
        }

        private void TrayMenuItem_Skins()
        {
            RestoreFromTray();
            SwitchToPage(5);
        }

        private void TrayMenuItem_Logout()
        {
            Dispatcher.UIThread.Post(() => LogoutButton_Click(null, new RoutedEventArgs()));
        }

        private void TrayMenuItem_Exit()
        {
            _isExitingApp = true;
            KillMinecraftProcess();
            Dispatcher.UIThread.Post(() =>
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.Shutdown();
                }
            });
        }

        // Method to restore window from tray
        private void RestoreFromTray()
        {
            if (_isShuttingDown) return; // Don't try to restore if we are closing

            Dispatcher.UIThread.Post(() =>
            {
                if (_isShuttingDown) return; // Double-check on UI thread

                try
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                    this.Activate();
                    if (_trayIcon != null)
                        _trayIcon.IsVisible = false;
                }
                catch (InvalidOperationException)
                {
                    // Window is already closed or closing, ignore.
                }
            });
        }


        private void MinimizeToTray()
        {
            this.Hide();
            if (_trayIcon != null)
                _trayIcon.IsVisible = true;

            // Show banner if notifications are allowed
            if (_currentSettings.ClosingNotificationsPreference != NotificationPreference.Never)
            {
                NotificationWindow.Show("Leaf Client", "Running in background", "Restore", () => RestoreFromTray());
            }
        }



        // Helper method to kill Minecraft process
        private void KillMinecraftProcess()
        {
            if (_gameProcess != null && !_gameProcess.HasExited)
            {
                try
                {
                    _gameProcess.Kill();
                    _gameProcess.WaitForExit(5000);
                    Console.WriteLine("[Launcher] Minecraft process terminated.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Launcher] Error terminating game process: {ex.Message}");
                }
            }
        }

        private void ClearModsFolder()
        {
            try
            {
                string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
                if (Directory.Exists(modsFolder))
                {
                    Console.WriteLine("[Launcher] Clearing mods folder for a fresh launch...");
                    Directory.Delete(modsFolder, true); // Recursively delete the folder and its contents
                }
                Directory.CreateDirectory(modsFolder); // Recreate the empty folder
                Console.WriteLine("[Launcher] Mods folder cleared successfully.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Launcher ERROR] Failed to clear mods folder: {ex.Message}");
                // Optionally, show a banner to the user if clearing fails
                ShowLaunchErrorBanner("Failed to clear the mods folder. It might be in use.");
            }
        }


        private void InitializeLauncher()
        {
            var path = new MinecraftPath(_minecraftFolder);
            // Ensure base directory structure exists
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(_minecraftFolder, "versions"));
            _launcher = new MinecraftLauncher(path);
        }



        private async Task LoadSessionAsync()
        {
            try
            {
                _currentSettings = await _settingsService.LoadSettingsAsync(); // Ensure _currentSettings is up-to-date

                if (_currentSettings.IsLoggedIn && _currentSettings.AccountType == "microsoft" &&
                    !string.IsNullOrWhiteSpace(_currentSettings.SessionAccessToken) &&
                    !string.IsNullOrWhiteSpace(_currentSettings.SessionUuid) &&
                    !string.IsNullOrWhiteSpace(_currentSettings.SessionUsername))
                {
                    // Attempt to create a session from saved Microsoft credentials
                    _session = new MSession(
                        _currentSettings.SessionUsername,
                        _currentSettings.SessionAccessToken,
                        _currentSettings.SessionUuid
                    );

                    _session.UserType = "msa"; // Tells Minecraft this is a Microsoft account
                    _session.Xuid = _currentSettings.SessionXuid; // Required for 1.19+ online servers

                    // Crucial: Re-validate the session. Access tokens can expire.
                    // If the session is invalid, we should try to refresh it silently.
                    if (!_session.CheckIsValid())
                    {
                        Console.WriteLine("[Launch] Saved Microsoft session is invalid. Attempting silent re-authentication...");
                        try
                        {
                            var app = await MsalClientHelper.BuildApplicationWithCache("499c8d36-be2a-4231-9ebd-ef291b7bb64c");
                            var loginHandler = new JELoginHandlerBuilder().Build();


                            var authenticator = loginHandler.CreateAuthenticatorWithNewAccount();

                            authenticator.AddMsalOAuth(app, msal => msal.Silent()); // Try silent refresh first
                            authenticator.AddXboxAuthForJE(xbox => xbox.Basic());
                            authenticator.AddForceJEAuthenticator();

                            var refreshedSession = await authenticator.ExecuteForLauncherAsync();

                            if (refreshedSession.CheckIsValid())
                            {
                                _session = refreshedSession;
                                _currentSettings.SessionUsername = _session.Username;
                                _currentSettings.SessionUuid = _session.UUID;
                                _currentSettings.SessionAccessToken = _session.AccessToken;
                                await _settingsService.SaveSettingsAsync(_currentSettings);
                                Console.WriteLine("[Launch] Microsoft session successfully refreshed silently.");
                            }
                            else
                            {
                                Console.WriteLine("[Launch] Silent Microsoft session refresh failed. Forcing re-login.");
                                // If silent refresh fails, it means the refresh token is also expired or invalid.
                                // Force a full re-login by clearing the session.
                                _currentSettings.ClearAuthOnly();
                                await _settingsService.SaveSettingsAsync(_currentSettings);
                                _session = null; // Mark session as null to trigger login required
                            }
                        }
                        catch (Exception exRefresh)
                        {
                            Console.WriteLine($"[Launch] Error during silent Microsoft session refresh: {exRefresh.Message}");
                            _currentSettings.ClearAuthOnly();
                            await _settingsService.SaveSettingsAsync(_currentSettings);
                            _session = null; // Mark session as null to trigger login required
                        }
                    }
                }
                else if (_currentSettings.IsLoggedIn && _currentSettings.AccountType == "offline" &&
                         !string.IsNullOrWhiteSpace(_currentSettings.OfflineUsername))
                {
                    // Offline session
                    _session = MSession.CreateOfflineSession(_currentSettings.OfflineUsername);
                }
                else
                {
                    // Not logged in or invalid settings, ensure _session is null
                    _session = null;
                    // Optionally, clear any stale data if IsLoggedIn is true but data is missing
                    if (_currentSettings.IsLoggedIn)
                    {
                        _currentSettings.ClearAuthOnly();
                        await _settingsService.SaveSettingsAsync(_currentSettings);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Launch] Failed to load or refresh session: {ex.Message}");
                _session = null; // Ensure session is null on any error
                _currentSettings.ClearAuthOnly(); // Clear potentially corrupt data
                await _settingsService.SaveSettingsAsync(_currentSettings);
            }
        }

        /// <summary>
        /// Helper method to upload the skin file to Mojang's servers.
        /// </summary>
        private async Task UploadSkinToMojangAsync(string filePath, string accessToken, string variant = "classic")
        {
            string url = "https://api.minecraftservices.com/minecraft/profile/skins";

            if (!System.IO.File.Exists(filePath)) return;

            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                    using (var form = new MultipartFormDataContent())
                    {
                        // 1. Variant: "classic" (Steve) or "slim" (Alex)
                        form.Add(new StringContent(variant), "variant");

                        // 2. File Content
                        byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                        var imageContent = new ByteArrayContent(fileBytes);
                        imageContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("image/png");

                        form.Add(imageContent, "file", "skin.png");

                        // 3. Send POST
                        var response = await client.PostAsync(url, form);

                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine("[Skin Upload] Success! Skin updated.");

                            // 4. Refresh UI Previews immediately
                            await RefreshAccountPanelPoseAsync();
                            if (_currentSettings.IsLoggedIn)
                            {
                                await UpdateSkinPreviewsAsync(_currentSettings.SessionUsername, _currentSettings.SessionUuid);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[Skin Upload] Failed: {response.StatusCode}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Skin Upload] Error: {ex.Message}");
            }
        }

        private void ShowProgress(bool show, string text = "")
        {
            if (_launchProgressPanel != null)
                _launchProgressPanel.IsVisible = show;
            if (_launchProgressText != null)
                _launchProgressText.Text = text;

            if (show)
            {
                string conciseOperation = "TASK IN PROGRESS"; // Default fallback
                if (text.StartsWith("Downloading Minecraft"))
                    conciseOperation = "DOWNLOADING MINECRAFT";
                else if (text.StartsWith("Installing Fabric loader"))
                    conciseOperation = "INSTALLING FABRIC";
                else if (text.StartsWith("Preparing Fabric profile"))
                    conciseOperation = "PREPARING FABRIC PROFILE";
                else if (text.StartsWith("Installing OptiFine & OptiFabric"))
                    conciseOperation = "INSTALLING OPTIFINE";
                else if (text.StartsWith("Installing Fabric API"))
                    conciseOperation = "INSTALLING FABRIC API";
                else if (text.StartsWith("Installing Sodium")) // Added for Sodium
                    conciseOperation = "INSTALLING SODIUM";
                else if (text.StartsWith("Installing Lithium")) // Added for Lithium
                    conciseOperation = "INSTALLING LITHIUM";
                else if (text.StartsWith("Downloading OptiFine for Fabric modpack")) // Added for OptiFine pack
                    conciseOperation = "DOWNLOADING OPTIFINE PACK";


                _currentOperationText = conciseOperation;
                _currentOperationColor = "DeepSkyBlue"; // Progress is usually blue
                _isInstalling = true; // Indicate that an installation is in progress

                // When progress starts, the button should show the task, not "CANCEL OPERATION" yet.
                // The "CANCEL OPERATION" will appear on hover.
                UpdateLaunchButton(conciseOperation, "DeepSkyBlue"); // Show the current task
            }
            else // When progress is hidden
            {
                _isInstalling = false; // Installation is no longer in progress
                                       // When progress finishes, the button should revert to LAUNCH GAME (or other appropriate state)
                ApplyLaunchButtonState(); // Let ApplyLaunchButtonState determine the next appropriate state
            }
        }

        private bool IsOptiFineForFabricSupported(string version)
        {
            // Check if OptiFine is actually available for this version
            var supportedVersions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "1.21.10", "1.21.9", "1.21.8", "1.21.7", "1.21.6", "1.21.5", "1.21.4", "1.21.3", "1.21.1", // <-- Added "1.21.3"
    "1.20.6", "1.20.5", "1.20.4", "1.20.1", "1.19.2", "1.18.2", "1.17.1", "1.16.5"
};

            bool isSupported = supportedVersions.Contains(version);

            // If the version isn't supported, disable the toggle
            if (!isSupported && _optiFineToggle != null && _optiFineToggle.IsChecked == true)
            {
                _optiFineToggle.IsChecked = false;
                Console.WriteLine($"[OptiFine] Version {version} not supported. Disabled toggle.");
            }

            return isSupported;
        }

        private async Task<string?> GetOptiFineMrpackUrlForVersion(string mcVersion)
        {
            Console.WriteLine($"[OptiFineForFabric] Looking for .mrpack for MC {mcVersion}...");
            try
            {
                using var client = new HttpClient();
                string baseApiUrl = "https://api.modrinth.com/v2/project/BHtwz1lb/version";

                // --- Attempt 1: Exact Version Match ---
                string exactVersionUrl = $"{baseApiUrl}?game_versions=[\"{mcVersion}\"]&loaders=[\"fabric\"]";
                var json = await client.GetStringAsync(exactVersionUrl);
                var versions = JsonSerializer.Deserialize<List<ModrinthVersion>>(json, Json.Options);

                if (versions != null && versions.Any())
                {
                    var latestVersion = versions.First();
                    var mrpackFile = latestVersion.files?.FirstOrDefault(f => !string.IsNullOrEmpty(f?.filename) && f.filename.EndsWith(".mrpack", StringComparison.OrdinalIgnoreCase));
                    if (mrpackFile?.url != null)
                    {
                        return mrpackFile.url;
                    }
                }

                // --- Attempt 2: Major.Minor Version Match ---
                string[] mcVersionParts = mcVersion.Split('.');
                if (mcVersionParts.Length >= 2)
                {
                    string majorMinorVersion = $"{mcVersionParts[0]}.{mcVersionParts[1]}";
                    string majorMinorUrl = $"{baseApiUrl}?game_versions=[\"{majorMinorVersion}\"]&loaders=[\"fabric\"]";
                    json = await client.GetStringAsync(majorMinorUrl);
                    versions = JsonSerializer.Deserialize(json, JsonContext.Default.ListModrinthVersion);

                    if (versions != null && versions.Any())
                    {
                        var latestVersion = versions.First();
                        var mrpackFile = latestVersion.files?.FirstOrDefault(f => !string.IsNullOrEmpty(f?.filename) && f.filename.EndsWith(".mrpack", StringComparison.OrdinalIgnoreCase));
                        if (mrpackFile?.url != null)
                        {
                            return mrpackFile.url;
                        }
                    }
                }

                // --- Attempt 3: Fallback to manual parsing ---
                Console.WriteLine("[OptiFineForFabric] No direct API filter match. Falling back to manual search of all versions.");
                json = await client.GetStringAsync(baseApiUrl); // Fetch all versions
                var allVersions = JsonSerializer.Deserialize<List<ModrinthVersion>>(json, Json.Options);

                if (allVersions != null && allVersions.Any())
                {
                    if (Version.TryParse(mcVersion, out Version? targetSemver))
                    {
                        Models.ModrinthVersion? bestFallbackMatch = null; // Correctly typed
                        Version bestFallbackSemver = new Version(0, 0);

                        foreach (var v in allVersions)
                        {
                            if (!v.loaders?.Any(l => l.Equals("fabric", StringComparison.OrdinalIgnoreCase)) ?? true) continue;

                            foreach (string gameVerStr in v.GameVersions ?? new List<string>())
                            {
                                if (Version.TryParse(gameVerStr, out Version? candidateSemver))
                                {
                                    if (candidateSemver <= targetSemver && candidateSemver > bestFallbackSemver)
                                    {
                                        bestFallbackSemver = candidateSemver;
                                        bestFallbackMatch = v; // This now works
                                    }
                                }
                            }
                        }

                        if (bestFallbackMatch != null)
                        {
                            var mrpackFile = bestFallbackMatch.files?.FirstOrDefault(f => !string.IsNullOrEmpty(f?.filename) && f.filename.EndsWith(".mrpack", StringComparison.OrdinalIgnoreCase));
                            if (mrpackFile?.url != null)
                            {
                                Console.WriteLine($"[OptiFineForFabric] Found fallback match via manual search: '{bestFallbackMatch.name}'");
                                return mrpackFile.url;
                            }
                        }
                    }
                }

                Console.Error.WriteLine($"[OptiFineForFabric ERROR] All attempts failed. No compatible .mrpack found for Minecraft {mcVersion}.");
                return null;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[OptiFineForFabric ERROR] An exception occurred while fetching/parsing Modrinth data: {ex.Message}");
                return null;
            }
        }

        private async Task<bool> DownloadFileAsync(string url, string destinationPath)
        {
            try
            {
                using var client = new HttpClient();
                var downloadUrl = url; // Use the URL directly as it's already a raw link

                var response = await client.GetAsync(downloadUrl);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[DownloadFile] Failed to download from {downloadUrl}: {response.StatusCode}");
                    Console.WriteLine($"[DownloadFile] Response content: {await response.Content.ReadAsStringAsync()}"); // Log response for debugging
                    return false;
                }

                var fileBytes = await response.Content.ReadAsByteArrayAsync();

                await System.IO.File.WriteAllBytesAsync(destinationPath, fileBytes);
                Console.WriteLine($"[DownloadFile] Downloaded: {System.IO.Path.GetFileName(destinationPath)}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DownloadFile] Download error: {ex.Message}");
                return false;
            }
        }

        private async Task RegisterAutoInstalledMod(string modId, string modName, string version, string mcVersion, string fileName, string url)
        {
            // Check if this specific mod file is already tracked
            var existing = _currentSettings.InstalledMods.FirstOrDefault(m =>
                m.ModId == modId &&
                m.MinecraftVersion == mcVersion);

            if (existing == null)
            {
                _currentSettings.InstalledMods.Add(new InstalledMod
                {
                    ModId = modId,
                    Name = modName,
                    Description = $"Auto-installed {modName}",
                    Version = version,
                    MinecraftVersion = mcVersion,
                    FileName = fileName,
                    DownloadUrl = url,
                    Enabled = true,
                    InstallDate = DateTime.Now,
                    IconUrl = ""
                });
                Console.WriteLine($"[Mod Tracker] Registered {modName} for MC {mcVersion} in settings.");
            }
            else
            {
                // Update details if it exists (e.g. filename changed or url updated)
                existing.FileName = fileName;
                existing.DownloadUrl = url;
                existing.Version = version;
                existing.Enabled = true; // Ensure it's enabled
            }

            await _settingsService.SaveSettingsAsync(_currentSettings);
        }


        private async Task<bool> InstallSodiumIfNeededAsync(string mcVersion)
        {
            if (!_currentSettings.IsSodiumEnabled)
            {
                Console.WriteLine("[Sodium] Installation skipped - disabled in settings.");
                return true;
            }

            string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
            System.IO.Directory.CreateDirectory(modsFolder);

            try
            {
                ShowProgress(true, $"Installing Sodium for Minecraft {mcVersion}...");

                using var client = new HttpClient();
                var apiUrl = $"https://api.modrinth.com/v2/project/AANobbMI/version?game_versions=[\"{mcVersion}\"]&loaders=[\"fabric\"]";
                var response = await client.GetStringAsync(apiUrl);

                if (string.IsNullOrWhiteSpace(response)) throw new Exception("Empty response from Modrinth API for Sodium");

                var versions = JsonSerializer.Deserialize<List<ModrinthVersion>>(response, Json.Options);
                var latest = versions?.FirstOrDefault();

                if (latest?.files == null || latest.files.Count == 0) throw new Exception("No download files found for Sodium");

                var file = latest.files.FirstOrDefault(f => f.filename?.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) == true) ?? latest.files.First();
                var downloadUrl = file.url;

                if (string.IsNullOrWhiteSpace(downloadUrl)) throw new Exception("Invalid download URL for Sodium");

                Console.WriteLine($"[Sodium] Downloading from: {downloadUrl}");
                var jarBytes = await client.GetByteArrayAsync(downloadUrl);

                string fileName = $"sodium-fabric-{mcVersion}.jar";
                string sodiumPath = System.IO.Path.Combine(modsFolder, fileName);

                await File.WriteAllBytesAsync(sodiumPath, jarBytes);
                Console.WriteLine($"[Sodium] Successfully installed for {mcVersion} at {sodiumPath}");

                // TRACKING FIX: Register the mod so the cleaner knows about it later
                await RegisterAutoInstalledMod("sodium", "Sodium", latest.versionNumber, mcVersion, fileName, downloadUrl);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Sodium] Installation failed: {ex.Message}");
                ShowLaunchErrorBanner($"Failed to install Sodium for {mcVersion}. It may not be available for this version.");
                return false;
            }
            finally
            {
                ShowProgress(false);
            }
        }



        private async Task<bool> ProcessModrinthPackInstallation(string mrpackPath, string modsFolder, string mcVersion)
        {
            Console.WriteLine($"[Modpack] Starting to process .mrpack installation from {mrpackPath} for MC {mcVersion}...");

            string tempExtractionFolder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ModrinthPackExtract_" + Guid.NewGuid());
            System.IO.Directory.CreateDirectory(tempExtractionFolder);
            Console.WriteLine($"[Modpack] Created temporary extraction folder: {tempExtractionFolder}");

            try
            {
                // 1. Extract the .mrpack (which is a ZIP file) to a temporary folder
                try
                {
                    ShowProgress(true, $"Extracting modpack files for {mcVersion}...");
                    System.IO.Compression.ZipFile.ExtractToDirectory(mrpackPath, tempExtractionFolder);
                    Console.WriteLine($"[Modpack] Successfully extracted .mrpack to {tempExtractionFolder}");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Modpack ERROR] Error extracting .mrpack '{mrpackPath}': {ex.GetType().Name} — {ex.Message}");
                    return false;
                }

                // 2. Process overrides directories if present
                string overridesDir = System.IO.Path.Combine(tempExtractionFolder, "overrides");
                if (System.IO.Directory.Exists(overridesDir))
                {
                    Console.WriteLine("[Modpack] Found 'overrides' directory, attempting to copy to .minecraft folder...");
                    CopyDirectory(overridesDir, _minecraftFolder);
                    Console.WriteLine("[Modpack] Finished copying 'overrides'.");
                }

                string clientOverrides = System.IO.Path.Combine(tempExtractionFolder, "client-overrides");
                if (System.IO.Directory.Exists(clientOverrides))
                {
                    Console.WriteLine("[Modpack] Found 'client-overrides' directory, attempting to copy to .minecraft folder...");
                    CopyDirectory(clientOverrides, _minecraftFolder);
                    Console.WriteLine("[Modpack] Finished copying 'client-overrides'.");
                }

                string serverOverrides = System.IO.Path.Combine(tempExtractionFolder, "server-overrides");
                if (System.IO.Directory.Exists(serverOverrides))
                {
                    Console.WriteLine("[Modpack] Found 'server-overrides' directory, attempting to copy to .minecraft folder...");
                    CopyDirectory(serverOverrides, _minecraftFolder);
                    Console.WriteLine("[Modpack] Finished copying 'server-overrides'.");
                }

                // 3. Parse modrinth.index.json to get actual mod files and their download URLs
                string indexPath = System.IO.Path.Combine(tempExtractionFolder, "modrinth.index.json");
                if (!System.IO.File.Exists(indexPath))
                {
                    Console.Error.WriteLine($"[Modpack ERROR] modrinth.index.json not found at {indexPath}. Cannot install mods.");
                    return false;
                }

                string indexJson = await System.IO.File.ReadAllTextAsync(indexPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var pack = JsonSerializer.Deserialize<ModrinthPack>(indexJson, Json.Options);

                if (pack == null || pack.Files == null || pack.Files.Count == 0)
                {
                    Console.Error.WriteLine("[Modpack ERROR] ModrinthPack deserialized to null or contains no files.");
                    return false;
                }

                using (var httpClient = new HttpClient()) // Use a local HttpClient for this loop
                {
                    foreach (var file in pack.Files)
                    {
                        if (file == null)
                        {
                            Console.WriteLine("[Modpack] Skipping null file entry in modrinth.index.json.");
                            continue;
                        }

                        // Sanity check for path (important for security with modpacks)
                        if (string.IsNullOrEmpty(file.Path) || file.Path.Contains("..") || System.IO.Path.IsPathRooted(file.Path))
                        {
                            Console.Error.WriteLine($"[Modpack ERROR] Unsafe or invalid file path '{file.Path}', skipping download.");
                            continue;
                        }

                        // Only download .jar files into the mods folder
                        if (!file.Path.StartsWith("mods/", StringComparison.OrdinalIgnoreCase) || !file.Path.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine($"[Modpack] Skipping non-mod file or non-jar file from modrinth.index.json: {file.Path}");
                            continue;
                        }

                        if (file.Downloads == null || file.Downloads.Count == 0)
                        {
                            Console.Error.WriteLine($"[Modpack ERROR] No download URLs found for file: {file.Path}, skipping.");
                            continue;
                        }

                        string fileName = System.IO.Path.GetFileName(file.Path);
                        string destPath = System.IO.Path.Combine(modsFolder, fileName);
                        string downloadUrl = file.Downloads[0]; // Use the first download URL

                        Console.WriteLine($"[Modpack] Downloading mod: {fileName} from {downloadUrl}");
                        ShowProgress(true, $"Downloading {fileName}...");

                        try
                        {
                            byte[] modData = await httpClient.GetByteArrayAsync(downloadUrl);

                            // Optional: Hash validation
                            if (file.Hashes != null && file.Hashes.TryGetValue("sha512", out var expectedHash))
                            {
                                using var sha512 = System.Security.Cryptography.SHA512.Create();
                                var actualHashBytes = sha512.ComputeHash(modData);
                                var actualHashString = BitConverter.ToString(actualHashBytes).Replace("-", "").ToLowerInvariant();

                                if (!actualHashString.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
                                {
                                    Console.Error.WriteLine($"[Modpack ERROR] Hash mismatch for {fileName}! Expected: {expectedHash}, Got: {actualHashString}. Skipping file.");
                                    continue; // Skip this file due to hash mismatch
                                }
                                Console.WriteLine($"[Modpack] Hash validated for {fileName}.");
                            }

                            await System.IO.File.WriteAllBytesAsync(destPath, modData);
                            Console.WriteLine($"[Modpack] Successfully installed mod: {fileName} to {destPath}");

                            var installedMod = new InstalledMod
                            {
                                ModId = System.IO.Path.GetFileNameWithoutExtension(fileName), // Use filename as a simple ID for now
                                Name = fileName, // Use filename as name for now
                                Description = "Installed from Modrinth pack",
                                Version = "N/A", // Version is not easily extracted from .mrpack file entry
                                MinecraftVersion = mcVersion, // Modpack is for this MC version
                                FileName = fileName,
                                DownloadUrl = downloadUrl,
                                Enabled = true, // Enabled by default
                                InstallDate = DateTime.Now,
                                IconUrl = "" // No icon URL in .mrpack file entry
                            };

                            // Check for existing entry to avoid duplicates or update if necessary
                            var existingMod = _currentSettings.InstalledMods.FirstOrDefault(m => m.ModId == installedMod.ModId && m.MinecraftVersion == installedMod.MinecraftVersion);
                            if (existingMod == null)
                            {
                                _currentSettings.InstalledMods.Add(installedMod);
                                Console.WriteLine($"[Modpack] Added '{installedMod.Name}' to settings.");
                            }
                            else
                            {
                                // Update existing mod details (e.g., download URL, enable state)
                                existingMod.Name = installedMod.Name;
                                existingMod.Description = installedMod.Description;
                                existingMod.Version = installedMod.Version;
                                existingMod.FileName = installedMod.FileName;
                                existingMod.DownloadUrl = installedMod.DownloadUrl;
                                existingMod.Enabled = true; // Always enable if re-installed via pack
                                existingMod.InstallDate = DateTime.Now;
                                existingMod.IconUrl = installedMod.IconUrl;
                                Console.WriteLine($"[Modpack] Updated '{installedMod.Name}' in settings.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"[Modpack ERROR] Error downloading or writing mod {fileName}: {ex.GetType().Name} — {ex.Message}");
                        }
                    }
                }

                // Save settings after all mods from the pack are processed
                await _settingsService.SaveSettingsAsync(_currentSettings);
                Console.WriteLine("[Modpack] Settings saved after pack installation.");

                Console.WriteLine($"[Modpack] Modpack installation for {mcVersion} completed successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Modpack ERROR] Unexpected error during modpack processing: {ex.GetType().Name} — {ex.Message}");
                return false;
            }
            finally
            {
                ShowProgress(false); // Hide progress bar
                                     // Clean up the temporary extraction folder
                try
                {
                    if (System.IO.Directory.Exists(tempExtractionFolder))
                    {
                        System.IO.Directory.Delete(tempExtractionFolder, true);
                        Console.WriteLine($"[Modpack] Cleaned up temporary extraction folder: {tempExtractionFolder}");
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Modpack ERROR] Cleanup failed for {tempExtractionFolder}: {ex.Message}");
                }
            }
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            foreach (var dir in System.IO.Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                string targetDir = dir.Replace(sourceDir, destDir);
                System.IO.Directory.CreateDirectory(targetDir);
            }

            foreach (var file in System.IO.Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                string targetFile = file.Replace(sourceDir, destDir);
                try
                {
                    System.IO.File.Copy(file, targetFile, overwrite: true);
                    Console.WriteLine($"[Modpack] Successfully copied override file: {targetFile}");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Modpack ERROR] Failed to copy override file {file} to {targetFile}: {ex.Message}");
                }
            }
        }


        // Add this new method to handle launching from the versions sidebar
        private async void LaunchFromVersionsSidebar(object? sender, RoutedEventArgs e)
        {
            // 1. Switch to the Game/Launch page (index 0)
            AnimateSelectionIndicator(0);
            _currentSelectedIndex = 0;
            SwitchToPage(0);

            // 2. Small delay to let the page switch animation complete
            await Task.Delay(300);

            // 3. Trigger the launch button click programmatically
            var launchBtn = this.FindControl<Button>("LaunchGameButton");
            if (launchBtn != null)
            {
                // Simulate the launch button click
                if (_isLaunching)
                {
                    if (_gameProcess != null && !_gameProcess.HasExited)
                    {
                        _gameProcess.Kill();
                    }
                    _isLaunching = false;
                    UpdateLaunchButton("LAUNCH GAME", "SeaGreen");
                    return;
                }

                var selectedVersionInfo = _allVersions.FirstOrDefault(v => v.FullVersion == _currentSettings.SelectedSubVersion);
                if (selectedVersionInfo == null)
                {
                    UpdateLaunchButton("SELECT VERSION", "OrangeRed");
                    return;
                }

                bool isFabric = selectedVersionInfo.Loader.Equals("Fabric", StringComparison.OrdinalIgnoreCase);
                await LaunchGameAsync(selectedVersionInfo.FullVersion, isFabric);
            }
        }

        private async void LaunchGameToServer(ServerInfo server)
        {
            // If a launch is already in progress, or launcher isn't ready, do nothing.
            // This check is crucial for preventing multiple launches.
            if (_isLaunching || _launcher == null)
            {
                Console.WriteLine("[Launch] Game is already launching or launcher is not initialized. Aborting new launch request.");
                return;
            }

            _currentSettings.QuickJoinServerAddress = server.Address;
            _currentSettings.QuickJoinServerPort = server.Port.ToString();
            await _settingsService.SaveSettingsAsync(_currentSettings);

            Console.WriteLine($"[Server] Preparing to join {server.Name} at {server.Address}:{server.Port}");

            // Switch to the Game/Launch page (index 0)
            AnimateSelectionIndicator(0);
            _currentSelectedIndex = 0;
            SwitchToPage(0);

            // Small delay to let the page switch animation complete
            await Task.Delay(300);

            // Trigger the launch button click programmatically
            var launchBtn = this.FindControl<Button>("LaunchGameButton");
            if (launchBtn != null)
            {
                // Check if a version is selected before attempting to launch
                var selectedVersionInfo = _allVersions.FirstOrDefault(v => v.FullVersion == _currentSettings.SelectedSubVersion);
                if (selectedVersionInfo == null)
                {
                    UpdateLaunchButton("SELECT VERSION", "OrangeRed");
                    ShowLaunchErrorBanner("Please select a Minecraft version before joining a server.");
                    return;
                }

                bool isFabric = selectedVersionInfo.Loader.Equals("Fabric", StringComparison.OrdinalIgnoreCase);
                await LaunchGameAsync(selectedVersionInfo.FullVersion, isFabric);
            }
        }

        private async Task UpdateServerButtonStates()
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                bool isAnyLaunchInProgress = _isLaunching || _isInstalling;

                // 1. Update Quick Play buttons (always enabled, opacity based on online status or launch in progress)
                if (_quickPlayServersContainer != null)
                {
                    foreach (var child in _quickPlayServersContainer.Children)
                    {
                        if (child is Button qpBtn && qpBtn.Tag is ServerInfo qpServer)
                        {
                            qpBtn.IsEnabled = true; // Quick Play buttons are ALWAYS enabled and clickable

                            // Opacity: 0.3 if offline OR if a launch is in progress; otherwise 1.0
                            qpBtn.Opacity = (isAnyLaunchInProgress || !qpServer.IsOnline) ? 0.3 : 1.0;
                        }
                    }
                }

                // 2. Update Server list "JOIN" buttons (IsEnabled based on online status AND launch state)
                if (_serversWrapPanel != null)
                {
                    foreach (var card in _serversWrapPanel.Children.OfType<Border>())
                    {
                        if (card.Child is Grid grid)
                        {
                            var actionStack = grid.Children.OfType<StackPanel>()
                                .FirstOrDefault(sp => Grid.GetColumn(sp) == 2);
                            if (actionStack != null)
                            {
                                var joinButton = actionStack.Children.OfType<Button>().FirstOrDefault();
                                if (joinButton != null && card.Tag is string serverId)
                                {
                                    var server = _currentSettings.CustomServers.FirstOrDefault(s => s.Id == serverId);
                                    if (server != null)
                                    {
                                        // Server list JOIN button is enabled only if no launch in progress AND server is online
                                        joinButton.IsEnabled = !isAnyLaunchInProgress && server.IsOnline;
                                        // Set background/foreground for visual feedback (even when disabled by Avalonia default styling)
                                        joinButton.Background = server.IsOnline
                                            ? GetBrush("PrimaryAccentBrush")
                                            : GetBrush("HoverBackgroundBrush");
                                        joinButton.Foreground = server.IsOnline
                                            ? GetBrush("AccentButtonForegroundBrush")
                                            : GetBrush("DisabledForegroundBrush");
                                        joinButton.Opacity = 1.0; // Server list buttons always full opacity, disabled state is visual only
                                    }
                                    else
                                    {
                                        // If server info is not found (e.g., deleted), ensure it's disabled
                                        joinButton.IsEnabled = false;
                                        joinButton.Background = GetBrush("HoverBackgroundBrush");
                                        joinButton.Foreground = GetBrush("DisabledForegroundBrush");
                                        joinButton.Opacity = 1.0;
                                    }
                                }
                            }
                        }
                    }
                }
            });
        }

        private async Task LaunchGameAsync(string version, bool isFabric = false)
        {
            if (_isLaunching || _launcher == null)
            {
                Console.WriteLine("[Launch] Game is already launching or launcher is not initialized. Aborting new launch request.");
                return;
            }

            _isLaunching = true;
            _gameStartingBannerShownForCurrentLaunch = false;
            _launchFailureBannerShownForCurrentLaunch = false;
            await UpdateServerButtonStates();

            _launchCancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = _launchCancellationTokenSource.Token;

            HideLaunchErrorBanner();
            HideLaunchFailureBanner();

            await LoadSessionAsync();

            if (_session == null)
            {
                UpdateLaunchButton("LOGIN REQUIRED", "OrangeRed");
                ShowLaunchErrorBanner("LOGIN REQUIRED: Please log in to your Minecraft account to launch.");
                _isLaunching = false;
                await UpdateServerButtonStates();
                return;
            }

            bool isNetworkAvailable = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
            if (!isNetworkAvailable && _currentSettings.AccountType != "offline")
            {
                UpdateLaunchButton("YOU'RE OFFLINE", "Gray");
                ShowLaunchErrorBanner("No internet connection. Cannot launch online game.");
                _isLaunching = false;
                await UpdateServerButtonStates();
                return;
            }

            string versionToLaunch = version;

            try
            {
                UpdateLaunchButton("PREPARING LAUNCH...", "DeepSkyBlue");

                if (GetSelectedAddon(version).Equals("Fabric", StringComparison.OrdinalIgnoreCase))
                {
                    versionToLaunch = await EnsureFabricProfileAsync(version);
                    if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException(cancellationToken);
                    if (string.IsNullOrEmpty(versionToLaunch))
                    {
                        UpdateLaunchButton("FABRIC INSTALL FAILED", "Red");
                        ShowLaunchErrorBanner("Failed to install Fabric loader. Please try again.");
                        _isLaunching = false;
                        await UpdateServerButtonStates();
                        return;
                    }

                    _currentSettings.SelectedFabricProfileName = versionToLaunch;
                    await _settingsService.SaveSettingsAsync(_currentSettings);

                    if (_currentSettings.IsOptiFineEnabled)
                        await InstallOptiFineForFabricIfNeededAsync(version, versionToLaunch, true);
                    if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException(cancellationToken);

                    await InstallFabricApiIfNeededAsync(version, versionToLaunch);
                    if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException(cancellationToken);

                    if (_currentSettings.IsSodiumEnabled)
                        await InstallSodiumIfNeededAsync(version);
                    if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException(cancellationToken);

                    if (_currentSettings.IsLithiumEnabled)
                        await InstallLithiumIfNeededAsync(version);
                    if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException(cancellationToken);

                    await InstallUserModsAsync(version);
                    if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException(cancellationToken);
                }
                else
                {
                    _currentSettings.SelectedFabricProfileName = null;
                    await _settingsService.SaveSettingsAsync(_currentSettings);
                }

                if (string.IsNullOrEmpty(versionToLaunch))
                {
                    UpdateLaunchButton("LAUNCH FAILED", "Red");
                    ShowLaunchErrorBanner("Failed to determine version to launch. Please check your installation.");
                    _isLaunching = false;
                    await UpdateServerButtonStates();
                    return;
                }

                // FIX: Pass the base 'version' (e.g. "1.20.5"), NOT 'versionToLaunch' (e.g. "fabric-loader...").
                // The mods are tracked by the base Minecraft version.
                SyncLauncherManagedMods(version);

                // Only add Leaf Runtime Mod for modern versions (1.17+) to avoid crashes on old versions
                bool isModernVersion = false;
                if (Version.TryParse(version, out Version? semVer))
                {
                    isModernVersion = semVer >= new Version(1, 17);
                }

                if (isModernVersion)
                {
                    string leafClientModFileName = "leafclient-runtime-1.0.0.jar";
                    if (!_currentSettings.InstalledMods.Any(m => m.ModId == "leafclient" && m.MinecraftVersion == version))
                    {
                        _currentSettings.InstalledMods.Add(new InstalledMod
                        {
                            ModId = "leafclient",
                            Name = "Leaf Client Runtime",
                            Description = "Core Leaf Client runtime mod.",
                            Version = "1.0.0",
                            MinecraftVersion = version, // Use base version
                            FileName = leafClientModFileName,
                            DownloadUrl = "internal",
                            Enabled = true,
                            InstallDate = DateTime.Now,
                            IconUrl = ""
                        });
                        await _settingsService.SaveSettingsAsync(_currentSettings);
                    }
                    else
                    {
                        var leafClientMod = _currentSettings.InstalledMods.FirstOrDefault(m => m.ModId == "leafclient" && m.MinecraftVersion == version);
                        if (leafClientMod != null && !leafClientMod.Enabled)
                        {
                            leafClientMod.Enabled = true;
                            await _settingsService.SaveSettingsAsync(_currentSettings);
                        }
                    }
                }

                var jvmArguments = new List<MArgument>
        {
            new("-Dleaf.client=true"),
            new("-Dleaf.version=1.1.0")
        };

                if (!string.IsNullOrWhiteSpace(_currentSettings.JvmArguments))
                {
                    var customArgs = _currentSettings.JvmArguments.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var arg in customArgs)
                        jvmArguments.Add(new MArgument(arg));
                }

                var launchOption = new MLaunchOption
                {
                    Session = _session,
                    MaximumRamMb = GetMaxRam(),
                    MinimumRamMb = GetMinRam(),
                    JvmArgumentOverrides = jvmArguments
                };

                if (_currentSettings.UseCustomGameResolution)
                {
                    launchOption.ScreenWidth = _currentSettings.GameResolutionWidth;
                    launchOption.ScreenHeight = _currentSettings.GameResolutionHeight;
                }

                if (_currentSettings.QuickLaunchEnabled && !string.IsNullOrWhiteSpace(_currentSettings.QuickJoinServerAddress))
                {
                    launchOption.ServerIp = _currentSettings.QuickJoinServerAddress;
                    launchOption.ServerPort = int.TryParse(_currentSettings.QuickJoinServerPort, out int port) ? port : 25565;
                }

                UpdateLaunchButton("LAUNCHING GAME...", "Purple");

                if (_currentSettings.LauncherVisibilityOnGameLaunch == LauncherVisibility.Hide)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        MinimizeToTray();
                        Console.WriteLine("[Launch] Launcher minimized to tray as per setting.");
                        NotificationWindow.Show("Game Starting", "Launcher hidden while game runs", "Restore", () => RestoreFromTray());
                    });
                }

                var process = await _launcher.CreateProcessAsync(versionToLaunch, launchOption);
                if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException(cancellationToken);

                // --- BOOTSTRAP INJECTION LOGIC ---

                // 1. Define which versions your Runtime Mod actually supports
                // Currently, it seems built for 1.20.1. Add others only if you have updated the jar to support them.
                var supportedRuntimeVersions = new HashSet<string> { "1.20.1" };
                bool isRuntimeCompatible = supportedRuntimeVersions.Contains(version);

                if (isModernVersion)
                {
                    // Only download/update if compatible
                    await DownloadLeafRuntimeDependencies(version, GetSelectedAddon(version) == "Fabric");

                    string originalJavaPath = process.StartInfo.FileName;
                    string originalArguments = process.StartInfo.Arguments ?? string.Empty;
                    string originalWorkingDirectory = process.StartInfo.WorkingDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                    string leafRuntimeDir = System.IO.Path.Combine(_minecraftFolder, "leaf-runtime");
                    string bootstrapJarPath = System.IO.Path.Combine(leafRuntimeDir, "LeafBootstrap-1.0.0.jar");
                    string runtimeJarPath = System.IO.Path.Combine(leafRuntimeDir, "LeafRuntime-1.0.0.jar");

                    // CRITICAL FIX: Only inject if the version is explicitly supported
                    if (isRuntimeCompatible && File.Exists(bootstrapJarPath) && File.Exists(runtimeJarPath))
                    {
                        Console.WriteLine($"[Leaf] Injecting Bootstrap for {version}...");

                        process.StartInfo.FileName = originalJavaPath;
                        process.StartInfo.Arguments = $"-jar \"{bootstrapJarPath}\" {originalArguments}";
                        process.StartInfo.WorkingDirectory = leafRuntimeDir;

                        try
                        {
                            process.StartInfo.EnvironmentVariables["LEAF_RUNTIME_PATH"] = runtimeJarPath;
                            process.StartInfo.EnvironmentVariables["LEAF_BOOTSTRAP_DIR"] = leafRuntimeDir;
                            process.StartInfo.EnvironmentVariables["LEAF_SPAWNER_WORKDIR"] = originalWorkingDirectory;
                        }
                        catch
                        {
                            Console.WriteLine("[Leaf] Warning: could not set process environment variables for bootstrap.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[Leaf] Skipping Runtime injection for {version} (Not in supported list or files missing). Launching Vanilla/Fabric directly.");
                    }
                }
                else
                {
                    Console.WriteLine($"[Leaf] Legacy version {version} detected. Skipping Bootstrap injection to prevent Java 8 crash.");
                }
                // --- END BOOTSTRAP INJECTION ---

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                process.EnableRaisingEvents = true;
                process.Exited += OnGameExited;
                process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Console.WriteLine($"[MC] {e.Data}");
                };
                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Console.Error.WriteLine($"[MC ERROR] {e.Data}");
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                _gameProcess = process;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        bool isIdle = _gameProcess.WaitForInputIdle(90000);

                        if (isIdle && _gameProcess != null && !_gameProcess.HasExited && _gameProcess.MainWindowHandle != IntPtr.Zero)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                if (_gameProcess != null && !_gameProcess.HasExited)
                                {
                                    UpdateRichPresenceFromState();
                                    UpdateLaunchButton("PLAYING ON LEAF CLIENT", "DeepSkyBlue");
                                }
                            });
                        }
                        else if (_gameProcess != null && !_gameProcess.HasExited)
                        {
                            Console.WriteLine("[Window Watcher] Timed out waiting for game window to become idle.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[Window Watcher ERROR] An error occurred while waiting for game window: {ex.Message}");
                    }
                }, cancellationToken);

                _isLaunching = false;
                await UpdateServerButtonStates();
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[Launch] Game launch operation was cancelled by user.");
                UpdateLaunchButton("LAUNCH CANCELLED", "Orange");
                _isLaunching = false;
                _gameStartingBannerShownForCurrentLaunch = false;
                _launchFailureBannerShownForCurrentLaunch = false;
                await UpdateServerButtonStates();

                _ = Task.Run(async () =>
                {
                    await Task.Delay(2000);
                    if (!_isLaunching && !_isInstalling && _gameProcess == null)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            UpdateLaunchButton("LAUNCH GAME", "SeaGreen");
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Launch ERROR] Failed to launch game: {ex.Message}");
                UpdateLaunchButton("LAUNCH FAILED", "Red");
                ShowLaunchErrorBanner($"Launch failed: {ex.Message}");

                ShowLaunchFailureBanner("Failed to launch Minecraft. Please check logs.");

                if (!this.IsVisible || _currentSettings.LauncherVisibilityOnGameLaunch == LauncherVisibility.Hide)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        RestoreFromTray();
                        NotificationWindow.Show("Launch Failed", "Failed to start the game process.", "View Logs", () => OpenLauncherLogsFolder(null, new RoutedEventArgs()), true);
                    });
                }
                _isLaunching = false;
                _gameStartingBannerShownForCurrentLaunch = false;
                _launchFailureBannerShownForCurrentLaunch = false;
                await UpdateServerButtonStates();
            }
            finally
            {
                _launchCancellationTokenSource?.Dispose();
                _launchCancellationTokenSource = null;
            }
        }



        private async Task ManageOptiFineForFabricMods(string mcVersion, bool enable)
        {
            // These are the mods that are part of the OptiFine for Fabric pack
            // We need to ensure their state in _currentSettings.InstalledMods is correct.
            string[] optifineModNames = new[] {
        "optifine", "optifabric", "modmenu", "sodium", "iris", "lithium", "starlight"
    }; // These are partial names/IDs, not full filenames.

            // Find all launcher-managed mods for the current MC version that are OptiFine-related
            var relevantMods = _currentSettings.InstalledMods
                .Where(m => m.MinecraftVersion.Equals(mcVersion, StringComparison.OrdinalIgnoreCase) &&
                            optifineModNames.Any(name => m.Name.Contains(name, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            bool settingsChanged = false;
            foreach (var mod in relevantMods)
            {
                if (mod.Enabled != enable)
                {
                    mod.Enabled = enable;
                    settingsChanged = true;
                    Console.WriteLine($"[OptiFineForFabric] Setting '{mod.Name}' to enabled={enable} in settings.");
                }
            }

            // Also handle the LeafClient runtime mod itself if it's considered part of this group
            // (though it's usually managed separately)
            var leafClientMod = _currentSettings.InstalledMods.FirstOrDefault(m => m.ModId == "leafclient" && m.MinecraftVersion == mcVersion);
            if (leafClientMod != null && leafClientMod.Enabled != enable)
            {
                leafClientMod.Enabled = enable; // Forcing leafclient-runtime to match OptiFine pack state
                settingsChanged = true;
                Console.WriteLine($"[OptiFineForFabric] Setting 'Leaf Client Runtime' to enabled={enable} in settings.");
            }


            if (settingsChanged)
            {
                await _settingsService.SaveSettingsAsync(_currentSettings);
                // The actual file system sync will happen during SyncLauncherManagedMods in LaunchGameAsync
            }
        }


        // MainWindow.cs — UpdateSidebarDetails(string subVersion) (REPLACE method)
        private void UpdateSidebarDetails(string subVersion)
        {
            var versionInfo = _allVersions.FirstOrDefault(v => v.FullVersion == subVersion);
            if (versionInfo != null)
            {
                if (_versionType != null) _versionType.Text = versionInfo.Type;
                if (_versionLoader != null) _versionLoader.Text = versionInfo.Loader;
                if (_versionDate != null) _versionDate.Text = versionInfo.ReleaseDate;

                if (_versionDescription != null)
                {
                    _versionDescription.Text = versionInfo.Description;
                }
            }
            else
            {
                if (_versionType != null) _versionType.Text = "N/A";
                if (_versionLoader != null) _versionLoader.Text = "N/A";
                if (_versionDate != null) _versionDate.Text = "N/A";
                if (_versionDescription != null) _versionDescription.Text = "Details not available.";
            }
        }

        private void OnGameExited(object? sender, EventArgs e)
        {
            if (_isShuttingDown) return;

            Dispatcher.UIThread.Post(async () =>
            {
                if (_isShuttingDown) return;
                string logoutSignalPath = System.IO.Path.Combine(_minecraftFolder, "logout.signal");
                if (System.IO.File.Exists(logoutSignalPath))
                {
                    try
                    {
                        // Delete signal
                        System.IO.File.Delete(logoutSignalPath);
                        Console.WriteLine("[Launcher] Logout signal detected from game. Performing logout...");

                        // Perform logout logic
                        // We call the existing LogoutButton_Click method.
                        // Since it hides the current window and opens LoginWindow, we should NOT proceed with
                        // the rest of OnGameExited logic (restoring tray, banners, etc.)
                        LogoutButton_Click(null, new RoutedEventArgs());
                        return;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[Launcher] Error processing logout signal: {ex.Message}");
                    }
                }


                int exitCode = _gameProcess?.ExitCode ?? -1;

                if (exitCode != 0)
                {
                    Console.Error.WriteLine($"[Launcher] Game process exited with error code: {exitCode}");

                    // If the window was hidden (minimized to tray), show the banner
                    if (!this.IsVisible)
                    {
                        RestoreFromTray();
                        NotificationWindow.Show("Launch Failed", "Game exited unexpectedly.", "View Logs", () => OpenLauncherLogsFolder(null, new RoutedEventArgs()), true);
                    }
                    else
                    {
                        ShowLaunchFailureBanner("Failed to launch Minecraft. Please check logs.");
                    }

                    UpdateLaunchButton("LAUNCH FAILED", "Red");

                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(5000);
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (!_isLaunching && !_isInstalling && _gameProcess == null)
                            {
                                UpdateLaunchButton("LAUNCH GAME", "SeaGreen");
                                HideLaunchFailureBanner();
                            }
                        });
                    });
                }
                else
                {
                    UpdateLaunchButton("LAUNCH GAME", "SeaGreen");
                    HideLaunchFailureBanner();

                    if (_currentSettings.LauncherVisibilityOnGameLaunch == LauncherVisibility.Hide)
                    {
                        Console.WriteLine("[Launch] Game closed - restoring launcher from tray");
                        RestoreFromTray();
                    }
                }

                _gameProcess = null;
                _isLaunching = false;
                _gameStartingBannerShownForCurrentLaunch = false;

                await UpdateServerButtonStates();
                HideGameStartingBanner();

                if (_currentSettings.LockGameAspectRatio)
                {
                    try
                    {
                        await Task.Delay(1000); // Wait for game to finish writing options.txt

                        string optionsPath = System.IO.Path.Combine(_minecraftFolder, "options.txt");
                        if (System.IO.File.Exists(optionsPath))
                        {
                            var lines = await System.IO.File.ReadAllLinesAsync(optionsPath);
                            int? width = null;
                            int? height = null;

                            foreach (var line in lines)
                            {
                                if (line.StartsWith("overrideWidth:"))
                                {
                                    if (int.TryParse(line.Split(':')[1], out int w))
                                        width = w;
                                }
                                else if (line.StartsWith("overrideHeight:"))
                                {
                                    if (int.TryParse(line.Split(':')[1], out int h))
                                        height = h;
                                }
                            }

                            if (width.HasValue && height.HasValue && width > 0 && height > 0)
                            {
                                Console.WriteLine($"[Resolution Lock] Game closed with resolution: {width}x{height}");

                                // Update settings
                                _currentSettings.GameResolutionWidth = width.Value;
                                _currentSettings.GameResolutionHeight = height.Value;
                                await _settingsService.SaveSettingsAsync(_currentSettings);

                                // Update UI
                                if (_gameResolutionWidthTextBox != null)
                                    _gameResolutionWidthTextBox.Text = width.Value.ToString();
                                if (_gameResolutionHeightTextBox != null)
                                    _gameResolutionHeightTextBox.Text = height.Value.ToString();

                                Console.WriteLine($"[Resolution Lock] Saved new resolution: {width}x{height}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[Resolution Lock] Failed to read game resolution: {ex.Message}");
                    }
                }

                // Restore launcher window if it was hidden
                if (_currentSettings.LauncherVisibilityOnGameLaunch == LauncherVisibility.Hide)
                {
                    Console.WriteLine("[Launch] Game closed - restoring launcher from tray");
                    RestoreFromTray();
                }
            });
        }


        private int GetMaxRam()
        {
            if (_maxRamAllocationComboBox?.SelectedItem is ComboBoxItem item && item.Content is string text)
            {
                if (int.TryParse(text.Replace(" GB", ""), out int gb))
                    return gb * 1024;
            }
            return 4096;
        }

        private int GetMinRam()
        {
            if (int.TryParse(_minRamAllocationTextBox?.Text, out int mb))
                return Math.Max(512, mb);
            return 1024;
        }

        private Color DarkenColor(Color color, float factor)
        {
            // Ensure factor is between 0 and 1
            factor = Math.Clamp(factor, 0f, 1f);

            byte r = (byte)Math.Max(0, color.R * (1f - factor));
            byte g = (byte)Math.Max(0, color.G * (1f - factor));
            byte b = (byte)Math.Max(0, color.B * (1f - factor));
            return Color.FromArgb(color.A, r, g, b);
        }

        private void UpdateLaunchButton(string text, string colorName)
        {
            _currentOperationText = text;
            _currentOperationColor = colorName;

            // Reset flags based on the intended *terminal* state of the button text.
            // If text indicates a non-active state, ensure flags are false.
            if (text == "LAUNCH GAME" || text.StartsWith("LAUNCH FAILED") || text == "SELECT VERSION" || text == "LOGIN REQUIRED" || text == "LAUNCH CANCELLED" || text == "YOU'RE OFFLINE" || text == "FABRIC INSTALL FAILED")
            {
                _isLaunching = false;
                _isInstalling = false;
            }
            // Note: _gameProcess is managed by OnGameExited and the click handler for termination.

            ApplyLaunchButtonState(); // Let ApplyLaunchButtonState resolve the final UI state
        }


        private void ApplyLaunchButtonState()
        {
            if (this.FindControl<Button>("LaunchGameButton") is { } btn)
            {
                string displayText;
                string colorName;
                bool isButtonEnabled = true; // Default to enabled

                // Order of priority for determining button state:

                // 1. Game running (highest priority)
                if (_gameProcess != null && !_gameProcess.HasExited)
                {
                    displayText = "PLAYING ON LEAF CLIENT";
                    colorName = "DeepSkyBlue";
                    isButtonEnabled = true; // Always clickable to terminate

                    ShowGameStartingBanner("Minecraft may take a few seconds to appear on your screen");
                    HideLaunchFailureBanner(); // Ensure failure banner is hidden
                }
                // 2. Launch/Install in progress (display current task, allow cancellation)
                else if (_isLaunching || _isInstalling)
                {
                    displayText = _currentOperationText;
                    colorName = _currentOperationColor;
                    isButtonEnabled = true; // Enable to allow cancellation
                    HideGameStartingBanner(); // Ensure banner is hidden if not in "PLAYING" state
                    HideLaunchFailureBanner(); // Ensure failure banner is hidden
                }
                // 3. Offline (disabled, when not running/launching)
                else if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                {
                    displayText = "YOU'RE OFFLINE";
                    colorName = "Gray";
                    isButtonEnabled = false; // Cannot launch when offline
                    HideGameStartingBanner(); // Ensure banner is hidden
                    HideLaunchFailureBanner(); // Ensure failure banner is hidden
                }
                // 4. Specific terminal states (e.g., LAUNCH CANCELLED, LOGIN REQUIRED, LAUNCH FAILED)
                else if (_currentOperationText == "LAUNCH CANCELLED" ||
                         _currentOperationText == "SELECT VERSION" ||
                         _currentOperationText == "LOGIN REQUIRED" ||
                         _currentOperationText == "FABRIC INSTALL FAILED" ||
                         _currentOperationText.StartsWith("LAUNCH FAILED")) // Handle "LAUNCH FAILED: ..."
                {
                    displayText = _currentOperationText;
                    colorName = _currentOperationColor;
                    // MODIFIED: The button should ALWAYS be enabled in these states to allow the user to retry or re-login.
                    isButtonEnabled = true;
                    HideGameStartingBanner(); // Ensure banner is hidden

                    // If the button text is "LAUNCH FAILED", the failure banner should already be shown by OnGameExited.
                    // We don't hide it here, but ensure its state is consistent.
                    if (_currentOperationText.StartsWith("LAUNCH FAILED")) { /* leave failure banner visible if it was just shown */ }
                    else { HideLaunchFailureBanner(); } // Hide if it's another terminal state
                }
                // 5. Default "LAUNCH GAME" state
                else
                {
                    displayText = "LAUNCH GAME";
                    colorName = "SeaGreen";
                    isButtonEnabled = true;
                    HideGameStartingBanner(); // Ensure banner is hidden
                    HideLaunchFailureBanner(); // Ensure failure banner is hidden
                }

                btn.IsEnabled = isButtonEnabled;

                Color baseColor = colorName switch
                {
                    "Purple" => Colors.Purple,
                    "OrangeRed" => Colors.OrangeRed,
                    "Firebrick" => Colors.Firebrick,
                    "Red" => Colors.Red,
                    "SeaGreen" => Color.FromRgb(50, 205, 50),
                    "DeepSkyBlue" => Colors.DeepSkyBlue,
                    "Gray" => Colors.Gray,
                    "Orange" => Colors.Orange,
                    _ => Colors.White
                };

                btn.Background = new SolidColorBrush(baseColor);

                if (this.FindControl<Border>("LaunchButtonOuterBorder") is { } outerBorder)
                {
                    Color borderColor = DarkenColor(baseColor, 0.2f);
                    outerBorder.BorderBrush = new SolidColorBrush(borderColor);
                    outerBorder.BorderThickness = new Thickness(1);
                }

                if (btn.Content is StackPanel contentStack && contentStack.Children.OfType<TextBlock>().FirstOrDefault() is TextBlock mainTextBlock)
                {
                    mainTextBlock.Text = displayText;
                }
                if (btn.Content is StackPanel contentStackWithSub && contentStackWithSub.Children.OfType<StackPanel>().FirstOrDefault() is StackPanel subStack && subStack.Children.OfType<TextBlock>().LastOrDefault() is TextBlock subTextBlock)
                {
                    subTextBlock.Text = _launchVersionText?.Text ?? "Leaf Client";
                }
            }
        }


        private void StartRichPresenceIfEnabled()
        {
            try
            {
                if (_currentSettings.DiscordRichPresence)
                {
                    if (!_drp.IsInitialized)
                    {
                        _drp.Initialize(DiscordClientId);
                        _drpSessionStart = DateTime.UtcNow;
                    }
                    UpdateRichPresenceFromState();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DRP] Failed to start: {ex.Message}");
            }
        }

        // Position helper (unchanged)
        private void PositionTooltipNextTo(Border button)
        {
            if (_sidebarHoverTooltip == null) return;

            var origin = button.TranslatePoint(new Point(0, 0), this);
            double top;

            if (origin.HasValue)
            {
                top = origin.Value.Y + (button.Bounds.Height - TooltipHeight) / 2.0;
            }
            else
            {
                // Fallback if TranslatePoint not available yet
                var index = button.Tag is string s && int.TryParse(s, out var i) ? i : 0;
                top = index * 60 + (50 - TooltipHeight) / 2.0;
            }

            double left = SidebarWidth + GapRightOfSidebar;

            Canvas.SetTop(_sidebarHoverTooltip, Math.Max(0, top));
            Canvas.SetLeft(_sidebarHoverTooltip, left);
        }

        private void OnSidebarButtonPointerEntered(object? sender, PointerEventArgs e)
        {
            if (sender is not Border b) return;

            _tooltipHideCts?.Cancel();

            if (_sidebarHoverTooltipText != null)
                _sidebarHoverTooltipText.Text = b.Tag switch
                {
                    "0" => "LAUNCH",
                    "1" => "VERSIONS",
                    "2" => "SERVERS",
                    "3" => "MODS",
                    "4" => "SETTINGS",
                    _ => ""
                };

            if (_sidebarHoverTooltip == null) return;

            if (!AreAnimationsEnabled())
            {
                PositionTooltipNextTo(b);
                _sidebarHoverTooltip.IsVisible = true;
                _sidebarHoverTooltip.Opacity = 1;
                return;
            }

            if (!_tooltipHasShown)
            {
                // Disable position animations for the very first appearance
                _sidebarHoverTooltip.Transitions = new Transitions(); // no transitions
                PositionTooltipNextTo(b);                              // place exactly
                _sidebarHoverTooltip.IsVisible = true;                 // show
                _sidebarHoverTooltip.Opacity = 1;                      // make opaque

                // Restore original transitions for smooth following afterwards
                var restore = new Transitions();
                if (_savedTooltipTransitions != null)
                {
                    foreach (var t in _savedTooltipTransitions)
                        restore.Add(t);
                }
                _sidebarHoverTooltip.Transitions = restore;

                _tooltipHasShown = true;
                return; // done (no initial flicker)
            }

            // Subsequent hovers: keep transitions enabled for smooth follow
            PositionTooltipNextTo(b);
            _sidebarHoverTooltip.IsVisible = true;
            _sidebarHoverTooltip.Opacity = 1;
        }

        private void OnSidebarButtonPointerExited(object? sender, PointerEventArgs e)
        {
            _tooltipHideCts?.Cancel();
            _tooltipHideCts = new CancellationTokenSource();
            var ct = _tooltipHideCts.Token;

            if (!AreAnimationsEnabled()) // NEW
            {
                if (_sidebarHoverTooltip != null)
                {
                    _sidebarHoverTooltip.Opacity = 0;
                    _sidebarHoverTooltip.IsVisible = false;
                }
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(140, ct); // grace period
                    if (ct.IsCancellationRequested || _sidebarHoverTooltip == null) return;

                    Dispatcher.UIThread.Post(() => { _sidebarHoverTooltip!.Opacity = 0; });
                    await Task.Delay(200, ct);
                    if (!ct.IsCancellationRequested)
                    {
                        Dispatcher.UIThread.Post(() => { _sidebarHoverTooltip!.IsVisible = false; });
                    }
                }
                catch (OperationCanceledException) { }
            }, ct);
        }

        private void StopRichPresence()
        {
            try { _drp.Shutdown(); } catch { }
        }

        private void UpdateRichPresenceFromState()
        {
            if (!_currentSettings.DiscordRichPresence || !_drp.IsInitialized) return;

            string pageLabel = _currentSelectedIndex switch
            {
                0 => "Game",
                1 => "Versions",
                2 => "Servers",
                3 => "Mods",
                4 => "Settings",
                5 => "Skins",
                _ => "Game"
            };

            // --- MODIFIED: Apply username masking ---
            string detailsTop;
            if (_loggedIn && !string.IsNullOrWhiteSpace(_currentUsername))
            {
                if (_currentSettings.ShowUsernameInDiscordRichPresence)
                {
                    // Show full username
                    detailsTop = $"Playing as {_currentUsername}";
                }
                else
                {
                    // Show masked username (e.g., "Z****Y")
                    string maskedUsername = MaskUsername(_currentUsername);
                    detailsTop = $"Playing as {maskedUsername}";
                }
            }
            else
            {
                detailsTop = "Leaf Client";
            }
            // --- END MODIFIED ---

            string state = $"On {pageLabel} page";

            _drp.SetPresence(
                details: detailsTop,
                state: state,
                largeImageKey: "leaf_logo",
                largeImageText: "Leaf Client",
                start: _drpSessionStart,
                buttons: new[]
                {
            new DiscordRPC.Button { Label = "Download LeafClient", Url = "https://github.com/LeafClientMC/LeafClient" }
                }
            );
        }

        public void HandleDeepLinkJoin(string server, string? port)
        {
            _currentSettings.QuickJoinServerAddress = server;
            _currentSettings.QuickJoinServerPort = string.IsNullOrWhiteSpace(port) ? "25565" : port;
            _ = _settingsService.SaveSettingsAsync(_currentSettings);
            _currentSelectedIndex = 2;
            SwitchToPage(2);
            Console.WriteLine($"[DeepLink] Join requested: {server}:{_currentSettings.QuickJoinServerPort}");
            UpdateRichPresenceFromState();
        }

        // Initialize skins page controls
        private void InitializeSkinsControls()
        {
            _skinsPage = this.FindControl<Grid>("SkinsPage");
            _skinsWrapPanel = this.FindControl<WrapPanel>("SkinsWrapPanel");
            _noSkinsMessage = this.FindControl<Border>("NoSkinsMessage");
        }

        private void OpenSkinsPage(object? sender, RoutedEventArgs e)
        {
            CloseAccountPanel(null, new RoutedEventArgs());
            SwitchToPage(5); // New page index for skins
            LoadSkins();
        }

        private async void CreateNewSkin(object? sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Window
                {
                    Title = "Create New Skin",
                    Width = 400,
                    Height = 250,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false
                };

                var panel = new StackPanel { Margin = new Thickness(20), Spacing = 15 };

                panel.Children.Add(new TextBlock
                {
                    Text = "Skin Name",
                    FontWeight = FontWeight.Bold
                });

                var nameBox = new TextBox
                {
                    Watermark = "Enter skin name...",
                    MinWidth = 300
                };
                panel.Children.Add(nameBox);

                var filePathText = new TextBlock
                {
                    Text = "No file selected",
                    Foreground = Brushes.Gray
                };
                panel.Children.Add(filePathText);

                string? selectedFilePath = null;

                var browseButton = new Button
                {
                    Content = "Browse for PNG...",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                    Padding = new Thickness(10, 8)
                };

                browseButton.Click += async (s, args) =>
                {
                    var openDialog = new Avalonia.Platform.Storage.FilePickerOpenOptions
                    {
                        Title = "Select Minecraft Skin (PNG)",
                        AllowMultiple = false,
                        FileTypeFilter = new[]
                        {
                            new Avalonia.Platform.Storage.FilePickerFileType("PNG Images")
                            {
                                Patterns = new[] { "*.png" }
                            }
                        }
                    };

                    var result = await dialog.StorageProvider.OpenFilePickerAsync(openDialog);
                    if (result.Count > 0)
                    {
                        selectedFilePath = result[0].Path.LocalPath;
                        filePathText.Text = System.IO.Path.GetFileName(selectedFilePath);
                    }
                };
                panel.Children.Add(browseButton);

                var buttonPanel = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Spacing = 10,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                var cancelButton = new Button { Content = "Cancel", Padding = new Thickness(15, 8) };
                cancelButton.Click += (s, args) => dialog.Close();

                var createButton = new Button
                {
                    Content = "Create",
                    Padding = new Thickness(15, 8),
                    Background = Brushes.Green
                };

                createButton.Click += async (s, args) =>
                {
                    if (string.IsNullOrWhiteSpace(nameBox.Text))
                    {
                        // Show error
                        return;
                    }

                    if (string.IsNullOrEmpty(selectedFilePath))
                    {
                        // Show error
                        return;
                    }

                    await SaveSkinAsync(nameBox.Text, selectedFilePath);
                    dialog.Close();
                    LoadSkins();
                };

                buttonPanel.Children.Add(cancelButton);
                buttonPanel.Children.Add(createButton);
                panel.Children.Add(buttonPanel);

                dialog.Content = panel;
                await dialog.ShowDialog(this);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating skin: {ex.Message}");
            }
        }

        private async Task SaveSkinAsync(string name, string sourcePath)
        {
            try
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var skinsDirectory = System.IO.Path.Combine(appDataPath, "LeafClient", "Skins");
                System.IO.Directory.CreateDirectory(skinsDirectory);

                var skinId = Guid.NewGuid().ToString();
                var fileName = $"{skinId}.png";
                var destPath = System.IO.Path.Combine(skinsDirectory, fileName);

                System.IO.File.Copy(sourcePath, destPath, true);

                var skinInfo = new SkinInfo
                {
                    Id = skinId,
                    Name = name,
                    FilePath = destPath
                };

                _currentSettings.CustomSkins.Add(skinInfo);
                await _settingsService.SaveSettingsAsync(_currentSettings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving skin: {ex.Message}");
            }
        }

        // 2) MODIFY LoadSkins() so that it sets the flag while it restores selection

        private void LoadSkins()
        {
            if (_skinsWrapPanel == null || _noSkinsMessage == null) return;

            _skinsWrapPanel.Children.Clear();
            _currentlySelectedSkinCard = null; // Reset selected card when reloading skins

            if (_currentSettings.CustomSkins.Count == 0)
            {
                _noSkinsMessage.IsVisible = true;
                _currentSettings.SelectedSkinId = null; // No skin selected if list is empty
                _ = _settingsService.SaveSettingsAsync(_currentSettings); // Save this change
                return;
            }

            _noSkinsMessage.IsVisible = false;

            foreach (var skin in _currentSettings.CustomSkins)
            {
                var skinCard = CreateSkinCard(skin);
                _skinsWrapPanel.Children.Add(skinCard);
            }

            // --- IMPORTANT PART: prevent uploads when restoring selection ---
            _isProgrammaticallySelectingSkin = true;
            try
            {
                if (!string.IsNullOrEmpty(_currentSettings.SelectedSkinId))
                {
                    // This will set the visual border, but SelectSkin will early‑return
                    SelectSkin(_currentSettings.SelectedSkinId);
                }
                else if (_currentSettings.CustomSkins.Any())
                {
                    SelectSkin(_currentSettings.CustomSkins.First().Id);
                }
            }
            finally
            {
                _isProgrammaticallySelectingSkin = false;
            }
        }


        private Border CreateSkinCard(SkinInfo skin)
        {

            var selectedBorderBrush = new SolidColorBrush(Color.FromRgb(50, 205, 50));

            var card = new Border
            {
                Width = 180,
                Height = 240,
                Background = GetBrush("CardBackgroundColor"),
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(0, 0, 15, 15),
                Padding = new Thickness(15),
                Cursor = new Cursor(StandardCursorType.Hand),
                Tag = skin.Id, // Store the skin ID in the Tag for easy retrieval
                               // Initialize border based on whether this skin is currently selected
                BorderBrush = skin.Id == _currentSettings.SelectedSkinId ? selectedBorderBrush : Brushes.Transparent,
                BorderThickness = new Thickness(skin.Id == _currentSettings.SelectedSkinId ? 3 : 0)
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Image container
            var imageContainer = new Border
            {
                Background = GetBrush("HoverBackgroundBrush"),
                CornerRadius = new CornerRadius(8),
                ClipToBounds = true
            };
            _ = LoadSkinPreviewAsync(skin, imageContainer);
            Grid.SetRow(imageContainer, 0);
            grid.Children.Add(imageContainer);

            // Name
            var nameText = new TextBlock
            {
                Text = skin.Name,
                Foreground = GetBrush("PrimaryForegroundBrush"),
                FontWeight = FontWeight.Bold,
                FontSize = 14,
                TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 10, 0, 0)
            };
            Grid.SetRow(nameText, 1);
            grid.Children.Add(nameText);

            // Three dots menu (appears on hover)
            var menuButton = new Button
            {
                Content = "⋮",
                FontSize = 20,
                Width = 30,
                Height = 30,
                Background = GetBrush("HoverBackgroundBrush"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                CornerRadius = new CornerRadius(15),
                Opacity = 0,
                IsVisible = false
            };

            menuButton.Flyout = CreateSkinContextMenu(skin);

            Grid.SetRow(menuButton, 0);
            grid.Children.Add(menuButton);

            // Show menu on hover
            card.PointerEntered += (s, e) =>
            {
                menuButton.IsVisible = true;
                menuButton.Opacity = 1;
            };

            card.PointerExited += (s, e) =>
            {
                // Only hide if the flyout is not open
                if (menuButton.Flyout == null || !menuButton.Flyout.IsOpen)
                {
                    menuButton.Opacity = 0;
                    menuButton.IsVisible = false;
                }
            };

            // Handle clicking the card to select it
            card.PointerPressed += (s, e) =>
            {
                // Check if the left mouse button was pressed
                if (e.GetCurrentPoint(s as Control).Properties.IsLeftButtonPressed)
                {
                    SelectSkin(skin.Id);
                    e.Handled = true; // Prevent other handlers from processing this click
                }
            };



            card.Child = grid;

            // If this is the currently selected skin, store its card reference
            if (skin.Id == _currentSettings.SelectedSkinId)
            {
                _currentlySelectedSkinCard = card;
            }

            return card;
        }

        // 3) MODIFY SelectSkin(string skinId) to skip Mojang upload when called during LoadSkins()

        private async void SelectSkin(string skinId)
        {

            if (_isProgrammaticallySelectingSkin)
            {
                // 1. Visual selection logic
                if (_currentlySelectedSkinCard != null)
                {
                    _currentlySelectedSkinCard.BorderBrush = Brushes.Transparent;
                    _currentlySelectedSkinCard.BorderThickness = new Thickness(0);
                }

                var newSelectedCard = _skinsWrapPanel?.Children
                    .OfType<Border>()
                    .FirstOrDefault(b => b.Tag as string == skinId);

                if (newSelectedCard != null)
                {
                    newSelectedCard.BorderBrush = new SolidColorBrush(Color.FromRgb(50, 205, 50)); // SeaGreen
                    newSelectedCard.BorderThickness = new Thickness(3);
                    _currentlySelectedSkinCard = newSelectedCard;
                }

                _currentSettings.SelectedSkinId = skinId;
                await _settingsService.SaveSettingsAsync(_currentSettings);

                // Do NOT call Mojang API, do NOT show status banner
                return;
            }

            // --- ORIGINAL USER‑INTENTIONAL FLOW BELOW (kept as‑is) ---

            // 1. Visual Selection Logic (UI)
            if (_currentlySelectedSkinCard != null)
            {
                _currentlySelectedSkinCard.BorderBrush = Brushes.Transparent;
                _currentlySelectedSkinCard.BorderThickness = new Thickness(0);
            }

            var card = _skinsWrapPanel?.Children.OfType<Border>()
                .FirstOrDefault(b => b.Tag as string == skinId);

            if (card != null)
            {
                card.BorderBrush = new SolidColorBrush(Color.FromRgb(50, 205, 50)); // SeaGreen
                card.BorderThickness = new Thickness(3);
                _currentlySelectedSkinCard = card;
            }

            // 2. Save to Settings
            _currentSettings.SelectedSkinId = skinId;
            await _settingsService.SaveSettingsAsync(_currentSettings);

            // 3. OFFLINE CHECK
            if (_currentSettings.AccountType == "offline")
            {
                Console.WriteLine("[Skin Upload] Skipped: User is in Offline Mode.");
                ShowSkinStatusBanner("⚠️ Offline Mode: Skins cannot be changed within Offline Mode.", SkinBannerStatus.Error);
                return;
            }

            // 4. Find Skin File Info
            var selectedSkin = _currentSettings.CustomSkins.FirstOrDefault(s => s.Id == skinId);
            if (selectedSkin == null || !System.IO.File.Exists(selectedSkin.FilePath))
            {
                Console.WriteLine("[Skin Upload] Skipped: File not found.");
                ShowSkinStatusBanner("❌ Error: Skin file not found.", SkinBannerStatus.Error);
                return;
            }

            // 5. Validate Session & Upload
            Console.WriteLine("[Skin Upload] Verifying session...");

            await LoadSessionAsync();

            if (_session != null && _session.CheckIsValid() && !string.IsNullOrEmpty(_session.AccessToken))
            {
                Console.WriteLine($"[Skin Upload] Uploading {selectedSkin.Name}...");
                await UploadSkinToMojangAsync(selectedSkin.FilePath, _session.AccessToken, "classic");
                ShowSkinStatusBanner($"✅ Skin '{selectedSkin.Name}' successfully uploaded!", SkinBannerStatus.Success);
            }
            else
            {
                Console.WriteLine("[Skin Upload] Failed: Session invalid or expired.");
                ShowSkinStatusBanner("❌ Upload Failed: Please log in again to update your skin.", SkinBannerStatus.Warning);
            }
        }

        private async Task LoadSkinPreviewAsync(SkinInfo skin, Border container)
        {
            try
            {
                if (!System.IO.File.Exists(skin.FilePath))
                {
                    container.Child = new TextBlock
                    {
                        Text = "❌",
                        FontSize = 32,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    };
                    return;
                }

                // Show loading indicator
                var loadingText = new TextBlock
                {
                    Text = "⏳",
                    FontSize = 32,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                container.Child = loadingText;

                // Use the skin file path directly to render it
                // We'll use a random pose for variety
                var pose = _skinRenderService.GetRandomLargePoseName();

                // Create a temporary identifier for the skin (use the skin ID)
                var renderedBitmap = await _skinRenderService.LoadSkinImageFromFileAsync(
                    skin.FilePath,
                    pose,
                    "full"
                );

                if (renderedBitmap != null)
                {
                    var image = new Image
                    {
                        Source = renderedBitmap,
                        Stretch = Avalonia.Media.Stretch.Uniform,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                    };
                    container.Child = image;
                }
                else
                {
                    // Fallback to raw PNG if rendering fails
                    var bitmap = new Avalonia.Media.Imaging.Bitmap(skin.FilePath);
                    var image = new Image
                    {
                        Source = bitmap,
                        Stretch = Avalonia.Media.Stretch.Uniform
                    };
                    container.Child = image;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading skin preview: {ex.Message}");
                container.Child = new TextBlock
                {
                    Text = "❌",
                    FontSize = 32,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
            }
        }


        private MenuFlyout CreateSkinContextMenu(SkinInfo skin)
        {
            var menu = new MenuFlyout();

            var editItem = new MenuItem { Header = "Edit Skin" };
            editItem.Click += (s, e) => EditSkin(skin);
            menu.Items.Add(editItem);

            var deleteItem = new MenuItem { Header = "Delete Skin" };
            deleteItem.Click += (s, e) => DeleteSkin(skin);
            menu.Items.Add(deleteItem);

            return menu;
        }

        private async void EditSkin(SkinInfo skin)
        {
            try
            {
                var dialog = new Window
                {
                    Title = "Edit Skin",
                    Width = 400,
                    Height = 250,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false
                };

                var panel = new StackPanel { Margin = new Thickness(20), Spacing = 15 };

                panel.Children.Add(new TextBlock
                {
                    Text = "Skin Name",
                    FontWeight = FontWeight.Bold
                });

                var nameBox = new TextBox
                {
                    Text = skin.Name,
                    MinWidth = 300
                };
                panel.Children.Add(nameBox);

                var filePathText = new TextBlock
                {
                    Text = System.IO.Path.GetFileName(skin.FilePath),
                    Foreground = Brushes.Gray
                };
                panel.Children.Add(filePathText);

                string? selectedFilePath = null;

                var browseButton = new Button
                {
                    Content = "Replace PNG...",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                    Padding = new Thickness(10, 8)
                };

                browseButton.Click += async (s, args) =>
                {
                    var openDialog = new Avalonia.Platform.Storage.FilePickerOpenOptions
                    {
                        Title = "Select Minecraft Skin (PNG)",
                        AllowMultiple = false,
                        FileTypeFilter = new[]
                        {
                            new Avalonia.Platform.Storage.FilePickerFileType("PNG Images")
                            {
                                Patterns = new[] { "*.png" }
                            }
                        }
                    };

                    var result = await dialog.StorageProvider.OpenFilePickerAsync(openDialog);
                    if (result.Count > 0)
                    {
                        selectedFilePath = result[0].Path.LocalPath;
                        filePathText.Text = System.IO.Path.GetFileName(selectedFilePath);
                    }
                };
                panel.Children.Add(browseButton);

                var buttonPanel = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Spacing = 10,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                var cancelButton = new Button { Content = "Cancel", Padding = new Thickness(15, 8) };
                cancelButton.Click += (s, args) => dialog.Close();

                var saveButton = new Button
                {
                    Content = "Save",
                    Padding = new Thickness(15, 8),
                    Background = Brushes.Green
                };

                saveButton.Click += async (s, args) =>
                {
                    if (string.IsNullOrWhiteSpace(nameBox.Text))
                    {
                        return;
                    }

                    skin.Name = nameBox.Text;
                    skin.ModifiedDate = DateTime.Now;

                    // If a new file was selected, replace the old one
                    if (!string.IsNullOrEmpty(selectedFilePath))
                    {
                        try
                        {
                            if (System.IO.File.Exists(skin.FilePath))
                            {
                                System.IO.File.Delete(skin.FilePath);
                            }
                            System.IO.File.Copy(selectedFilePath, skin.FilePath, true);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error replacing skin file: {ex.Message}");
                        }
                    }

                    await _settingsService.SaveSettingsAsync(_currentSettings);
                    dialog.Close();
                    LoadSkins();
                };

                buttonPanel.Children.Add(cancelButton);
                buttonPanel.Children.Add(saveButton);
                panel.Children.Add(buttonPanel);

                dialog.Content = panel;
                await dialog.ShowDialog(this);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error editing skin: {ex.Message}");
            }
        }


        private async void DeleteSkin(SkinInfo skin)
        {
            try
            {
                // If the deleted skin was the currently selected one, clear selection
                if (_currentSettings.SelectedSkinId == skin.Id)
                {
                    _currentSettings.SelectedSkinId = null;
                    _currentlySelectedSkinCard = null;
                }

                _currentSettings.CustomSkins.Remove(skin);

                if (System.IO.File.Exists(skin.FilePath))
                {
                    System.IO.File.Delete(skin.FilePath);
                }

                await _settingsService.SaveSettingsAsync(_currentSettings);
                LoadSkins(); // Reloads skins and handles potential new auto-selection
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting skin: {ex.Message}");
            }
        }

        private void ShowLaunchErrorBanner(string message)
        {
            if (_launchErrorBanner == null || _launchErrorBannerText == null) return;

            _launchErrorBannerText.Text = message;
            _launchErrorBanner.IsVisible = true;
            _launchErrorBanner.Opacity = 1;

            if (!AreAnimationsEnabled()) // NEW
            {
                // No animation, just show and then hide after delay
                _ = Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_launchErrorBanner != null)
                        {
                            _launchErrorBanner.IsVisible = false;
                            _launchErrorBanner.Opacity = 0; // Ensure it's fully hidden
                        }
                    });
                });
                return;
            }

            // Auto-hide after 5 seconds
            _ = Task.Run(async () =>
            {
                await Task.Delay(3500);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_launchErrorBanner != null)
                    {
                        _launchErrorBanner.Opacity = 0;
                        // Hide completely after opacity animation
                        _ = Task.Delay(300).ContinueWith(_ =>
                        {
                            Dispatcher.UIThread.Post(() => _launchErrorBanner.IsVisible = false);
                        });
                    }
                });
            });
        }

        private void HideLaunchErrorBanner()
        {
            if (_launchErrorBanner == null) return;

            if (!AreAnimationsEnabled()) // NEW
            {
                _launchErrorBanner.Opacity = 0;
                _launchErrorBanner.IsVisible = false;
                return;
            }
            _launchErrorBanner.Opacity = 0;
            // Hide completely after opacity animation
            _ = Task.Delay(300).ContinueWith(_ =>
            {
                Dispatcher.UIThread.Post(() => _launchErrorBanner.IsVisible = false);
            });
        }

        private void OnQuickPlayServerPointerEntered(object? sender, PointerEventArgs e)
        {
            if (sender is not Button btn) return;

            _quickPlayTooltipHideCts?.Cancel();

            if (_quickPlayTooltipText != null && btn.Tag is ServerInfo si)
                _quickPlayTooltipText.Text = si.Name;

            if (_quickPlayTooltip == null) return;

            if (!AreAnimationsEnabled()) // NEW
            {
                PositionQuickPlayTooltipAbove(btn);
                _quickPlayTooltip.IsVisible = true;
                _quickPlayTooltip.Opacity = 1;
                return;
            }

            PositionQuickPlayTooltipAbove(btn);

            if (!_quickPlayTooltipHasShown)
            {
                _quickPlayTooltip.Transitions = new Transitions();
                _quickPlayTooltip.IsVisible = true;
                _quickPlayTooltip.Opacity = 1;

                var restore = new Transitions();
                if (_savedQuickPlayTooltipTransitions != null)
                    foreach (var t in _savedQuickPlayTooltipTransitions) restore.Add(t);
                _quickPlayTooltip.Transitions = restore;

                _quickPlayTooltipHasShown = true;
                return;
            }

            _quickPlayTooltip.IsVisible = true;
            _quickPlayTooltip.Opacity = 1;
        }

        private void OnQuickPlayServerPointerExited(object? sender, PointerEventArgs e)
        {
            _quickPlayTooltipHideCts?.Cancel();
            _quickPlayTooltipHideCts = new CancellationTokenSource();
            var ct = _quickPlayTooltipHideCts.Token;

            if (!AreAnimationsEnabled()) // NEW
            {
                if (_quickPlayTooltip != null)
                {
                    _quickPlayTooltip.Opacity = 0;
                    _quickPlayTooltip.IsVisible = false;
                }
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(140, ct); // Grace period
                    if (ct.IsCancellationRequested || _quickPlayTooltip == null) return;

                    Dispatcher.UIThread.Post(() => { _quickPlayTooltip!.Opacity = 0; });
                    await Task.Delay(200, ct);
                    if (!ct.IsCancellationRequested)
                    {
                        Dispatcher.UIThread.Post(() => { _quickPlayTooltip!.IsVisible = false; });
                    }
                }
                catch (OperationCanceledException) { }
            }, ct);
        }

        private void PositionQuickPlayTooltipAbove(Button button)
        {
            if (_quickPlayTooltip == null) return;

            // Get the button's position relative to the main window
            var origin = button.TranslatePoint(new Point(0, 0), this);
            if (!origin.HasValue) return;

            const double tooltipHeight = 26;
            const double gapAboveButton = 8;

            // Calculate tooltip width (use MinWidth if bounds aren't ready yet)
            double tooltipWidth = _quickPlayTooltip.Bounds.Width > 0
                ? _quickPlayTooltip.Bounds.Width
                : 80; // MinWidth fallback

            // Center horizontally on the button
            double buttonCenterX = origin.Value.X + (button.Bounds.Width / 2.0);
            double left = buttonCenterX - (tooltipWidth / 2.0);

            // Position ABOVE the button (subtract from Y position)
            double top = origin.Value.Y - tooltipHeight - gapAboveButton;

            Canvas.SetLeft(_quickPlayTooltip, Math.Max(0, left)); // Ensure it doesn't go off-screen left
            Canvas.SetTop(_quickPlayTooltip, Math.Max(0, top));   // Ensure it doesn't go off-screen top

            Console.WriteLine($"[QuickPlay Tooltip] Button at ({origin.Value.X}, {origin.Value.Y}), Tooltip at ({left}, {top})");
        }

        private void LoadServers()
        {
            if (_serversWrapPanel == null || _noServersMessage == null) return;

            _serversWrapPanel.Children.Clear();

            if (_currentSettings.CustomServers.Count == 0)
            {
                _noServersMessage.IsVisible = true;
            }
            else
            {
                _noServersMessage.IsVisible = false;
                foreach (var server in _currentSettings.CustomServers)
                {
                    var serverCard = CreateServerCard(server);
                    _serversWrapPanel.Children.Add(serverCard);
                }
            }
            // After creating/loading server cards, update their button states
            _ = UpdateServerButtonStates(); // Fire and forget for UI update
        }




        // MainWindow.cs
        private Border CreateServerCard(ServerInfo server)
        {
            var card = new Border
            {
                Background = GetBrush("CardBackgroundColor"),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 15),
                Tag = server.Id,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch // Ensure the card stretches
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // This column takes all available space
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Server Icon (existing code - no changes needed here)
            var iconBorder = new Border
            {
                Width = 48,
                Height = 48,
                Margin = new Thickness(0, 0, 15, 0),
                CornerRadius = new CornerRadius(8),
                ClipToBounds = true,
                Background = GetBrush("HoverBackgroundBrush")
            };
            var serverIcon = new Image
            {
                Name = $"ServerIcon_{server.Id}",
                Width = 48,
                Height = 48,
                Stretch = Avalonia.Media.Stretch.UniformToFill
            };
            if (!string.IsNullOrEmpty(server.IconBase64))
            {
                try
                {
                    var iconBytes = Convert.FromBase64String(server.IconBase64);
                    using var ms = new MemoryStream(iconBytes);
                    serverIcon.Source = new Avalonia.Media.Imaging.Bitmap(ms);
                }
                catch { }
            }
            iconBorder.Child = serverIcon;
            Grid.SetColumn(iconBorder, 0);
            grid.Children.Add(iconBorder);

            // Server Info
            var infoStack = new StackPanel
            {
                Spacing = 5,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch // Ensure infoStack stretches
            };

            var nameText = new TextBlock
            {
                Name = $"ServerName_{server.Id}",
                Text = server.Name,
                Foreground = GetBrush("PrimaryForegroundBrush"),
                FontWeight = FontWeight.Bold,
                FontSize = 16
            };
            infoStack.Children.Add(nameText);

            var statusStack = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 8
            };
            var statusDot = new Ellipse
            {
                Name = $"StatusDot_{server.Id}",
                Width = 8,
                Height = 8,
                Fill = Brushes.Gray,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            statusStack.Children.Add(statusDot);
            var statusText = new TextBlock
            {
                Name = $"StatusText_{server.Id}",
                Text = "Checking...",
                Foreground = GetBrush("SecondaryForegroundBrush"),
                FontSize = 12
            };
            statusStack.Children.Add(statusText);
            infoStack.Children.Add(statusStack);

            var motdText = new TextBlock
            {
                Name = $"Motd_{server.Id}",
                Text = "",
                Foreground = GetBrush("SecondaryForegroundBrush"),
                FontSize = 10,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch // Ensure MOTD text stretches
            };
            infoStack.Children.Add(motdText);

            Grid.SetColumn(infoStack, 1);
            grid.Children.Add(infoStack);

            // Action Buttons
            var actionStack = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 10,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right // This is correct
            };

            var joinButton = new Button
            {
                Name = $"JoinButton_{server.Id}",
                Content = "JOIN",
                Background = GetBrush("PrimaryAccentBrush"),
                Foreground = GetBrush("AccentButtonForegroundBrush"),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20, 10),
                FontWeight = FontWeight.Bold,
                Cursor = new Cursor(StandardCursorType.Hand),
                IsEnabled = false,
                Opacity = 1.0
            };
            joinButton.Click += (s, e) => JoinServer(server);
            actionStack.Children.Add(joinButton);

            // MODIFIED: Explicitly create TextBlock for content and center it
            var menuButton = new Button
            {
                FontSize = 20, // This applies to the TextBlock content
                Width = 40,
                Height = 40,
                Background = GetBrush("HoverBackgroundBrush"),
                CornerRadius = new CornerRadius(8),
                Cursor = new Cursor(StandardCursorType.Hand),
                // Explicitly set content alignment for the button itself
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Content = new TextBlock // Wrap the '⋮' in a TextBlock
                {
                    Text = "⋮",
                    Foreground = GetBrush("PrimaryForegroundBrush"), // Ensure correct color
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                }
            };
            menuButton.Flyout = CreateServerContextMenu(server);
            actionStack.Children.Add(menuButton);

            Grid.SetColumn(actionStack, 2);
            grid.Children.Add(actionStack);

            card.Child = grid;
            return card;
        }


        private MenuFlyout CreateServerContextMenu(ServerInfo server)
        {
            var menu = new MenuFlyout();

            var editItem = new MenuItem { Header = "Edit Server" };
            editItem.Click += (s, e) => EditServer(server);
            menu.Items.Add(editItem);

            var deleteItem = new MenuItem { Header = "Delete Server" };
            deleteItem.Click += (s, e) => DeleteServer(server);
            menu.Items.Add(deleteItem);

            var refreshItem = new MenuItem { Header = "Refresh Status" };
            refreshItem.Click += async (s, e) => await RefreshSingleServerAsync(server);
            menu.Items.Add(refreshItem);

            return menu;
        }

        private async void EditServer(ServerInfo server)
        {
            var dialog = new Window
            {
                Title = "Edit Server",
                Width = 450,
                Height = 280,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var panel = new StackPanel { Margin = new Thickness(20), Spacing = 15 };

            panel.Children.Add(new TextBlock { Text = "Server Name", FontWeight = FontWeight.Bold });
            var nameBox = new TextBox { MinWidth = 300, Text = server.Name };
            panel.Children.Add(nameBox);

            panel.Children.Add(new TextBlock { Text = "Server Address", FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) });
            var addressBox = new TextBox { MinWidth = 300, Text = server.Address };
            panel.Children.Add(addressBox);

            panel.Children.Add(new TextBlock { Text = "Port (optional, default: 25565)", FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 0) });
            var portBox = new TextBox { MinWidth = 300, Text = server.Port.ToString() };
            panel.Children.Add(portBox);

            var statusText = new TextBlock { Text = "", Foreground = Brushes.Gray, FontSize = 11 };
            panel.Children.Add(statusText);

            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 10,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var cancelButton = new Button { Content = "Cancel", Padding = new Thickness(15, 8) };
            cancelButton.Click += (s, e) => dialog.Close();

            var saveButton = new Button
            {
                Content = "Save Changes",
                Padding = new Thickness(15, 8),
                Background = Brushes.Green
            };

            saveButton.Click += async (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(nameBox.Text) || string.IsNullOrWhiteSpace(addressBox.Text))
                {
                    statusText.Text = "❌ Please enter both name and address";
                    statusText.Foreground = Brushes.Red;
                    return;
                }

                int port = 25565;
                if (!int.TryParse(portBox.Text, out port))
                    port = 25565;

                statusText.Text = "⏳ Validating server...";
                statusText.Foreground = Brushes.Orange;
                saveButton.IsEnabled = false;

                // Validate server using MrServer
                var status = await _serverChecker.GetServerStatusAsync(addressBox.Text, port);

                server.Name = nameBox.Text;
                server.Address = addressBox.Text;
                server.Port = port;
                server.ModifiedDate = DateTime.Now;

                if (!string.IsNullOrEmpty(status.IconData))
                {
                    server.IconBase64 = status.IconData;
                }

                await _settingsService.SaveSettingsAsync(_currentSettings);
                dialog.Close();

                LoadServers();
                RefreshQuickPlayBar();

                await RefreshSingleServerAsync(server);
            };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(saveButton);
            panel.Children.Add(buttonPanel);

            dialog.Content = panel;
            await dialog.ShowDialog(this);
        }


        private async void DeleteServer(ServerInfo server)
        {
            _currentSettings.CustomServers.Remove(server);
            await _settingsService.SaveSettingsAsync(_currentSettings);
            LoadServers();
            RefreshQuickPlayBar();
            // Trigger a full refresh of all server statuses after deleting a server
            await RefreshAllServerStatusesAsync();
        }
        // Add this helper method inside your MainWindow class in MainWindow.axaml.cs
        private async void ShowOfflineDialog(string serverName)
        {
            var dialog = new Window
            {
                Title = "Server Offline",
                Width = 350,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 15,
                    Children =
            {
                new TextBlock
                {
                    Text = $"The server '{serverName}' is currently offline or cannot be fetched.",
                    Foreground = GetBrush("PrimaryForegroundBrush"),
                    FontWeight = FontWeight.Bold,
                    FontSize = 16,
                    TextWrapping = TextWrapping.Wrap
                },
                new TextBlock
                {
                    Text = "Please try again later.",
                    Foreground = GetBrush("SecondaryForegroundBrush"),
                    FontSize = 13
                },
                new Button
                {
                    Content = "OK",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Padding = new Thickness(15, 8),
                    Background = GetBrush("PrimaryAccentBrush"),
                    Foreground = GetBrush("AccentButtonForegroundBrush"),
                    CornerRadius = new CornerRadius(8)
                }
            }
                }
            };

            // Set the button's click event to close the dialog
            if ((dialog.Content as StackPanel)?.Children.OfType<Button>().Last() is Button okButton)
            {
                okButton.Click += (s, e) => dialog.Close();
            }

            await dialog.ShowDialog(this);
        }

        // Add this new helper method inside your MainWindow class in MainWindow.axaml.cs
        private async void ShowGameAlreadyLaunchingDialog()
        {
            var dialog = new Window
            {
                Title = "Game Already Launching",
                Width = 400,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 15,
                    Children =
            {
                new TextBlock
                {
                    Text = "A game is already launching or running.",
                    Foreground = GetBrush("PrimaryForegroundBrush"),
                    FontWeight = FontWeight.Bold,
                    FontSize = 16,
                    TextWrapping = TextWrapping.Wrap
                },
                new TextBlock
                {
                    Text = "Please close the current process to join a server and start a new minecraft process.",
                    Foreground = GetBrush("SecondaryForegroundBrush"),
                    FontSize = 13
                },
                new Button
                {
                    Content = "OK",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    Padding = new Thickness(15, 8),
                    Background = GetBrush("PrimaryAccentBrush"),
                    Foreground = GetBrush("AccentButtonForegroundBrush"),
                    CornerRadius = new CornerRadius(8)
                }
            }
                }
            };

            // Set the button's click event to close the dialog
            if ((dialog.Content as StackPanel)?.Children.OfType<Button>().Last() is Button okButton)
            {
                okButton.Click += (s, e) => dialog.Close();
            }

            await dialog.ShowDialog(this);
        }

        private void JoinServer(ServerInfo server)
        {
            // First, check if a game is already launching/running
            if (_isLaunching || _isInstalling)
            {
                ShowGameAlreadyLaunchingDialog();
                return; // Stop here, game is already launching
            }

            // If no game is launching, then check server online status
            if (!server.IsOnline)
            {
                ShowOfflineDialog(server.Name);
                return; // Don't proceed with launch if offline
            }

            // If online and no launch in progress, proceed to launch the game to the server
            LaunchGameToServer(server);
        }

        private void UpdateServerCardUI(ServerInfo server)
        {
            var card = _serversWrapPanel?.Children.OfType<Border>()
                .FirstOrDefault(b => b.Tag as string == server.Id);

            if (card == null)
            {
                Console.WriteLine($"[UI Update] Card not found for {server.Name}");
                return;
            }

            // The card's child is a Grid, get it
            if (!(card.Child is Grid grid))
            {
                Console.WriteLine($"[UI Update] Grid not found in card for {server.Name}");
                return;
            }

            // Find the info StackPanel (it's in column 1)
            var infoStack = grid.Children.OfType<StackPanel>()
                .FirstOrDefault(sp => Grid.GetColumn(sp) == 1);

            if (infoStack == null)
            {
                Console.WriteLine($"[UI Update] Info stack not found for {server.Name}");
                return;
            }

            // Get the status stack (it's the second child of infoStack)
            var statusStack = infoStack.Children.OfType<StackPanel>()
                .FirstOrDefault(sp => sp.Orientation == Avalonia.Layout.Orientation.Horizontal);

            if (statusStack != null)
            {
                // Update status dot (first child - Ellipse)
                var statusDot = statusStack.Children.OfType<Ellipse>().FirstOrDefault();
                if (statusDot != null)
                {
                    statusDot.Fill = server.IsOnline ? Brushes.Green : Brushes.Red;
                }

                // Update status text (second child - TextBlock)
                var statusText = statusStack.Children.OfType<TextBlock>().FirstOrDefault();
                if (statusText != null)
                {
                    if (server.IsOnline)
                    {
                        statusText.Text = $"Online • {server.CurrentPlayers}/{server.MaxPlayers}";
                    }
                    else
                    {
                        statusText.Text = "Offline";
                    }
                }
            }

            // Update MOTD (third child of infoStack)
            var motdText = infoStack.Children.OfType<TextBlock>()
                .Skip(1) // Skip the name TextBlock
                .FirstOrDefault();

            if (motdText != null)
            {
                motdText.Text = string.IsNullOrEmpty(server.Motd) ? "No description" : server.Motd;
            }

            // Find action stack (column 2) and update join button
            var actionStack = grid.Children.OfType<StackPanel>()
                .FirstOrDefault(sp => Grid.GetColumn(sp) == 2);

            if (actionStack != null)
            {
                var joinButton = actionStack.Children.OfType<Button>().FirstOrDefault();
                if (joinButton != null)
                {
                    joinButton.IsEnabled = server.IsOnline;
                    joinButton.Background = server.IsOnline
                        ? GetBrush("PrimaryAccentBrush")
                        : GetBrush("HoverBackgroundBrush");
                    joinButton.Foreground = server.IsOnline
                        ? GetBrush("AccentButtonForegroundBrush")
                        : GetBrush("DisabledForegroundBrush");
                }
            }

            // Update server icon (in column 0, inside a Border)
            var iconBorder = grid.Children.OfType<Border>()
                .FirstOrDefault(b => Grid.GetColumn(b) == 0);

            if (iconBorder != null)
            {
                var serverIcon = iconBorder.Child as Image;
                if (serverIcon != null && !string.IsNullOrEmpty(server.IconBase64))
                {
                    try
                    {
                        // Clean the base64 string (remove data URI prefix if present)
                        var base64Data = server.IconBase64;
                        if (base64Data.StartsWith("data:image"))
                        {
                            var commaIndex = base64Data.IndexOf(',');
                            if (commaIndex > 0)
                            {
                                base64Data = base64Data.Substring(commaIndex + 1);
                            }
                        }

                        var iconBytes = Convert.FromBase64String(base64Data);
                        using var ms = new MemoryStream(iconBytes);
                        serverIcon.Source = new Avalonia.Media.Imaging.Bitmap(ms);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[UI Update] Error updating icon for {server.Name}: {ex.Message}");
                        // Icon update failed, but don't crash - just skip it
                    }
                }

            }

            Console.WriteLine($"[UI Update] Updated {server.Name} - Online: {server.IsOnline}");
        }


        private void RefreshQuickPlayBar()
        {
            Dispatcher.UIThread.Post(() =>
            {
                var quickPlayContainer = this.FindControl<StackPanel>("QuickPlayServersContainer");
                if (quickPlayContainer == null) return;

                quickPlayContainer.Children.Clear();

                foreach (var server in _currentSettings.CustomServers)
                {
                    var serverButton = new Button
                    {
                        Classes = { "IconButton" },
                        Width = 50,
                        Height = 40,
                        Tag = server, // Store ServerInfo in Tag
                        IsEnabled = true, // Quick Play buttons are always enabled (unless a launch is in progress)
                        Opacity = 1.0 // Initial opacity, will be updated by UpdateServerButtonStates
                    };

                    serverButton.Click += (s, e) => JoinServer(server);

                    // Add hover events for tooltip
                    serverButton.PointerEntered += OnQuickPlayServerPointerEntered;
                    serverButton.PointerExited += OnQuickPlayServerPointerExited;

                    var icon = new Image { Width = 32, Height = 32 };

                    if (!string.IsNullOrEmpty(server.IconBase64))
                    {
                        try
                        {
                            // Clean the base64 string (remove data URI prefix if present)
                            var base64Data = server.IconBase64;
                            if (base64Data.StartsWith("data:image"))
                            {
                                var commaIndex = base64Data.IndexOf(',');
                                if (commaIndex > 0)
                                {
                                    base64Data = base64Data.Substring(commaIndex + 1);
                                }
                            }

                            var iconBytes = Convert.FromBase64String(base64Data);
                            using var ms = new MemoryStream(iconBytes);
                            icon.Source = new Avalonia.Media.Imaging.Bitmap(ms);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[QuickPlay] Error loading icon for {server.Name}: {ex.Message}");
                            icon.Source = new Avalonia.Media.Imaging.Bitmap(
                                AssetLoader.Open(new Uri("avares://LeafClient/Assets/minecraft.png")));
                        }
                    }
                    else
                    {
                        icon.Source = new Avalonia.Media.Imaging.Bitmap(
                            AssetLoader.Open(new Uri("avares://LeafClient/Assets/minecraft.png")));
                    }

                    var roundedIconBorder = new Border
                    {
                        CornerRadius = new CornerRadius(8), // Adjust this value for desired roundness
                        ClipToBounds = true, // Crucial for clipping the image to the rounded corners
                        Background = Brushes.Transparent, // Ensure no background interferes
                        Child = icon
                    };

                    serverButton.Content = roundedIconBorder; // Set the Border as the button's content
                    quickPlayContainer.Children.Add(serverButton);
                }

                Console.WriteLine($"[QuickPlay] Refreshed bar with {_currentSettings.CustomServers.Count} servers");
                // After creating/recreating quick play buttons, update their states
                _ = UpdateServerButtonStates(); // Fire and forget for UI update
            });
        }

        private async void AddServerButton_Click(object? sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "Add Server",
                Width = 450,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false
            };

            var panel = new StackPanel { Margin = new Thickness(20), Spacing = 15 };

            // Server Name
            panel.Children.Add(new TextBlock
            {
                Text = "Server Name",
                FontWeight = FontWeight.Bold
            });
            var nameBox = new TextBox
            {
                Watermark = "My Awesome Server",
                MinWidth = 300
            };
            panel.Children.Add(nameBox);

            // Server Address
            panel.Children.Add(new TextBlock
            {
                Text = "Server Address",
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 10, 0, 0)
            });
            var addressBox = new TextBox
            {
                Watermark = "play.example.com",
                MinWidth = 300
            };
            panel.Children.Add(addressBox);

            // Server Port
            panel.Children.Add(new TextBlock
            {
                Text = "Port (optional, default: 25565)",
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 10, 0, 0)
            });
            var portBox = new TextBox
            {
                Text = "25565",
                MinWidth = 300
            };
            panel.Children.Add(portBox);

            // Status text
            var statusText = new TextBlock
            {
                Text = "",
                Foreground = Brushes.Gray,
                FontSize = 11
            };
            panel.Children.Add(statusText);

            // Buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 10,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var cancelButton = new Button { Content = "Cancel", Padding = new Thickness(15, 8) };
            cancelButton.Click += (s, args) => dialog.Close();

            var addButton = new Button
            {
                Content = "Add Server",
                Padding = new Thickness(15, 8),
                Background = Brushes.Green
            };

            addButton.Click += async (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(nameBox.Text) || string.IsNullOrWhiteSpace(addressBox.Text))
                {
                    statusText.Text = "❌ Please enter both name and address";
                    statusText.Foreground = Brushes.Red;
                    return;
                }

                int port = 25565;
                if (!int.TryParse(portBox.Text, out port))
                    port = 25565;

                statusText.Text = "⏳ Validating server...";
                statusText.Foreground = Brushes.Orange;
                addButton.IsEnabled = false;

                // Validate server using MrServer
                var serverStatus = await _serverChecker.GetServerStatusAsync(addressBox.Text, port);

                if (!serverStatus.IsOnline)
                {
                    statusText.Text = "⚠️ Server appears offline, but you can still add it";
                    statusText.Foreground = Brushes.Orange;
                    await Task.Delay(1500);
                }

                var newServer = new ServerInfo
                {
                    Name = nameBox.Text,
                    Address = addressBox.Text,
                    Port = port,
                    IconBase64 = serverStatus.IconData ?? ""
                };

                _currentSettings.CustomServers.Add(newServer);
                await _settingsService.SaveSettingsAsync(_currentSettings);

                dialog.Close();
                LoadServers();
                RefreshQuickPlayBar();
                // Trigger a full refresh of all server statuses after adding a server
                await RefreshAllServerStatusesAsync();
            };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(addButton);
            panel.Children.Add(buttonPanel);

            dialog.Content = panel;
            await dialog.ShowDialog(this);
        }


        private async void RefreshServersButton_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                btn.IsEnabled = false;
                try
                {
                    await RefreshAllServerStatusesAsync();
                }
                finally
                {
                    btn.IsEnabled = true;
                }
            }
        }

        private async void OpenJvmArgumentsEditor(object? sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "Edit JVM Arguments",
                Width = 600,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = true,
                Content = new StackPanel
                {
                    Margin = new Thickness(20),
                    Spacing = 15,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Custom Java Virtual Machine Arguments",
                            FontWeight = FontWeight.Bold,
                            FontSize = 16,
                            Margin = new Thickness(0, 0, 0, 5)
                        },
                        new TextBlock
                        {
                            Text = "Enter arguments separated by spaces. Example: -Xmx2G -Xms1G",
                            Foreground = GetBrush("SecondaryForegroundBrush"),
                            FontSize = 12,
                            TextWrapping = TextWrapping.Wrap,
                            Margin = new Thickness(0, 0, 0, 10)
                        }
                    }
                }
            };

            var textBox = new TextBox
            {
                AcceptsReturn = true, // Allow multiline input
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Top,
                TextWrapping = TextWrapping.Wrap,
                Height = 200, // Make it taller for easier editing
                Watermark = "e.g., -Xmx2G -Xms1G -XX:+UseG1GC",
                Text = _currentSettings.JvmArguments // Load existing arguments
            };

            var stackPanel = (dialog.Content as StackPanel);
            stackPanel?.Children.Add(textBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 10,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var cancelButton = new Button { Content = "Cancel", Padding = new Thickness(15, 8) };
            cancelButton.Click += (s, args) => dialog.Close();

            var saveButton = new Button
            {
                Content = "Save",
                Padding = new Thickness(15, 8),
                Background = GetBrush("SuccessBrush")
            };
            saveButton.Click += async (s, args) =>
            {
                _currentSettings.JvmArguments = textBox.Text.Trim();
                await _settingsService.SaveSettingsAsync(_currentSettings);
                MarkSettingsDirty(); // Mark settings as dirty to show the banner
                dialog.Close();
            };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(saveButton);
            stackPanel?.Children.Add(buttonPanel);

            await dialog.ShowDialog(this);
        }


        private void OpenDeveloperGitHub(object? sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/voidZiAD",
                UseShellExecute = true
            });
        }

        private bool AreAnimationsEnabled()
        {
            return _currentSettings.AnimationsEnabled;
        }

        private async Task<bool> InstallLithiumIfNeededAsync(string mcVersion)
        {
            // Check if we should install Lithium based on settings
            if (!_currentSettings.IsLithiumEnabled)
            {
                Console.WriteLine("[Lithium] Lithium installation skipped - disabled in settings");
                return true;
            }

            // IMPORTANT: Use the global .minecraft/mods folder
            string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
            Directory.CreateDirectory(modsFolder);

            try
            {
                ShowProgress(true, $"Installing Lithium for Minecraft {mcVersion}...");

                using var client = new HttpClient();
                var apiUrl = $"https://api.modrinth.com/v2/project/gvQqBUqZ/version?game_versions=[\"{mcVersion}\"]&loaders=[\"fabric\"]";
                var response = await client.GetStringAsync(apiUrl);

                if (string.IsNullOrWhiteSpace(response))
                {
                    throw new Exception("Empty response from Modrinth API");
                }

                var versions = JsonSerializer.Deserialize<List<ModrinthVersion>>(response, Json.Options);
                if (versions == null || versions.Count == 0)
                {
                    throw new Exception($"No Lithium versions found for Minecraft {mcVersion}");
                }

                var latest = versions.FirstOrDefault();
                if (latest?.files == null || latest.files.Count == 0)
                {
                    throw new Exception("No download files found for Lithium");
                }

                var downloadUrl = latest.files[0].url;
                if (string.IsNullOrWhiteSpace(downloadUrl))
                {
                    throw new Exception("Invalid download URL for Lithium");
                }

                Console.WriteLine($"[Lithium] Downloading from: {downloadUrl}");

                var jarBytes = await client.GetByteArrayAsync(downloadUrl);
                if (jarBytes == null || jarBytes.Length == 0)
                {
                    throw new Exception("Downloaded file is empty");
                }

                string fileName = $"lithium-{mcVersion}.jar";
                string lithiumPath = System.IO.Path.Combine(modsFolder, fileName);

                await File.WriteAllBytesAsync(lithiumPath, jarBytes);
                Console.WriteLine($"[Lithium] Successfully installed for {mcVersion} at {lithiumPath}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Lithium] Installation failed: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[Lithium] Inner exception: {ex.InnerException.Message}");
                }
                return false;
            }
            finally
            {
                ShowProgress(false);
            }
        }


        private void WireSettingsDirtyHandlers()
        {
            // Toggles
            if (_enablePrayerTimeReminderToggle != null) { _enablePrayerTimeReminderToggle.Checked += (_, __) => MarkSettingsDirty(); _enablePrayerTimeReminderToggle.Unchecked += (_, __) => MarkSettingsDirty(); }
            if (_prayerTimeCountryComboBox != null) { _prayerTimeCountryComboBox.SelectionChanged += (_, __) => MarkSettingsDirty(); } // UPDATED: Added SelectionChanged handler
            if (_prayerTimeCityTextBox != null) { _prayerTimeCityTextBox.PropertyChanged += (s, e) => { if (e.Property == TextBox.TextProperty) MarkSettingsDirty(); }; }
            if (_prayerCalculationMethodComboBox != null) { _prayerCalculationMethodComboBox.SelectionChanged += (_, __) => MarkSettingsDirty(); }
            if (_prayerReminderMinutesBeforeSlider != null) { _prayerReminderMinutesBeforeSlider.PropertyChanged += (s, e) => { if (e.Property == Slider.ValueProperty) MarkSettingsDirty(); }; }
            if (_gameResolutionWidthTextBox != null) _gameResolutionWidthTextBox.PropertyChanged += (s, e) =>
            {
                if (e.Property == TextBox.TextProperty) MarkSettingsDirty();
            };
            if (_gameResolutionHeightTextBox != null) _gameResolutionHeightTextBox.PropertyChanged += (s, e) =>
            {
                if (e.Property == TextBox.TextProperty) MarkSettingsDirty();
            };
            if (_useCustomGameResolutionToggle != null) { _useCustomGameResolutionToggle.Checked += (_, __) => MarkSettingsDirty(); _useCustomGameResolutionToggle.Unchecked += (_, __) => MarkSettingsDirty(); }
            if (_lockGameAspectRatioToggle != null) { _lockGameAspectRatioToggle.Checked += (_, __) => MarkSettingsDirty(); _lockGameAspectRatioToggle.Unchecked += (_, __) => MarkSettingsDirty(); }

            if (_launcherVisibilityKeepOpenRadio != null) _launcherVisibilityKeepOpenRadio.Checked += (_, __) => MarkSettingsDirty();
            if (_launcherVisibilityHideRadio != null) _launcherVisibilityHideRadio.Checked += (_, __) => MarkSettingsDirty();

            if (_updateDeliveryNormalRadio != null) _updateDeliveryNormalRadio.Checked += (_, __) => MarkSettingsDirty();
            if (_updateDeliveryEarlyRadio != null) _updateDeliveryEarlyRadio.Checked += (_, __) => MarkSettingsDirty();
            if (_updateDeliveryLateRadio != null) _updateDeliveryLateRadio.Checked += (_, __) => MarkSettingsDirty();

            if (_showUsernameInDiscordRichPresenceToggle != null) { _showUsernameInDiscordRichPresenceToggle.Checked += (_, __) => MarkSettingsDirty(); _showUsernameInDiscordRichPresenceToggle.Unchecked += (_, __) => MarkSettingsDirty(); }

            if (_closingNotificationsAlwaysRadio != null) _closingNotificationsAlwaysRadio.Checked += (_, __) => MarkSettingsDirty();
            if (_closingNotificationsJustOnceRadio != null) _closingNotificationsJustOnceRadio.Checked += (_, __) => MarkSettingsDirty();
            if (_closingNotificationsNeverRadio != null) _closingNotificationsNeverRadio.Checked += (_, __) => MarkSettingsDirty();
            if (_enableUpdateNotificationsToggle != null) { _enableUpdateNotificationsToggle.Checked += (_, __) => MarkSettingsDirty(); _enableUpdateNotificationsToggle.Unchecked += (_, __) => MarkSettingsDirty(); }
            if (_enableNewContentIndicatorsToggle != null) { _enableNewContentIndicatorsToggle.Checked += (_, __) => MarkSettingsDirty(); _enableNewContentIndicatorsToggle.Unchecked += (_, __) => MarkSettingsDirty(); }
            if (_optiFineToggle != null)
            {
                _optiFineToggle.Checked += async (_, __) =>
                {
                    if (_isApplyingSettings) return; // Prevent saving while settings are initially loading
                    _currentSettings.IsOptiFineEnabled = true;
                    await _settingsService.SaveSettingsAsync(_currentSettings);
                };
                _optiFineToggle.Unchecked += async (_, __) =>
                {
                    if (_isApplyingSettings) return; // Prevent saving while settings are initially loading
                    _currentSettings.IsOptiFineEnabled = false;
                    await _settingsService.SaveSettingsAsync(_currentSettings);
                };
            }
            if (_lithiumToggle != null)
            {
                _lithiumToggle.Checked += async (_, __) =>
                {
                    if (_isApplyingSettings) return;
                    _currentSettings.IsLithiumEnabled = true;
                    await _settingsService.SaveSettingsAsync(_currentSettings);
                };
                _lithiumToggle.Unchecked += async (_, __) =>
                {
                    if (_isApplyingSettings) return;
                    _currentSettings.IsLithiumEnabled = false;
                    await _settingsService.SaveSettingsAsync(_currentSettings);
                };
            }
            if (_sodiumToggle != null)
            {
                _sodiumToggle.Checked += async (_, __) =>
                {
                    if (_isApplyingSettings) return;
                    _currentSettings.IsSodiumEnabled = true;
                    await _settingsService.SaveSettingsAsync(_currentSettings);
                };
                _sodiumToggle.Unchecked += async (_, __) =>
                {
                    if (_isApplyingSettings) return;
                    _currentSettings.IsSodiumEnabled = false;
                    await _settingsService.SaveSettingsAsync(_currentSettings);
                };
            }
            if (_launchOnStartupToggle != null)
            {
                _launchOnStartupToggle.Checked += (_, __) => MarkSettingsDirty();
                _launchOnStartupToggle.Unchecked += (_, __) => MarkSettingsDirty();
            }
            if (_minimizeToTrayToggle != null)
            {
                _minimizeToTrayToggle.Checked += async (_, __) =>
                {
                    if (_isApplyingSettings) return;
                    _currentSettings.MinimizeToTray = true;
                    await _settingsService.SaveSettingsAsync(_currentSettings);
                    MarkSettingsDirty(); // if you still want the "Save" banner visual
                };
                _minimizeToTrayToggle.Unchecked += async (_, __) =>
                {
                    if (_isApplyingSettings) return;
                    _currentSettings.MinimizeToTray = false;
                    await _settingsService.SaveSettingsAsync(_currentSettings);
                    MarkSettingsDirty();
                };
            }
            if (_discordRichPresenceToggle != null)
            {
                _discordRichPresenceToggle.Checked += (_, __) => MarkSettingsDirty();
                _discordRichPresenceToggle.Unchecked += (_, __) => MarkSettingsDirty();
            }
            if (_quickLaunchEnabledToggle != null)
            {
                _quickLaunchEnabledToggle.Checked += (_, __) => MarkSettingsDirty();
                _quickLaunchEnabledToggle.Unchecked += (_, __) => MarkSettingsDirty();
            }
            if (_animationsEnabledToggle != null)
            {
                _animationsEnabledToggle.Checked += (_, __) =>
                {
                    MarkSettingsDirty();
                    OnAnimationsEnabledChanged(); // Apply immediately
                };
                _animationsEnabledToggle.Unchecked += (_, __) =>
                {
                    MarkSettingsDirty();
                    OnAnimationsEnabledChanged(); // Apply immediately
                };
            }
            if (_autoJumpToggle != null)
            {
                _autoJumpToggle.Checked += (_, __) => MarkSettingsDirty();
                _autoJumpToggle.Unchecked += (_, __) => MarkSettingsDirty();
            }
            if (_touchscreenToggle != null)
            {
                _touchscreenToggle.Checked += (_, __) => MarkSettingsDirty();
                _touchscreenToggle.Unchecked += (_, __) => MarkSettingsDirty();
            }
            if (_toggleSprintToggle != null)
            {
                _toggleSprintToggle.Checked += (_, __) => MarkSettingsDirty();
                _toggleSprintToggle.Unchecked += (_, __) => MarkSettingsDirty();
            }
            if (_toggleCrouchToggle != null)
            {
                _toggleCrouchToggle.Checked += (_, __) => MarkSettingsDirty();
                _toggleCrouchToggle.Unchecked += (_, __) => MarkSettingsDirty();
            }
            if (_subtitlesToggle != null)
            {
                _subtitlesToggle.Checked += (_, __) => MarkSettingsDirty();
                _subtitlesToggle.Unchecked += (_, __) => MarkSettingsDirty();
            }
            if (_vSyncToggle != null)
            {
                _vSyncToggle.Checked += (_, __) => MarkSettingsDirty();
                _vSyncToggle.Unchecked += (_, __) => MarkSettingsDirty();
            }
            if (_fullscreenToggle != null)
            {
                _fullscreenToggle.Checked += (_, __) => MarkSettingsDirty();
                _fullscreenToggle.Unchecked += (_, __) => MarkSettingsDirty();
            }
            if (_entityShadowsToggle != null)
            {
                _entityShadowsToggle.Checked += (_, __) => MarkSettingsDirty();
                _entityShadowsToggle.Unchecked += (_, __) => MarkSettingsDirty();
            }
            if (_highContrastToggle != null)
            {
                _highContrastToggle.Checked += (_, __) => MarkSettingsDirty();
                _highContrastToggle.Unchecked += (_, __) => MarkSettingsDirty();
            }

            // TextBoxes
            if (_minRamAllocationTextBox != null)
            {
                _minRamAllocationTextBox.PropertyChanged += (s, e) =>
                {
                    if (e.Property == TextBox.TextProperty) MarkSettingsDirty();
                };
            }
            if (_quickJoinServerAddressTextBox != null)
            {
                _quickJoinServerAddressTextBox.PropertyChanged += (s, e) =>
                {
                    if (e.Property == TextBox.TextProperty) MarkSettingsDirty();
                };
            }
            if (_quickJoinServerPortTextBox != null)
            {
                _quickJoinServerPortTextBox.PropertyChanged += (s, e) =>
                {
                    if (e.Property == TextBox.TextProperty) MarkSettingsDirty();
                };
            }

            // Sliders
            if (_mouseSensitivitySlider != null)
            {
                _mouseSensitivitySlider.PropertyChanged += (s, e) =>
                {
                    if (e.Property == Slider.ValueProperty) MarkSettingsDirty();
                };
            }
            if (_scrollSensitivitySlider != null)
            {
                _scrollSensitivitySlider.PropertyChanged += (s, e) =>
                {
                    if (e.Property == Slider.ValueProperty) MarkSettingsDirty();
                };
            }
            if (_renderDistanceSlider != null)
            {
                _renderDistanceSlider.PropertyChanged += (s, e) =>
                {
                    if (e.Property == Slider.ValueProperty) MarkSettingsDirty();
                };
            }
            if (_simulationDistanceSlider != null)
            {
                _simulationDistanceSlider.PropertyChanged += (s, e) =>
                {
                    if (e.Property == Slider.ValueProperty) MarkSettingsDirty();
                };
            }
            if (_entityDistanceSlider != null)
            {
                _entityDistanceSlider.PropertyChanged += (s, e) =>
                {
                    if (e.Property == Slider.ValueProperty) MarkSettingsDirty();
                };
            }
            if (_maxFpsSlider != null)
            {
                _maxFpsSlider.PropertyChanged += (s, e) =>
                {
                    if (e.Property == Slider.ValueProperty) MarkSettingsDirty();
                };
            }

            // ComboBoxes
            if (_maxRamAllocationComboBox != null)
                _maxRamAllocationComboBox.SelectionChanged += (_, __) => MarkSettingsDirty();

            if (_renderCloudsComboBox != null)
                _renderCloudsComboBox.SelectionChanged += (_, __) => MarkSettingsDirty();

            if (_playerMainHandComboBox != null)
                _playerMainHandComboBox.SelectionChanged += (_, __) => MarkSettingsDirty();

            if (_themeComboBox != null)
                _themeComboBox.SelectionChanged += (_, __) => MarkSettingsDirty();
        }


        private void InitializeSkinStatusBanner()
        {
            _skinStatusBanner = this.FindControl<Border>("SkinStatusBanner");
            _skinStatusBannerText = this.FindControl<TextBlock>("SkinStatusBannerText");
            _skinStatusBannerButton = this.FindControl<Button>("SkinStatusBannerButton");

            if (_skinStatusBannerButton != null)
            {
                _skinStatusBannerButton.Click += CloseSkinStatusBanner;
            }
        }

        // Add this method to close the banner
        private void CloseSkinStatusBanner(object? sender, RoutedEventArgs e)
        {
            if (_skinStatusBanner != null)
            {
                _skinStatusBanner.IsVisible = false;
            }
        }

        // Add this method to show the banner with different messages
        private void ShowSkinStatusBanner(string message, SkinBannerStatus status = SkinBannerStatus.Info)
        {
            if (_skinStatusBanner == null || _skinStatusBannerText == null) return;

            // Set background color based on status
            _skinStatusBanner.Background = status switch
            {
                SkinBannerStatus.Success => new SolidColorBrush(Color.FromRgb(34, 139, 34)), // Green
                SkinBannerStatus.Error => new SolidColorBrush(Color.FromRgb(220, 38, 38)), // Red
                SkinBannerStatus.Warning => new SolidColorBrush(Color.FromRgb(234, 179, 8)), // Yellow/Orange
                _ => new SolidColorBrush(Color.FromRgb(42, 42, 42)) // Default gray
            };

            _skinStatusBannerText.Text = message;
            _skinStatusBanner.IsVisible = true;

            // Auto-hide after 5 seconds for success/error messages, keep warning visible
            if (status != SkinBannerStatus.Warning)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_skinStatusBanner != null)
                        {
                            _skinStatusBanner.IsVisible = false;
                        }
                    });
                });
            }
        }

        // Add this enum near the top of your class (after the fields section)
        private enum SkinBannerStatus
        {
            Success,
            Error,
            Warning,
            Info
        }


        private void WireGameOptionsHandlers()
        {
            if (_simulationDistanceSlider != null)
            {
                _simulationDistanceSlider.ValueChanged += (s, e) =>
                {
                    _optionsService.SetInt("simulationDistance", (int)e.NewValue);
                    _optionsService.Save();
                };
            }
            if (_renderDistanceSlider != null)
            {
                _renderDistanceSlider.ValueChanged += (s, e) =>
                {
                    _optionsService.SetInt("renderDistance", (int)e.NewValue);
                    _optionsService.Save();
                };
            }
            if (_entityDistanceSlider != null)
            {
                _entityDistanceSlider.ValueChanged += (s, e) =>
                {
                    _optionsService.SetFloat("entityDistanceScaling", e.NewValue);
                    _optionsService.Save();
                };
            }
            if (_maxFpsSlider != null)
            {
                _maxFpsSlider.ValueChanged += (s, e) =>
                {
                    _optionsService.SetInt("maxFps", (int)e.NewValue);
                    _optionsService.Save();
                };
            }
            if (_mouseSensitivitySlider != null)
            {
                _mouseSensitivitySlider.ValueChanged += (s, e) =>
                {
                    _optionsService.SetFloat("mouseSensitivity", e.NewValue);
                    _optionsService.Save();
                };
            }
            if (_scrollSensitivitySlider != null)
            {
                _scrollSensitivitySlider.ValueChanged += (s, e) =>
                {
                    _optionsService.SetFloat("scrollSensitivity", e.NewValue);
                    _optionsService.Save();
                };
            }
            if (_autoJumpToggle != null)
            {
                _autoJumpToggle.Checked += (s, e) => { _optionsService.SetBool("autoJump", true); _optionsService.Save(); };
                _autoJumpToggle.Unchecked += (s, e) => { _optionsService.SetBool("autoJump", false); _optionsService.Save(); };
            }
            if (_touchscreenToggle != null)
            {
                _touchscreenToggle.Checked += (s, e) => { _optionsService.SetBool("touchscreen", true); _optionsService.Save(); };
                _touchscreenToggle.Unchecked += (s, e) => { _optionsService.SetBool("touchscreen", false); _optionsService.Save(); };
            }
            if (_toggleSprintToggle != null)
            {
                _toggleSprintToggle.Checked += (s, e) => { _optionsService.SetBool("toggleSprint", true); _optionsService.Save(); };
                _toggleSprintToggle.Unchecked += (s, e) => { _optionsService.SetBool("toggleSprint", false); _optionsService.Save(); };
            }
            if (_toggleCrouchToggle != null)
            {
                _toggleCrouchToggle.Checked += (s, e) => { _optionsService.SetBool("toggleCrouch", true); _optionsService.Save(); };
                _toggleCrouchToggle.Unchecked += (s, e) => { _optionsService.SetBool("toggleCrouch", false); _optionsService.Save(); };
            }
            if (_subtitlesToggle != null)
            {
                _subtitlesToggle.Checked += (s, e) => { _optionsService.SetBool("showSubtitles", true); _optionsService.Save(); };
                _subtitlesToggle.Unchecked += (s, e) => { _optionsService.SetBool("showSubtitles", false); _optionsService.Save(); };
            }
            if (_vSyncToggle != null)
            {
                _vSyncToggle.Checked += (s, e) => { _optionsService.SetBool("enableVsync", true); _optionsService.Save(); };
                _vSyncToggle.Unchecked += (s, e) => { _optionsService.SetBool("enableVsync", false); _optionsService.Save(); };
            }
            if (_fullscreenToggle != null)
            {
                _fullscreenToggle.Checked += (s, e) => { _optionsService.SetBool("fullscreen", true); _optionsService.Save(); };
                _fullscreenToggle.Unchecked += (s, e) => { _optionsService.SetBool("fullscreen", false); _optionsService.Save(); };
            }
            if (_entityShadowsToggle != null)
            {
                _entityShadowsToggle.Checked += (s, e) => { _optionsService.SetBool("entityShadows", true); _optionsService.Save(); };
                _entityShadowsToggle.Unchecked += (s, e) => { _optionsService.SetBool("entityShadows", false); _optionsService.Save(); };
            }
            if (_renderCloudsComboBox != null)
            {
                _renderCloudsComboBox.SelectionChanged += (s, e) =>
                {
                    if (_renderCloudsComboBox.SelectedItem is ComboBoxItem item && item.Content is string val)
                    {
                        var mapped = val.ToLowerInvariant() switch
                        {
                            "off" => "off",
                            "fast" => "fast",
                            "fancy" => "fancy",
                            _ => "fast"
                        };
                        _optionsService.SetEnum("renderClouds", mapped);
                        _optionsService.Save();
                    }
                };
            }
            if (_playerMainHandComboBox != null)
            {
                _playerMainHandComboBox.SelectionChanged += (s, e) =>
                {
                    if (_playerMainHandComboBox.SelectedItem is ComboBoxItem item && item.Content is string val)
                    {
                        var hand = val.ToLowerInvariant() == "left" ? "left" : "right";
                        _optionsService.SetEnum("mainHand", hand);
                        _optionsService.Save();
                    }
                };
            }
        }

        private IBrush GetBrush(string key, IBrush? fallback = null)
        {
            if (this.TryFindResource(key, out var res))
            {
                if (res is IBrush brush)
                    return brush;
            }
            return fallback ?? new SolidColorBrush(Colors.Transparent);
        }

        private string GetSelectedAddon(string? subVersion)
        {
            if (string.IsNullOrWhiteSpace(subVersion)) return "Fabric";
            var map = _currentSettings.SelectedAddonByVersion;
            if (map != null && map.TryGetValue(subVersion, out var val))
                return string.Equals(val, "Vanilla", StringComparison.OrdinalIgnoreCase) ? "Vanilla" : "Fabric";
            return "Fabric";
        }

        private async Task SaveSelectedAddon(string subVersion, string addon)
        {
            _currentSettings.SelectedAddonByVersion ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _currentSettings.SelectedAddonByVersion[subVersion] = addon;
            _ = _settingsService.SaveSettingsAsync(_currentSettings);
        }

        private void UpdateAddonSelectionUI(string? subVersion)
        {
            if (string.IsNullOrWhiteSpace(subVersion)) return;

            var selectedVersionInfo = _allVersions.FirstOrDefault(v => v.FullVersion == subVersion);
            string activeLoader = selectedVersionInfo?.Loader ?? "Vanilla";

            // Fabric is only available for 1.14 and above
            bool isFabricCompatible = Version.TryParse(subVersion, out Version? semver) &&
                                      semver >= new Version(1, 14);

            if (_addonFabricButton != null)
            {
                _addonFabricButton.IsVisible = isFabricCompatible;
                if (_addonFabricButton.Content is Border fabricBorder)
                {
                    fabricBorder.Background = activeLoader == "Fabric"
                        ? GetBrush("PrimaryAccentBrush")
                        : GetBrush("HoverBackgroundBrush");
                }
            }

            if (_addonVanillaButton != null)
            {
                // Hide the toggle buttons if only Vanilla is available (incompatible with Fabric)
                _addonVanillaButton.IsVisible = isFabricCompatible;
                if (_addonVanillaButton.Content is Border vanillaBorder)
                {
                    vanillaBorder.Background = activeLoader == "Vanilla"
                        ? GetBrush("PrimaryAccentBrush")
                        : GetBrush("HoverBackgroundBrush");
                }
            }

            if (_versionLoader != null)
                _versionLoader.Text = activeLoader;
        }



        private async Task<bool> InstallOptiFineForFabricIfNeededAsync(string mcVersion, string profileName, bool isFabric)
        {
            if (!isFabric)
            {
                Console.WriteLine("[OptiFineForFabric] Not installing: Not a Fabric profile.");
                return false;
            }

            if (!_currentSettings.IsOptiFineEnabled)
            {
                Console.WriteLine("[OptiFineForFabric] Not installing: OptiFine toggle is off. Ensuring mods are disabled.");
                ManageOptiFineForFabricMods(mcVersion, enable: false);
                return true;
            }

            // Get the .mrpack URL
            string? mrpackUrl = await GetOptiFineMrpackUrlForVersion(mcVersion);
            if (string.IsNullOrEmpty(mrpackUrl))
            {
                Console.Error.WriteLine($"[OptiFineForFabric ERROR] No .mrpack available for Minecraft {mcVersion}. Skipping installation.");
                ShowLaunchErrorBanner($"OptiFine for Fabric is not available for Minecraft {mcVersion}.");
                ManageOptiFineForFabricMods(mcVersion, enable: false); // Ensure any old mods are disabled
                return false;
            }

            // Define the path for the downloaded .mrpack file in a temporary location
            string mrpackPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"OptiFineForFabric_{mcVersion}.mrpack");

            // Check if the .mrpack already exists and its hash matches (optional but good for efficiency)
            // For now, we'll always re-download to ensure the latest version based on the URL.
            // You could add logic here to check if the file exists and its hash matches before downloading.

            // Download the .mrpack file
            try
            {
                Console.WriteLine($"[OptiFineForFabric] Downloading .mrpack from {mrpackUrl} to {mrpackPath}");
                ShowProgress(true, $"Downloading OptiFine for Fabric modpack for {mcVersion}...");
                using var client = new HttpClient();
                byte[] mrpackBytes = await client.GetByteArrayAsync(mrpackUrl);
                await File.WriteAllBytesAsync(mrpackPath, mrpackBytes);
                Console.WriteLine($"[OptiFineForFabric] Successfully downloaded .mrpack to {mrpackPath}");
                ShowProgress(false); // Hide download progress
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[OptiFineForFabric ERROR] Failed to download .mrpack from {mrpackUrl}: {ex.Message}");
                ShowLaunchErrorBanner($"Failed to download OptiFine for Fabric pack: {ex.Message}");
                return false;
            }

            // Now, process the downloaded .mrpack
            bool installationSuccess = false;
            try
            {
                // Call the new helper method to handle extraction, overrides, and mod downloads
                installationSuccess = await ProcessModrinthPackInstallation(mrpackPath, System.IO.Path.Combine(_minecraftFolder, "mods"), mcVersion);

                if (installationSuccess)
                {
                    Console.WriteLine("[OptiFineForFabric] Modpack processing completed successfully.");
                    // Ensure mods are enabled after installation
                    ManageOptiFineForFabricMods(mcVersion, enable: true);
                }
                else
                {
                    Console.Error.WriteLine("[OptiFineForFabric ERROR] Modpack processing failed.");
                    ShowLaunchErrorBanner($"Failed to install OptiFine for Fabric modpack for {mcVersion}. Check logs.");
                    ManageOptiFineForFabricMods(mcVersion, enable: false); // Disable any partial installs
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[OptiFineForFabric ERROR] Unexpected error during modpack processing: {ex.Message}");
                ShowLaunchErrorBanner($"An unexpected error occurred during OptiFine for Fabric installation: {ex.Message}");
                ManageOptiFineForFabricMods(mcVersion, enable: false); // Disable any partial installs
            }
            finally
            {
                // Ensure the temporary .mrpack file is deleted, regardless of success or failure
                try
                {
                    if (File.Exists(mrpackPath))
                    {
                        File.Delete(mrpackPath);
                        Console.WriteLine($"[OptiFineForFabric] Deleted temporary .mrpack file: {mrpackPath}");
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[OptiFineForFabric ERROR] Failed to delete temporary .mrpack file {mrpackPath}: {ex.Message}");
                }
            }

            return installationSuccess;
        }

        private async void OnAddonFabricClick(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentSettings.SelectedSubVersion)) return;

            // Find the version info object and update its loader in memory
            var versionInfo = _allVersions.FirstOrDefault(v => v.FullVersion == _currentSettings.SelectedSubVersion);
            if (versionInfo != null)
            {
                versionInfo.Loader = "Fabric";
            }

            // Save the selection and update the UI
            await SaveSelectedAddon(_currentSettings.SelectedSubVersion, "Fabric");
            UpdateAddonSelectionUI(_currentSettings.SelectedSubVersion);
            UpdateLaunchVersionText(); // Update the main launch button text
        }


        private async void OnAddonVanillaClick(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentSettings.SelectedSubVersion)) return;

            // Find the version info object and update its loader in memory
            var versionInfo = _allVersions.FirstOrDefault(v => v.FullVersion == _currentSettings.SelectedSubVersion);
            if (versionInfo != null)
            {
                versionInfo.Loader = "Vanilla";
            }

            await SaveSelectedAddon(_currentSettings.SelectedSubVersion, "Vanilla");
            UpdateAddonSelectionUI(_currentSettings.SelectedSubVersion);
            UpdateLaunchVersionText(); // Update the main launch button text

            // Disable OptiFineForFabric mods when switching to Vanilla
            if (IsOptiFineForFabricSupported(_currentSettings.SelectedSubVersion))
            {
                ManageOptiFineForFabricMods(_currentSettings.SelectedSubVersion, false);
            }
        }


        // Navigate to Settings from Versions sidebar button
        private void OpenSettingsFromVersions(object? sender, RoutedEventArgs e)
        {
            _currentSelectedIndex = 4;
            SwitchToPage(4);
        }

        // Navigate to Settings from top-right menu
        private void OpenSettingsFromMenu(object? sender, RoutedEventArgs e)
        {
            _currentSelectedIndex = 4;
            SwitchToPage(4);
        }


        private void InitializeSettingsControls()
        {
            _launchOnStartupToggle = this.FindControl<ToggleSwitch>("LaunchOnStartupToggle");
            _minimizeToTrayToggle = this.FindControl<ToggleSwitch>("MinimizeToTrayToggle");
            _discordRichPresenceToggle = this.FindControl<ToggleSwitch>("DiscordRichPresenceToggle");
            _minRamAllocationTextBox = this.FindControl<TextBox>("MinRamAllocationTextBox");
            _maxRamAllocationComboBox = this.FindControl<ComboBox>("MaxRamAllocationComboBox");
            _quickJoinServerAddressTextBox = this.FindControl<TextBox>("QuickJoinServerAddressTextBox");
            _quickJoinServerPortTextBox = this.FindControl<TextBox>("QuickJoinServerPortTextBox");
            _quickLaunchEnabledToggle = this.FindControl<ToggleSwitch>("QuickLaunchEnabledToggle");
            _mouseSensitivitySlider = this.FindControl<Slider>("MouseSensitivitySlider");
            if (_mouseSensitivitySlider != null) _mouseSensitivitySlider.ValueChanged += (s, e) => { if (_mouseSensitivityValueText != null) _mouseSensitivityValueText.Text = e.NewValue.ToString("F1"); };
            _mouseSensitivityValueText = this.FindControl<TextBlock>("MouseSensitivityValueText");
            _scrollSensitivitySlider = this.FindControl<Slider>("ScrollSensitivitySlider");
            if (_scrollSensitivitySlider != null) _scrollSensitivitySlider.ValueChanged += (s, e) => { if (_scrollSensitivityValueText != null) _scrollSensitivityValueText.Text = e.NewValue.ToString("F1"); };
            _scrollSensitivityValueText = this.FindControl<TextBlock>("ScrollSensitivityValueText");
            _autoJumpToggle = this.FindControl<ToggleSwitch>("AutoJumpToggle");
            _touchscreenToggle = this.FindControl<ToggleSwitch>("TouchscreenToggle");
            _toggleSprintToggle = this.FindControl<ToggleSwitch>("ToggleSprintToggle");
            _toggleCrouchToggle = this.FindControl<ToggleSwitch>("ToggleCrouchToggle");
            _subtitlesToggle = this.FindControl<ToggleSwitch>("SubtitlesToggle");
            _renderDistanceSlider = this.FindControl<Slider>("RenderDistanceSlider");
            if (_renderDistanceSlider != null) _renderDistanceSlider.ValueChanged += (s, e) => { if (_renderDistanceValueText != null) _renderDistanceValueText.Text = $"{e.NewValue:F0} chunks"; };
            _renderDistanceValueText = this.FindControl<TextBlock>("RenderDistanceValueText");
            _simulationDistanceSlider = this.FindControl<Slider>("SimulationDistanceSlider");
            if (_simulationDistanceSlider != null) _simulationDistanceSlider.ValueChanged += (s, e) => { if (_simulationDistanceValueText != null) _simulationDistanceValueText.Text = $"{e.NewValue:F0} chunks"; };
            _simulationDistanceValueText = this.FindControl<TextBlock>("SimulationDistanceValueText");
            _entityDistanceSlider = this.FindControl<Slider>("EntityDistanceSlider");
            if (_entityDistanceSlider != null) _entityDistanceSlider.ValueChanged += (s, e) => { if (_entityDistanceValueText != null) _entityDistanceValueText.Text = $"{e.NewValue * 100:F0}%"; };
            _entityDistanceValueText = this.FindControl<TextBlock>("EntityDistanceValueText");
            _maxFpsSlider = this.FindControl<Slider>("MaxFpsSlider");
            if (_maxFpsSlider != null)
            {
                // Set min/max values for the slider. Max should be the highest finite FPS you want to show.
                _maxFpsSlider.Minimum = 0;
                _maxFpsSlider.Maximum = 300; // Or a higher value like 500, depends on your UI design.

                _maxFpsSlider.ValueChanged += (s, e) =>
                {
                    if (_maxFpsValueText != null)
                    {
                        if (e.NewValue == 0)
                        {
                            _maxFpsValueText.Text = "Unlimited";
                        }
                        else
                        {
                            _maxFpsValueText.Text = $"{e.NewValue:F0} FPS";
                        }
                    }
                };
            }
            _maxFpsValueText = this.FindControl<TextBlock>("MaxFpsValueText");
            _vSyncToggle = this.FindControl<ToggleSwitch>("VSyncToggle");
            _fullscreenToggle = this.FindControl<ToggleSwitch>("FullscreenToggle");
            _entityShadowsToggle = this.FindControl<ToggleSwitch>("EntityShadowsToggle");
            _highContrastToggle = this.FindControl<ToggleSwitch>("HighContrastToggle");
            _renderCloudsComboBox = this.FindControl<ComboBox>("RenderCloudsComboBox");
            _playerHatToggle = this.FindControl<ToggleSwitch>("PlayerHatToggle");
            _playerCapeToggle = this.FindControl<ToggleSwitch>("PlayerCapeToggle");
            _playerJacketToggle = this.FindControl<ToggleSwitch>("PlayerJacketToggle");
            _playerLeftSleeveToggle = this.FindControl<ToggleSwitch>("PlayerLeftSleeveToggle");
            _playerRightSleeveToggle = this.FindControl<ToggleSwitch>("PlayerRightSleeveToggle");
            _playerLeftPantToggle = this.FindControl<ToggleSwitch>("PlayerLeftPantToggle");
            _playerRightPantToggle = this.FindControl<ToggleSwitch>("PlayerRightPantToggle");
            _playerMainHandComboBox = this.FindControl<ComboBox>("PlayerMainHandComboBox");
            _themeComboBox = this.FindControl<ComboBox>("ThemeComboBox");
            if (_themeComboBox != null)
            {
                _themeComboBox.SelectionChanged += OnThemeChanged;
            }
            _animationsEnabledToggle = this.FindControl<ToggleSwitch>("AnimationsEnabledToggle");
            if (_discordRichPresenceToggle != null)
            {
                _discordRichPresenceToggle.Checked += (_, __) =>
                {
                    _currentSettings.DiscordRichPresence = true;
                    StartRichPresenceIfEnabled();
                };
                _discordRichPresenceToggle.Unchecked += (_, __) =>
                {
                    _currentSettings.DiscordRichPresence = false;
                    StopRichPresence();
                };
            }
        }

        // MainWindow.cs — make UpdateSkinPreviewsAsync robust to missing username (prefer uuid)
        private async Task UpdateSkinPreviewsAsync(string? username, string? uuid)
        {
            if (string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(uuid))
                return;

            try
            {
                var smallPose = _skinRenderService.GetRandomSmallPoseName();
                var largePose = _skinRenderService.GetRandomLargePoseName();
                _lastSmallPose = smallPose;

                // Use uuid if available; SkinRenderService will still prefer uuid in the URL
                var id = username ?? uuid ?? "Player";

                var smallBitmap = await _skinRenderService.LoadSkinImageAsync(id, smallPose, "bust", uuid);
                var largeBitmap = await _skinRenderService.LoadSkinImageAsync(id, largePose, "full", uuid);

                if (_playingAsImage != null && smallBitmap != null)
                    _playingAsImage.Source = smallBitmap;

                if (_accountCharacterImage != null && largeBitmap != null)
                    _accountCharacterImage.Source = largeBitmap;

                UpdateRichPresenceFromState();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating skin previews: {ex.Message}");
            }
        }




        // MainWindow.cs — make UpdateSkinPreviewsAsync calls robust
        private async Task LoadUserInfoAsync()
        {
            try
            {
                // Ensure session is current before reading username/uuid
                await LoadSessionAsync();

                var settings = await _settingsService.LoadSettingsAsync();

                // Prefer session; fall back to saved settings; never fall back to "Player" unless truly offline
                var username = _session?.Username ?? settings.SessionUsername;
                var uuid = _session?.UUID ?? settings.SessionUuid;

                _currentUsername = username;
                _loggedIn = !string.IsNullOrWhiteSpace(uuid);

                if (_accountUsernameDisplay != null)
                    _accountUsernameDisplay.Text = (username ?? "Player").ToUpper();

                if (_accountUuidDisplay != null)
                    _accountUuidDisplay.Text = _loggedIn ? uuid : "N/A (Offline)";

                if (_playingAsUsername != null)
                    _playingAsUsername.Text = (username ?? "Player").ToUpper();

                // IMPORTANT: pass uuid if available so SkinRenderService uses it
                await UpdateSkinPreviewsAsync(username ?? "Player", uuid);

                UpdateRichPresenceFromState();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading user info: {ex.Message}");
            }
        }


        private async void LogoutButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("[MainWindow] Starting proper logout process...");

                _isLoggingOut = true;

                // PROPERLY clear session data
                _currentSettings.IsLoggedIn = false;
                _currentSettings.AccountType = null;
                _currentSettings.SessionUsername = null;
                _currentSettings.SessionUuid = null;
                _currentSettings.SessionAccessToken = null;
                _currentSettings.SessionXuid = null;
                _currentSettings.OfflineUsername = null;

                // Save settings FIRST
                await _settingsService.SaveSettingsAsync(_currentSettings);
                Console.WriteLine("[MainWindow] Settings saved with logged out state");

                // Reset state
                _loggedIn = false;
                _currentUsername = null;
                _lastSmallPose = null;
                _session = null;

                // Stop services
                StopRichPresence();
                await _sessionService.LogoutAsync();

                // Get the desktop lifetime
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    Console.WriteLine("[MainWindow] Hiding MainWindow and creating LoginWindow...");

                    // HIDE the MainWindow immediately
                    this.Hide();

                    // Create login window
                    var loginWindow = new LoginWindow();

                    // Set up login completion handler
                    loginWindow.LoginCompleted += (success) =>
                    {
                        Console.WriteLine($"[MainWindow] Login completed with success: {success}");
                        if (success)
                        {
                            // Login successful - create new main window
                            Dispatcher.UIThread.Post(() =>
                            {
                                var newMainWindow = new MainWindow();
                                newMainWindow.Show();
                                desktop.MainWindow = newMainWindow;
                                loginWindow.Close();

                                // Close the old (hidden) main window
                                this.Close();
                            });
                        }
                    };

                    // Handle login window closing without successful login
                    loginWindow.Closed += (_, __) =>
                    {
                        Console.WriteLine($"[MainWindow] LoginWindow closed - LoginSuccessful: {loginWindow.LoginSuccessful}");

                        // If login was not successful, shut down the app (close the hidden MainWindow too)
                        if (!loginWindow.LoginSuccessful)
                        {
                            Console.WriteLine("[MainWindow] No successful login - shutting down application");
                            // Close the hidden MainWindow first
                            Dispatcher.UIThread.Post(() =>
                            {
                                this.Close(); // This will trigger shutdown since it's the last window
                            });
                        }
                    };

                    // Set as main window and show
                    desktop.MainWindow = loginWindow;
                    loginWindow.Show();
                    loginWindow.Activate();

                    Console.WriteLine("[MainWindow] LoginWindow shown, MainWindow hidden");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainWindow] Error during logout: {ex.Message}");
                Console.WriteLine($"[MainWindow] Stack trace: {ex.StackTrace}");
            }
            finally
            {
                _isLoggingOut = false;
            }
        }

        private void OnThemeChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_themeComboBox?.SelectedItem is ComboBoxItem selectedItem && selectedItem.Content is string themeName)
            {
                ThemeService.SetTheme(themeName);
                this.InvalidateVisual();
            }
        }

        private async void AnimateBannersOnLoad()
        {
            // --- MODIFIED: Check EnableNewContentIndicators ---
            if (!AreAnimationsEnabled() || !_currentSettings.EnableNewContentIndicators)
            {
                // Use class fields directly
                if (_promoBanner != null) { _promoBanner.Opacity = 1; if (_promoBanner.RenderTransform is TranslateTransform ttPromo) ttPromo.Y = 0; _promoBanner.IsVisible = true; }
                if (_launchSection != null) { _launchSection.Opacity = 1; if (_launchSection.RenderTransform is TranslateTransform ttLaunch) ttLaunch.Y = 0; _launchSection.IsVisible = true; }
                if (_newsSectionGrid != null) { _newsSectionGrid.Opacity = 1; if (_newsSectionGrid.RenderTransform is TranslateTransform ttNews) ttNews.Y = 0; _newsSectionGrid.IsVisible = true; }
                return;
            }

            await Task.Delay(100);
            // Use class fields directly
            if (_promoBanner != null) { _promoBanner.Opacity = 1; if (_promoBanner.RenderTransform is TranslateTransform ttPromo) ttPromo.Y = 0; }
            await Task.Delay(150);
            if (_launchSection != null) { _launchSection.Opacity = 1; if (_launchSection.RenderTransform is TranslateTransform ttLaunch) ttLaunch.Y = 0; }
            await Task.Delay(150);
            if (_newsSectionGrid != null) { _newsSectionGrid.Opacity = 1; if (_newsSectionGrid.RenderTransform is TranslateTransform ttNews) ttNews.Y = 0; }
        }



        private void SetupParticles()
        {
            if (_launchSection == null || _particleLayer == null) return;
            if (_launchSection.Bounds.Width <= 0 || _launchSection.Bounds.Height <= 0)
            {
                _launchSection.LayoutUpdated += LaunchSection_LayoutUpdated;
                return;
            }
            CreateParticles();
        }

        private void LaunchSection_LayoutUpdated(object? sender, EventArgs e)
        {
            if (_launchSection == null) return;
            if (_launchSection.Bounds.Width > 0 && _launchSection.Bounds.Height > 0)
            {
                _launchSection.LayoutUpdated -= LaunchSection_LayoutUpdated;
                CreateParticles();
            }
        }

        private void CreateParticles()
        {
            if (_particleLayer == null || _launchSection == null) return;

            // If animations are disabled, stop and clear particles
            if (!AreAnimationsEnabled())
            {
                _particleCts?.Cancel();
                _particles.Clear();
                _particleLayer?.Children.Clear(); // Use null-conditional operator here
                return;
            }
            _particleCts?.Cancel();
            _particles.Clear();
            _particleLayer.Children.Clear();
            double w = _launchSection.Bounds.Width;
            double h = _launchSection.Bounds.Height;
            for (int i = 0; i < 40; i++)
            {
                var size = _rand.Next(2, 5);
                var star = new Ellipse { Width = size, Height = size, Fill = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)), Opacity = 0.35 + _rand.NextDouble() * 0.45 };
                double x = _rand.NextDouble() * w;
                double y = _rand.NextDouble() * h;
                Canvas.SetLeft(star, x);
                Canvas.SetTop(star, y);
                _particleLayer.Children.Add(star);
                _particles.Add(new Particle { Shape = star, X = x, Y = y, Speed = 0.25 + _rand.NextDouble() * 0.6, Drift = (_rand.NextDouble() - 0.5) * 0.25 });
            }
            _particleCts = new CancellationTokenSource();
            var ct = _particleCts.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    while (!ct.IsCancellationRequested)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (_particleLayer == null || _particleLayer.GetVisualRoot() == null) return;
                            for (int i = 0; i < _particles.Count; i++)
                            {
                                var p = _particles[i];
                                p.Y -= p.Speed;
                                p.X += p.Drift;
                                if (p.Y < -6 || p.X < -10 || p.X > w + 10)
                                {
                                    p.Y = h + _rand.NextDouble() * 40;
                                    p.X = _rand.NextDouble() * w;
                                }
                                Canvas.SetLeft(p.Shape, p.X);
                                Canvas.SetTop(p.Shape, p.Y);
                                _particles[i] = p;
                            }
                        });
                        await Task.Delay(33, ct);
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception) { }
            }, ct);
        }

        private struct Particle
        {
            public Ellipse Shape; public double X; public double Y; public double Speed; public double Drift;
        }

        private async void LoadFriendsAsync()
        {
            if (_friendsLoadingPanel != null) _friendsLoadingPanel.IsVisible = true;
            if (_noFriendsMessage != null) _noFriendsMessage.IsVisible = false;
            await Task.Delay(3000);
            if (_friendsLoadingPanel != null) _friendsLoadingPanel.IsVisible = false;
            if (_noFriendsMessage != null) _noFriendsMessage.IsVisible = true;
        }

        private void OnNavButtonClick(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border border && border.Tag is string tagStr && int.TryParse(tagStr, out var index))
            {
                AnimateSelectionIndicator(index);
                _currentSelectedIndex = index;
                SwitchToPage(index);
            }
        }

        private async Task<bool> EnsureMinecraftVersionInstalledAsync(string version)
        {
            try
            {
                ShowProgress(true, $"Verifying Minecraft {version}...");

                // Get the required files list without triggering install
                var files = await _launcher.ExtractFiles(version);
                bool allFilesPresent = files.All(f => System.IO.File.Exists(f.Path));

                if (allFilesPresent)
                {
                    Console.WriteLine($"[Install] Base Minecraft {version} already present. Skipping install.");
                    ShowProgress(false);
                    return true;
                }

                // Not all files present. Run InstallAsync to fetch missing ones.
                await _launcher.InstallAsync(version);
                ShowProgress(false);

                Console.WriteLine($"[Install] Base Minecraft {version} is verified/installed.");
                return true;
            }
            catch (Exception ex)
            {
                ShowProgress(false);
                Console.WriteLine($"[Install] Failed to ensure base version {version} is installed: {ex.Message}");
                ShowLaunchErrorBanner($"Failed to verify/install Minecraft {version}: {ex.Message}");
                return false;
            }
        }


        private async void SwitchToPage(int index)
        {
            // Hide all pages
            if (_gamePage != null) _gamePage.IsVisible = false;
            if (_versionsPage != null) _versionsPage.IsVisible = false;
            if (_serversPage != null) _serversPage.IsVisible = false;
            if (_modsPage != null) _modsPage.IsVisible = false;
            if (_settingsPage != null) _settingsPage.IsVisible = false;
            if (_skinsPage != null) _skinsPage.IsVisible = false;

            // Stop the timer when switching away from the servers page
            _serverStatusRefreshTimer?.Stop();

            // Show selected page
            switch (index)
            {
                case 0:
                    if (_gamePage != null) _gamePage.IsVisible = true;
                    UpdateLaunchVersionText();
                    break;

                case 1:
                    if (_versionsPage != null) _versionsPage.IsVisible = true;
                    break;

                case 2:
                    if (_serversPage != null) _serversPage.IsVisible = true;

                    // Ensure defaults exist here too
                    await InitializeDefaultServersAsync();

                    LoadServers();

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(100);
                            await RefreshAllServerStatusesAsync();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Server Status Initial] Error: {ex}");
                        }
                    });

                    _serverStatusRefreshTimer?.Start();
                    break;

                case 3:
                    if (_modsPage != null) _modsPage.IsVisible = true;
                    LoadUserMods();
                    break;

                case 4:
                    if (_settingsPage != null) _settingsPage.IsVisible = true;
                    if (!_settingsDirty) HideSettingsSaveBanner();
                    await CalculateDiskUsageAsync();
                    break;

                case 5:
                    if (_skinsPage != null) _skinsPage.IsVisible = true;
                    break;
            }

            if (index != 4) HideSettingsSaveBanner();

            _currentSelectedIndex = index;
            UpdateRichPresenceFromState();
        }


        private async Task<string?> EnsureFabricProfileAsync(string version)
        {
            Console.WriteLine($"[DEBUG] EnsureFabricProfileAsync started with version: '{version}'");

            // Ensure base vanilla version present before Fabric install
            var baseOk = await EnsureMinecraftVersionInstalledAsync(version);
            if (!baseOk) return null;

            string versionsPath = System.IO.Path.Combine(_minecraftFolder, "versions");
            string? foundProfile = null;

            // Check if Fabric profile already exists locally
            if (System.IO.Directory.Exists(versionsPath))
            {
                foundProfile = System.IO.Directory.GetDirectories(versionsPath)
                    .Select(dir => System.IO.Path.GetFileName(dir))
                    .FirstOrDefault(dirName =>
                        dirName.StartsWith("fabric-loader-", StringComparison.OrdinalIgnoreCase) &&
                        dirName.EndsWith($"-{version}", StringComparison.OrdinalIgnoreCase) &&
                        System.IO.File.Exists(System.IO.Path.Combine(versionsPath, dirName, $"{dirName}.json")));
            }

            if (foundProfile != null)
            {
                Console.WriteLine($"[DEBUG] Using existing Fabric profile: {foundProfile}");
                _currentSettings.SelectedFabricProfileName = foundProfile;
                await _settingsService.SaveSettingsAsync(_currentSettings);
                return foundProfile;
            }

            // Install Fabric only if missing
            try
            {
                UpdateLaunchButton("INSTALLING FABRIC...", "DeepSkyBlue");
                ShowProgress(true, $"Installing Fabric loader for Minecraft {version}...");

                foundProfile = await RunExclusiveInstall(async () =>
                    await new FabricInstaller(new HttpClient())
                        .Install(version, new MinecraftPath(_minecraftFolder)));

                ShowProgress(false);
                UpdateLaunchButton("LAUNCH GAME", "SeaGreen");

                _currentSettings.SelectedFabricProfileName = foundProfile;
                await _settingsService.SaveSettingsAsync(_currentSettings);
                return foundProfile;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Fabric] Install failed: {ex.Message}");
                ShowProgress(false);
                return null;
            }
        }



        private void OnMajorVersionClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Border border && border.Tag is string majorVersion)
            {
                if (_versionDetailsSidebar != null)
                    _versionDetailsSidebar.IsVisible = true;
                if (_versionTitle != null)
                    _versionTitle.Text = $"Minecraft {majorVersion}";

                var subVersions = _allVersions.Where(v => v.MajorVersion == majorVersion).ToList();

                if (_versionDropdown != null)
                {
                    _versionDropdown.SelectionChanged -= OnSubVersionSelected; // Temporarily unsubscribe
                    _versionDropdown.Items.Clear();

                    foreach (var versionInfo in subVersions.OrderByDescending(v => v.FullVersion))
                    {
                        _versionDropdown.Items.Add(new ComboBoxItem { Content = versionInfo.FullVersion });
                    }

                    if (_versionDropdown.ItemCount > 0)
                    {
                        var savedSub = _currentSettings.SelectedSubVersion;
                        var itemToSelect = _versionDropdown.Items.OfType<ComboBoxItem>()
                            .FirstOrDefault(item => item.Content?.ToString() == savedSub);

                        if (itemToSelect != null)
                        {
                            _versionDropdown.SelectedItem = itemToSelect;
                        }
                        else
                        {
                            _versionDropdown.SelectedIndex = 0;
                        }

                        // Explicitly call OnSubVersionSelected to update UI for the newly selected item,
                        // even if the selection didn't technically "change" (e.g., first item remains selected).
                        OnSubVersionSelected(null, null); // Pass null for sender/e as it's a programmatic call
                    }

                    _versionDropdown.SelectionChanged += OnSubVersionSelected; // Re-subscribe
                }

                _currentSettings.SelectedMajorVersion = majorVersion;
            }
            UpdateRichPresenceFromState();
        }

        private async Task<bool> InstallFabricApiIfNeededAsync(string mcVersion, string versionFolderName)
        {
            string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
            System.IO.Directory.CreateDirectory(modsFolder);

            string fabricApiPath = System.IO.Path.Combine(modsFolder, $"fabric-api-{mcVersion}.jar");

            // Even if it exists, we ensure it's registered in settings
            bool fileExists = System.IO.File.Exists(fabricApiPath);

            try
            {
                // If file exists, we might still want to register it if it's missing from settings
                // But to be safe and simple, we'll just run the install check logic.

                if (fileExists && _currentSettings.InstalledMods.Any(m => m.ModId == "fabric-api" && m.MinecraftVersion == mcVersion))
                {
                    Console.WriteLine($"[Fabric API] Fabric API for {mcVersion} already exists and is tracked.");
                    return true;
                }

                ShowProgress(true, $"Installing Fabric API for {mcVersion}...");

                using var client = new HttpClient();
                var apiUrl = $"https://api.modrinth.com/v2/project/P7dR8mSH/version?game_versions=[\"{mcVersion}\"]&loaders=[\"fabric\"]";
                var response = await client.GetStringAsync(apiUrl);

                if (string.IsNullOrWhiteSpace(response)) throw new Exception("Empty response from Modrinth API");

                var versions = JsonSerializer.Deserialize<List<ModrinthVersion>>(response, Json.Options);
                var latest = versions?.FirstOrDefault();

                if (latest?.files == null || latest.files.Count == 0) throw new Exception("No Fabric API file found");

                var file = latest.files.FirstOrDefault(f => f.filename?.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) == true) ?? latest.files.First();
                var downloadUrl = file.url;

                if (!fileExists)
                {
                    var jarBytes = await client.GetByteArrayAsync(downloadUrl);
                    await System.IO.File.WriteAllBytesAsync(fabricApiPath, jarBytes);
                    Console.WriteLine($"[Fabric API] Installed file for {mcVersion}");
                }

                // TRACKING FIX: Register the mod so the cleaner knows about it later
                string fileName = $"fabric-api-{mcVersion}.jar";
                await RegisterAutoInstalledMod("fabric-api", "Fabric API", latest.versionNumber, mcVersion, fileName, downloadUrl);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Fabric API] Install failed: {ex.Message}");
                return false;
            }
            finally
            {
                ShowProgress(false);
            }
        }


        private async void OnSubVersionSelected(object? sender, SelectionChangedEventArgs? e)
        {
            if (_versionDropdown?.SelectedItem is ComboBoxItem selectedItem && selectedItem.Content is string selectedSubVersion)
            {
                _currentSettings.SelectedSubVersion = selectedSubVersion;

                var selectedVersionInfo = _allVersions.FirstOrDefault(v => v.FullVersion == selectedSubVersion);
                if (selectedVersionInfo != null)
                {
                    _currentSettings.SelectedMajorVersion = selectedVersionInfo.MajorVersion;

                    // Check compatibility: Fabric requires 1.14+
                    bool isFabricCompatible = Version.TryParse(selectedSubVersion, out Version? semver) &&
                                              semver >= new Version(1, 14);

                    string savedLoader = GetSelectedAddon(selectedSubVersion);

                    // FORCE Vanilla if Fabric is not supported for this version
                    if (!isFabricCompatible)
                    {
                        savedLoader = "Vanilla";

                        // Ensure the dictionary exists and update the preference to Vanilla
                        if (_currentSettings.SelectedAddonByVersion == null)
                            _currentSettings.SelectedAddonByVersion = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                        _currentSettings.SelectedAddonByVersion[selectedSubVersion] = "Vanilla";
                    }

                    selectedVersionInfo.Loader = savedLoader;
                }

                await _settingsService.SaveSettingsAsync(_currentSettings);

                UpdateSidebarDetails(selectedSubVersion);
                UpdateAddonSelectionUI(selectedSubVersion);
                UpdateLaunchVersionText();
            }
            else if (_versionDropdown?.ItemCount > 0 && _versionDropdown.SelectedIndex == -1)
            {
                _versionDropdown.SelectedIndex = 0;
            }

            UpdateRichPresenceFromState();
        }

        private async Task<string?> FindClosestOptiFineForFabricVersionAsync(string targetVersion)
        {
            try
            {
                using var client = new HttpClient();
                string apiUrl = "https://api.modrinth.com/v2/project/BHtwz1lb/version";
                string response = await client.GetStringAsync(apiUrl);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var allVersions = JsonSerializer.Deserialize<List<ModrinthVersion>>(response, Json.Options);

                if (allVersions == null) return null;

                // Parse target version (e.g., "1.21.3" → [1, 21, 3])
                var targetParts = targetVersion.Split('.').Select(int.Parse).ToArray();
                Version targetSemver = targetParts.Length switch
                {
                    >= 4 => new Version(targetParts[0], targetParts[1], targetParts[2], targetParts[3]),
                    3 => new Version(targetParts[0], targetParts[1], targetParts[2]),
                    _ => new Version(targetParts[0], targetParts[1])
                };

                string? bestMatch = null;
                Version bestSemver = new Version(0, 0);

                foreach (var ver in allVersions)
                {
                    if (ver.loaders?.Contains("fabric", StringComparer.OrdinalIgnoreCase) != true)
                        continue;

                    foreach (string gameVer in ver.GameVersions ?? Enumerable.Empty<string>())
                    {
                        if (Version.TryParse(gameVer, out Version candidate) && candidate <= targetSemver && candidate > bestSemver)
                        {
                            bestSemver = candidate;
                            bestMatch = gameVer;
                        }
                    }
                }

                return bestMatch;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Fallback] Error finding closest OptiFineForFabric version: {ex.Message}");
                return null;
            }
        }

        private void UpdateLaunchVersionText()
        {
            if (_launchVersionText != null)
            {
                var selectedVersionInfo = _allVersions.FirstOrDefault(v => v.FullVersion == _currentSettings.SelectedSubVersion);
                if (selectedVersionInfo != null)
                {
                    // Use the Loader property directly from the VersionInfo object
                    // which is updated in OnSubVersionSelected
                    _launchVersionText.Text = $"Leaf Client {selectedVersionInfo.FullVersion} with {selectedVersionInfo.Loader}";
                }
                else
                {
                    _launchVersionText.Text = "Leaf Client (Version Not Selected)";
                }
                if (!_isLaunching)
                    UpdateLaunchButton("LAUNCH GAME", "SeaGreen");
            }
            UpdateRichPresenceFromState();
        }



        private void PopulateAllVersionsData()
        {
            _allVersions.Add(new VersionInfo("1.21.4", "1.21", "Latest Release", "Fabric", "December 3, 2024", "The Tricky Trials Update continues with further refinements and bug fixes."));
            _allVersions.Add(new VersionInfo("1.21.3", "1.21", "Release", "Fabric", "October 23, 2024", "Bug fixes and performance improvements for the Tricky Trials Update."));
            _allVersions.Add(new VersionInfo("1.21.2", "1.21", "Release", "Fabric", "October 22, 2024", "Additional bug fixes and optimizations for the Tricky Trials Update."));
            _allVersions.Add(new VersionInfo("1.21.1", "1.21", "Release", "Fabric", "June 13, 2024", "First major update for the Tricky Trials, addressing critical issues."));
            _allVersions.Add(new VersionInfo("1.21", "1.21", "Release", "Fabric", "June 13, 2024", "The Tricky Trials Update - Initial release, introducing Trial Chambers, Breeze, Bogged, Mace, and Wind Charge."));
            _allVersions.Add(new VersionInfo("1.20.6", "1.20", "Stable Release", "Fabric", "April 29, 2024", "Armored Paws update, featuring new wolf variants and the Armadillo mob."));
            _allVersions.Add(new VersionInfo("1.20.5", "1.20", "Release", "Fabric", "April 23, 2024", "Bug fixes and improvements for the Trails & Tales Update."));
            _allVersions.Add(new VersionInfo("1.20.4", "1.20", "Release", "Fabric", "December 7, 2023", "Performance improvements and bug fixes for the Trails & Tales Update."));
            _allVersions.Add(new VersionInfo("1.20.3", "1.20", "Release", "Fabric", "December 5, 2023", "Additional updates and fixes for the Trails & Tales experience."));
            _allVersions.Add(new VersionInfo("1.20.2", "1.20", "Release", "Fabric", "September 21, 2023", "Bug fixes and optimizations for the Trails & Tales Update."));
            _allVersions.Add(new VersionInfo("1.20.1", "1.20", "Popular", "Fabric", "June 12, 2023", "Most popular 1.20 version with extensive mod support, stable for modding."));
            _allVersions.Add(new VersionInfo("1.20", "1.20", "Release", "Fabric", "June 7, 2023", "Trails & Tales Update: archaeology, armor trims, cherry blossom biome, camels, and sniffers."));
            _allVersions.Add(new VersionInfo("1.19.4", "1.19", "Legacy", "Fabric", "March 14, 2023", "Final 1.19 update, primarily bug fixes and minor technical changes."));
            _allVersions.Add(new VersionInfo("1.19.3", "1.19", "Legacy", "Fabric", "December 7, 2022", "Creative inventory improvements and minor fixes."));
            _allVersions.Add(new VersionInfo("1.19.2", "1.19", "Popular", "Fabric", "August 5, 2022", "Most stable 1.19 version with extensive mod support."));
            _allVersions.Add(new VersionInfo("1.19.1", "1.19", "Legacy", "Fabric", "July 27, 2022", "Bug fixes and chat reporting system introduction."));
            _allVersions.Add(new VersionInfo("1.19", "1.19", "Release", "Fabric", "June 7, 2022", "The Wild Update: Deep Dark biome, Ancient Cities, Warden, Mangrove Swamp, Frogs, and Allays."));
            _allVersions.Add(new VersionInfo("1.18.2", "1.18", "Legacy", "Fabric", "February 28, 2022", "Final 1.18 version with improved stability and bug fixes."));
            _allVersions.Add(new VersionInfo("1.18.1", "1.18", "Legacy", "Fabric", "December 10, 2021", "Bug fixes and performance improvements for Caves & Cliffs Part II."));
            _allVersions.Add(new VersionInfo("1.18", "1.18", "Release", "Fabric", "November 30, 2021", "Caves & Cliffs Part II: new world generation, increased height limit, new cave types."));
            _allVersions.Add(new VersionInfo("1.17.1", "1.17", "Legacy", "Fabric", "July 6, 2021", "Final 1.17 update with bug fixes and minor changes."));
            _allVersions.Add(new VersionInfo("1.17", "1.17", "Release", "Fabric", "June 8, 2021", "Caves & Cliffs Part I: copper, amethyst, axolotls, goats, glow squids, and new blocks."));
            _allVersions.Add(new VersionInfo("1.16.5", "1.16", "Popular", "Fabric", "January 15, 2021", "Most popular 1.16 version with extensive mod support and stability."));
            _allVersions.Add(new VersionInfo("1.16.4", "1.16", "Legacy", "Fabric", "November 2, 2020", "Bug fixes and minor improvements."));
            _allVersions.Add(new VersionInfo("1.16.3", "1.16", "Legacy", "Fabric", "September 10, 2020", "Performance improvements and bug fixes."));
            _allVersions.Add(new VersionInfo("1.16.2", "1.16", "Legacy", "Fabric", "August 11, 2020", "Bug fixes and minor content additions."));
            _allVersions.Add(new VersionInfo("1.16.1", "1.16", "Legacy", "Fabric", "June 24, 2020", "First major update for the Nether Update."));
            _allVersions.Add(new VersionInfo("1.16", "1.16", "Release", "Fabric", "June 23, 2020", "Nether Update: new biomes, piglins, bastions, netherite, and more."));
            _allVersions.Add(new VersionInfo("1.12.2", "1.12", "Classic", "Vanilla", "September 18, 2017", "Most popular modded version with thousands of mods available."));
            _allVersions.Add(new VersionInfo("1.12.1", "1.12", "Legacy", "Vanilla", "June 2, 2017", "Bug fixes for the World of Color Update."));
            _allVersions.Add(new VersionInfo("1.12", "1.12", "Release", "Vanilla", "June 7, 2017", "World of Color Update: terracotta, glazed terracotta, concrete, and parrots."));
            _allVersions.Add(new VersionInfo("1.8.9", "1.8", "Classic", "Vanilla", "December 9, 2015", "Most popular PvP version, widely used for Hypixel and competitive play."));
            _allVersions.Add(new VersionInfo("1.8.8", "1.8", "Legacy", "Vanilla", "September 28, 2015", "Bug fixes for 1.8."));
            _allVersions.Add(new VersionInfo("1.8.7", "1.8", "Legacy", "Vanilla", "July 1, 2015", "Bug fixes for 1.8."));
            _allVersions.Add(new VersionInfo("1.8.6", "1.8", "Legacy", "Vanilla", "June 25, 2015", "Bug fixes for 1.8.")); // ADDED
            _allVersions.Add(new VersionInfo("1.8.5", "1.8", "Legacy", "Vanilla", "June 23, 2015", "Bug fixes for 1.8.")); // ADDED
            _allVersions.Add(new VersionInfo("1.8.4", "1.8", "Legacy", "Vanilla", "May 21, 2015", "Bug fixes for 1.8.")); // ADDED
            _allVersions.Add(new VersionInfo("1.8.3", "1.8", "Legacy", "Vanilla", "February 20, 2015", "Bug fixes for 1.8.")); // ADDED
            _allVersions.Add(new VersionInfo("1.8.2", "1.8", "Legacy", "Vanilla", "February 19, 2015", "Bug fixes for 1.8.")); // ADDED
            _allVersions.Add(new VersionInfo("1.8.1", "1.8", "Legacy", "Vanilla", "November 24, 2014", "Bug fixes for 1.8.")); // ADDED
            _allVersions.Add(new VersionInfo("1.8", "1.8", "Release", "Vanilla", "September 2, 2014", "The Bountiful Update."));
            _allVersions.Add(new VersionInfo("1.7.10", "1.7", "Classic", "Vanilla", "June 26, 2014", "Classic modded version, widely used for legacy modpacks."));
            _allVersions.Add(new VersionInfo("1.7.9", "1.7", "Legacy", "Vanilla", "April 14, 2014", "Bug fixes for 1.7."));
            _allVersions.Add(new VersionInfo("1.7.5", "1.7", "Legacy", "Vanilla", "February 26, 2014", "Performance improvements."));
            _allVersions.Add(new VersionInfo("1.7.4", "1.7", "Legacy", "Vanilla", "December 10, 2013", "Bug fixes."));
            _allVersions.Add(new VersionInfo("1.7.2", "1.7", "Release", "Vanilla", "October 25, 2013", "The Update that Changed the World: new biomes and fishing mechanics."));
        }



        private void AnimateSelectionIndicator(int index)
        {
            if (_selectionIndicator == null || _settingsIndicator == null) return;

            bool animationsEnabled = AreAnimationsEnabled();

            // First, set the transitions based on the animation setting
            if (animationsEnabled && _savedSelectionIndicatorTransitions != null)
            {
                var newTransitions = new Transitions();
                foreach (var t in _savedSelectionIndicatorTransitions)
                {
                    newTransitions.Add(t);
                }
                _selectionIndicator.Transitions = newTransitions;
            }
            else
            {
                _selectionIndicator.Transitions = new Transitions(); // No transitions
            }

            if (animationsEnabled && _savedSettingsIndicatorTransitions != null)
            {
                var newTransitions = new Transitions();
                foreach (var t in _savedSettingsIndicatorTransitions)
                {
                    newTransitions.Add(t);
                }
                _settingsIndicator.Transitions = newTransitions;
            }
            else
            {
                _settingsIndicator.Transitions = new Transitions(); // No transitions
            }


            // Now, apply the visual state (margin, opacity, visibility)
            // This logic is the same regardless of animationsEnabled,
            // as the 'Transitions' property will handle if it animates or snaps.
            if (index == 4) // Settings button is selected
            {
                _selectionIndicator.Opacity = 0;
                _settingsIndicator.IsVisible = true;
                _settingsIndicator.Opacity = 0.3;
            }
            else // Any other sidebar button is selected
            {
                _selectionIndicator.Opacity = 0.3;
                _settingsIndicator.Opacity = 0;
                _settingsIndicator.IsVisible = false;
                _selectionIndicator.Margin = new Thickness(10, index * 60, 10, 0);
            }
        }

        private void MinimizeWindow(object? s, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
            // Minimize button ONLY minimizes, never goes to tray
        }

        private void MaximizeWindow(object? s, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void CloseWindow(object? s, RoutedEventArgs e)
        {
            // X button behavior depends on MinimizeToTray setting
            if (_currentSettings.MinimizeToTray)
            {
                MinimizeToTray();
            }
            else
            {
                _isExitingApp = true;
                Close();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Only kill Minecraft process and dispose tray icon
            // Do NOT dispose the log writer here - it's handled in OnWindowClosing for true shutdown
            KillMinecraftProcess();
            if (_trayIcon != null && !_isLoggingOut)
            {
                _trayIcon.IsVisible = false;
                _trayIcon.Dispose();
            }
            base.OnClosed(e);
        }

        private void CloseBanner(object? s, RoutedEventArgs e)
        {
            var banner = this.FindControl<Border>("PromoBanner");
            if (banner != null) banner.IsVisible = false;
        }

        private async void OnLaunchSectionPointerEntered(object? s, PointerEventArgs e) => await AnimateScaleTransform(_launchBgScale, 1.06, 220, (cts) => _launchBgCts = cts, () => _launchBgCts);
        private async void OnLaunchSectionPointerExited(object? s, PointerEventArgs e) => await AnimateScaleTransform(_launchBgScale, 1.00, 220, (cts) => _launchBgCts = cts, () => _launchBgCts);

        private void OnAnimationsEnabledChanged()
        {
            bool animationsEnabled = AreAnimationsEnabled();
            Transitions emptyTransitions = new Transitions();

            // Helper to apply/clear transitions
            void SetTransitions(Control? control, IList<ITransition>? savedTransitions)
            {
                if (control == null) return;

                if (animationsEnabled && savedTransitions != null)
                {
                    var newTransitions = new Transitions();
                    foreach (var t in savedTransitions)
                    {
                        newTransitions.Add(t);
                    }
                    control.Transitions = newTransitions;
                }
                else
                {
                    control.Transitions = emptyTransitions;
                }
            }

            // Helper to set transform and opacity directly if animations are off
            void SetToFinalState(Control? control, double finalY = 0, double finalX = 0, double finalOpacity = 1.0)
            {
                if (control == null || animationsEnabled) return;

                if (control.RenderTransform is TranslateTransform tt)
                {
                    tt.Y = finalY;
                    tt.X = finalX;
                }
                else if (control.RenderTransform is ScaleTransform st)
                {
                    st.ScaleX = st.ScaleY = 1.0; // Assuming 1.0 is the "final" non-hovered state for scale
                }
                control.Opacity = finalOpacity;
                control.IsVisible = (finalOpacity > 0); // Set visibility based on opacity
            }

            // Selection Indicator - now handled by AnimateSelectionIndicator
            // Just ensure its transitions are correctly set
            SetTransitions(_selectionIndicator, _savedSelectionIndicatorTransitions);
            SetTransitions(_settingsIndicator, _savedSettingsIndicatorTransitions);
            // After setting transitions, re-apply the current selection state
            AnimateSelectionIndicator(_currentSelectedIndex);

            SetTransitions(_gameStartingBanner, _gameStartingBanner?.Transitions?.ToList());
            SetToFinalState(_gameStartingBanner, finalY: -100, finalOpacity: 0);

            // Promo Banner (initially hidden, Y=80, Opacity=0; final visible state Y=0, Opacity=1)
            SetTransitions(_promoBanner, _savedPromoBannerTransitions);
            // --- MODIFIED: Only show if enabled in settings ---
            if (animationsEnabled && _currentSettings.EnableNewContentIndicators)
            {
                SetToFinalState(_promoBanner, finalY: 0, finalOpacity: 1.0);
            }
            else
            {
                SetToFinalState(_promoBanner, finalY: 80, finalOpacity: 0);
            }
            // --- END MODIFIED ---

            // Launch Section (initially hidden, Y=80, Opacity=0; final visible state Y=0, Opacity=1)
            SetTransitions(_launchSection, _savedLaunchSectionTransitions);
            SetToFinalState(_launchSection, finalY: 0, finalOpacity: 1.0);

            // News Section Grid (initially hidden, Y=80, Opacity=0; final visible state Y=0, Opacity=1)
            SetTransitions(_newsSectionGrid, _savedNewsSectionGridTransitions);
            // --- MODIFIED: Only show if enabled in settings ---
            if (animationsEnabled && _currentSettings.EnableNewContentIndicators)
            {
                SetToFinalState(_newsSectionGrid, finalY: 0, finalOpacity: 1.0);
            }
            else
            {
                SetToFinalState(_newsSectionGrid, finalY: 80, finalOpacity: 0);
            }
            // --- END MODIFIED ---

            // Launch Error Banner (initially hidden, Opacity=0; final visible state Opacity=1)
            SetTransitions(_launchErrorBanner, _savedLaunchErrorBannerTransitions);
            SetToFinalState(_launchErrorBanner, finalOpacity: 0);

            // Settings Save Banner (Visibility and position depend on _settingsDirty)
            SetTransitions(_settingsSaveBanner, _savedSettingsSaveBannerTransitions);
            if (!animationsEnabled && _settingsDirty && _settingsSaveBanner != null)
            {
                // If animations are off and settings are dirty, ensure it's visible instantly
                if (_settingsSaveBanner.RenderTransform is TranslateTransform tt) tt.Y = 0; // Visible position
                _settingsSaveBanner.Opacity = 1;
                _settingsSaveBanner.IsVisible = true;
            }
            else if (!animationsEnabled && !_settingsDirty && _settingsSaveBanner != null)
            {
                // If animations are off and settings are NOT dirty, ensure it's hidden instantly
                if (_settingsSaveBanner.RenderTransform is TranslateTransform tt) tt.Y = 80; // Hidden position
                _settingsSaveBanner.Opacity = 0;
                _settingsSaveBanner.IsVisible = false;
            }
            // If animations are enabled, its state is managed by Show/HideSettingsSaveBanner, which will animate.


            // Account Panel (transitions are on its RenderTransform)
            if (_accountPanel?.RenderTransform is TranslateTransform accountPanelTt)
            {
                if (animationsEnabled && _savedAccountPanelTransitions != null)
                {
                    var newTransitions = new Transitions();
                    foreach (var t in _savedAccountPanelTransitions)
                    {
                        newTransitions.Add(t);
                    }
                    accountPanelTt.Transitions = newTransitions;
                }
                else
                {
                    accountPanelTt.Transitions = emptyTransitions;
                }
            }
            SetToFinalState(_accountPanel, finalX: 320, finalOpacity: 0); // Initially hidden


            // Sidebar Hover Tooltip (initially hidden, Opacity=0)
            SetTransitions(_sidebarHoverTooltip, _savedTooltipTransitions);
            SetToFinalState(_sidebarHoverTooltip, finalOpacity: 0);

            // Quick Play Tooltip (initially hidden, Opacity=0)
            SetTransitions(_quickPlayTooltip, _savedQuickPlayTooltipTransitions);
            SetToFinalState(_quickPlayTooltip, finalOpacity: 0);

            // Sidebar Icons (Margin transitions)
            // For sidebar buttons, we need to ensure their transitions are set,
            // and their margins are reset if animations are disabled
            SetTransitions(_gameButton, _savedGameButtonTransitions);
            if (!animationsEnabled && _gameButton != null) _gameButton.Margin = new Thickness(0);

            SetTransitions(_versionsButton, _savedVersionsButtonTransitions);
            if (!animationsEnabled && _versionsButton != null) _versionsButton.Margin = new Thickness(0);

            SetTransitions(_serversButton, _savedServersButtonTransitions);
            if (!animationsEnabled && _serversButton != null) _serversButton.Margin = new Thickness(0);

            SetTransitions(_modsButton, _savedModsButtonTransitions);
            if (!animationsEnabled && _modsButton != null) _modsButton.Margin = new Thickness(0);

            SetTransitions(_settingsButton, _savedSettingsButtonTransitions);
            if (!animationsEnabled && _settingsButton != null) _settingsButton.Margin = new Thickness(0);


            // Particle animation
            if (!animationsEnabled)
            {
                _particleCts?.Cancel(); // Stop particle generation
                _particles.Clear(); // Clear existing particles
                _particleLayer?.Children.Clear(); // Clear particles from UI
            }
            else
            {
                // If animations are enabled and particles are not running, restart them.
                if (_particleCts == null || _particleCts.IsCancellationRequested)
                {
                    CreateParticles();
                }
            }
        }


        private async void OnNewsItem1PointerEntered(object? s, PointerEventArgs e) => await AnimateScaleTransform(_newsItem1Scale, 1.05, 200, (cts) => _newsItem1Cts = cts, () => _newsItem1Cts);
        private async void OnNewsItem1PointerExited(object? s, PointerEventArgs e) => await AnimateScaleTransform(_newsItem1Scale, 1.00, 200, (cts) => _newsItem1Cts = cts, () => _newsItem1Cts);
        private async void OnNewsItem2PointerEntered(object? s, PointerEventArgs e) => await AnimateScaleTransform(_newsItem2Scale, 1.05, 200, (cts) => _newsItem2Cts = cts, () => _newsItem2Cts);
        private async void OnNewsItem2PointerExited(object? s, PointerEventArgs e) => await AnimateScaleTransform(_newsItem2Scale, 1.00, 200, (cts) => _newsItem2Cts = cts, () => _newsItem2Cts);


        /// <summary>
        /// Animates a ScaleTransform to a target value over a specified duration,
        /// managing its associated CancellationTokenSource for smooth cancellations.
        /// </summary>
        /// <param name="st">The ScaleTransform to animate.</param>
        /// <param name="target">The target scale value.</param>
        /// <param name="durationMs">The duration of the animation in milliseconds.</param>
        /// <param name="setCtsAction">An Action to set the CancellationTokenSource field (e.g., (cts) => _myCtsField = cts).</param>
        /// <param name="getCtsFunc">A Func to get the current CancellationTokenSource field (e.g., () => _myCtsField).</param>
        private async Task AnimateScaleTransform(
            ScaleTransform? st,
            double target,
            int durationMs,
            Action<CancellationTokenSource?> setCtsAction,
            Func<CancellationTokenSource?> getCtsFunc
        )
        {
            if (st == null) return;

            // Get the current CTS from the field via the Func
            CancellationTokenSource? currentCts = getCtsFunc();

            // If animations are disabled, snap to the target immediately
            if (!AreAnimationsEnabled())
            {
                st.ScaleX = st.ScaleY = target;
                currentCts?.Cancel();
                currentCts?.Dispose();
                setCtsAction(null); // Clear the CTS field
                return;
            }

            // Cancel any existing animation for this specific ScaleTransform
            currentCts?.Cancel();
            currentCts?.Dispose(); // Dispose the old one

            // Create a new CancellationTokenSource for the current animation and set it to the field
            CancellationTokenSource newCts = new CancellationTokenSource();
            setCtsAction(newCts); // Update the field with the new CTS
            var ct = newCts.Token;

            double start = st.ScaleX;
            double delta = target - start;

            // If already at target or very close, just set and return to avoid unnecessary animation
            if (Math.Abs(delta) < 0.001)
            {
                st.ScaleX = st.ScaleY = target;
                newCts.Dispose();
                setCtsAction(null); // Clear the CTS field
                return;
            }

            const int steps = 16; // Number of steps for the animation
            int delayMs = durationMs / steps;

            try
            {
                for (int i = 1; i <= steps; i++)
                {
                    ct.ThrowIfCancellationRequested(); // Check for cancellation at each step

                    double t = (double)i / steps;
                    double eased = 1 - Math.Pow(1 - t, 3); // Cubic ease out

                    // Dispatcher.UIThread.InvokeAsync ensures UI updates are on the main thread
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (st != null) // Double check if st is still valid on UI thread
                        {
                            st.ScaleX = st.ScaleY = start + (delta * eased);
                        }
                    });

                    await Task.Delay(delayMs, ct);
                }
                // Animation completed successfully without cancellation
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (st != null)
                    {
                        st.ScaleX = st.ScaleY = target; // Ensure it ends exactly at the target
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // Animation was cancelled. The ScaleTransform remains at its current position.
                // A new animation (either zoom-in or zoom-out) will be initiated by the next event.
            }
            finally
            {
                // Dispose the CancellationTokenSource when the animation task finishes or is cancelled
                // and clear the reference in the field.
                newCts.Dispose();
                // Only clear the field if it still holds *this specific* CTS,
                // to avoid clearing a new CTS that might have been set by a subsequent rapid call.
                if (getCtsFunc() == newCts)
                {
                    setCtsAction(null);
                }
            }
        }


        private async Task AnimateBgScale(ScaleTransform? st, double target, int ms)
        {
            if (st == null) return;

            if (!AreAnimationsEnabled())
            {
                st.ScaleX = st.ScaleY = target;
                return;
            }

            _launchBgCts?.Cancel(); // Cancel any existing animation for the background
            _launchBgCts = new CancellationTokenSource(); // Create a new CTS for this animation
            var ct = _launchBgCts.Token;
            double start = st.ScaleX;
            double delta = target - start;

            try
            {
                for (int i = 1; i <= 16; i++)
                {
                    if (ct.IsCancellationRequested) break;
                    double t = (double)i / 16;
                    double eased = 1 - Math.Pow(1 - t, 3);
                    st.ScaleX = st.ScaleY = start + (delta * eased);
                    await Task.Delay(ms / 16, ct);
                }
            }
            catch (OperationCanceledException)
            {
                // If cancelled, smoothly animate back to 1.00
                if (st.ScaleX != 1.00)
                {
                    double currentScale = st.ScaleX;
                    double resetDelta = 1.00 - currentScale;
                    for (int i = 1; i <= 8; i++) // Shorter, faster reset animation
                    {
                        double t = (double)i / 8;
                        double eased = 1 - Math.Pow(1 - t, 3);
                        st.ScaleX = st.ScaleY = currentScale + (resetDelta * eased);
                        await Task.Delay(ms / 32); // Even faster delay for smooth snap-back
                    }
                    st.ScaleX = st.ScaleY = 1.00; // Ensure it ends exactly at 1.00
                }
            }
            finally
            {
                if (!ct.IsCancellationRequested) { st.ScaleX = st.ScaleY = target; }
            }
        }

        private async Task AnimateNewsImageScale(ScaleTransform? st, double target, int ms, CancellationTokenSource? cts)
        {
            if (st == null) return;

            if (!AreAnimationsEnabled())
            {
                st.ScaleX = st.ScaleY = target;
                return;
            }

            cts?.Cancel(); // Cancel any existing animation for this specific image
            cts = new CancellationTokenSource(); // Create a new CTS for this animation
            var ct = cts.Token;
            double start = st.ScaleX;
            double delta = target - start;

            try
            {
                for (int i = 1; i <= 12; i++)
                {
                    if (ct.IsCancellationRequested) break;
                    double t = (double)i / 12;
                    double eased = 1 - Math.Pow(1 - t, 3);
                    st.ScaleX = st.ScaleY = start + (delta * eased);
                    await Task.Delay(ms / 12, ct);
                }
            }
            catch (OperationCanceledException)
            {
                // If cancelled, smoothly animate back to 1.00
                if (st.ScaleX != 1.00)
                {
                    double currentScale = st.ScaleX;
                    double resetDelta = 1.00 - currentScale;
                    for (int i = 1; i <= 8; i++) // Shorter, faster reset animation
                    {
                        double t = (double)i / 8;
                        double eased = 1 - Math.Pow(1 - t, 3);
                        st.ScaleX = st.ScaleY = currentScale + (resetDelta * eased);
                        await Task.Delay(ms / 32); // Even faster delay for smooth snap-back
                    }
                    st.ScaleX = st.ScaleY = 1.00; // Ensure it ends exactly at 1.00
                }
            }
            finally
            {
                if (!ct.IsCancellationRequested) { st.ScaleX = st.ScaleY = target; }
            }
        }

        private async void OpenAccountPanel(object? s, RoutedEventArgs e)
        {
            if (_accountPanelOverlay == null || _accountPanel == null) return;
            _accountPanelOverlay.IsVisible = true;

            // Declare tt once here
            TranslateTransform? tt = _accountPanel.RenderTransform as TranslateTransform;
            if (tt == null && _accountPanel != null) // Ensure tt is initialized if not present
            {
                tt = new TranslateTransform();
                _accountPanel.RenderTransform = tt;
            }

            if (!AreAnimationsEnabled()) // NEW
            {
                if (tt != null) tt.X = 0; // Use the already declared tt
                return;
            }
            if (tt != null) tt.X = 0; // Use the already declared tt
            _ = RefreshAccountPanelPoseAsync();
            UpdateRichPresenceFromState();
        }

        private async Task RefreshAccountPanelPoseAsync()
        {
            try
            {
                // Ensure _session is current
                await LoadSessionAsync();

                var settings = await _settingsService.LoadSettingsAsync();
                var username = _session?.Username ?? settings.SessionUsername;
                var uuid = _session?.UUID ?? settings.SessionUuid;

                // Tiny backoff if nothing is ready yet (first paint race)
                if (string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(uuid))
                {
                    await Task.Delay(250);
                    settings = await _settingsService.LoadSettingsAsync();
                    username = _session?.Username ?? settings.SessionUsername;
                    uuid = _session?.UUID ?? settings.SessionUuid;
                }

                var largePose = _skinRenderService.GetRandomLargePoseName();

                // Pass uuid so SkinRenderService uses it in the URL
                var largeBitmap = await _skinRenderService.LoadSkinImageAsync(
                    username ?? "Player",
                    largePose,
                    "full",
                    uuid
                );

                if (_accountCharacterImage != null && largeBitmap != null)
                    _accountCharacterImage.Source = largeBitmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing account panel pose: {ex.Message}");
            }
        }

        private async void CloseAccountPanel(object? s, RoutedEventArgs e)
        {
            if (_accountPanel == null || _accountPanelOverlay == null) return;

            // Declare tt once here
            TranslateTransform? tt = _accountPanel.RenderTransform as TranslateTransform;
            if (tt == null && _accountPanel != null) // Ensure tt is initialized if not present
            {
                tt = new TranslateTransform();
                _accountPanel.RenderTransform = tt;
            }

            if (!AreAnimationsEnabled()) // NEW
            {
                if (tt != null) tt.X = 320; // Use the already declared tt
                _accountPanelOverlay.IsVisible = false;
                return;
            }

            if (tt != null) tt.X = 320; // Use the already declared tt
            await Task.Delay(300);
            _accountPanelOverlay.IsVisible = false;
        }

        private static void OpenDiscord(object? s, RoutedEventArgs e) =>
            Process.Start(new ProcessStartInfo { FileName = "https://discord.gg/F4MW2CAT94", UseShellExecute = true });
        private static void OpenWebsite(object? s, RoutedEventArgs e) =>
            Process.Start(new ProcessStartInfo { FileName = "https://leafclient.net", UseShellExecute = true });
        private static void OpenInstagram(object? s, RoutedEventArgs e) =>
            Process.Start(new ProcessStartInfo { FileName = "https://instagram.com/leafclient", UseShellExecute = true });


        private async void LoadAndApplySettings()
        {
            _currentSettings = await _settingsService.LoadSettingsAsync();

            // Ensure lists exist
            _currentSettings.CustomServers ??= new List<ServerInfo>();
            _currentSettings.CustomSkins ??= new List<SkinInfo>();

            // Insert defaults before any UI build
            await InitializeDefaultServersAsync();

            ApplySettingsToUi(_currentSettings);

            // Build UI now that servers exist
            LoadServers();
            RefreshQuickPlayBar();

            StartRichPresenceIfEnabled();

            InitializePrayerTimeReminder();
        }
        private string GetModFilePath(string modsFolder, InstalledMod mod, bool isDisabled = false)
        {
            string fileName = mod.FileName;
            // Ensure the base filename doesn't already have .disabled if we're trying to add/remove it
            if (fileName.EndsWith(".jar.disabled", StringComparison.OrdinalIgnoreCase))
            {
                fileName = fileName.Replace(".jar.disabled", ".jar");
            }

            if (isDisabled)
            {
                return System.IO.Path.Combine(modsFolder, fileName + ".disabled");
            }
            else
            {
                return System.IO.Path.Combine(modsFolder, fileName);
            }
        }

        private void ApplySettingsToUi(LauncherSettings settings)
        {
            _isApplyingSettings = true;
            if (_enablePrayerTimeReminderToggle != null) _enablePrayerTimeReminderToggle.IsChecked = settings.EnablePrayerTimeReminder;
            if (_prayerTimeCountryComboBox != null) _prayerTimeCountryComboBox.SelectedItem = settings.PrayerTimeCountry; // UPDATED
            if (_prayerTimeCityTextBox != null) _prayerTimeCityTextBox.Text = settings.PrayerTimeCity;

            if (_prayerCalculationMethodComboBox != null)
            {
                var itemToSelect = _prayerCalculationMethodComboBox.Items.OfType<ComboBoxItem>()
                    .FirstOrDefault(i => (i.Content as string) == settings.PrayerTimeCalculationMethod.ToString());
                if (itemToSelect != null)
                {
                    _prayerCalculationMethodComboBox.SelectedItem = itemToSelect;
                }
                else
                {
                    _prayerCalculationMethodComboBox.SelectedIndex = (int)PrayerCalculationMethod.ISNA; // Default fallback
                }
            }
            if (_prayerReminderMinutesBeforeSlider != null) _prayerReminderMinutesBeforeSlider.Value = settings.PrayerReminderMinutesBefore;
            if (_prayerReminderMinutesBeforeValueText != null) _prayerReminderMinutesBeforeValueText.Text = $"{settings.PrayerReminderMinutesBefore:F0} minutes";

            if (_gameResolutionWidthTextBox != null) _gameResolutionWidthTextBox.Text = settings.GameResolutionWidth.ToString();
            if (_gameResolutionHeightTextBox != null) _gameResolutionHeightTextBox.Text = settings.GameResolutionHeight.ToString();
            if (settings.GameResolutionWidth > 0 && settings.GameResolutionHeight > 0)
            {
                _lastAspectRatio = (double)settings.GameResolutionHeight / settings.GameResolutionWidth;
                Console.WriteLine($"[Aspect Ratio Lock] Initialized ratio: {_lastAspectRatio:F4} ({settings.GameResolutionWidth}x{settings.GameResolutionHeight})");
            }

            if (_useCustomGameResolutionToggle != null) _useCustomGameResolutionToggle.IsChecked = settings.UseCustomGameResolution;
            if (_lockGameAspectRatioToggle != null) _lockGameAspectRatioToggle.IsChecked = settings.LockGameAspectRatio;

            if (_launcherVisibilityKeepOpenRadio != null) _launcherVisibilityKeepOpenRadio.IsChecked = (settings.LauncherVisibilityOnGameLaunch == LauncherVisibility.KeepOpen);
            if (_launcherVisibilityHideRadio != null) _launcherVisibilityHideRadio.IsChecked = (settings.LauncherVisibilityOnGameLaunch == LauncherVisibility.Hide);

            if (_updateDeliveryNormalRadio != null) _updateDeliveryNormalRadio.IsChecked = (settings.GameUpdateDelivery == UpdateDelivery.Normal);
            if (_updateDeliveryEarlyRadio != null) _updateDeliveryEarlyRadio.IsChecked = (settings.GameUpdateDelivery == UpdateDelivery.EarlyOptIn);
            if (_updateDeliveryLateRadio != null) _updateDeliveryLateRadio.IsChecked = (settings.GameUpdateDelivery == UpdateDelivery.LateOptOut);

            if (_showUsernameInDiscordRichPresenceToggle != null) _showUsernameInDiscordRichPresenceToggle.IsChecked = settings.ShowUsernameInDiscordRichPresence;

            if (_closingNotificationsAlwaysRadio != null) _closingNotificationsAlwaysRadio.IsChecked = (settings.ClosingNotificationsPreference == NotificationPreference.Always);
            if (_closingNotificationsJustOnceRadio != null) _closingNotificationsJustOnceRadio.IsChecked = (settings.ClosingNotificationsPreference == NotificationPreference.JustOnce);
            if (_closingNotificationsNeverRadio != null) _closingNotificationsNeverRadio.IsChecked = (settings.ClosingNotificationsPreference == NotificationPreference.Never);
            if (_enableUpdateNotificationsToggle != null) _enableUpdateNotificationsToggle.IsChecked = settings.EnableUpdateNotifications;
            if (_enableNewContentIndicatorsToggle != null) _enableNewContentIndicatorsToggle.IsChecked = settings.EnableNewContentIndicators;

            // Trigger disk usage calculation
            _ = CalculateDiskUsageAsync();

            if (_optiFineToggle != null) _optiFineToggle.IsChecked = settings.IsOptiFineEnabled;
            if (_lithiumToggle != null) _lithiumToggle.IsChecked = settings.IsLithiumEnabled;
            if (_sodiumToggle != null) _sodiumToggle.IsChecked = settings.IsSodiumEnabled;

            if (_launchOnStartupToggle != null) _launchOnStartupToggle.IsChecked = settings.LaunchOnStartup;
            if (_minimizeToTrayToggle != null) _minimizeToTrayToggle.IsChecked = settings.MinimizeToTray;
            if (_discordRichPresenceToggle != null) _discordRichPresenceToggle.IsChecked = settings.DiscordRichPresence;

            if (_minRamAllocationTextBox != null)
            {
                _minRamAllocationTextBox.Text = settings.MinRamAllocationMb.ToString();
            }

            if (_maxRamAllocationComboBox != null)
            {
                var itemToSelect = _maxRamAllocationComboBox.Items.OfType<ComboBoxItem>()
                    .FirstOrDefault(i => (i.Content as string) == settings.MaxRamAllocationGb);

                if (itemToSelect != null)
                {
                    _maxRamAllocationComboBox.SelectedItem = itemToSelect;
                }
                else
                {
                    // Fallback if the saved value is not found in the ComboBox items
                    var defaultItem = _maxRamAllocationComboBox.Items.OfType<ComboBoxItem>()
                        .FirstOrDefault(i => (i.Content as string) == "8 GB");
                    _maxRamAllocationComboBox.SelectedItem = defaultItem ?? _maxRamAllocationComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault();
                }
            }

            if (_quickLaunchEnabledToggle != null)
            {
                _quickLaunchEnabledToggle.IsChecked = settings.QuickLaunchEnabled;
            }

            if (_quickJoinServerAddressTextBox != null) _quickJoinServerAddressTextBox.Text = settings.QuickJoinServerAddress;
            if (_quickJoinServerPortTextBox != null) _quickJoinServerPortTextBox.Text = settings.QuickJoinServerPort;
            // The line below is redundant as _quickLaunchEnabledToggle.IsChecked is already set above
            // _currentSettings.QuickLaunchEnabled = _quickLaunchEnabledToggle?.IsChecked ?? false;

            _currentSettings.MouseSensitivity = _mouseSensitivitySlider?.Value ?? 0.5;
            _currentSettings.ScrollSensitivity = _scrollSensitivitySlider?.Value ?? 1.0;
            _currentSettings.AutoJump = _autoJumpToggle?.IsChecked ?? false;
            _currentSettings.Touchscreen = _touchscreenToggle?.IsChecked ?? false;
            _currentSettings.ToggleSprint = _toggleSprintToggle?.IsChecked ?? false;
            _currentSettings.ToggleCrouch = _toggleCrouchToggle?.IsChecked ?? false;
            _currentSettings.Subtitles = _subtitlesToggle?.IsChecked ?? false;
            _currentSettings.RenderDistance = _renderDistanceSlider?.Value ?? 32;
            _currentSettings.SimulationDistance = _simulationDistanceSlider?.Value ?? 32;
            _currentSettings.EntityDistance = _entityDistanceSlider?.Value ?? 1;
            _currentSettings.MaxFps = _maxFpsSlider?.Value ?? 0; // Ensure 0 is saved for unlimited
            _currentSettings.VSync = _vSyncToggle?.IsChecked ?? false;
            _currentSettings.Fullscreen = _fullscreenToggle?.IsChecked ?? false;
            _currentSettings.EntityShadows = _entityShadowsToggle?.IsChecked ?? false;
            _currentSettings.HighContrast = _highContrastToggle?.IsChecked ?? false;

            if (_renderCloudsComboBox != null)
            {
                var itemToSelect = _renderCloudsComboBox.Items.OfType<ComboBoxItem>()
                    .FirstOrDefault(i => i.Content?.ToString() == settings.RenderClouds);
                if (itemToSelect != null)
                {
                    _renderCloudsComboBox.SelectedItem = itemToSelect;
                }
                else
                {
                    var defaultItem = _renderCloudsComboBox.Items.OfType<ComboBoxItem>()
                        .FirstOrDefault(i => i.Content?.ToString() == "Fast");
                    _renderCloudsComboBox.SelectedItem = defaultItem ?? _renderCloudsComboBox.Items[0];
                }
            }

            if (_playerHatToggle != null) _playerHatToggle.IsChecked = settings.PlayerHat;
            if (_playerCapeToggle != null) _playerCapeToggle.IsChecked = settings.PlayerCape;
            if (_playerJacketToggle != null) _playerJacketToggle.IsChecked = settings.PlayerJacket;
            if (_playerLeftSleeveToggle != null) _playerLeftSleeveToggle.IsChecked = settings.PlayerLeftSleeve;
            if (_playerRightSleeveToggle != null) _playerRightSleeveToggle.IsChecked = settings.PlayerRightSleeve;
            if (_playerLeftPantToggle != null) _playerLeftPantToggle.IsChecked = settings.PlayerLeftPant;
            if (_playerRightPantToggle != null) _playerRightPantToggle.IsChecked = settings.PlayerRightPant;
            if (settings.CustomSkins == null)
            {
                settings.CustomSkins = new List<SkinInfo>();
            }
            if (_playerMainHandComboBox != null)
            {
                var itemToSelect = _playerMainHandComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(i => i.Content?.ToString() == settings.PlayerMainHand);
                if (itemToSelect != null)
                {
                    _playerMainHandComboBox.SelectedItem = itemToSelect;
                }
                else
                {
                    _playerMainHandComboBox.SelectedIndex = 1;
                }
            }

            ThemeService.SetTheme(settings.Theme);
            if (_themeComboBox != null)
            {
                _themeComboBox.SelectionChanged -= OnThemeChanged;
                var itemToSelect = _themeComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(i => i.Content?.ToString() == settings.Theme);
                if (itemToSelect != null)
                {
                    _themeComboBox.SelectedItem = itemToSelect;
                }
                else
                {
                    _themeComboBox.SelectedIndex = 0;
                }
                _themeComboBox.SelectionChanged += OnThemeChanged;
            }
            if (_animationsEnabledToggle != null) _animationsEnabledToggle.IsChecked = settings.AnimationsEnabled;

            string validSubVersion = settings.SelectedSubVersion;
            string validMajorVersion = settings.SelectedMajorVersion;

            if (string.IsNullOrWhiteSpace(validSubVersion) || !_allVersions.Any(v => v.FullVersion == validSubVersion))
            {
                var fallbackVersion = _allVersions.FirstOrDefault() ?? new VersionInfo("1.20.1", "1.20", "Release", "Fabric", "2023", "Most popular 1.20 version with extensive mod support, stable for modding.");
                validSubVersion = fallbackVersion.FullVersion;
                validMajorVersion = fallbackVersion.MajorVersion;

                _currentSettings.SelectedSubVersion = validSubVersion;
                _currentSettings.SelectedMajorVersion = validMajorVersion;

                _ = _settingsService.SaveSettingsAsync(_currentSettings);
            }

            // Now safely trigger UI update using the VALID version
            if (_majorVersionsStackPanel != null)
            {
                var majorVersionBorder = _majorVersionsStackPanel.Children
                    .OfType<Border>()
                    .FirstOrDefault(b => b.Tag is string tag && tag == validMajorVersion);

                if (majorVersionBorder != null)
                {
                    // Temporarily unsubscribe to prevent OnSubVersionSelected from saving prematurely
                    // when OnMajorVersionClick programmatically selects an item.
                    if (_versionDropdown != null)
                    {
                        _versionDropdown.SelectionChanged -= OnSubVersionSelected;
                    }

                    OnMajorVersionClick(majorVersionBorder, new RoutedEventArgs()); // This populates _versionDropdown

                    // After OnMajorVersionClick populates, explicitly select the saved sub-version
                    if (_versionDropdown != null)
                    {
                        var itemToSelect = _versionDropdown.Items.OfType<ComboBoxItem>()
                            .FirstOrDefault(item => item.Content?.ToString() == validSubVersion);

                        if (itemToSelect != null)
                        {
                            _versionDropdown.SelectedItem = itemToSelect;
                        }
                        else if (_versionDropdown.ItemCount > 0)
                        {
                            _versionDropdown.SelectedIndex = 0; // Fallback to first if saved sub-version isn't in this major group
                        }
                        // Re-subscribe the event handler now that initialization is complete
                        _versionDropdown.SelectionChanged += OnSubVersionSelected;
                    }
                }
                else
                {
                    // Fallback: if major version not found, use the first one
                    var firstMajor = _majorVersionsStackPanel.Children.OfType<Border>().FirstOrDefault();
                    if (firstMajor != null)
                    {
                        // Temporarily unsubscribe
                        if (_versionDropdown != null)
                        {
                            _versionDropdown.SelectionChanged -= OnSubVersionSelected;
                        }
                        OnMajorVersionClick(firstMajor, new RoutedEventArgs());
                        // After OnMajorVersionClick populates, explicitly select the saved sub-version
                        if (_versionDropdown != null)
                        {
                            var itemToSelect = _versionDropdown.Items.OfType<ComboBoxItem>()
                               .FirstOrDefault(item => item.Content?.ToString() == validSubVersion);

                            if (itemToSelect != null)
                            {
                                _versionDropdown.SelectedItem = itemToSelect;
                            }
                            else if (_versionDropdown.ItemCount > 0)
                            {
                                _versionDropdown.SelectedIndex = 0;
                            }
                            // Re-subscribe
                            _versionDropdown.SelectionChanged += OnSubVersionSelected;
                        }
                    }
                }
            }

            // These methods will now use the _currentSettings.SelectedSubVersion which should be correctly set.
            UpdateLaunchVersionText();
            UpdateAddonSelectionUI(validSubVersion);

            LoadServers();
            RefreshQuickPlayBar();

            _isApplyingSettings = false;
        }


        private static void OpenPrivacyPolicy(object? s, RoutedEventArgs e) =>
           Process.Start(new ProcessStartInfo { FileName = "https://leafclient.net/privacypolicy", UseShellExecute = true });

        private static void OpenTermsOfService(object? s, RoutedEventArgs e) =>
            Process.Start(new ProcessStartInfo { FileName = "https://leafclient.net/tos", UseShellExecute = true });

        private static void OpenLicenses(object? s, RoutedEventArgs e) =>
            Process.Start(new ProcessStartInfo { FileName = "https://github.com/LeafClientMC/LeafClient/blob/main/LICENSE.md", UseShellExecute = true });


        private async void SaveSettingsFromUi()
        {
            _currentSettings.EnablePrayerTimeReminder = _enablePrayerTimeReminderToggle?.IsChecked ?? false;
            _currentSettings.PrayerTimeCountry = _prayerTimeCountryComboBox?.SelectedItem as string ?? "USA"; // UPDATED
            _currentSettings.PrayerTimeCity = _prayerTimeCityTextBox?.Text ?? "New York";

            if (_prayerCalculationMethodComboBox?.SelectedItem is ComboBoxItem selectedMethodItem && selectedMethodItem.Content is string methodString)
            {
                if (Enum.TryParse(methodString, out PrayerCalculationMethod method))
                {
                    _currentSettings.PrayerTimeCalculationMethod = method;
                }
            }
            _currentSettings.PrayerReminderMinutesBefore = (int)(_prayerReminderMinutesBeforeSlider?.Value ?? 10);
            if (int.TryParse(_gameResolutionWidthTextBox?.Text, out int resWidth)) _currentSettings.GameResolutionWidth = resWidth;
            if (int.TryParse(_gameResolutionHeightTextBox?.Text, out int resHeight)) _currentSettings.GameResolutionHeight = resHeight;
            _currentSettings.UseCustomGameResolution = _useCustomGameResolutionToggle?.IsChecked ?? false;
            _currentSettings.LockGameAspectRatio = _lockGameAspectRatioToggle?.IsChecked ?? true;

            if (_launcherVisibilityKeepOpenRadio?.IsChecked == true) _currentSettings.LauncherVisibilityOnGameLaunch = LauncherVisibility.KeepOpen;
            else if (_launcherVisibilityHideRadio?.IsChecked == true) _currentSettings.LauncherVisibilityOnGameLaunch = LauncherVisibility.Hide;

            if (_updateDeliveryNormalRadio?.IsChecked == true) _currentSettings.GameUpdateDelivery = UpdateDelivery.Normal;
            else if (_updateDeliveryEarlyRadio?.IsChecked == true) _currentSettings.GameUpdateDelivery = UpdateDelivery.EarlyOptIn;
            else if (_updateDeliveryLateRadio?.IsChecked == true) _currentSettings.GameUpdateDelivery = UpdateDelivery.LateOptOut;

            _currentSettings.ShowUsernameInDiscordRichPresence = _showUsernameInDiscordRichPresenceToggle?.IsChecked ?? true;

            if (_closingNotificationsAlwaysRadio?.IsChecked == true) _currentSettings.ClosingNotificationsPreference = NotificationPreference.Always;
            else if (_closingNotificationsJustOnceRadio?.IsChecked == true) _currentSettings.ClosingNotificationsPreference = NotificationPreference.JustOnce;
            else if (_closingNotificationsNeverRadio?.IsChecked == true) _currentSettings.ClosingNotificationsPreference = NotificationPreference.Never;
            _currentSettings.EnableUpdateNotifications = _enableUpdateNotificationsToggle?.IsChecked ?? true;
            _currentSettings.EnableNewContentIndicators = _enableNewContentIndicatorsToggle?.IsChecked ?? true;

            _currentSettings.IsSodiumEnabled = _sodiumToggle?.IsChecked ?? false;
            _currentSettings.IsOptiFineEnabled = _optiFineToggle?.IsChecked ?? false;
            _currentSettings.IsLithiumEnabled = _lithiumToggle?.IsChecked ?? true;
            _currentSettings.LaunchOnStartup = _launchOnStartupToggle?.IsChecked ?? false;
            _currentSettings.MinimizeToTray = _minimizeToTrayToggle?.IsChecked ?? false;
            _currentSettings.DiscordRichPresence = _discordRichPresenceToggle?.IsChecked ?? false;
            if (int.TryParse(_minRamAllocationTextBox?.Text, out int minRam))
            {
                _currentSettings.MinRamAllocationMb = minRam;
            }
            _currentSettings.MaxRamAllocationGb = (_maxRamAllocationComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "8 GB";
            _currentSettings.QuickJoinServerAddress = _quickJoinServerAddressTextBox?.Text ?? "";
            _currentSettings.QuickJoinServerPort = _quickJoinServerPortTextBox?.Text ?? "25565";
            _currentSettings.QuickLaunchEnabled = _quickLaunchEnabledToggle?.IsChecked ?? false;
            _currentSettings.MouseSensitivity = _mouseSensitivitySlider?.Value ?? 0.5;
            _currentSettings.ScrollSensitivity = _scrollSensitivitySlider?.Value ?? 1.0;
            _currentSettings.AutoJump = _autoJumpToggle?.IsChecked ?? false;
            _currentSettings.Touchscreen = _touchscreenToggle?.IsChecked ?? false;
            _currentSettings.ToggleSprint = _toggleSprintToggle?.IsChecked ?? false;
            _currentSettings.ToggleCrouch = _toggleCrouchToggle?.IsChecked ?? false;
            _currentSettings.Subtitles = _subtitlesToggle?.IsChecked ?? false;
            _currentSettings.RenderDistance = _renderDistanceSlider?.Value ?? 32;
            _currentSettings.SimulationDistance = _simulationDistanceSlider?.Value ?? 32;
            _currentSettings.EntityDistance = _entityDistanceSlider?.Value ?? 1;
            _currentSettings.MaxFps = _maxFpsSlider?.Value ?? 0; // Ensure 0 is saved for unlimited
            _currentSettings.VSync = _vSyncToggle?.IsChecked ?? false;
            _currentSettings.Fullscreen = _fullscreenToggle?.IsChecked ?? false;
            _currentSettings.EntityShadows = _entityShadowsToggle?.IsChecked ?? false;
            _currentSettings.HighContrast = _highContrastToggle?.IsChecked ?? false;
            _currentSettings.RenderClouds = (_renderCloudsComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Fast";
            _currentSettings.PlayerHat = _playerHatToggle?.IsChecked ?? false;
            _currentSettings.PlayerCape = _playerCapeToggle?.IsChecked ?? false;
            _currentSettings.PlayerJacket = _playerJacketToggle?.IsChecked ?? false;
            _currentSettings.PlayerLeftSleeve = _playerLeftSleeveToggle?.IsChecked ?? false;
            _currentSettings.PlayerRightSleeve = _playerRightSleeveToggle?.IsChecked ?? false;
            _currentSettings.PlayerLeftPant = _playerLeftPantToggle?.IsChecked ?? false;
            _currentSettings.PlayerRightPant = _playerRightPantToggle?.IsChecked ?? false;
            _currentSettings.PlayerMainHand = (_playerMainHandComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Right";
            _currentSettings.Theme = (_themeComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Dark";
            _currentSettings.AnimationsEnabled = _animationsEnabledToggle?.IsChecked ?? false;
            if (_versionTitle?.Text != null && _versionTitle.Text.StartsWith("Minecraft "))
            {
                string versionText = _versionTitle.Text.Replace("Minecraft ", "");
                var parts = versionText.Split('.');
                if (parts.Length >= 2)
                {
                    _currentSettings.SelectedMajorVersion = $"{parts[0]}.{parts[1]}";
                }
            }
            _currentSettings.SelectedSubVersion = (_versionDropdown?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "1.21.4";
            var sub = _currentSettings.SelectedSubVersion;
            if (!string.IsNullOrWhiteSpace(sub))
            {
                var addon = GetSelectedAddon(sub);
                _currentSettings.SelectedAddonByVersion ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _currentSettings.SelectedAddonByVersion[sub] = addon;
            }
            await _settingsService.SaveSettingsAsync(_currentSettings);
        }

        private void InitializePrayerTimeReminder()
        {
            _prayerTimeCheckTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1) // Check every minute
            };
            _prayerTimeCheckTimer.Tick += async (s, e) => await CheckPrayerTimesAndRemind();
            _prayerTimeCheckTimer.Start();
            Console.WriteLine("[Prayer Reminder] Initialized prayer time reminder timer.");
        }

        private async Task CheckPrayerTimesAndRemind()
        {
            if (!_currentSettings.EnablePrayerTimeReminder)
            {
                _nextPrayerTimeReminder = null; // Clear if disabled
                return;
            }

            // Only fetch new prayer times once a day, or if it's the first check for the day
            if (_lastFetchedPrayerTimes == null || _lastFetchedPrayerTimes.data?.date.Gregorian.Date != DateTime.Today.ToString("dd-MM-yyyy"))
            {
                try
                {
                    // Aladhan API for prayer times by city and country
                    string apiUrl = $"https://api.aladhan.com/v1/timingsByCity?city={Uri.EscapeDataString(_currentSettings.PrayerTimeCity)}&country={Uri.EscapeDataString(_currentSettings.PrayerTimeCountry)}&method={(int)_currentSettings.PrayerTimeCalculationMethod}";
                    Console.WriteLine($"[Prayer Reminder] Fetching prayer times from: {apiUrl}");

                    var response = await _httpClient.GetStringAsync(apiUrl);
                    _lastFetchedPrayerTimes = JsonSerializer.Deserialize<AladhanPrayerTimesResponse>(response, Json.Options);

                    if (_lastFetchedPrayerTimes == null || _lastFetchedPrayerTimes.code != 200 || _lastFetchedPrayerTimes.data == null)
                    {
                        Console.Error.WriteLine($"[Prayer Reminder ERROR] Failed to fetch prayer times: {_lastFetchedPrayerTimes?.status}");
                        _nextPrayerTimeReminder = null;
                        return;
                    }
                    Console.WriteLine($"[Prayer Reminder] Successfully fetched prayer times for {_currentSettings.PrayerTimeCity}.");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Prayer Reminder ERROR] Exception while fetching prayer times: {ex.Message}");
                    _nextPrayerTimeReminder = null;
                    return;
                }
            }

            if (_lastFetchedPrayerTimes?.data?.timings == null) return;

            var timings = _lastFetchedPrayerTimes.data.timings;
            var now = DateTime.Now;

            // Map prayer names to their times
            var prayerTimes = new Dictionary<string, DateTime>
        {
            { "Fajr", DateTime.ParseExact(timings.Fajr, "HH:mm", null).AddDays(now.Date == DateTime.ParseExact(_lastFetchedPrayerTimes.data.date.Gregorian.Date, "dd-MM-yyyy", null) ? 0 : 1) }, // Adjust date if next day's Fajr
            { "Dhuhr", DateTime.ParseExact(timings.Dhuhr, "HH:mm", null).Date == now.Date ? DateTime.ParseExact(timings.Dhuhr, "HH:mm", null) : DateTime.ParseExact(timings.Dhuhr, "HH:mm", null).AddDays(1) },
            { "Asr", DateTime.ParseExact(timings.Asr, "HH:mm", null).Date == now.Date ? DateTime.ParseExact(timings.Asr, "HH:mm", null) : DateTime.ParseExact(timings.Asr, "HH:mm", null).AddDays(1) },
            { "Maghrib", DateTime.ParseExact(timings.Maghrib, "HH:mm", null).Date == now.Date ? DateTime.ParseExact(timings.Maghrib, "HH:mm", null) : DateTime.ParseExact(timings.Maghrib, "HH:mm", null).AddDays(1) },
            { "Isha", DateTime.ParseExact(timings.Isha, "HH:mm", null).Date == now.Date ? DateTime.ParseExact(timings.Isha, "HH:mm", null) : DateTime.ParseExact(timings.Isha, "HH:mm", null).AddDays(1) }
        };

            // Find the next prayer
            DateTime nextPrayerTime = DateTime.MaxValue;
            string nextPrayerName = "None";

            foreach (var prayer in prayerTimes)
            {
                // Ensure prayer times are for today or tomorrow if they've passed today
                DateTime currentPrayerDateTime = now.Date + prayer.Value.TimeOfDay;
                if (currentPrayerDateTime < now)
                {
                    currentPrayerDateTime = currentPrayerDateTime.AddDays(1);
                }

                if (currentPrayerDateTime > now && currentPrayerDateTime < nextPrayerTime)
                {
                    nextPrayerTime = currentPrayerDateTime;
                    nextPrayerName = prayer.Key;
                }
            }

            if (nextPrayerName != "None")
            {
                TimeSpan timeUntilNextPrayer = nextPrayerTime - now;
                int reminderMinutes = _currentSettings.PrayerReminderMinutesBefore;

                if (timeUntilNextPrayer.TotalMinutes <= reminderMinutes && timeUntilNextPrayer.TotalMinutes > 0)
                {
                    // Avoid showing multiple reminders for the same prayer
                    if (_nextPrayerTimeReminder == null || nextPrayerTime != _nextPrayerTimeReminder.Value || nextPrayerName != _nextPrayerName)
                    {
                        Console.WriteLine($"[Prayer Reminder] Next prayer ({nextPrayerName}) is in {timeUntilNextPrayer.TotalMinutes:F0} minutes. Showing reminder.");
                        await ShowPrayerReminderNotification(nextPrayerName, nextPrayerTime);
                        _nextPrayerTimeReminder = nextPrayerTime;
                        _nextPrayerName = nextPrayerName;
                    }
                }
                else if (timeUntilNextPrayer.TotalMinutes < 0) // If prayer time has passed, clear reminder
                {
                    _nextPrayerTimeReminder = null;
                    _nextPrayerName = null;
                }
            }
            else
            {
                _nextPrayerTimeReminder = null;
                _nextPrayerName = null;
            }
        }


        private async Task ShowPrayerReminderNotification(string prayerName, DateTime prayerTime)
        {
            var reminderWindow = new Window
            {
                Title = "Prayer Time Reminder",
                SystemDecorations = SystemDecorations.None,
                TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent },
                Background = Brushes.Transparent,
                Topmost = true,
                ShowInTaskbar = false,
                CanResize = false,
                SizeToContent = SizeToContent.WidthAndHeight,
                Width = 450,
                Height = 250
            };

            // Get primary screen to position the window
            Screen? primaryScreen = null;
            if (this.Screens != null) primaryScreen = this.Screens.Primary;
            if (primaryScreen == null && Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                primaryScreen = desktop.MainWindow?.Screens.Primary;
            }

            if (primaryScreen != null)
            {
                var workingArea = primaryScreen.WorkingArea;
                int x = workingArea.X + (workingArea.Width / 2) - ((int)reminderWindow.Width / 2);
                int y = workingArea.Y + (workingArea.Height / 2) - ((int)reminderWindow.Height / 2);
                reminderWindow.Position = new PixelPoint(x, y);
            }

            var cardBackgroundBrush = GetBrush("CardBackgroundColor");
            var primaryForegroundBrush = GetBrush("PrimaryForegroundBrush");
            var accentButtonForegroundBrush = GetBrush("AccentButtonForegroundBrush");
            var successBrush = GetBrush("SuccessBrush");
            var errorBrush = GetBrush("ErrorBrush");

            var contentGrid = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*,Auto")
            };

            var headerTextBlock = new TextBlock
            {
                Text = $"It's almost {prayerName} Prayer time!",
                Foreground = primaryForegroundBrush,
                FontWeight = FontWeight.Bold,
                FontSize = 20,
                Margin = new Thickness(20, 20, 20, 10),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };
            Grid.SetRow(headerTextBlock, 0);
            contentGrid.Children.Add(headerTextBlock);

            var messageTextBlock = new TextBlock
            {
                Text = $"Prayer will be at {prayerTime:hh:mm tt}. Prepare for prayer.",
                Foreground = primaryForegroundBrush,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(20, 0, 20, 20),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };
            Grid.SetRow(messageTextBlock, 1);
            contentGrid.Children.Add(messageTextBlock);

            var buttonStackPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Spacing = 15,
                Margin = new Thickness(20, 0, 20, 20)
            };

            var prayNowButton = new Button
            {
                Content = "I'll pray now",
                Background = successBrush,
                Foreground = accentButtonForegroundBrush,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20, 10),
                FontWeight = FontWeight.Bold,
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            prayNowButton.Click += (s, e) => reminderWindow.Close();
            buttonStackPanel.Children.Add(prayNowButton);

            var skipButton = new Button
            {
                Content = "Skip",
                Background = errorBrush,
                Foreground = accentButtonForegroundBrush,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20, 10),
                FontWeight = FontWeight.Bold,
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            skipButton.Click += async (s, e) =>
            {
                // Show Quranic verse
                var verseTextBlock = new TextBlock
                {
                    Text = "“Recite what has been revealed to you of the Book and establish prayer. Indeed, prayer prohibits immorality and wrongdoing, and the remembrance of Allah is greater.” — Qur'an 29:45",
                    Foreground = primaryForegroundBrush,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Margin = new Thickness(20)
                };

                // Hide original content and show verse
                contentGrid.Children.Clear();
                contentGrid.Children.Add(verseTextBlock);
                Grid.SetRow(verseTextBlock, 0);
                Grid.SetRowSpan(verseTextBlock, 3);

                // Animate blur and fade
                var blurEffect = new BlurEffect { Radius = 0 };
                verseTextBlock.Effect = blurEffect;

                if (AreAnimationsEnabled())
                {
                    // Fade in and blur
                    for (int i = 0; i <= 20; i++)
                    {
                        double t = i / 20.0;
                        verseTextBlock.Opacity = t;
                        blurEffect.Radius = 0 + (4 * t); // Blur up to 4
                        await Task.Delay(15);
                    }

                    await Task.Delay(4000); // Show for 4 seconds

                    // Fade out and unblur (reverse)
                    for (int i = 0; i <= 20; i++)
                    {
                        double t = i / 20.0;
                        verseTextBlock.Opacity = 1 - t;
                        blurEffect.Radius = 4 - (4 * t); // Blur down to 0
                        await Task.Delay(15);
                    }
                }
                else
                {
                    verseTextBlock.Opacity = 1;
                    blurEffect.Radius = 4;
                    await Task.Delay(4000);
                    verseTextBlock.Opacity = 0;
                    blurEffect.Radius = 0;
                }

                reminderWindow.Close();
            };
            buttonStackPanel.Children.Add(skipButton);

            Grid.SetRow(buttonStackPanel, 2);
            contentGrid.Children.Add(buttonStackPanel);

            var mainBorder = new Border
            {
                Background = cardBackgroundBrush,
                CornerRadius = new CornerRadius(12),
                BoxShadow = new BoxShadows(new BoxShadow { Blur = 15, Color = Color.FromArgb(128, 0, 0, 0), OffsetY = 4 }),
                Child = contentGrid
            };

            reminderWindow.Content = mainBorder;
            reminderWindow.Show();

            // Auto-close after a longer period if not interacted with
            if (AreAnimationsEnabled())
            {
                await Task.Delay(20000); // 20 seconds
                if (reminderWindow.IsVisible)
                {
                    // Fade out if still open
                    for (int i = 0; i <= 20; i++)
                    {
                        double t = i / 20.0;
                        reminderWindow.Opacity = 1 - t;
                        await Task.Delay(15);
                    }
                }
            }
            else
            {
                await Task.Delay(20000);
            }

            if (reminderWindow.IsVisible)
            {
                reminderWindow.Close();
            }
        }


        private void SyncLauncherManagedMods(string mcVersion)
        {
            string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
            if (!Directory.Exists(modsFolder))
            {
                Directory.CreateDirectory(modsFolder);
                Console.WriteLine("[Mod Sync] Mods folder did not exist, created.");
                return; // Nothing to sync if folder was just created
            }

            Console.WriteLine($"[Mod Sync] Synchronizing launcher-managed mods for MC {mcVersion}...");

            // Get all files currently in the mods folder (both .jar and .jar.disabled)
            var allModFilesOnDisk = Directory.GetFiles(modsFolder, "*.jar*", SearchOption.TopDirectoryOnly)
                                             .ToList();

            // Group launcher-managed mods by their target Minecraft version
            var launcherManagedModsForCurrentVersion = _currentSettings.InstalledMods
                .Where(m => m.MinecraftVersion.Equals(mcVersion, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(m => GetModFilePath(modsFolder, m, isDisabled: false), m => m, StringComparer.OrdinalIgnoreCase); // Key: full .jar path

            var launcherManagedModsForOtherVersions = _currentSettings.InstalledMods
                .Where(m => !m.MinecraftVersion.Equals(mcVersion, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // --- Phase 1: Sync mods for the current Minecraft version ---
            foreach (var mod in launcherManagedModsForCurrentVersion.Values)
            {
                string enabledJarPath = GetModFilePath(modsFolder, mod, isDisabled: false); // e.g., modname.jar
                string disabledJarPath = GetModFilePath(modsFolder, mod, isDisabled: true); // e.g., modname.jar.disabled

                if (mod.Enabled)
                {
                    // If mod should be ENABLED:
                    if (File.Exists(disabledJarPath))
                    {
                        // It's disabled on disk, but should be enabled. Rename it.
                        try
                        {
                            File.Move(disabledJarPath, enabledJarPath);
                            Console.WriteLine($"[Mod Sync] Enabled '{mod.Name}' for MC {mcVersion}.");
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"[Mod Sync ERROR] Failed to enable '{mod.Name}': {ex.Message}");
                            // Don't show banner here, as this runs on every launch. Let InstallUserModsAsync handle missing.
                        }
                    }
                    // If it's already enabledJarPath, or missing, InstallUserModsAsync will handle missing.
                }
                else // mod.Enabled is false
                {
                    // If mod should be DISABLED:
                    if (File.Exists(enabledJarPath))
                    {
                        // It's enabled on disk, but should be disabled. Rename it.
                        try
                        {
                            File.Move(enabledJarPath, disabledJarPath);
                            Console.WriteLine($"[Mod Sync] Disabled '{mod.Name}' for MC {mcVersion}.");
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"[Mod Sync ERROR] Failed to disable '{mod.Name}': {ex.Message}");
                            // Don't show banner here, as this runs on every launch.
                        }
                    }
                    // If it's already disabledJarPath, or missing, it's fine.
                }
            }

            // --- Phase 2: Clean up launcher-managed mods for OTHER Minecraft versions ---
            // These should not be in the mods folder if we are launching a different MC version.
            foreach (var mod in launcherManagedModsForOtherVersions)
            {
                string jarPath = GetModFilePath(modsFolder, mod, isDisabled: false);
                string disabledPath = GetModFilePath(modsFolder, mod, isDisabled: true);

                bool removed = false;
                if (File.Exists(jarPath))
                {
                    try
                    {
                        File.Delete(jarPath);
                        Console.WriteLine($"[Mod Sync] Removed incompatible version of '{mod.Name}' (MC {mod.MinecraftVersion}) from mods folder.");
                        removed = true;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[Mod Sync ERROR] Failed to remove incompatible '{mod.Name}' JAR: {ex.Message}");
                    }
                }
                if (File.Exists(disabledPath))
                {
                    try
                    {
                        File.Delete(disabledPath);
                        Console.WriteLine($"[Mod Sync] Removed incompatible version of '{mod.Name}' (MC {mod.MinecraftVersion}) from mods folder.");
                        removed = true;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[Mod Sync ERROR] Failed to remove incompatible '{mod.Name}' DISABLED JAR: {ex.Message}");
                    }
                }
                if (removed)
                {
                    // If a mod file for another version was removed, we should remove it from settings too,
                    // as it implies it's no longer relevant or was a leftover.
                    // However, we only remove from settings if it's not the current version.
                    _currentSettings.InstalledMods.Remove(mod);
                    // No need to save settings immediately here; it's saved after LaunchGameAsync completes.
                }
            }

            // --- Phase 3: Clean up potentially orphaned .disabled files for current version ---
            // Check for .disabled files on disk that are NOT in launcherManagedModsForCurrentVersion.
            // This handles cases where a mod was deleted from settings but its .disabled file remained.
            foreach (var fileOnDisk in allModFilesOnDisk)
            {
                if (fileOnDisk.EndsWith(".jar.disabled", StringComparison.OrdinalIgnoreCase))
                {
                    string baseFileName = System.IO.Path.GetFileName(fileOnDisk).Replace(".jar.disabled", ".jar");
                    // Check if this disabled file corresponds to a launcher-managed mod for the current MC version
                    // OR if it's not a launcher-managed mod at all (and thus an orphaned .disabled file)
                    if (!launcherManagedModsForCurrentVersion.ContainsKey(System.IO.Path.Combine(modsFolder, baseFileName)))
                    {
                        try
                        {
                            File.Delete(fileOnDisk);
                            Console.WriteLine($"[Mod Sync] Cleaned up orphaned disabled file: '{System.IO.Path.GetFileName(fileOnDisk)}'.");
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"[Mod Sync ERROR] Failed to delete orphaned disabled file '{System.IO.Path.GetFileName(fileOnDisk)}': {ex.Message}");
                        }
                    }
                }
            }

            Console.WriteLine("[Mod Sync] Synchronization complete.");
        }

        /// <summary>
        /// Attempts to detect the Minecraft version a mod is built for based on its filename.
        /// </summary>
        private string? DetectModVersion(string fileName)
        {
            // Common patterns: modname-1.20.1.jar, modname_1.20.jar, modname-mc1.20.4.jar
            var patterns = new[]
            {
                @"-(\d+\.\d+\.\d+)\.jar$",           // matches: -1.20.1.jar
                @"-(\d+\.\d+)\.jar$",                 // matches: -1.20.jar
                @"-mc(\d+\.\d+\.\d+)\.jar$",         // matches: -mc1.20.1.jar
                @"-mc(\d+\.\d+)\.jar$",               // matches: -mc1.20.jar
                @"-fabric-(\d+\.\d+\.\d+)\.jar$", // matches: -fabric-1.20.1.jar
                @"-fabric-(\d+\.\d+)\.jar$"        // matches: -fabric-1.20.jar
            };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(fileName, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return null; // Version couldn't be determined
        }

        /// <summary>
        /// Checks if two versions have a major version mismatch (e.g., 1.20 vs 1.21).
        /// </summary>
        private bool IsMajorVersionMismatch(string version1, string version2)
        {
            var v1Parts = version1.Split('.');
            var v2Parts = version2.Split('.');

            if (v1Parts.Length >= 2 && v2Parts.Length >= 2)
            {
                // Compare major.minor (e.g., "1.20" vs "1.21")
                string v1Major = $"{v1Parts[0]}.{v1Parts[1]}";
                string v2Major = $"{v2Parts[0]}.{v2Parts[1]}";

                return v1Major != v2Major;
            }

            return false;
        }

        /// <summary>
        /// Disables all mods in the mods folder (for vanilla launches).
        /// </summary>
        private void DisableAllMods(string modsFolder)
        {
            var modFiles = System.IO.Directory.GetFiles(modsFolder, "*.jar", System.IO.SearchOption.TopDirectoryOnly);

            foreach (var modFile in modFiles)
            {
                try
                {
                    string disabledPath = modFile + ".disabled";
                    if (!System.IO.File.Exists(disabledPath))
                    {
                        System.IO.File.Move(modFile, disabledPath, true);
                        Console.WriteLine($"[Mod Cleaner] Disabled: {System.IO.Path.GetFileName(modFile)}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Mod Cleaner] Failed to disable {System.IO.Path.GetFileName(modFile)}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Re-enables mods that match the current Minecraft version.
        /// </summary>
        private void ReenableModsForVersion(string modsFolder, string currentVersion)
        {
            var disabledFiles = System.IO.Directory.GetFiles(modsFolder, "*.jar.disabled", System.IO.SearchOption.TopDirectoryOnly);

            foreach (var disabledFile in disabledFiles)
            {
                try
                {
                    string fileName = System.IO.Path.GetFileName(disabledFile).Replace(".disabled", "");
                    var detectedVersion = DetectModVersion(fileName);

                    // Re-enable if version matches or version couldn't be determined (assume compatible)
                    if (detectedVersion == null || detectedVersion == currentVersion || !IsMajorVersionMismatch(currentVersion, detectedVersion))
                    {
                        string enabledPath = disabledFile.Replace(".disabled", "");
                        System.IO.File.Move(disabledFile, enabledPath, true);
                        Console.WriteLine($"[Mod Cleaner] Re-enabled: {fileName}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Mod Cleaner] Failed to re-enable {System.IO.Path.GetFileName(disabledFile)}: {ex.Message}");
                }
            }
        }


        private void SaveSettingsButton_Click(object? sender, RoutedEventArgs e)
        {
            SaveSettingsFromUi();
        }
    }

    public class VersionInfo
    {
        public string FullVersion { get; set; }
        public string MajorVersion { get; set; }
        public string Type { get; set; }
        public string Loader { get; set; }
        public string ReleaseDate { get; set; }
        public string Description { get; set; }

        public VersionInfo(string fullVersion, string majorVersion, string type, string loader, string releaseDate, string description)
        {
            FullVersion = fullVersion;
            MajorVersion = majorVersion;
            Type = type;
            Loader = loader;
            ReleaseDate = releaseDate;
            Description = description;
        }
    }

    public class MinecraftServerChecker
    {

        // Use a static HttpClient instance for efficiency
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string ApiBaseUrl = "https://api.mcstatus.io/v2/status/java/";

        public async Task<ServerStatusResult> GetServerStatusAsync(string address, int port = 25565)
        {
            string url = $"{ApiBaseUrl}{address}:{port}";
            Console.WriteLine($"[ServerCheck] Pinging {address}:{port} using mcstatus.io API...");

            try
            {
                var jsonResponse = await _httpClient.GetStringAsync(url);
                var response = JsonSerializer.Deserialize<ServerStatusResponse>(jsonResponse, Json.Options);

                if (response == null || !response.Online)
                {
                    return new ServerStatusResult
                    {
                        IsOnline = false,
                        CurrentPlayers = 0,
                        MaxPlayers = 0,
                        Motd = "No Response from the server, but the server may be online",
                        IconData = ""
                    };
                }

                return new ServerStatusResult
                {
                    IsOnline = true,
                    CurrentPlayers = response.Players?.Online ?? 0,
                    MaxPlayers = response.Players?.Max ?? 0,
                    Motd = response.Motd?.Clean ?? "Minecraft Server",
                    IconData = response.Icon ?? ""
                };
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"[API HTTP Error] {address}:{port} -> {httpEx.Message}");
                return new ServerStatusResult { IsOnline = false, CurrentPlayers = 0, MaxPlayers = 0, Motd = $"Network Error: {httpEx.Message}", IconData = "" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API General Error] {address}:{port} -> {ex.GetType().Name}: {ex.Message}");
                return new ServerStatusResult { IsOnline = false, CurrentPlayers = 0, MaxPlayers = 0, Motd = $"API Check Failed: {ex.GetType().Name}", IconData = "" };
            }
        }
    }
}

public class ServerStatusResult
{
    public bool IsOnline { get; set; }
    public int CurrentPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public string Motd { get; set; }
    public string IconData { get; set; } // Keep this, as MineStat returns it
}



// Add this class to your project
public static class UiUpdateService
{
    private static readonly SemaphoreSlim _uiUpdateLock = new(1, 1);
    private static readonly HashSet<string> _activeStatusUpdates = new();

    public static async Task SafeUpdateServerStatusUI(
        ServerInfo server,
        Action<ServerInfo, Action> uiUpdateAction,
        ILogger? logger = null)
    {
        string updateId = $"{server.Address}:{server.Port}:{DateTime.Now.Ticks}";

        try
        {
            await _uiUpdateLock.WaitAsync();
            _activeStatusUpdates.Add(updateId);

            try
            {
                uiUpdateAction(server, () => { }); // Execute the UI update
            }
            catch (Exception ex)
            {
                // Log but don't rethrow - this is where Avalonia might be triggering the ding
                logger?.LogError($"UI update failed for {server.Name}: {ex.Message}");
                Console.WriteLine($"[UI UPDATE FAILED] {server.Name}: {ex.Message}");
            }
        }
        finally
        {
            _activeStatusUpdates.Remove(updateId);
            _uiUpdateLock.Release();
        }
    }

    public static bool IsUpdateInProgress(string address)
    {
        return _activeStatusUpdates.Any(u => u.StartsWith(address));
    }
}

public static class StreamExtensions
{
    public static async Task<int> ReadByteAsync(this Stream stream)
    {
        var buffer = new byte[1];
        await stream.ReadExactlyAsync(buffer, 0, 1);
        return buffer[0];
    }
}

public class ModrinthPack
{
    public int FormatVersion { get; set; } // Changed from string to int
    public string Game { get; set; }
    public string VersionId { get; set; }
    public string Name { get; set; }
    public string? Summary { get; set; }
    public List<ModrinthPackFile> Files { get; set; }
    public Dictionary<string, string> Dependencies { get; set; } // Changed from List<string>
}

public class ModrinthPackFile
{
    public string Path { get; set; }
    public Dictionary<string, string> Hashes { get; set; }
    public List<string> Downloads { get; set; }
    public long FileSize { get; set; }
    public string FileType { get; set; }
}

public class NotificationWindow : Window
{
    private static NotificationWindow? _currentBanner;
    private readonly Border _card;
    private readonly TranslateTransform _transform;

    public NotificationWindow(string title, string message, string? buttonText, Action? buttonAction, bool isError)
    {
        // 1. Window Properties
        SystemDecorations = SystemDecorations.None;
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        CanResize = false;
        SizeToContent = SizeToContent.WidthAndHeight;
        Title = "Notification";

        // 2. UI Structure
        _transform = new TranslateTransform { Y = -150 }; // Start off-screen

        _card = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#1E1E1E")),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16),
            Margin = new Thickness(10),
            BoxShadow = new BoxShadows(new BoxShadow { Blur = 15, Color = Color.Parse("#80000000"), OffsetY = 4 }),
            RenderTransform = _transform,
            Width = 400
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto")
        };

        // Icon
        var iconBorder = new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(isError ? Color.Parse("#33FF0000") : Color.Parse("#33FFFFFF")),
            Margin = new Thickness(0, 0, 15, 0)
        };

        // You can load an actual image here if you want, using a placeholder for now
        var iconText = new TextBlock
        {
            Text = isError ? "⚠️" : "ℹ️",
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            FontSize = 20
        };
        iconBorder.Child = iconText;

        // Text Content
        var textStack = new StackPanel { VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Spacing = 2 };
        textStack.Children.Add(new TextBlock { Text = title, FontWeight = FontWeight.Bold, Foreground = Brushes.White, FontSize = 14 });
        textStack.Children.Add(new TextBlock { Text = message, Foreground = new SolidColorBrush(Color.Parse("#CCCCCC")), FontSize = 12, TextWrapping = TextWrapping.Wrap });

        // Action Button
        Button? actionBtn = null;
        if (!string.IsNullOrEmpty(buttonText))
        {
            actionBtn = new Button
            {
                Content = buttonText,
                Background = new SolidColorBrush(Color.Parse(isError ? "#D32F2F" : "#2E7D32")), // Red if error, Green if normal
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 6),
                Margin = new Thickness(15, 0, 0, 0),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            actionBtn.Click += (_, __) => { buttonAction?.Invoke(); CloseBanner(); };
        }

        // Assemble Grid
        Grid.SetColumn(iconBorder, 0);
        grid.Children.Add(iconBorder);

        Grid.SetColumn(textStack, 1);
        grid.Children.Add(textStack);

        if (actionBtn != null)
        {
            Grid.SetColumn(actionBtn, 2);
            grid.Children.Add(actionBtn);
        }

        _card.Child = grid;
        Content = _card;

        // Close on click
        this.PointerPressed += (_, __) => CloseBanner();
    }

    public static void Show(string title, string message, string? buttonText = null, Action? buttonAction = null, bool isError = false)
    {
        // Close existing banner if open
        if (_currentBanner != null)
        {
            try { _currentBanner.Close(); } catch { }
        }

        Dispatcher.UIThread.Post(async () =>
        {
            var window = new NotificationWindow(title, message, buttonText, buttonAction, isError);
            _currentBanner = window;
            Screen? primaryScreen = null;

            // Try getting screen from the new window (might be null before Show)
            if (window.Screens != null)
                primaryScreen = window.Screens.Primary;

            // Fallback to Main Window if new window doesn't have screen info yet
            if (primaryScreen == null && Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                primaryScreen = desktop.MainWindow?.Screens.Primary;
            }

            // Position at Top Center of Screen
            if (primaryScreen != null)
            {
                var workingArea = primaryScreen.WorkingArea;
                // Center X: Screen X + (Screen Width / 2) - (Window Width / 2)
                int x = workingArea.X + (workingArea.Width / 2) - 200; // Window width is 400
                int y = workingArea.Y + 20; // Top padding
                window.Position = new PixelPoint(x, y);
            }

            window.Show();
            await window.AnimateIn();
        });
    }

    private async Task AnimateIn()
    {
        // Slide Down: -150 to 0
        for (int i = 0; i <= 30; i++)
        {
            double t = i / 30.0;
            double eased = 1 - Math.Pow(1 - t, 3); // Cubic Ease Out
            _transform.Y = -150 + (150 * eased);
            await Task.Delay(10);
        }

        // Wait 4 seconds
        await Task.Delay(4000);

        // Slide Up
        CloseBanner();
    }

    private async void CloseBanner()
    {
        // Slide Up: 0 to -150
        for (int i = 0; i <= 20; i++)
        {
            double t = i / 20.0;
            double eased = t * t; // Quad Ease In
            _transform.Y = 0 - (150 * eased);
            await Task.Delay(10);
        }

        Close();
        if (_currentBanner == this) _currentBanner = null;
    }
}
