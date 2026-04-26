#pragma warning disable CS0618, CS8600, CS8601, CS8602, CS8603, CS8604, CS8618, CS8625, CS4014, CS0162, CS0219, CS0168, CS0169, CS0649, CS0414
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Transformation;
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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using XboxAuthNet.Game.Msal;
using static LeafClient.Models.LauncherSettings;


namespace LeafClient.Views
{
    public partial class MainWindow : Window, LeafClient.Services.IMainWindowHost
    {

        #region Defs/Vars

        private Border? _selectionIndicator;
        private Border? _settingsIndicator;
        private int _currentSelectedIndex = 0;
        private Image? _launchBgImage;
        private ScaleTransform? _launchBgScale;
        private TranslateTransform? _launchBgTranslate;
        private double _parallaxTargetX = 0;
        private double _parallaxTargetY = 0;
        private double _parallaxCurrentX = 0;
        private double _parallaxCurrentY = 0;
        private CancellationTokenSource? _parallaxCts;
        private CancellationTokenSource? _launchBgCts;
        private CancellationTokenSource? _newsItem1Cts;
        private CancellationTokenSource? _newsItem2Cts;
        private CancellationTokenSource? _zoomCts;
        private Canvas? _particleLayer;
        private Border? _launchSection;
        private CancellationTokenSource? _particleCts;
        private DateTime _lastPoseUpdateTime = DateTime.MinValue;
        private CancellationTokenSource? _accountPanelCloseCts;
        private readonly Random _rand = new();
        private readonly List<Particle> _particles = new();
        private Grid? _accountPanelOverlay;
        private StackPanel? _accountsListPanel;
        private TextBlock? _accountTypeLabel;
        private Avalonia.Controls.Shapes.Ellipse? _accountOnlineDot;
        private Border? _accountPanel;
        // Cache of player-head bitmaps keyed by UUID (null = failed/offline)
        private readonly Dictionary<string, Bitmap?> _skinHeadCache = new();
        private TextBlock? _accountUsernameDisplay;
        private TextBlock? _accountUuidDisplay;
        private TextBlock? _playingAsUsername;
        private Image? _newsItem1Image;
        private ScaleTransform? _newsItem1Scale;
        private Image? _newsItem2Image;
        private ScaleTransform? _newsItem2Scale;

        // MAJOR UPDATE badge on NewsItem1 — holds an animated purple gradient
        // whose stop colors are rewritten each frame by StartMajorUpdateBadgeAnimation.
        private Border? _majorUpdateBadge;
        private CancellationTokenSource? _majorUpdateBadgeCts;
        private Grid? _gamePage;
        private Grid? _versionsPage;
        private Grid? _serversPage;
        private Grid? _modsPage;
        private Grid? _settingsPage;
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
        private SettingsService _settingsService = new SettingsService();
        private SessionService _sessionService = new SessionService();
        private SuggestionsService? _suggestionsService;
        private LauncherSettings _currentSettings = new LauncherSettings();
        private List<VersionInfo> _allVersions = new List<VersionInfo>();
        private ToggleSwitch? _launchOnStartupToggle;
        private ToggleSwitch? _minimizeToTrayToggle;
        private ToggleSwitch? _discordRichPresenceToggle;
        private Slider? _minRamSlider;
        private TextBlock? _minRamValueText;
        private Slider? _maxRamSlider;
        private TextBlock? _maxRamValueText;
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
        private readonly SkinRenderService? _skinRenderService;
        private GameOptionsService _optionsService = new GameOptionsService();
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
        private DateTime? _sessionStartUtc; // set when game launches, consumed on exit for playtime tracking
        private bool _isLaunching = false;
        private bool _leafModExpected = false;   // true when launching a version with IsLeafClientModSupported
        private bool _leafModLoaded = false;     // set true when we detect the mod initialized in game output
        private bool _autoRestartAttempted = false; // prevent infinite restart loops
        private bool _userTerminatedGame = false;   // set true when user intentionally kills the game
        private string? _lastLaunchVersion = null;
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

        private static string GetLeafOfflineJarPath(string mcVersion)
        {
            string dir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ".leafclient", "offline", mcVersion);
            Directory.CreateDirectory(dir);
            return System.IO.Path.Combine(dir, "leaf.jar");
        }


        private StackPanel? _launchProgressPanel;
        private ProgressBar? _launchProgressBar;
        private TextBlock? _launchProgressText;

        private Button? _addonFabricButton;
        private Button? _addonVanillaButton;
        private TextBlock? _versionOptiFineSupport;

        private Border? _sidebarHoverTooltip;
        private TextBlock? _sidebarHoverTooltipText;
        private CancellationTokenSource? _tooltipHideCts;
        private IList<ITransition>? _savedTooltipTransitions;
        private bool _tooltipHasShown = false;
        private int? _currentHoverIndex;
        private const double TooltipLeft = 86;
        private const double GapRightOfSidebar = 6;    
        private const double TooltipHeight = 26;
        private const double SidebarWidth = 90;

        private Grid? _skinsPage;
        private LeafClient.Views.Pages.CosmeticsPageView? _cosmeticsPage;
        private LeafClient.Views.Pages.ScreenshotsPageView? _screenshotsPage;
        private LeafClient.Views.Pages.ResourcePacksPageView? _resourcePacksPage;
        private LeafClient.Controls.SkinRendererControl? _skinRenderer;

        private System.Collections.Generic.HashSet<string> _ownedCosmeticIds = new();
        private static readonly string OwnedJsonPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LeafClient", "owned.json");
        private WrapPanel? _skinsWrapPanel;
        private Border? _noSkinsMessage;
        private Border? _currentlySelectedSkinCard;

        private Border? _skinStatusBanner;
        private TextBlock? _skinStatusBannerText;
        private Button? _skinStatusBannerButton;
        private bool _isProgrammaticallySelectingSkin = false;

        private TrayIcon? _trayIcon;
        private bool _isExitingApp = false;

        private TextBlock? _modsInfoText;

        private readonly SemaphoreSlim _installGate = new(1, 1);
        private volatile bool _isInstalling = false;
        private string _currentOperationText = "LAUNCH GAME";
        private string _currentOperationColor = "SeaGreen";
        private Color _launchButtonGlowColor = Color.FromRgb(50, 205, 50);
        private bool _launchButtonHovered;
        private CancellationTokenSource? _launchCancellationTokenSource;

        private Border? _launchErrorBanner;
        private TextBlock? _launchErrorBannerText;
        private Button? _launchErrorBannerCloseButton;

        private Border? _settingsSaveBanner;
        private Button? _settingsSaveBannerSaveButton;
        private Button? _settingsSaveBannerCancelButton;
        private bool _isApplyingSettings = false;
        private bool _settingsDirty = false;
        private string? _settingsSnapshotJson = null; // JSON snapshot for Cancel revert

        private Border? _quickPlayTooltip;
        private TextBlock? _quickPlayTooltipText;
        private CancellationTokenSource? _quickPlayTooltipHideCts;
        private IList<ITransition>? _savedQuickPlayTooltipTransitions;
        private bool _quickPlayTooltipHasShown = false;

        private MinecraftServerChecker _serverChecker = new MinecraftServerChecker();
        private DispatcherTimer? _serverStatusRefreshTimer;
        private bool _serversLoaded = false;

        private StackPanel? _serversWrapPanel;
        private StackPanel? _featuredServersPanel;
        private Border? _noServersMessage;
        private StackPanel? _quickPlayServersContainer;

        private Grid? _addServerOverlay;
        private TextBox? _addServerNameBox;
        private TextBox? _addServerAddressBox;
        private TextBox? _addServerPortBox;
        private TextBlock? _addServerStatusText;
        private Button? _addServerSaveButton;

        private static readonly List<ServerInfo> _featuredServers = new()
        {
            new ServerInfo { Id = "featured_hypixel",   Name = "Hypixel",   Address = "mc.hypixel.net",     Port = 25565 },
            new ServerInfo { Id = "featured_2b2t",      Name = "2b2t",      Address = "2b2t.org",           Port = 25565 },
            new ServerInfo { Id = "featured_pvplegacy", Name = "PvPLegacy", Address = "play.pvplegacy.net", Port = 25565 }
        };

        // Profiles page
        private StackPanel? _profilesListPanel;
        private Border? _noProfilesMessage;

        // Profile Editor Overlay controls
        private Grid? _profileEditorOverlay;
        private Grid? _mainContentGrid;
        private TextBlock? _po_TitleText;
        private TextBlock? _po_AvatarLetter;
        private Button? _po_NavBtnGeneral;
        private Button? _po_NavBtnPerformance;
        private Button? _po_NavBtnMods;
        private Button? _po_NavBtnAdvanced;
        private Border? _po_NavIndicator;
        private Border? _po_NavActiveBar;
        private StackPanel? _po_TabIdentity;
        private StackPanel? _po_TabPerformance;
        private StackPanel? _po_TabMods;
        private StackPanel? _po_TabAdvanced;

        // Advanced-tab controls (profile override fields)
        private TextBox? _po_DescriptionBox;
        private TextBox? _po_IconEmojiBox;
        private TextBox? _po_JvmArgsBox;
        private ToggleSwitch? _po_UseCustomResolutionToggle;
        private TextBox? _po_ResWidthBox;
        private TextBox? _po_ResHeightBox;
        private TextBox? _po_QuickJoinAddressBox;
        private TextBox? _po_QuickJoinPortBox;
        private TextBlock? _po_StatLaunches;
        private TextBlock? _po_StatPlaytime;
        private TextBlock? _po_StatLastUsed;
        private TextBox? _po_NameBox;
        private ComboBox? _po_AccountCombo;
        private ComboBox? _po_VersionCombo;
        private ComboBox? _po_JavaCombo;
        private CheckBox? _po_ShowAllVersions;
        private Border? _po_SupportBanner;
        private Ellipse? _po_SupportDot;
        private TextBlock? _po_SupportTitle;
        private TextBlock? _po_SupportSub;
        private TextBlock? _po_JavaBannerText;
        private Slider? _po_MemSlider;
        private TextBlock? _po_MemLabel;
        private Border? _po_ModsSupportBanner;
        private TextBlock? _po_ModsSupportText;
        private Border? _po_PresetBalanced;
        private Border? _po_PresetEnhanced;
        private Border? _po_PresetLite;
        private WrapPanel? _po_BadgesBalanced;
        private WrapPanel? _po_BadgesEnhanced;
        private WrapPanel? _po_BadgesLite;

        private int _po_ActiveTab = 0;
        private string _po_SelectedPreset = "balanced";
        private string _po_SelectedVersion = "";
        private bool _po_ShowAllVersionsFlag = false;
        private LauncherProfile? _po_EditingProfile;

        private static readonly string[] _po_RecommendedVersions =
        {
            "1.21.11", "1.21.10", "1.21.9", "1.21.8", "1.21.7", "1.21.6", "1.21.5", "1.21.4",
            "1.21.3", "1.21.2", "1.21.1", "1.21", "1.20.2", "1.20.1"
        };
        // Side nav: 54px height + 8px spacing = 62px per nav item

        private static readonly Dictionary<string, string[]> _po_PresetMods = new()
        {
            ["balanced"] = new[] { "Fabric API", "Sodium", "Lithium", "Phosphor" },
            ["enhanced"] = new[] { "Fabric API", "Sodium", "Lithium", "Iris Shaders", "Dynamic Lights", "Better Fps" },
            ["lite"]     = new[] { "Fabric API", "Sodium", "Lithium" }
        };

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

        private int _navigationToken = 0;

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

        private Grid? _checkoutOverlay;
        private Border? _checkoutPanel;
        private bool _isCheckoutAnimating = false;
        private CancellationTokenSource? _checkoutPollCts;
        private HashSet<string>? _checkoutPreOwnedIds;
        private bool _checkoutPurchaseDetected;

        private Grid? _purchasePopupOverlay;
        private Border? _purchasePopupPanel;
        private bool _isPurchasePopupAnimating;
        private string? _pendingPurchaseItemId;
        private int _currentCoinBalance;

        private string _logFolderPath = "";
        private string _logFilePath = "";
        private static StreamWriter? _logStreamWriter;
        private static TextWriter? _originalConsoleOut; 
        private static TextWriter? _originalConsoleError; 

        private Grid? _commonQuestionsOverlay;
        private Border? _commonQuestionsPanel;

        private static readonly HttpClient _httpClient = new HttpClient();

        private Version GetCurrentAppVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version ?? new Version("0.0.0.0");
        }

        private MojangApiService? _mojangApiService;

        private ToggleSwitch? _optiFineToggle;
        private ToggleSwitch? _testModeToggle;
        private TextBox? _testModePathBox;
        private Border? _testModePathPanel;


        private Grid? _modBrowserOverlay;
        private Border? _modBrowserPanel;
        private Border? _modBrowserBackdrop;
        private WrapPanel? _modsResultsPanel;
        private ItemsControl? _modsResultsGrid;
        private ItemsControl? _modsSkeletonPanel;
        private StackPanel? _modsLoadingPanel;
        private StackPanel? _modsEmptyPanel;
        private Button? _modsLoadMoreBtn;
        private TextBlock? _modsResultCount;
        private Border? _modsBrowserVersionNotice;
        private TextBlock? _modsBrowserVersionNoticeText;
        private TextBox? _modSearchBox;
        private Button? _modSearchClearBtn;
        private ComboBox? _modVersionDropdown;
        private ComboBox? _modSortDropdown;
        private StackPanel? _modCategorySidebar;
        private Border? _modLoaderAllChip;
        private Border? _modLoaderFabricChip;
        private Border? _modLoaderForgeChip;
        private Border? _modLoaderQuiltChip;
        private Border? _modDetailsSidebar;
        private Image? _modDetailsIcon;
        private TextBlock? _modDetailsTitle;
        private TextBlock? _modDetailsDescription;
        private TextBlock? _modDetailsStats;
        private Button? _modDetailsDownloadButton;
        private ModrinthProject? _selectedMod;
        private string _selectedLoader = "all";
        private string? _selectedCategory;
        private string _selectedSort = "downloads";
        private string? _selectedMcVersion;
        private int _modOffset;
        private int _modTotalHits;
        private string _currentModQuery = "";
        private static readonly HashSet<string> BundledModIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "AANobbMI",
            "gvQqBUqZ",
            "uXXizFIs",
            "oftqHBKC",
            "NNAgCjsB",
            "nmDcB62a",
        };

        private static readonly (string Id, string Label, string Icon)[] ModCategories = new[]
        {
            ("adventure", "Adventure", "🗺️"),
            ("cursed", "Cursed", "😈"),
            ("decoration", "Decoration", "🎨"),
            ("economy", "Economy", "💰"),
            ("equipment", "Equipment", "⚔️"),
            ("food", "Food", "🍖"),
            ("game-mechanics", "Game Mechanics", "⚙️"),
            ("library", "Library", "📚"),
            ("magic", "Magic", "✨"),
            ("management", "Management", "📋"),
            ("minigame", "Minigame", "🎮"),
            ("mobs", "Mobs", "🐺"),
            ("optimization", "Optimization", "⚡"),
            ("social", "Social", "💬"),
            ("storage", "Storage", "📦"),
            ("technology", "Technology", "🔧"),
            ("transportation", "Transportation", "🚂"),
            ("utility", "Utility", "🛠️"),
            ("worldgen", "World Gen", "🌍"),
        };
        private StackPanel? _userModsPanel;
        private Border? _noUserModsMessage;
        private CancellationTokenSource? _modSearchCts;
        private readonly HttpClient _modrinthClient = new HttpClient();

        private OnlineCountService? _onlineCountService;
        private TextBlock? _onlineCountTextBlock;
        private Ellipse? _onlineStatusDot;
        private TextBlock? _motdTextBlock;
        private Border? _motdBanner;
        private TextBlock? _leafsBalanceText;
        private Border? _leafsCurrencyWidget;

        private DispatcherTimer? _networkMonitorTimer;
        private Button? _cancelLaunchButton;
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

        private bool _isAboutLeafClientAnimating = false;
        private bool _isCommonQuestionsAnimating = false;

        // Launch animation overlay (portal / energy-ring animation)
        private Border? _launchAnimOverlay;
        private TextBlock? _launchAnimText;
        private TextBlock? _launchAnimSubText;
        private DispatcherTimer? _launchAnimTimer;
        private DispatcherTimer? _launchAnimTextTimer;
        private Canvas? _blockCanvas;
        private Ellipse? _centerOrb;
        private double _animTime      = 0;
        private bool   _animFadingOut = false;
        private double _animFadeTime  = 0;

        // Launch animation — block assembly scene
        private struct BlockState
        {
            public double X, Y, TargetX, TargetY, StartX, StartY;
            public double Progress, Speed, Delay;        // Progress 0→1, eased
            public double BobPhase, BobSpeed, BobAmp;   // idle bob after arrival
            public double Size;                          // isometric half-size
            public string BlockType;                     // "grass"|"dirt"|"wood"|"stone"
            public bool Arrived;
        }
        private struct SparkleState
        {
            public double X, Y, Vx, Vy, Life, MaxLife, Size, Angle, AngularV;
        }
        private readonly List<BlockState>   _blockStates   = new();
        private readonly List<(Polygon Top, Polygon Left, Polygon Right)> _blockControls = new();
        private readonly List<SparkleState> _sparkleStates = new();
        private readonly List<Rectangle>    _sparkleControls = new();
        // Legacy ring/particle lists kept to satisfy any lingering references (empty at runtime)
        private readonly List<Ellipse>      _ringEllipses     = new();
        private readonly List<Control>      _particleControls = new();

        // Leaf Vortex animation — orbiting particles
        private struct OrbiterState
        {
            public double Angle, Speed, Rx, Ry;
            public double GlowPhase, GlowSpeed;
        }
        private readonly List<OrbiterState> _orbiterStates   = new();
        private readonly List<Ellipse>      _orbiterControls = new();
        private readonly List<Ellipse>      _orbitPathRings  = new();

        private TextBox? _gameResolutionWidthTextBox;
        private TextBox? _gameResolutionHeightTextBox;
        private Button? _selectResolutionPresetButton;
        private Button? _visualiseResolutionButton;
        private ToggleSwitch? _useCustomGameResolutionToggle;
        private ToggleSwitch? _lockGameAspectRatioToggle;

        private RadioButton? _launcherVisibilityKeepOpenRadio;
        private RadioButton? _launcherVisibilityHideRadio;

        private RadioButton? _updateDeliveryNormalRadio;
        private RadioButton? _updateDeliveryEarlyRadio;
        private RadioButton? _updateDeliveryLateRadio;

        private ToggleSwitch? _showUsernameInDiscordRichPresenceToggle;

        private RadioButton? _closingNotificationsAlwaysRadio;
        private RadioButton? _closingNotificationsJustOnceRadio;
        private RadioButton? _closingNotificationsNeverRadio;
        private ToggleSwitch? _enableUpdateNotificationsToggle;
        private ToggleSwitch? _enableNewContentIndicatorsToggle;

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
        private bool _isFeedbackAnimating = false;
        private string _feedbackLogFolderPath = "";
        private string? _attachedLogFileContent;

        // Crash report overlay
        private Grid? _crashReportOverlay;
        private Border? _crashReportPanel;
        private TextBlock? _crashScreenshotLine;
        private TextBlock? _crashStatusLine;
        private Button? _crashSendButton;
        private Button? _crashDismissButton;
        private bool _isCrashAnimating = false;
        private Exception? _currentCrashException;
        private byte[]? _currentScreenshotBytes;

        private ModCleanupService? _modCleanupService;

        private Grid? _startupOverlay;
        private Image? _startupBg;
        private Image? _startupLogo;
        private Image? _startupLogoText;

        private Grid? _updateCheckOverlay;
        private Canvas? _updateSpinner;
        private CancellationTokenSource? _updateSpinnerCts;
        private double _progressBarMaxWidth;

        private ToggleSwitch? _natureThemeToggle;
        private TextBlock? _gameWindowPendingText;

        private Button? _adButton;
        private Image? _adImage;
        private TextBlock? _adLoadingText;
        private AAdsAdResponse? _currentAdData;

        private const string AAdsAdUnitId = "2423964";
        private const string AAdsApiBaseUrl = "https://a-ads.com/ads/json/";

        private GameOutputWindow? _gameOutputWindow;

        private readonly List<LeafParticle> _leaves = new();
        private DispatcherTimer? _leafTimer;
        private readonly Random _leafRand = new();
        public class LeafParticle
        {
            public Avalonia.Controls.Shapes.Path Shape { get; set; } = new();
            public double X { get; set; }
            public double Y { get; set; }
            public double SpeedY { get; set; }
            public double SwayAmplitude { get; set; }
            public double SwayFrequency { get; set; }
            public double SwayPhase { get; set; }
            public double Rotation { get; set; }
            public double RotationSpeed { get; set; }
        }


        #endregion

        #region IMainWindowHost

        CmlLib.Core.Auth.MSession? Services.IMainWindowHost.CurrentSession => _session;
        CmlLib.Core.MinecraftLauncher? Services.IMainWindowHost.Launcher => _launcher;
        string Services.IMainWindowHost.MinecraftFolder => _minecraftFolder;
        Models.LauncherSettings Services.IMainWindowHost.CurrentSettings => _currentSettings;
        Services.SettingsService Services.IMainWindowHost.SettingsService => _settingsService;

        void Services.IMainWindowHost.SwitchToPage(int index) => SwitchToPage(index);

        async Task<byte[]?> Services.IMainWindowHost.FetchSkinBytesAsync() => await FetchSkinBytesAsync();

        bool Services.IMainWindowHost.IsCosmeticEquipped(string cosmeticId, string category) => IsCosmeticEquipped(cosmeticId, category);

        string? Services.IMainWindowHost.LeafIdentifier => DecodeJwtMinecraftUsername(_currentSettings?.LeafApiJwt);

        int Services.IMainWindowHost.CoinBalance => _currentCoinBalance;

        void Services.IMainWindowHost.OpenCheckout(string url) => OpenCheckout(url);

        void Services.IMainWindowHost.ShowPurchaseChoice(string itemId, string itemName, string preview, string rarity, string priceText, int coinPrice, string checkoutUrl)
            => ShowPurchaseChoice(itemId, itemName, preview, rarity, priceText, coinPrice, checkoutUrl);

        bool Services.IMainWindowHost.IsOwned(string cosmeticId) => _ownedCosmeticIds.Contains(cosmeticId);

        void Services.IMainWindowHost.AddOwnedCosmetic(string cosmeticId)
        {
            _ownedCosmeticIds.Add(cosmeticId);
            SaveOwnedJson();
            _cosmeticsPage?.RefreshOwnedList(_ownedCosmeticIds);

            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    var jwt = await EnsureLeafJwtAsync();
                    if (string.IsNullOrEmpty(jwt)) return;
                    await LeafApiService.PurchaseCosmeticWithCoinsAsync(jwt, cosmeticId);
                }
                catch { }
                finally
                {
                    await SyncOwnedCosmeticsFromApiAsync();
                }
            });
        }

        void Services.IMainWindowHost.ShowPurchaseCelebration(string id, string name, string preview, string rarity)
            => ShowPurchaseCelebration(id, name, preview, rarity);

        void Services.IMainWindowHost.ShowMonthlyPassPopup() => ShowMonthlyPassPopup();

        void Services.IMainWindowHost.RefreshLeafPlusPrices() => RefreshLeafPlusPrices();

        void Services.IMainWindowHost.UpdateCoinBalance(int newBalance)
        {
            _currentCoinBalance = newBalance;
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _leafsBalanceText ??= this.FindControl<TextBlock>("LeafsBalanceText");
                if (_leafsBalanceText != null)
                    _leafsBalanceText.Text = newBalance.ToString("N0");
            });
        }

        private void OnToastRequested(string message, ToastType type)
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => ShowToast(message, type));
        }

        private async void ShowToast(string message, ToastType type)
        {
            var container = this.FindControl<Avalonia.Controls.ItemsControl>("ToastContainer");
            if (container == null) return;

            // Pick accent colour
            var bg = type switch
            {
                ToastType.Success => Avalonia.Media.Color.Parse("#166534"),
                ToastType.Error   => Avalonia.Media.Color.Parse("#7F1D1D"),
                _                 => Avalonia.Media.Color.Parse("#1C2A38"),
            };
            var border = type switch
            {
                ToastType.Success => Avalonia.Media.Color.Parse("#22C55E"),
                ToastType.Error   => Avalonia.Media.Color.Parse("#EF4444"),
                _                 => Avalonia.Media.Color.Parse("#3B82F6"),
            };

            var toast = new Avalonia.Controls.Border
            {
                Background       = new Avalonia.Media.SolidColorBrush(bg),
                BorderBrush      = new Avalonia.Media.SolidColorBrush(border),
                BorderThickness  = new Avalonia.Thickness(1),
                CornerRadius     = new Avalonia.CornerRadius(10),
                Padding          = new Avalonia.Thickness(16, 10),
                MaxWidth         = 320,
                Opacity          = 0,
                Child = BuildToastContent(message, type)
            };

            // Add to container
            var items = (container.ItemsSource as System.Collections.ObjectModel.ObservableCollection<Avalonia.Controls.Control>);
            if (items == null)
            {
                items = new System.Collections.ObjectModel.ObservableCollection<Avalonia.Controls.Control>();
                container.ItemsSource = items;
            }
            items.Add(toast);

            // Fade in
            var fadeIn = new Avalonia.Animation.Animation
            {
                Duration = TimeSpan.FromMilliseconds(200),
                Children =
                {
                    new Avalonia.Animation.KeyFrame { Cue = new Avalonia.Animation.Cue(0d), Setters = { new Avalonia.Styling.Setter(Avalonia.Controls.Control.OpacityProperty, 0d) } },
                    new Avalonia.Animation.KeyFrame { Cue = new Avalonia.Animation.Cue(1d), Setters = { new Avalonia.Styling.Setter(Avalonia.Controls.Control.OpacityProperty, 1d) } },
                }
            };
            await fadeIn.RunAsync(toast);
            toast.Opacity = 1;

            // Hold
            await Task.Delay(3000);

            // Fade out
            var fadeOut = new Avalonia.Animation.Animation
            {
                Duration = TimeSpan.FromMilliseconds(300),
                Children =
                {
                    new Avalonia.Animation.KeyFrame { Cue = new Avalonia.Animation.Cue(0d), Setters = { new Avalonia.Styling.Setter(Avalonia.Controls.Control.OpacityProperty, 1d) } },
                    new Avalonia.Animation.KeyFrame { Cue = new Avalonia.Animation.Cue(1d), Setters = { new Avalonia.Styling.Setter(Avalonia.Controls.Control.OpacityProperty, 0d) } },
                }
            };
            await fadeOut.RunAsync(toast);
            items.Remove(toast);
        }

        private static Avalonia.Controls.Control BuildToastContent(string message, ToastType type)
        {
            var parts = message.Split('\n', 2);
            var primary = parts[0];
            var detail = parts.Length > 1 ? parts[1] : null;

            var icon = type switch
            {
                ToastType.Success => "✓",
                ToastType.Error   => "⚠",
                _                 => "ℹ"
            };

            var iconBlock = new Avalonia.Controls.TextBlock
            {
                Text = icon,
                Foreground = Avalonia.Media.Brushes.White,
                FontSize = 18,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                Margin = new Avalonia.Thickness(0, 0, 10, 0)
            };

            var textPanel = new Avalonia.Controls.StackPanel { Spacing = 2 };

            textPanel.Children.Add(new Avalonia.Controls.TextBlock
            {
                Text = primary,
                Foreground = Avalonia.Media.Brushes.White,
                FontSize = 13,
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            });

            if (detail != null)
            {
                textPanel.Children.Add(new Avalonia.Controls.TextBlock
                {
                    Text = detail,
                    Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(180, 255, 255, 255)),
                    FontSize = 11,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                });
            }

            var row = new Avalonia.Controls.StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
            };
            row.Children.Add(iconBlock);
            row.Children.Add(textPanel);
            return row;
        }

        private void RefreshLeafPlusPrices()
        {
            var monthly   = Services.CurrencyService.FormatPrice(4.99m);
            var quarterly = Services.CurrencyService.FormatPrice(13.99m);
            var yearly    = Services.CurrencyService.FormatPrice(49.99m);
            var qtlySave  = Services.CurrencyService.FormatPrice(4.99m * 3 - 13.99m);
            var yrlySave  = Services.CurrencyService.FormatPrice(4.99m * 12 - 49.99m);

            var tb = this.FindControl<TextBlock>("LeafPlusMonthlyPrice");
            if (tb != null) tb.Text = monthly;

            tb = this.FindControl<TextBlock>("LeafPlusQuarterlyPrice");
            if (tb != null) tb.Text = quarterly;

            tb = this.FindControl<TextBlock>("LeafPlusQuarterlySave");
            if (tb != null) tb.Text = $"Save {qtlySave}";

            tb = this.FindControl<TextBlock>("LeafPlusYearlyPrice");
            if (tb != null) tb.Text = yearly;

            tb = this.FindControl<TextBlock>("LeafPlusYearlySave");
            if (tb != null) tb.Text = $"Save {yrlySave}";
        }

        private void RefreshPlaytimeStatsCard()
        {
            if (_currentSettings == null) return;

            // Stats moved from the home-page sidebar cards into the compact footer
            // strip, so news banners get their full height back. The footer is
            // always visible, so this also works across every page.
            var playtimeLabel    = this.FindControl<TextBlock>("FooterStatsPlaytime");
            var launchCountLabel = this.FindControl<TextBlock>("FooterStatsLaunches");
            var topVersionLabel  = this.FindControl<TextBlock>("FooterStatsTopVersion");

            if (playtimeLabel != null)
                playtimeLabel.Text = FormatPlaytimeShort(_currentSettings.TotalPlaytimeSeconds);

            if (launchCountLabel != null)
                launchCountLabel.Text = _currentSettings.TotalLaunchCount.ToString();

            if (topVersionLabel != null)
            {
                if (_currentSettings.PlaytimeByVersion != null && _currentSettings.PlaytimeByVersion.Count > 0)
                {
                    var top = _currentSettings.PlaytimeByVersion
                        .OrderByDescending(kv => kv.Value)
                        .First();
                    topVersionLabel.Text = top.Key;
                }
                else
                {
                    topVersionLabel.Text = "—";
                }
            }
        }

        private static string FormatPlaytimeShort(long seconds)
        {
            if (seconds < 60)         return $"{seconds}s";
            if (seconds < 3600)       return $"{seconds / 60}m";
            if (seconds < 86400)      return $"{seconds / 3600}h {(seconds % 3600) / 60}m";
            return $"{seconds / 86400}d {(seconds % 86400) / 3600}h";
        }

        private static string? NullIfBlank(string? s)
            => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        private static int? ParseNullableInt(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            return int.TryParse(s.Trim(), out int v) ? v : (int?)null;
        }

        // Recent Activity card was removed — its content (top version / top server)
        // was merged into the compact footer stats strip, which RefreshPlaytimeStatsCard
        // already populates.

        private async void ShowPurchaseCelebration(string id, string name, string preview, string rarity)
        {
            await RunTntAnimationAsync();
            ShowCelebrationPanel(id, name, preview, rarity);
        }

        #endregion

        public MainWindow()
        {
            try
            {
                if (_logStreamWriter == null)
                {
                    _originalConsoleOut = Console.Out;
                    _originalConsoleError = Console.Error;

                    var appDirectory = AppContext.BaseDirectory;
                    _logFolderPath = System.IO.Path.Combine(appDirectory, "Logs");

                    System.IO.Directory.CreateDirectory(_logFolderPath);

                    // Log rotation: delete launcher logs older than 30 days, and cap the
                    // total count at 50 (keep the most recent). Prevents unbounded growth.
                    try
                    {
                        var logDir = new System.IO.DirectoryInfo(_logFolderPath);
                        var oldLogs = logDir.GetFiles("launcher_log_*.txt");
                        var cutoff = DateTime.Now.AddDays(-30);
                        int deletedByAge = 0;
                        foreach (var f in oldLogs)
                        {
                            if (f.LastWriteTime < cutoff)
                            {
                                try { f.Delete(); deletedByAge++; } catch { }
                            }
                        }
                        // Refresh list after age-based deletion
                        var remaining = logDir.GetFiles("launcher_log_*.txt");
                        if (remaining.Length > 50)
                        {
                            Array.Sort(remaining, (a, b) => a.LastWriteTime.CompareTo(b.LastWriteTime));
                            int toDelete = remaining.Length - 50;
                            for (int i = 0; i < toDelete; i++)
                            {
                                try { remaining[i].Delete(); } catch { }
                            }
                        }
                    }
                    catch { /* best-effort rotation, never block startup */ }

                    var logFileName = $"launcher_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                    _logFilePath = System.IO.Path.Combine(_logFolderPath, logFileName);

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
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] Failed to set up file logging: {ex.Message}");
                Console.SetOut(_originalConsoleOut ?? new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
                Console.SetError(_originalConsoleError ?? new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
            }

            InitializeComponent();

            TutorialService.Instance.StepChanged += OnTutorialStepChanged;

            var tutorialOverlay = this.FindControl<TutorialOverlay>("TutorialOverlayControl");
            tutorialOverlay?.Initialize(this);

            BuildAddServerOverlay();

            var replayBtn = this.FindControl<Button>("ReplayTutorialBtn");
            if (replayBtn != null)
                replayBtn.Click += (_, _) =>
                {
                    TutorialService.Instance.SetCrackedAccount(_currentSettings?.AccountType == "offline");
                    TutorialService.Instance.StartTutorial();
                };

            ToastService.ToastRequested += OnToastRequested;
            InitializeNatureTheme();
            InitializeStartupControls();
            // Shimmer sweep: register once globally so every ColorBtn in the app
            // gets the left→right shine animation on hover, regardless of whether
            // the button was created in AXAML or C#.
            RegisterColorBtnShimmer();

            // Debug keyboard shortcut for testing the crash reporter.
            // Ctrl+Shift+X — throw a fake exception to trigger the crash overlay
            this.KeyDown += OnDebugKeyDown;

            _updateCheckOverlay = this.FindControl<Grid>("UpdateCheckOverlay");
            _updateSpinner = this.FindControl<Canvas>("UpdateSpinner");
            // Card is 380 wide with 36px horizontal padding on each side → 308 usable.
            _progressBarMaxWidth = 308;

            DataContext = new MainWindowViewModel();

            _skinRenderService = new SkinRenderService();
            _mojangApiService = new MojangApiService(_httpClient);
            _settingsService = new SettingsService();
            _suggestionsService = new SuggestionsService(_settingsService);

            _modCleanupService = new ModCleanupService(_minecraftFolder);

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
            InitializeControls();
            InitializeLauncher();
            PopulateAllVersionsData();
            AnimateBannersOnLoad();
            LoadAndApplySettings();
            LoadUserInfoAsync();
            LoadServerData();
            try
            {
                _onlineCountService = new OnlineCountService();
                _onlineCountService.SetTokenRefreshCallback(() => EnsureLeafJwtAsync());
                var mcUsername = _currentSettings?.SessionUsername
                    ?? DecodeJwtMinecraftUsername(_currentSettings?.LeafApiJwt);
                Console.WriteLine($"[OnlineCountService] Starting with username='{mcUsername}' (sessionUsername='{_currentSettings?.SessionUsername}')");
                if (!string.IsNullOrWhiteSpace(mcUsername))
                {
                    _onlineCountService.Start(mcUsername!, _currentSettings?.LeafApiJwt);
                }
                else
                {
                    Console.WriteLine("[OnlineCountService] WARN: No username available, heartbeats disabled");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[MainWindow ERROR] Failed to initialize OnlineCountService: {ex.Message}");
                _onlineCountService = null;
            }

            this.Opened += async (_, __) =>
            {
                try
                {
                    await PlayCinematicStartupAsync();
                    StartRichPresenceIfEnabled();
                    InitializeNatureTheme();

                    await InitializeDefaultServersAsync();

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        LoadServers();
                        RefreshQuickPlayBar();
                    });

                    _ = Task.Run(async () =>
                    {
                        try { await RefreshAllServerStatusesAsync(); } catch (Exception ex) { Console.WriteLine($"[RefreshAllServerStatusesAsync Error] {ex.Message}"); }
                        try { await WarmupServerIconsAsync(); } catch (Exception ex) { Console.WriteLine($"[WarmupServerIconsAsync Error] {ex.Message}"); }
                    });

                    UpdateLaunchButton("LAUNCH GAME", "SeaGreen");

                    if (_currentSettings.IsFirstLaunch)
                    {
                        _currentSettings.RenderDistance = 8;
                        _currentSettings.MaxFps = 0;
                        _currentSettings.VSync = false;
                        _currentSettings.SimulationDistance = 5;

                        var minimap = _currentSettings.InstalledMods.FirstOrDefault(m =>
                            m.Name.Contains("Minimap", StringComparison.OrdinalIgnoreCase) ||
                            m.ModId.Contains("minimap", StringComparison.OrdinalIgnoreCase));

                        if (minimap != null) minimap.Enabled = false;

                        ApplySettingsToUi(_currentSettings);
                        await _settingsService.SaveSettingsAsync(_currentSettings);

                        await Task.Delay(500);
                        TutorialService.Instance.SetCrackedAccount(_currentSettings?.AccountType == "offline");
                        TutorialService.Instance.StartTutorial();

                        _currentSettings.IsFirstLaunch = false;
                        await _settingsService.SaveSettingsAsync(_currentSettings);
                    }

                    await CheckForUpdatesAsync();

                    if (_onlineCountService != null)
                    {
                        var mcUsername = _currentSettings?.SessionUsername
                            ?? DecodeJwtMinecraftUsername(_currentSettings?.LeafApiJwt);
                        Console.WriteLine($"[OnlineCountService] Opened handler: mcUsername='{mcUsername}', hasJwt={!string.IsNullOrEmpty(_currentSettings?.LeafApiJwt)}");
                        if (!string.IsNullOrWhiteSpace(mcUsername))
                        {
                            _onlineCountService.Start(mcUsername!, _currentSettings?.LeafApiJwt);
                        }
                        try { await _onlineCountService.UpdateCount(true); } catch (Exception ex) { Console.WriteLine($"[UpdateCount Error] {ex.Message}"); }
                        try { await UpdateOnlineCountDisplay(); } catch (Exception ex) { Console.WriteLine($"[UpdateOnlineCountDisplay Error] {ex.Message}"); }
                    }

                    try { await UpdateLeafsBalanceAsync(); } catch (Exception ex) { Console.WriteLine($"[UpdateLeafsBalance Error] {ex.Message}"); }
                    _ = SyncOwnedCosmeticsFromApiAsync();

                    _ = Task.Run(async () => { try { await PerformModCleanup(); } catch (Exception ex) { Console.WriteLine($"[PerformModCleanup Error] {ex.Message}"); } });

                    // Pre-warm the cosmetics page in the background so it's instant when the user opens it.
                    // Small delay lets the home page finish rendering first, then we silently load
                    // the skin renderer and populate the cosmetics grid off-screen.
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(1500); // let startup settle
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                try { _cosmeticsPage?.LoadCosmeticsPage(); }
                                catch (Exception ex) { Console.WriteLine($"[CosmeticsPreload] {ex.Message}"); }
                            });
                        }
                        catch (Exception ex) { Console.WriteLine($"[CosmeticsPreload outer] {ex.Message}"); }
                    });
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[MainWindow.Opened Critical Error] An unhandled exception occurred during startup tasks: {ex.Message}");
                    FadeOutUpdateOverlay();
                }
            };

            this.Closing += OnWindowClosing;

            AppDomain.CurrentDomain.FirstChanceException += (sender, e) =>
            {
                if (e.Exception is JsonException jsonEx)
                {
                    Console.WriteLine($"[JSON ERROR] {jsonEx.Message}");
                }
            };

            _networkMonitorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _networkMonitorTimer.Tick += (_, __) => CheckNetworkConnectivity();
            _networkMonitorTimer.Start();
        }


        private void InitializeNatureTheme()
        {
            var canvas = this.FindControl<Canvas>("FallingLeavesCanvas");
            var overlay = this.FindControl<Grid>("NatureOverlay");

            if (!_currentSettings.EnableNatureTheme)
            {
                if (canvas != null) canvas.IsVisible = false;
                if (overlay != null) overlay.IsVisible = false;
                return;
            }

            if (canvas == null) return;

            canvas.IsVisible = true;
            if (overlay != null) overlay.IsVisible = true;

            if (_leafTimer == null)
            {
                _leafTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(16)
                };
                _leafTimer.Tick += UpdateLeaves;
            }

            if (!_leafTimer.IsEnabled)
            {
                _leafTimer.Start();

                if (_leaves.Count == 0)
                {
                    for (int i = 0; i < 20; i++)
                    {
                        SpawnLeaf(canvas, true);
                    }
                }
            }
        }

        private void UpdateNatureThemeState()
        {
            bool enabled = _natureThemeToggle?.IsChecked ?? true;
            _currentSettings.EnableNatureTheme = enabled;

            if (enabled)
            {
                InitializeNatureTheme();
            }
            else
            {
                _leafTimer?.Stop();

                var canvas = this.FindControl<Canvas>("FallingLeavesCanvas");
                if (canvas != null)
                {
                    canvas.Children.Clear();
                    canvas.IsVisible = false;
                }

                var overlay = this.FindControl<Grid>("NatureOverlay");
                if (overlay != null) overlay.IsVisible = false;

                _leaves.Clear();
            }
        }


        private void SpawnLeaf(Canvas canvas, bool randomY = false)
        {
            double size = _leafRand.Next(12, 24);

            // Leaf shape geometry
            var leafShape = new Avalonia.Controls.Shapes.Path
            {
                Data = Avalonia.Media.Geometry.Parse("M 0,0 C 5,10 10,10 10,0 C 10,-10 5,-10 0,0"),
                Width = size,
                Height = size,
                Stretch = Stretch.Uniform,
                Fill = new SolidColorBrush(GetRandomLeafColor()),
                Opacity = 0.7,
                RenderTransform = new TransformGroup
                {
                    Children = new Transforms
                    {
                        new RotateTransform(0),
                        new TranslateTransform(0, 0)
                    }
                }
            };

            // Fix: Check if Bounds are valid (loaded), otherwise use Design default (1400x750)
            // This prevents leaves from bunching up on the left side (X=0) during startup.
            double containerWidth = this.Bounds.Width > 0 ? this.Bounds.Width : 1400;
            double containerHeight = this.Bounds.Height > 0 ? this.Bounds.Height : 750;

            // Random start position
            double startX = _leafRand.NextDouble() * containerWidth;
            double startY = randomY ? _leafRand.NextDouble() * containerHeight : -50;

            var leaf = new LeafParticle
            {
                Shape = leafShape,
                X = startX,
                Y = startY,
                SpeedY = 0.5 + (_leafRand.NextDouble() * 1.0),
                SwayAmplitude = 0.5 + (_leafRand.NextDouble() * 1.0),
                SwayFrequency = 0.02 + (_leafRand.NextDouble() * 0.03),
                SwayPhase = _leafRand.NextDouble() * Math.PI * 2,
                Rotation = _leafRand.Next(0, 360),
                RotationSpeed = (_leafRand.NextDouble() - 0.5) * 3
            };

            _leaves.Add(leaf);
            canvas.Children.Add(leafShape);
        }


        private Color GetRandomLeafColor()
        {
            var colors = new[]
            {
        Color.Parse("#66BB6A"), // Green
        Color.Parse("#9CCC65"), // Light Green
        Color.Parse("#FFCA28"), // Yellow/Orange
        Color.Parse("#8D6E63")  // Brown
    };
            return colors[_leafRand.Next(colors.Length)];
        }

        private void UpdateLeaves(object? sender, EventArgs e)
        {
            var canvas = this.FindControl<Canvas>("FallingLeavesCanvas");
            if (canvas == null || !canvas.IsVisible) return;

            double windowHeight = this.Bounds.Height;

            for (int i = _leaves.Count - 1; i >= 0; i--)
            {
                var leaf = _leaves[i];

                // Physics
                leaf.Y += leaf.SpeedY;
                leaf.SwayPhase += leaf.SwayFrequency;
                double xOffset = Math.Sin(leaf.SwayPhase) * leaf.SwayAmplitude;
                leaf.Rotation += leaf.RotationSpeed;

                // Apply
                if (leaf.Shape.RenderTransform is TransformGroup group)
                {
                    if (group.Children[0] is RotateTransform rotate)
                        rotate.Angle = leaf.Rotation;

                    if (group.Children[1] is TranslateTransform translate)
                        translate.X = xOffset;
                }

                Canvas.SetLeft(leaf.Shape, leaf.X);
                Canvas.SetTop(leaf.Shape, leaf.Y);

                // Remove if off screen
                if (leaf.Y > windowHeight + 50)
                {
                    canvas.Children.Remove(leaf.Shape);
                    _leaves.RemoveAt(i);
                    SpawnLeaf(canvas); // Respawn at top
                }
            }

            // Keep population up
            if (_leaves.Count < 25 && _leafRand.NextDouble() < 0.05)
            {
                SpawnLeaf(canvas);
            }
        }

        private void StartUpdateSpinner()
        {
            if (_updateSpinner == null) return;

            _updateSpinnerCts?.Cancel();
            _updateSpinnerCts = new CancellationTokenSource();
            var token = _updateSpinnerCts.Token;

            // 12-dot chasing-opacity spinner.
            //
            // Dots are stationary (positioned in XAML around a 72x72 ring).
            // Every frame we recompute an opacity for each dot based on its
            // angular distance behind a "lead" position that sweeps around
            // the ring. An exponential decay gives the trail a smooth fade.
            //
            // Period: 1 full chase per second (12 dots × ~83ms each).
            // This works reliably on every Avalonia build because there is
            // NO transform involved — pure opacity changes on fixed shapes.
            Task.Run(async () =>
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                const double periodSec = 1.0;
                const int dotCount = 12;

                while (!token.IsCancellationRequested)
                {
                    double phase = (sw.Elapsed.TotalSeconds / periodSec) % 1.0;
                    double leadFloat = phase * dotCount;

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_updateSpinner == null) return;
                        var children = _updateSpinner.Children;
                        int count = Math.Min(children.Count, dotCount);
                        for (int i = 0; i < count; i++)
                        {
                            if (children[i] is Visual v)
                            {
                                // Angular distance behind the lead (wraps around 0..dotCount).
                                double rawDist = (leadFloat - i + dotCount) % dotCount;
                                // Exponential decay: lead ≈ 1.0, tail asymptotes to 0.15.
                                double opacity = 0.15 + 0.85 * Math.Exp(-rawDist * 0.55);
                                v.Opacity = opacity;
                            }
                        }
                    });
                    await Task.Delay(16, token);
                }
            }, token);
        }

        // ═══ SET TO TRUE TO PREVIEW THE UPDATE UI WITHOUT DOWNLOADING ═══
        private const bool UPDATE_TEST_MODE = false;
        private const string UPDATE_TEST_VERSION = "2.0";

        private async Task CheckForUpdatesAsync()
        {
            // Phase 1: Show "checking" spinner overlay — fade the scrim in and
            // spring the glass card in from scale(0.94). Both transitions are wired
            // up in XAML; we just flip Opacity / RenderTransform here and let
            // Avalonia interpolate on the next render tick.
            ShowUpdateCheckOverlay();
            StartUpdateSpinner();
            Console.WriteLine("[Updater] Checking for updates...");

            // ══════════════════════════════════════════
            //  TEST MODE — simulate the full flow
            // ══════════════════════════════════════════
            if (UPDATE_TEST_MODE)
            {
                Console.WriteLine("[Updater] TEST MODE — simulating update v" + UPDATE_TEST_VERSION);
                await Task.Delay(2000);
                FadeOutUpdateOverlay();

                // Show download overlay
                await ShowUpdateDownloadOverlay(UPDATE_TEST_VERSION);

                // Simulate progress
                for (int i = 0; i <= 100; i += 2)
                {
                    double pct = i / 100.0;
                    await UpdateProgress(pct, i < 80 ? "Downloading update..." : "Extracting files...");
                    await Task.Delay(50);
                }

                await UpdateProgress(1.0, "Update complete! Restarting...");
                await Task.Delay(1500);

                // In test mode, just fade the overlay out
                await Dispatcher.UIThread.InvokeAsync(FadeOutDownloadOverlay);
                Console.WriteLine("[Updater] TEST MODE — would restart here");
                return;
            }

            // ══════════════════════════════════════════
            //  PRODUCTION — mandatory update
            // ══════════════════════════════════════════
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                Console.WriteLine("[Updater] No network. Skipping check.");
                await Task.Delay(1000);
                FadeOutUpdateOverlay();
                return;
            }

            try
            {
                var delayTask = Task.Delay(1500);
                var checkTask = Services.UpdateService.CheckForUpdateAsync();
                await Task.WhenAll(delayTask, checkTask);
                string? newVersion = await checkTask;

                if (newVersion != null)
                {
                    Console.WriteLine($"[Updater] Update v{newVersion} found — mandatory update starting");
                    FadeOutUpdateOverlay();

                    // Show the download overlay (blocks interaction)
                    await ShowUpdateDownloadOverlay(newVersion);

                    // Download with progress
                    bool staged = await Services.UpdateService.DownloadAndStageAsync(
                        onProgress: async (pct) =>
                        {
                            await UpdateProgress(pct, pct < 0.8 ? "Downloading update..." : "Extracting files...");
                        }
                    );

                    if (staged)
                    {
                        await UpdateProgress(1.0, "Update complete! Restarting...");
                        await Task.Delay(1200);
                        Services.UpdateService.RestartToApply();
                    }
                    else
                    {
                        await UpdateProgress(0, "Update failed. Please try again later.");
                        await Task.Delay(3000);
                        await Dispatcher.UIThread.InvokeAsync(FadeOutDownloadOverlay);
                    }
                }
                else
                {
                    Console.WriteLine("[Updater] No updates found.");
                    FadeOutUpdateOverlay();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Updater ERROR] {ex.Message}");
                FadeOutUpdateOverlay();
            }
        }

        // ══════════════════════════════════════════════════════
        //  Update overlay show/hide helpers
        // ══════════════════════════════════════════════════════

        // Show the "checking for updates" overlay with a scrim fade + card spring.
        // XAML has transitions already wired; we just set IsVisible, then push
        // the target values on the next render tick so they animate from the
        // initial state (Opacity=0, scale=0.94) rather than snapping.
        private void ShowUpdateCheckOverlay()
        {
            if (_updateCheckOverlay == null) return;

            var card = this.FindControl<Border>("UpdateCheckCard");

            if (!AreAnimationsEnabled())
            {
                _updateCheckOverlay.Opacity = 1;
                _updateCheckOverlay.IsVisible = true;
                if (card != null)
                    card.RenderTransform = Avalonia.Media.Transformation.TransformOperations.Parse("scale(1.0)");
                return;
            }

            // Reset to the animation-start state.
            _updateCheckOverlay.Opacity = 0;
            _updateCheckOverlay.IsVisible = true;
            if (card != null)
                card.RenderTransform = Avalonia.Media.Transformation.TransformOperations.Parse("scale(0.94)");

            // Next render tick → push final values so transitions run.
            Dispatcher.UIThread.Post(() =>
            {
                if (_updateCheckOverlay != null)
                    _updateCheckOverlay.Opacity = 1;
                if (card != null)
                    card.RenderTransform = Avalonia.Media.Transformation.TransformOperations.Parse("scale(1.0)");
            }, DispatcherPriority.Render);
        }

        private async Task ShowUpdateDownloadOverlay(string version)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (UpdateOverlayVersion != null)
                    UpdateOverlayVersion.Text = $"Downloading v{version}";
                if (UpdateProgressBar != null)
                    UpdateProgressBar.Width = 0;
                if (UpdateProgressText != null)
                    UpdateProgressText.Text = "0%";
                if (UpdateDlStatusLabel != null)
                    UpdateDlStatusLabel.Text = "Preparing...";
                if (UpdateDownloadOverlay == null) return;

                var card = this.FindControl<Border>("UpdateDownloadCard");

                if (!AreAnimationsEnabled())
                {
                    UpdateDownloadOverlay.Opacity = 1;
                    UpdateDownloadOverlay.IsVisible = true;
                    if (card != null)
                        card.RenderTransform = Avalonia.Media.Transformation.TransformOperations.Parse("scale(1.0)");
                    return;
                }

                UpdateDownloadOverlay.Opacity = 0;
                UpdateDownloadOverlay.IsVisible = true;
                if (card != null)
                    card.RenderTransform = Avalonia.Media.Transformation.TransformOperations.Parse("scale(0.94)");

                Dispatcher.UIThread.Post(() =>
                {
                    if (UpdateDownloadOverlay != null)
                        UpdateDownloadOverlay.Opacity = 1;
                    if (card != null)
                        card.RenderTransform = Avalonia.Media.Transformation.TransformOperations.Parse("scale(1.0)");
                }, DispatcherPriority.Render);
            });
        }

        // Called periodically with a 0..1 fraction. Width transition is on the
        // XAML Border so setting Width here smoothly interpolates.
        private async Task UpdateProgress(double pct, string status)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (UpdateProgressBar != null)
                    UpdateProgressBar.Width = _progressBarMaxWidth * Math.Min(1.0, Math.Max(0, pct));
                if (UpdateProgressText != null)
                    UpdateProgressText.Text = $"{(int)(pct * 100)}%";
                if (UpdateDlStatusLabel != null)
                    UpdateDlStatusLabel.Text = status;
            });
        }

        private void StopUpdateSpinner()
        {
            _updateSpinnerCts?.Cancel();
        }

        // Fade the "checking" overlay out via the XAML Opacity transition, then
        // hide IsVisible after the animation finishes so the scrim doesn't snap.
        private async void FadeOutUpdateOverlay()
        {
            StopUpdateSpinner();

            if (_updateCheckOverlay == null) return;

            if (!AreAnimationsEnabled())
            {
                _updateCheckOverlay.IsVisible = false;
                _updateCheckOverlay.Opacity = 1;
                return;
            }

            // Collapse the card slightly as it fades — mirrors the spring-in.
            var card = this.FindControl<Border>("UpdateCheckCard");
            if (card != null)
                card.RenderTransform = Avalonia.Media.Transformation.TransformOperations.Parse("scale(0.96)");

            _updateCheckOverlay.Opacity = 0;
            await Task.Delay(300);

            if (_updateCheckOverlay != null)
            {
                _updateCheckOverlay.IsVisible = false;
                _updateCheckOverlay.Opacity = 1; // reset for next show
            }
        }

        // Same pattern for the download overlay — used when the update fails
        // or completes in test mode and we want to dismiss it cleanly.
        private async void FadeOutDownloadOverlay()
        {
            if (UpdateDownloadOverlay == null) return;

            if (!AreAnimationsEnabled())
            {
                UpdateDownloadOverlay.IsVisible = false;
                UpdateDownloadOverlay.Opacity = 1;
                return;
            }

            var card = this.FindControl<Border>("UpdateDownloadCard");
            if (card != null)
                card.RenderTransform = Avalonia.Media.Transformation.TransformOperations.Parse("scale(0.96)");

            UpdateDownloadOverlay.Opacity = 0;
            await Task.Delay(300);

            if (UpdateDownloadOverlay != null)
            {
                UpdateDownloadOverlay.IsVisible = false;
                UpdateDownloadOverlay.Opacity = 1;
            }
        }



        private void InitializeStartupControls()
        {
            _startupOverlay = this.FindControl<Grid>("StartupOverlay");
            _startupBg = this.FindControl<Image>("StartupBg");
            _startupLogo = this.FindControl<Image>("StartupLogo");
            _startupLogoText = this.FindControl<Image>("StartupLogoText");
        }

        private async Task PlayCinematicStartupAsync()
        {
            if (_startupOverlay == null || _startupLogo == null || _startupBg == null || _startupLogoText == null) return;

            _startupOverlay.IsVisible = true;
            _startupOverlay.Opacity = 1;
            _startupLogo.Opacity = 0;
            _startupLogoText.Opacity = 0;

            var easing = new CubicEaseOut();

            // Background fades in and slowly de-zooms
            _startupBg.Transitions = new Transitions
            {
                new DoubleTransition { Property = Image.OpacityProperty, Duration = TimeSpan.FromMilliseconds(1200), Easing = easing },
                new TransformOperationsTransition { Property = Image.RenderTransformProperty, Duration = TimeSpan.FromMilliseconds(3000), Easing = easing }
            };

            // Both logo elements fade in together with a gentle scale-up
            var logoTransitions = new Transitions
            {
                new DoubleTransition { Property = Image.OpacityProperty, Duration = TimeSpan.FromMilliseconds(680), Easing = easing },
                new TransformOperationsTransition { Property = Image.RenderTransformProperty, Duration = TimeSpan.FromMilliseconds(1800), Easing = easing }
            };
            _startupLogo.Transitions = logoTransitions;
            _startupLogoText.Transitions = logoTransitions;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _startupBg.Opacity = 1;
                _startupBg.RenderTransform = TransformOperations.Parse("scale(1.0)");

                // Both appear together as one lockup
                _startupLogo.Opacity = 1;
                _startupLogo.RenderTransform = TransformOperations.Parse("scale(1.0)");
                _startupLogoText.Opacity = 1;
                _startupLogoText.RenderTransform = TransformOperations.Parse("scale(1.0)");
            });

            await Task.Delay(1800);

            // Fade out the whole overlay together
            _startupOverlay.Transitions = new Transitions
            {
                new DoubleTransition { Property = Grid.OpacityProperty, Duration = TimeSpan.FromMilliseconds(600), Easing = new QuarticEaseIn() }
            };

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _startupOverlay.Opacity = 0;
            });

            await Task.Delay(600);
            _startupOverlay.IsVisible = false;
        }


        private async Task PerformModCleanup()
        {
            if (_modCleanupService == null) return;

            Console.WriteLine("[Mod Cleanup] Starting background cleanup process...");
            var cleanupList = _modCleanupService.GetCleanupList(); 
            var modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");

            List<ModCleanupEntry> successfullyCleanedMods = new List<ModCleanupEntry>();

            foreach (var entry in cleanupList)
            {
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

        private static readonly HttpClient _connectivityClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        private bool _offlineOverlayVisible = false;
        private bool _connectivityCheckInFlight = false;

        private void CheckNetworkConnectivity()
        {
            if (_connectivityCheckInFlight) return;
            _connectivityCheckInFlight = true;
            _ = Task.Run(async () =>
            {
                bool online = false;
                try
                {
                    if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                    {
                        using var req = new HttpRequestMessage(HttpMethod.Head, "https://api.leafclient.com/health");
                        using var res = await _connectivityClient.SendAsync(req).ConfigureAwait(false);
                        online = res.IsSuccessStatusCode;
                    }
                }
                catch { online = false; }
                finally
                {
                    _connectivityCheckInFlight = false;
                }
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    SetOfflineOverlay(!online);
                    ApplyLaunchButtonState();
                });
            });
        }

        private void SetOfflineOverlay(bool show)
        {
            if (_offlineOverlayVisible == show) return;
            _offlineOverlayVisible = show;
            var overlay = this.FindControl<Border>("OfflineOverlay");
            if (overlay == null) return;
            overlay.IsVisible = show;
            var statusText = this.FindControl<TextBlock>("OfflineStatusText");
            var dot = this.FindControl<Ellipse>("OfflineSpinnerDot");
            if (show)
            {
                if (statusText != null) statusText.Text = "Waiting for connection…";
                if (dot != null) dot.Fill = new SolidColorBrush(Color.Parse("#FF5252"));
                var retry = this.FindControl<Button>("OfflineRetryButton");
                if (retry != null)
                {
                    retry.Click -= OfflineRetry_Click;
                    retry.Click += OfflineRetry_Click;
                }
            }
        }

        private void OfflineRetry_Click(object? sender, RoutedEventArgs e)
        {
            var statusText = this.FindControl<TextBlock>("OfflineStatusText");
            var dot = this.FindControl<Ellipse>("OfflineSpinnerDot");
            if (statusText != null) statusText.Text = "Checking connection…";
            if (dot != null) dot.Fill = new SolidColorBrush(Color.Parse("#FFC857"));
            CheckNetworkConnectivity();
        }

        /// <summary>
        /// Fetches the current online count and updates the UI TextBlock and status dot.
        /// </summary>
        private async Task UpdateOnlineCountDisplay()
        {
            if (_onlineCountService == null)
            {
                Console.WriteLine("[OnlineCountService] Service not available. Cannot update UI.");
                if (_onlineCountTextBlock != null) _onlineCountTextBlock.Text = "You're offline";
                if (_onlineStatusDot != null) _onlineStatusDot.Fill = new SolidColorBrush(Colors.Red);
                return;
            }

            if (_onlineCountTextBlock == null || _onlineStatusDot == null)
            {
                _onlineCountTextBlock = this.FindControl<TextBlock>("OnlineCountTextBlock");
                _onlineStatusDot = this.FindControl<Ellipse>("OnlineStatusDot");

                if (_onlineCountTextBlock == null || _onlineStatusDot == null)
                {
                    Console.WriteLine("[OnlineCountService] Online count TextBlock or Ellipse not found in UI.");
                    return; 
                }
            }

            int count = await _onlineCountService.GetOnlineCount();
            string? motd = _onlineCountService.GetMotd();
            string? motdColor = _onlineCountService.GetMotdColor();

            if (count == int.MinValue)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _onlineCountTextBlock.Text = "N/A";
                    _onlineStatusDot.Fill = new SolidColorBrush(Colors.Gray);
                });
            }
            else if (count >= 0)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _onlineCountTextBlock.Text = $"{count} Online";
                    _onlineStatusDot.Fill = new SolidColorBrush(count > 0 ? Colors.Green : Colors.Gray);
                });
            }
            else
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _onlineCountTextBlock.Text = "You're offline";
                    _onlineStatusDot.Fill = new SolidColorBrush(Colors.Red);
                });
            }

            if (!string.IsNullOrEmpty(motd))
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _motdTextBlock ??= this.FindControl<TextBlock>("MotdTextBlock");
                    _motdBanner ??= this.FindControl<Border>("MotdBanner");
                    if (_motdTextBlock != null)
                    {
                        _motdTextBlock.Text = motd;
                        IBrush brush = new SolidColorBrush(Colors.White);
                        if (!string.IsNullOrWhiteSpace(motdColor))
                        {
                            try { brush = SolidColorBrush.Parse(motdColor); } catch { }
                        }
                        _motdTextBlock.Foreground = brush;
                    }
                    if (_motdBanner != null) _motdBanner.IsVisible = true;
                });
            }
        }

        private async Task ReportSessionPlaytimeAsync()
        {
            var jwt = _currentSettings?.LeafApiJwt;
            if (string.IsNullOrEmpty(jwt)) return;
            if (_sessionStartUtc == null) return;

            var elapsed = DateTime.UtcNow - _sessionStartUtc.Value;
            _sessionStartUtc = null;

            int minutes = (int)elapsed.TotalMinutes;
            if (minutes < 5) return;

            var result = await LeafClient.Services.LeafApiService.ReportPlaytimeAsync(jwt, minutes);
            if (result == null || result.Awarded == 0) return;

            _currentCoinBalance = result.Coins;
            Dispatcher.UIThread.Post(() =>
            {
                _leafsBalanceText ??= this.FindControl<TextBlock>("LeafsBalanceText");
                if (_leafsBalanceText != null)
                    _leafsBalanceText.Text = result.Coins.ToString("N0");
            });

            ToastService.Show($"+{result.Awarded} \U0001F343 Leaf Points earned!", ToastType.Success);
        }

        private async Task UpdateLeafsBalanceAsync()
        {
            var jwt = _currentSettings?.LeafApiJwt;
            if (string.IsNullOrEmpty(jwt)) return;

            Dispatcher.UIThread.Post(() =>
            {
                _leafsBalanceText ??= this.FindControl<TextBlock>("LeafsBalanceText");
                _leafsCurrencyWidget ??= this.FindControl<Border>("LeafsCurrencyWidget");
                if (_leafsCurrencyWidget != null)
                    _leafsCurrencyWidget.IsVisible = true;
            });

            var balance = await LeafClient.Services.LeafApiService.GetUserBalanceAsync(jwt);
            if (balance == null) return;

            _currentCoinBalance = balance.Coins;
            Dispatcher.UIThread.Post(() =>
            {
                _leafsBalanceText ??= this.FindControl<TextBlock>("LeafsBalanceText");
                if (_leafsBalanceText != null)
                    _leafsBalanceText.Text = balance.Coins.ToString("N0");
            });
        }

        private async Task<string?> EnsureLeafJwtAsync()
        {
            var jwt = _currentSettings?.LeafApiJwt;
            if (string.IsNullOrEmpty(jwt)) return null;

            try
            {
                var parts = jwt.Split('.');
                if (parts.Length >= 2)
                {
                    var payload = parts[1];
                    var padded = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                    var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(padded));
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("exp", out var expEl))
                    {
                        var exp = DateTimeOffset.FromUnixTimeSeconds(expEl.GetInt64());
                        if (exp > DateTimeOffset.UtcNow.AddMinutes(5))
                            return jwt;
                    }
                }
            }
            catch { }

            var refreshToken = _currentSettings?.LeafApiRefreshToken;
            if (string.IsNullOrEmpty(refreshToken)) return null;

            try
            {
                Console.WriteLine("[JWT] Access token expired, refreshing...");
                var result = await LeafApiService.RefreshAsync(refreshToken);
                if (result != null && _currentSettings != null)
                {
                    _currentSettings.LeafApiJwt = result.AccessToken;
                    WriteSessionJson(result.AccessToken);
                    _currentSettings.LeafApiRefreshToken = result.RefreshToken;
                    var activeEntry = _currentSettings.SavedAccounts.FirstOrDefault(a => a.Id == _currentSettings.ActiveAccountId);
                    if (activeEntry != null)
                    {
                        activeEntry.LeafApiJwt = result.AccessToken;
                        activeEntry.LeafApiRefreshToken = result.RefreshToken;
                    }
                    if (_settingsService != null) _ = _settingsService.SaveSettingsAsync(_currentSettings);
                    _onlineCountService?.UpdateAccessToken(result.AccessToken);
                    Console.WriteLine("[JWT] Refreshed successfully.");
                    return result.AccessToken;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[JWT] Refresh failed: {ex.Message}");
            }

            return null;
        }

        private async Task SyncOwnedCosmeticsFromApiAsync()
        {
            var jwt = await EnsureLeafJwtAsync();
            if (string.IsNullOrEmpty(jwt))
            {
                Console.WriteLine("[Owned] Skipping API sync — no valid JWT.");
                return;
            }

            try
            {
                Console.WriteLine("[Owned] Fetching owned cosmetics from API...");
                var owned = await LeafClient.Services.LeafApiService.GetOwnedCosmeticsAsync(jwt);
                if (owned == null)
                {
                    Console.WriteLine("[Owned] API returned null — request may have failed.");
                    return;
                }

                Console.WriteLine($"[Owned] API returned {owned.Count} cosmetic(s): {string.Join(", ", owned.Select(x => x.Id))}");

                _ownedCosmeticIds.Clear();
                foreach (var item in owned)
                    _ownedCosmeticIds.Add(item.Id);

                SaveOwnedJson();

                var activeEntry = _currentSettings?.SavedAccounts.FirstOrDefault(a => a.Id == _currentSettings.ActiveAccountId);
                if (activeEntry != null)
                {
                    activeEntry.OwnedCosmeticIds = new List<string>(_ownedCosmeticIds);
                    if (_settingsService != null) _ = _settingsService.SaveSettingsAsync(_currentSettings!);
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                    _cosmeticsPage?.RefreshOwnedList(_ownedCosmeticIds));
                Console.WriteLine($"[Owned] Synced {owned.Count} owned cosmetics from API. Current set: {string.Join(", ", _ownedCosmeticIds)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Owned] API sync failed: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private void InitializeModBrowserControls()
        {
            _modBrowserOverlay = this.FindControl<Grid>("ModBrowserOverlay");
            _modBrowserPanel = this.FindControl<Border>("ModBrowserPanel");
            _modBrowserBackdrop = this.FindControl<Border>("ModBrowserBackdrop");
            _modsResultsPanel = this.FindControl<WrapPanel>("ModsResultsPanel");
            _modsResultsGrid = this.FindControl<ItemsControl>("ModsResultsGrid");
            _modsSkeletonPanel = this.FindControl<ItemsControl>("ModsSkeletonPanel");
            _modsLoadingPanel = this.FindControl<StackPanel>("ModsLoadingPanel");
            _modsEmptyPanel = this.FindControl<StackPanel>("ModsEmptyPanel");
            _modsLoadMoreBtn = this.FindControl<Button>("ModsLoadMoreBtn");
            _modsResultCount = this.FindControl<TextBlock>("ModsResultCount");
            _modSearchBox = this.FindControl<TextBox>("ModSearchBox");
            _modSearchClearBtn = this.FindControl<Button>("ModSearchClearBtn");
            _modVersionDropdown = this.FindControl<ComboBox>("ModVersionDropdown");
            _modSortDropdown = this.FindControl<ComboBox>("ModSortDropdown");
            _modCategorySidebar = this.FindControl<StackPanel>("ModCategorySidebar");
            _modLoaderAllChip = this.FindControl<Border>("ModLoaderAllChip");
            _modLoaderFabricChip = this.FindControl<Border>("ModLoaderFabricChip");
            _modLoaderForgeChip = this.FindControl<Border>("ModLoaderForgeChip");
            _modLoaderQuiltChip = this.FindControl<Border>("ModLoaderQuiltChip");

            _modDetailsSidebar = this.FindControl<Border>("ModDetailsSidebar");
            _modDetailsIcon = this.FindControl<Image>("ModDetailsIcon");
            _modDetailsTitle = this.FindControl<TextBlock>("ModDetailsTitle");
            _modDetailsDescription = this.FindControl<TextBlock>("ModDetailsDescription");
            _modDetailsStats = this.FindControl<TextBlock>("ModDetailsStats");
            _modDetailsDownloadButton = this.FindControl<Button>("ModDetailsDownloadButton");

            if (_modSearchBox != null)
                _modSearchBox.TextChanged += OnModSearchTextChanged;

            PopulateModVersionDropdown();
            PopulateModCategorySidebar();

            _modrinthClient.DefaultRequestHeaders.Remove("User-Agent");
            _modrinthClient.DefaultRequestHeaders.Add("User-Agent", "LeafClient/1.1.0 (contact@leafclient.com)");

            _modsBrowserVersionNotice = this.FindControl<Border>("ModsBrowserVersionNotice");
            _modsBrowserVersionNoticeText = this.FindControl<TextBlock>("ModsBrowserVersionNoticeText");
        }

        private void PopulateModVersionDropdown()
        {
            if (_modVersionDropdown == null) return;
            var items = new List<ComboBoxItem>();
            items.Add(new ComboBoxItem { Content = "Any version", Tag = null, IsSelected = true });
            var sorted = LeafSupportedVersions
                .OrderByDescending(v => v, StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var v in sorted)
            {
                items.Add(new ComboBoxItem { Content = v, Tag = v });
            }
            _modVersionDropdown.ItemsSource = items;
            _modVersionDropdown.SelectedIndex = 0;
        }

        private void PopulateModCategorySidebar()
        {
            if (_modCategorySidebar == null) return;

            var allCategoryItem = BuildCategoryItem(null, "All mods", "🌟");
            _modCategorySidebar.Children.Add(allCategoryItem);

            foreach (var (id, label, icon) in ModCategories)
            {
                _modCategorySidebar.Children.Add(BuildCategoryItem(id, label, icon));
            }

            ApplyCategorySelection();
        }

        private Border BuildCategoryItem(string? categoryId, string label, string icon)
        {
            var border = new Border
            {
                Classes = { "CategoryItem" },
                Background = Avalonia.Media.Brushes.Transparent,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 8),
                Cursor = new Cursor(StandardCursorType.Hand),
                Tag = categoryId,
            };

            var row = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 10,
            };
            row.Children.Add(new TextBlock
            {
                Text = icon,
                FontSize = 13,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            });
            var text = new TextBlock
            {
                Text = label,
                FontSize = 12,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.Parse("#94A3B8")),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            };
            text.Classes.Add("CategoryItemText");
            row.Children.Add(text);

            border.Child = row;
            border.PointerPressed += OnCategoryTapped;
            return border;
        }
        #region Updater Important Variables

        string updaterDownloadUrl = "https://github.com/LeafClientMC/LeafClient/raw/refs/heads/main/latestexe/LeafClientUpdater.exe";
        string newExeDownloadUrl = "https://github.com/LeafClientMC/LeafClient/raw/refs/heads/main/latestexe/LeafClient.zip";
        string versionFileUrl = "https://raw.githubusercontent.com/LeafClientMC/LeafClient/main/latestversion.txt";

        #endregion

        private static readonly HashSet<string> LeafSupportedVersions = new(StringComparer.OrdinalIgnoreCase)
        {
            "1.20.1",
            "1.20.2",
            "1.21",
            "1.21.1",
            "1.21.2",
            "1.21.3",
            "1.21.4",
            "1.21.5",
            "1.21.6",
            "1.21.7",
            "1.21.8",
            "1.21.9",
            "1.21.10",
            "1.21.11",
        };

        private static bool IsLeafRuntimeVersion(string? version)
            => !string.IsNullOrWhiteSpace(version) && LeafSupportedVersions.Contains(version);

        private async Task DownloadLeafRuntimeDependencies(string version, bool isFabric)
        {
            if (!isFabric || !IsLeafRuntimeVersion(version))
                return;

            string leafRuntimeDir = System.IO.Path.Combine(_minecraftFolder, "leaf-runtime", version);
            System.IO.Directory.CreateDirectory(leafRuntimeDir);

            string runtimeUrl = $"https://github.com/LeafClientMC/LeafClient/raw/refs/heads/main/latestjars/{version}/leafclient.jar";

            string runtimePath = System.IO.Path.Combine(leafRuntimeDir, "leafclient.jar");

            ShowProgress(true, "Downloading Leaf Client Runtime...");

            try
            {
                await DownloadFileAsync(runtimeUrl, runtimePath);
                Console.WriteLine($"[Leaf Runtime] Runtime dependencies downloaded successfully for {version}.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Leaf Runtime ERROR] Failed to download dependencies for {version}: {ex.Message}");
            }
            finally
            {
                ShowProgress(false);
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



        private async void CheckForUpdatesManual(object? sender, RoutedEventArgs e)
        {
            if (_updateCheckOverlay != null)
            {
                _updateCheckOverlay.Opacity = 1;
                _updateCheckOverlay.IsVisible = true;
                StartUpdateSpinner();
            }

            await CheckForUpdatesAsync();
        }


        private async Task InitiateSelfUpdate(string newExeDownloadUrl)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("[Updater] auto-update not yet available on this platform");
                await ShowUpdateErrorDialog("Auto-update is not yet available on this platform. Please download the latest build manually.");
                return;
            }

            Console.WriteLine("[Updater] Preparing self-update process...");

            try
            {
                string currentExePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(currentExePath))
                {
                    currentExePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                }

                if (string.IsNullOrEmpty(currentExePath))
                {
                    throw new Exception("Could not determine executable path.");
                }

                string appDirectory = System.IO.Path.GetDirectoryName(currentExePath)!;
                string updaterDir = System.IO.Path.Combine(appDirectory, "Updater");
                System.IO.Directory.CreateDirectory(updaterDir);

                string updaterExeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "LeafClientUpdater.exe" : "LeafClientUpdater";
                string updaterLocalPath = System.IO.Path.Combine(updaterDir, updaterExeName);

                Console.WriteLine($"[Updater] Downloading updater from {updaterDownloadUrl} to {updaterLocalPath}");
                using (var client = new HttpClient())
                {
                    byte[] updaterBytes = await client.GetByteArrayAsync(updaterDownloadUrl);
                    await System.IO.File.WriteAllBytesAsync(updaterLocalPath, updaterBytes);
                }
                Console.WriteLine("[Updater] Updater downloaded successfully.");

                int currentPid = System.Diagnostics.Process.GetCurrentProcess().Id;

                string pathBytes = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(currentExePath));
                string urlBytes = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(newExeDownloadUrl));

                var startInfo = new ProcessStartInfo
                {
                    FileName = updaterLocalPath,
                    WorkingDirectory = updaterDir, 
                    Arguments = $"{currentPid} {pathBytes} {urlBytes}",
                    UseShellExecute = false,
                    CreateNoWindow = false 
                };

                Process.Start(startInfo);

                Console.WriteLine("[Updater] Launched updater. Exiting...");

                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    desktop.Shutdown();
                }
                else
                {
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Updater ERROR] Failed to initiate self-update: {ex.Message}");
                await ShowUpdateErrorDialog($"Failed to start update: {ex.Message}");
            }
        }

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

        // Mods the launcher installs automatically as part of mod presets.
        // These are hidden from the user-facing Installed Mods list because the user
        // didn't pick them — the launcher manages them based on the active profile's
        // ModPreset. Also used to backfill the IsAutoInstalled flag on settings saved
        // before the field existed.
        private static readonly System.Collections.Generic.HashSet<string> _launcherManagedModIds =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "sodium", "lithium", "ferritecore", "immediatelyfast",
                "entityculling", "modernfix", "fabric-api", "phosphor",
                "iris", "dynamiclights", "betterfps",
            };

        private void LoadUserMods()
        {
            if (_userModsPanel == null || _noUserModsMessage == null) return;

            Console.WriteLine($"[User Mods] Loading {_currentSettings.InstalledMods.Count} mods from settings");

            _userModsPanel.Children.Clear();

            string currentMcVersion = _currentSettings.SelectedSubVersion;

            // Backfill: any existing entry whose ID matches a known launcher-managed
            // mod gets flagged so it stops showing up in the user list even if the
            // settings.json predates the IsAutoInstalled field.
            foreach (var m in _currentSettings.InstalledMods)
            {
                if (!m.IsAutoInstalled && _launcherManagedModIds.Contains(m.ModId))
                    m.IsAutoInstalled = true;
            }

            var modsForCurrentVersion = _currentSettings.InstalledMods
                .Where(m => m.MinecraftVersion.Equals(currentMcVersion, StringComparison.OrdinalIgnoreCase))
                .Where(m => !m.IsAutoInstalled) // hide launcher-managed mods
                .OrderBy(m => m.Name)
                .ToList();

            if (!modsForCurrentVersion.Any())
            {
                Console.WriteLine("[User Mods] No user-installed mods found for current MC version.");
                _noUserModsMessage.IsVisible = true;
                return;
            }

            _noSkinsMessage.IsVisible = false;
            _noUserModsMessage.IsVisible = false;

            foreach (var mod in modsForCurrentVersion)
            {
                if (mod.ModId.Equals("leafclient", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[User Mods] Skipping display of internal mod: {mod.Name}");
                    continue;
                }

                Console.WriteLine($"[User Mods] Creating card for: {mod.Name} (MC {mod.MinecraftVersion}, Enabled: {mod.Enabled})");
                var modCard = CreateUserModCard(mod);
                _userModsPanel.Children.Add(modCard);
            }

            Console.WriteLine($"[User Mods] Loaded {_userModsPanel.Children.Count} user-mod cards for MC {currentMcVersion}");
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
            mod.Enabled = enabled; 

            string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
            string currentJarPath = GetModFilePath(modsFolder, mod, isDisabled: false); 
            string currentDisabledPath = GetModFilePath(modsFolder, mod, isDisabled: true); 

            string targetPath = GetModFilePath(modsFolder, mod, isDisabled: !enabled); 

            if (enabled) 
            {
                if (File.Exists(currentDisabledPath)) 
                {
                    try
                    {
                        File.Move(currentDisabledPath, currentJarPath); 
                        Console.WriteLine($"[Mod Manager] Enabled mod '{mod.Name}'.");
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[Mod Manager] Failed to enable mod '{mod.Name}': {ex.Message}");
                        ShowLaunchErrorBanner($"Failed to enable mod '{mod.Name}'. It might be in use.");
                        mod.Enabled = !enabled; 
                    }
                }
                else if (!File.Exists(currentJarPath))
                {
                    Console.WriteLine($"[Mod Manager] Mod '{mod.Name}' file missing. Will be re-downloaded on next launch if still enabled.");
                }
            }
            else 
            {
                if (File.Exists(currentJarPath)) 
                {
                    try
                    {
                        File.Move(currentJarPath, currentDisabledPath); 
                        Console.WriteLine($"[Mod Manager] Disabled mod '{mod.Name}'.");
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[Mod Manager] Failed to disable mod '{mod.Name}': {ex.Message}");
                        ShowLaunchErrorBanner($"Failed to disable mod '{mod.Name}'. It might be in use.");
                        mod.Enabled = !enabled; 
                    }
                }
            }

            await _settingsService.SaveSettingsAsync(_currentSettings);
            LoadUserMods(); 
        }


        private async Task DeleteUserMod(InstalledMod mod)
        {
            string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
            string jarPath = GetModFilePath(modsFolder, mod, isDisabled: false);
            string disabledPath = GetModFilePath(modsFolder, mod, isDisabled: true);

            _currentSettings.InstalledMods.Remove(mod);
            await _settingsService.SaveSettingsAsync(_currentSettings);
            Console.WriteLine($"[Mod Manager] Removed '{mod.Name}' from settings.");

            bool fileDeletedSuccessfully = false;
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
                    fileDeletedSuccessfully = true; 
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[Mod Manager] Failed to delete disabled mod file '{System.IO.Path.GetFileName(disabledPath)}': {ex.Message}");
                    _modCleanupService?.AddModToCleanup(mod);
                    ShowLaunchErrorBanner($"Failed to delete mod '{mod.Name}'. It might be in use. It will be retried later.");
                }
            }

            if (!fileDeletedSuccessfully && _modCleanupService != null)
            {
                _modCleanupService.RemoveModFromCleanup(new ModCleanupEntry
                {
                    ModId = mod.ModId,
                    FileName = mod.FileName,
                    MinecraftVersion = mod.MinecraftVersion
                });
            }

            if (_currentSettings.SelectedSkinId == mod.ModId)
            {
                _currentSettings.SelectedSkinId = null;
                _currentlySelectedSkinCard = null;
            }

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
            InitializeAAdsSystem();

            _gameWindowPendingText = this.FindControl<TextBlock>("GameWindowPendingText");


            if (this.FindControl<TextBlock>("AppVersionTextBlock") is { } appVersionTextBlock)
            {
                Version currentVersion = GetCurrentAppVersion();
                string versionString = $"Version {currentVersion.Major}.{currentVersion.Minor}.{currentVersion.Build} Beta"; 
                appVersionTextBlock.Text = versionString;
            }

            _feedbackOverlay = this.FindControl<Grid>("FeedbackOverlay");
            _feedbackPanel = this.FindControl<Border>("FeedbackPanel");

            _crashReportOverlay = this.FindControl<Grid>("CrashReportOverlay");
            _crashReportPanel = this.FindControl<Border>("CrashReportPanel");
            if (_crashReportPanel != null)
            {
                _crashScreenshotLine = _crashReportPanel.FindControl<TextBlock>("CrashScreenshotLine");
                _crashStatusLine = _crashReportPanel.FindControl<TextBlock>("CrashStatusLine");
                _crashSendButton = _crashReportPanel.FindControl<Button>("CrashSendButton");
                _crashDismissButton = _crashReportPanel.FindControl<Button>("CrashDismissButton");
            }

            if (_feedbackPanel != null) 
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

                var attachLogButton = _feedbackPanel.FindControl<Button>("AttachLogButton");
                var sendFeedbackButton = _feedbackPanel.FindControl<Button>("SendFeedbackButton");
                var cancelFeedbackButton = _feedbackPanel.FindControl<Button>("CancelFeedbackButton");

                if (attachLogButton != null) attachLogButton.Click += AttachLogButton_Click;
                if (sendFeedbackButton != null) sendFeedbackButton.Click += SendFeedbackButton_Click;
                if (cancelFeedbackButton != null) cancelFeedbackButton.Click += CancelFeedbackButton_Click;
                if (_feedbackTypeComboBox != null) _feedbackTypeComboBox.SelectionChanged += FeedbackTypeComboBox_SelectionChanged;

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
                    UpdateRichPresenceFromState(); 
                    MarkSettingsDirty();
                };
                _showUsernameInDiscordRichPresenceToggle.Unchecked += async (_, __) =>
                {
                    if (_isApplyingSettings) return;
                    _currentSettings.ShowUsernameInDiscordRichPresence = false;
                    await _settingsService.SaveSettingsAsync(_currentSettings);
                    UpdateRichPresenceFromState(); 
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

            _optiFineToggle  = this.FindControl<ToggleSwitch>("OptiFineToggle");
            _testModeToggle  = this.FindControl<ToggleSwitch>("TestModeToggle");
            _testModePathBox = this.FindControl<TextBox>("TestModePathBox");
            _testModePathPanel = this.FindControl<Border>("TestModePathPanel");

            _commonQuestionsOverlay = this.FindControl<Grid>("CommonQuestionsOverlay");
            _commonQuestionsPanel = this.FindControl<Border>("CommonQuestionsPanel");

            _aboutLeafClientOverlay = this.FindControl<Grid>("AboutLeafClientOverlay");
            _aboutLeafClientPanel = this.FindControl<Border>("AboutLeafClientPanel");

            _checkoutOverlay = this.FindControl<Grid>("CheckoutOverlay");
            _checkoutPanel = this.FindControl<Border>("CheckoutPanel");

            _jvmArgumentsEditButton = this.FindControl<Button>("JvmArgumentsEditButton");
            if (_jvmArgumentsEditButton != null)
            {
                _jvmArgumentsEditButton.Click += OpenJvmArgumentsEditor;
            }

            _selectionIndicator = this.FindControl<Border>("SelectionIndicator");
            _savedSelectionIndicatorTransitions = _selectionIndicator?.Transitions?.ToList();

            _settingsIndicator = this.FindControl<Border>("SettingsIndicator");
            _savedSettingsIndicatorTransitions = _settingsIndicator?.Transitions?.ToList();

            _promoBanner = this.FindControl<Border>("PromoBanner");
            _savedPromoBannerTransitions = _promoBanner?.Transitions?.ToList();

            _launchSection = this.FindControl<Border>("LaunchSection");
            _savedLaunchSectionTransitions = _launchSection?.Transitions?.ToList();

            _newsSectionGrid = this.FindControl<Grid>("NewsSectionGrid"); 
            _savedNewsSectionGridTransitions = _newsSectionGrid?.Transitions?.ToList();

            _launchErrorBanner = this.FindControl<Border>("LaunchErrorBanner");
            _savedLaunchErrorBannerTransitions = _launchErrorBanner?.Transitions?.ToList();

            _launchErrorBannerText = this.FindControl<TextBlock>("LaunchErrorBannerText");
            _launchErrorBannerCloseButton = this.FindControl<Button>("LaunchErrorBannerCloseButton");

            _settingsSaveBanner = this.FindControl<Border>("SettingsSaveBanner");
            _savedSettingsSaveBannerTransitions = _settingsSaveBanner?.Transitions?.ToList();

            _accountPanel = this.FindControl<Border>("AccountPanel");
            if (_accountPanel != null)
            {
                _savedAccountPanelTransitions = _accountPanel.Transitions?.ToList();
                // Ensure a ScaleTransform exists for the new centered overlay
                if (_accountPanel.RenderTransform is not ScaleTransform)
                    _accountPanel.RenderTransform = new ScaleTransform(0.94, 0.94);
            }


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
            _featuredServersPanel = this.FindControl<StackPanel>("FeaturedServersPanel");
            _quickPlayServersContainer = this.FindControl<StackPanel>("QuickPlayServersContainer");
            _noServersMessage = this.FindControl<Border>("NoServersMessage");

            InitLaunchAnimation();
            _profilesListPanel = this.FindControl<StackPanel>("ProfilesListPanel");
            _noProfilesMessage = this.FindControl<Border>("NoProfilesMessage");

            // Profile Editor Overlay
            _profileEditorOverlay = this.FindControl<Grid>("ProfileEditorOverlay");
            _mainContentGrid      = this.FindControl<Grid>("MainContentGrid");
            _po_TitleText         = this.FindControl<TextBlock>("PO_TitleText");
            _po_AvatarLetter      = this.FindControl<TextBlock>("PO_AvatarLetter");
            _po_NavBtnGeneral     = this.FindControl<Button>("PO_NavBtnGeneral");
            _po_NavBtnPerformance = this.FindControl<Button>("PO_NavBtnPerformance");
            _po_NavBtnMods        = this.FindControl<Button>("PO_NavBtnMods");
            _po_NavBtnAdvanced    = this.FindControl<Button>("PO_NavBtnAdvanced");
            _po_NavIndicator      = this.FindControl<Border>("PO_NavIndicator");
            _po_NavActiveBar      = this.FindControl<Border>("PO_NavActiveBar");
            _po_TabIdentity       = this.FindControl<StackPanel>("PO_TabIdentity");
            _po_TabPerformance    = this.FindControl<StackPanel>("PO_TabPerformance");
            _po_TabMods           = this.FindControl<StackPanel>("PO_TabMods");
            _po_TabAdvanced       = this.FindControl<StackPanel>("PO_TabAdvanced");
            _po_DescriptionBox    = this.FindControl<TextBox>("PO_DescriptionBox");
            _po_IconEmojiBox      = this.FindControl<TextBox>("PO_IconEmojiBox");
            _po_JvmArgsBox        = this.FindControl<TextBox>("PO_JvmArgsBox");
            _po_UseCustomResolutionToggle = this.FindControl<ToggleSwitch>("PO_UseCustomResolutionToggle");
            _po_ResWidthBox       = this.FindControl<TextBox>("PO_ResWidthBox");
            _po_ResHeightBox      = this.FindControl<TextBox>("PO_ResHeightBox");
            _po_QuickJoinAddressBox = this.FindControl<TextBox>("PO_QuickJoinAddressBox");
            _po_QuickJoinPortBox  = this.FindControl<TextBox>("PO_QuickJoinPortBox");
            _po_StatLaunches      = this.FindControl<TextBlock>("PO_StatLaunches");
            _po_StatPlaytime      = this.FindControl<TextBlock>("PO_StatPlaytime");
            _po_StatLastUsed      = this.FindControl<TextBlock>("PO_StatLastUsed");
            _po_NameBox           = this.FindControl<TextBox>("PO_NameBox");
            _po_AccountCombo      = this.FindControl<ComboBox>("PO_AccountCombo");
            _po_VersionCombo      = this.FindControl<ComboBox>("PO_VersionCombo");
            _po_JavaCombo         = this.FindControl<ComboBox>("PO_JavaCombo");
            _po_ShowAllVersions   = this.FindControl<CheckBox>("PO_ShowAllVersions");
            _po_SupportBanner     = this.FindControl<Border>("PO_SupportBanner");
            _po_SupportDot        = this.FindControl<Ellipse>("PO_SupportDot");
            _po_SupportTitle      = this.FindControl<TextBlock>("PO_SupportTitle");
            _po_SupportSub        = this.FindControl<TextBlock>("PO_SupportSub");
            _po_JavaBannerText    = this.FindControl<TextBlock>("PO_JavaBannerText");
            _po_MemSlider         = this.FindControl<Slider>("PO_MemSlider");
            _po_MemLabel          = this.FindControl<TextBlock>("PO_MemLabel");
            _po_ModsSupportBanner = this.FindControl<Border>("PO_ModsSupportBanner");
            _po_ModsSupportText   = this.FindControl<TextBlock>("PO_ModsSupportText");
            _po_PresetBalanced    = this.FindControl<Border>("PO_PresetBalanced");
            _po_PresetEnhanced    = this.FindControl<Border>("PO_PresetEnhanced");
            _po_PresetLite        = this.FindControl<Border>("PO_PresetLite");
            _po_BadgesBalanced    = this.FindControl<WrapPanel>("PO_BadgesBalanced");
            _po_BadgesEnhanced    = this.FindControl<WrapPanel>("PO_BadgesEnhanced");
            _po_BadgesLite        = this.FindControl<WrapPanel>("PO_BadgesLite");
            PO_PopulatePresetBadges();
            _quickPlayTooltip = this.FindControl<Border>("QuickPlayTooltip");
            _quickPlayTooltipText = this.FindControl<TextBlock>("QuickPlayTooltipText");

            _savedQuickPlayTooltipTransitions = _quickPlayTooltip?.Transitions?.ToList();

            if (_quickPlayTooltip != null)
            {
                _quickPlayTooltip.IsVisible = false;
                _quickPlayTooltip.Opacity = 0;
            }
            _quickPlayServersContainer = this.FindControl<StackPanel>("QuickPlayServersContainer");
            _playingAsImage = this.FindControl<Image>("PlayingAsImage");
            _accountCharacterImage = this.FindControl<Image>("AccountCharacterImage");
            _launchProgressPanel = this.FindControl<StackPanel>("LaunchProgressPanel");
            _launchProgressBar = this.FindControl<ProgressBar>("LaunchProgressBar");
            _launchProgressText = this.FindControl<TextBlock>("LaunchProgressText");
            _modsInfoText = this.FindControl<TextBlock>("ModsInfoText");
            _launchBgImage = this.FindControl<Image>("LaunchBgImage");
            if (_launchBgImage != null)
            {
                // The XAML defines a TransformGroup with named children; grab them
                if (_launchBgImage.RenderTransform is TransformGroup tg)
                {
                    _launchBgScale = tg.Children.OfType<ScaleTransform>().FirstOrDefault()
                                     ?? new ScaleTransform(1.08, 1.08);
                    _launchBgTranslate = tg.Children.OfType<TranslateTransform>().FirstOrDefault()
                                         ?? new TranslateTransform();
                }
                else
                {
                    // Fallback: build the group ourselves
                    _launchBgScale = new ScaleTransform(1.08, 1.08);
                    _launchBgTranslate = new TranslateTransform();
                    var group = new TransformGroup();
                    group.Children.Add(_launchBgScale);
                    group.Children.Add(_launchBgTranslate);
                    _launchBgImage.RenderTransform = group;
                }
                _launchBgImage.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
                StartParallaxLoop();
            }
            _particleLayer = this.FindControl<Canvas>("ParticleLayer");
            SetupParticles();
            _accountPanelOverlay = this.FindControl<Grid>("AccountPanelOverlay");
            _accountsListPanel = this.FindControl<StackPanel>("AccountsListPanel");
            _accountTypeLabel = this.FindControl<TextBlock>("AccountTypeLabel");
            _accountOnlineDot = this.FindControl<Avalonia.Controls.Shapes.Ellipse>("AccountOnlineDot");
            _accountUsernameDisplay = this.FindControl<TextBlock>("AccountUsernameDisplay");
            _accountUuidDisplay = this.FindControl<TextBlock>("AccountUuidDisplay");
            _playingAsUsername = this.FindControl<TextBlock>("PlayingAsUsername");
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
            _majorUpdateBadge = this.FindControl<Border>("MajorUpdateBadge");
            StartMajorUpdateBadgeAnimation();
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
            _cosmeticsPage = this.FindControl<LeafClient.Views.Pages.CosmeticsPageView>("CosmeticsPage");
            _cosmeticsPage?.SetHost(this);
            _storePage = this.FindControl<LeafClient.Views.Pages.StorePageView>("StorePage");
            _storePage?.SetHost(this);
            _screenshotsPage = this.FindControl<LeafClient.Views.Pages.ScreenshotsPageView>("ScreenshotsPage");
            _screenshotsPage?.SetHost(this);
            _resourcePacksPage = this.FindControl<LeafClient.Views.Pages.ResourcePacksPageView>("ResourcePacksPage");
            _resourcePacksPage?.SetHost(this);
            InitializeSkinStatusBanner();

            if (this.FindControl<Button>("LaunchGameButton") is { } launchBtn)
            {
                launchBtn.Click += async (s, e) =>
                {
                    // Allow terminating a RUNNING game (game window open)
                    if (_gameProcess != null && !_gameProcess.HasExited)
                    {
                        try
                        {
                            Console.WriteLine("[Launcher] User clicked to terminate running game process.");
                            _userTerminatedGame = true;
                            _gameProcess.Kill();
                            _gameProcess.WaitForExit(5000);
                            Console.WriteLine("[Launcher] Running game process terminated successfully.");
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"[Launcher ERROR] Failed to terminate game process on click: {ex.Message}");
                            ShowLaunchErrorBanner($"Failed to terminate game: {ex.Message}");
                        }
                        finally
                        {
                            _isLaunching = false;
                            _isInstalling = false;
                            _gameProcess = null;
                            UpdateLaunchButton("LAUNCH GAME", "SeaGreen");
                            await UpdateServerButtonStates();
                        }
                        return;
                    }

                    // REMOVED: Old cancellation logic for launching/installing.
                    // If launching, clicking the main button now does nothing.
                    if (_isLaunching || _isInstalling)
                    {
                        return;
                    }

                    // Active profile version takes priority over SelectedSubVersion
                    var activeProfileForLaunch = _currentSettings.Profiles?.FirstOrDefault(p => p.Id == _currentSettings.ActiveProfileId);
                    string versionForLaunch = activeProfileForLaunch?.MinecraftVersion ?? _currentSettings.SelectedSubVersion;

                    var selectedVersionInfo = _allVersions.FirstOrDefault(v => v.FullVersion == versionForLaunch);
                    if (selectedVersionInfo == null)
                    {
                        UpdateLaunchButton("SELECT VERSION", "OrangeRed");
                        ShowLaunchErrorBanner("Please select a Minecraft version to launch.");
                        return;
                    }

                    bool isFabric = selectedVersionInfo.Loader.Equals("Fabric", StringComparison.OrdinalIgnoreCase);
                    await LaunchGameAsync(selectedVersionInfo.FullVersion, isFabric);
                };

                launchBtn.PointerEntered += (s, e) =>
                {
                    _launchButtonHovered = true;
                    if (_gameProcess != null && !_gameProcess.HasExited)
                    {
                        if (s is Button hoveredBtn)
                        {
                            hoveredBtn.Background = new SolidColorBrush(Colors.DarkRed);
                            _launchButtonGlowColor = Colors.DarkRed;
                            hoveredBtn.Effect = BuildLaunchGlow(Colors.DarkRed, true);
                            if (this.FindControl<Border>("LaunchButtonOuterBorder") is { } outerBorder)
                            {
                                outerBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(139, 0, 0));
                            }
                            if (hoveredBtn.Content is StackPanel contentStack && contentStack.Children.OfType<TextBlock>().FirstOrDefault() is TextBlock mainTextBlock)
                            {
                                mainTextBlock.Text = "TERMINATE GAME";
                            }
                        }
                    }
                    else if (s is Button btn2)
                    {
                        btn2.Effect = BuildLaunchGlow(_launchButtonGlowColor, true);
                    }
                };

                launchBtn.PointerExited += (s, e) =>
                {
                    _launchButtonHovered = false;
                    ApplyLaunchButtonState();
                };
                _settingsSaveBanner = this.FindControl<Border>("SettingsSaveBanner");
                _settingsSaveBannerSaveButton = this.FindControl<Button>("SettingsSaveBannerSaveButton");
                _settingsSaveBannerCancelButton = this.FindControl<Button>("SettingsSaveBannerCancelButton");

                if (_settingsSaveBannerSaveButton != null)
                {
                    _settingsSaveBannerSaveButton.Click += (s, e) =>
                    {
                        SaveSettingsFromUi();
                        // Take fresh snapshot of the just-saved state so next Cancel can revert to it
                        try { _settingsSnapshotJson = System.Text.Json.JsonSerializer.Serialize(_currentSettings, JsonContext.Default.LauncherSettings); }
                        catch { _settingsSnapshotJson = null; }
                        HideSettingsSaveBanner();
                    };
                }

                if (_settingsSaveBannerCancelButton != null)
                {
                    _settingsSaveBannerCancelButton.Click += async (s, e) =>
                    {
                        // Revert to the snapshot taken before changes began
                        if (_settingsSnapshotJson != null)
                        {
                            try
                            {
                                var reverted = System.Text.Json.JsonSerializer.Deserialize(_settingsSnapshotJson, JsonContext.Default.LauncherSettings);
                                if (reverted != null)
                                {
                                    _currentSettings = reverted;
                                    await _settingsService.SaveSettingsAsync(_currentSettings);
                                    ApplySettingsToUi(_currentSettings);
                                }
                            }
                            catch
                            {
                                // Fallback: reload from disk
                                LoadAndApplySettings();
                            }
                        }
                        else
                        {
                            LoadAndApplySettings();
                        }
                        _settingsSnapshotJson = null;
                        HideSettingsSaveBanner();
                    };
                }

                WireSettingsDirtyHandlers();
            }
        }

        private void OpenGameOutput(object? sender, RoutedEventArgs e)
        {
            if (_gameOutputWindow != null)
            {
                _gameOutputWindow.Show();
                _gameOutputWindow.Activate();
            }
            else
            {
                _gameOutputWindow = new GameOutputWindow();
                _gameOutputWindow.Closed += (s, args) => _gameOutputWindow = null;
                _gameOutputWindow.Show();
            }
        }


        private void CancelLaunchButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_isLaunching || _isInstalling)
            {
                Console.WriteLine("[Launcher] User clicked CANCEL button.");
                _launchCancellationTokenSource?.Cancel();

                _isLaunching = false;
                _isInstalling = false;

                UpdateLaunchButton("LAUNCH CANCELLED", "Orange");
                _ = UpdateServerButtonStates();

                if (_cancelLaunchButton != null)
                    _cancelLaunchButton.IsVisible = false;
            }
        }


        private void InitializeAAdsSystem()
        {
            _adButton = this.FindControl<Button>("AdButton");
            _adImage = this.FindControl<Image>("AdImage");
            _adLoadingText = this.FindControl<TextBlock>("AdLoadingText");

            _ = LoadAAdsBannerAsync();
        }

        private async Task LoadAAdsBannerAsync()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("[A-Ads] Non-Windows OS detected. Hiding ad section.");
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (AdSection != null) AdSection.IsVisible = false;
                });
                return;
            }

            try
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_adLoadingText != null) _adLoadingText.IsVisible = true;
                    if (AdSection != null) AdSection.IsVisible = true; 
                });

                string apiUrl = $"{AAdsApiBaseUrl}{AAdsAdUnitId}";
                Console.WriteLine($"[A-Ads] Fetching from: {apiUrl}");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); 
                var json = await _httpClient.GetStringAsync(apiUrl, cts.Token);

                Console.WriteLine($"[A-Ads] Raw Response: {json}");

                var adResponse = JsonSerializer.Deserialize(json, JsonContext.Default.AAdsAdResponse);

                if (adResponse == null)
                {
                    Console.WriteLine("[A-Ads] Response was null.");
                    await HideAdSection();
                    return;
                }

                if (adResponse.status == false)
                {
                    Console.WriteLine("[A-Ads] Ad status is 'false' (likely pending verification or no fill).");
                    await HideAdSection();
                    return;
                }

                if (string.IsNullOrEmpty(adResponse.banner) || string.IsNullOrEmpty(adResponse.link))
                {
                    Console.WriteLine("[A-Ads] Banner image or Link URL is missing.");
                    await HideAdSection();
                    return;
                }

                _currentAdData = adResponse; 

                Console.WriteLine($"[A-Ads] Downloading banner image: {_currentAdData.banner}");
                var imageBytes = await _httpClient.GetByteArrayAsync(_currentAdData.banner, cts.Token);
                using var stream = new MemoryStream(imageBytes);
                var bitmap = new Avalonia.Media.Imaging.Bitmap(stream);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_adImage != null) _adImage.Source = bitmap;
                    if (_adLoadingText != null) _adLoadingText.IsVisible = false; 
                    if (AdSection != null) AdSection.IsVisible = true; 

                    if (_adButton != null)
                    {
                        ToolTip.SetTip(_adButton, _currentAdData.alt ?? "Anonymous Ad");
                    }

                    Console.WriteLine($"[A-Ads] Ad loaded successfully.");
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[A-Ads] Failed to load banner: {ex.Message}");
                await HideAdSection();
            }
        }

        private async Task HideAdSection()
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (AdSection != null) AdSection.IsVisible = false;
            });
        }


        private void OnAdClicked(object? sender, RoutedEventArgs e)
        {
            if (_currentAdData != null && !string.IsNullOrEmpty(_currentAdData.link))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _currentAdData.link,
                        UseShellExecute = true
                    });
                    Console.WriteLine($"[A-Ads] User clicked ad. Opening: {_currentAdData.link}");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[A-Ads] Failed to open ad link: {ex.Message}");
                }
            }
        }



        private string MaskUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length <= 2)
                return username; 

            int visibleChars = 1; 
            int maskedLength = username.Length - (visibleChars * 2);

            if (maskedLength <= 0)
                return username; 

            string masked = username[0] + new string('*', maskedLength) + username[username.Length - 1];
            return masked;
        }

        private double _lastAspectRatio = 16.0 / 9.0; 

        private void OnResolutionWidthChanged(object? sender, RoutedEventArgs e)
        {
            if (!_currentSettings.LockGameAspectRatio || _isApplyingSettings)
                return;

            if (_gameResolutionWidthTextBox != null && _gameResolutionHeightTextBox != null)
            {
                if (int.TryParse(_gameResolutionWidthTextBox.Text, out int newWidth) && newWidth > 0)
                {
                    if (int.TryParse(_gameResolutionHeightTextBox.Text, out int currentHeight) && currentHeight > 0)
                    {
                        _lastAspectRatio = (double)currentHeight / newWidth;
                    }

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
                    if (int.TryParse(_gameResolutionWidthTextBox.Text, out int currentWidth) && currentWidth > 0)
                    {
                        _lastAspectRatio = (double)newHeight / currentWidth;
                    }

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
                await CalculateDiskUsageAsync(); 
            }
        }

        private async void ClearCacheButton_Click(object? sender, RoutedEventArgs e)
        {
            var confirmed = await ShowConfirmationDialog("Clear Cache", "Are you sure you want to delete all launcher cache files? This includes downloaded assets and temporary files. This may temporarily increase game launch times.");
            if (!confirmed) return;

            try
            {
                string minecraftCachePath = System.IO.Path.Combine(_minecraftFolder, "assets", "objects");
                string cmlLibCachePath = System.IO.Path.Combine(_minecraftFolder, "libraries"); 

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
                await CalculateDiskUsageAsync(); 
            }
        }

        private async Task CalculateDiskUsageAsync()
        {
            if (_logsUsageBar == null || _profilesUsageBar == null || _assetsUsageBar == null || _cacheUsageBar == null ||
                _otherUsageBar == null || _logsUsageText == null || _profilesUsageText == null || _assetsUsageText == null ||
                _cacheUsageText == null || _otherUsageText == null || _totalUsageText == null) return;

            await Task.Run(async () =>
            {
                double logsSize = await GetDirectorySizeAsync(_logFolderPath);
                double profilesSize = await GetDirectorySizeAsync(System.IO.Path.Combine(_minecraftFolder, "versions"));
                double assetsSize = await GetDirectorySizeAsync(System.IO.Path.Combine(_minecraftFolder, "assets"));
                double librariesSize = await GetDirectorySizeAsync(System.IO.Path.Combine(_minecraftFolder, "libraries")); 

                double totalKnownSize = logsSize + profilesSize + assetsSize + librariesSize;

                double minecraftRootSize = await GetDirectorySizeAsync(_minecraftFolder);
                double otherSize = minecraftRootSize - totalKnownSize;
                if (otherSize < 0) otherSize = 0; 

                double totalActualDisplayedSize = totalKnownSize + otherSize; 

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _logsUsageText.Text = $"Logs - {FormatBytes(logsSize)}";
                    _profilesUsageText.Text = $"Profiles - {FormatBytes(profilesSize)}";
                    _assetsUsageText.Text = $"Assets - {FormatBytes(assetsSize)}";
                    _cacheUsageText.Text = $"Cache - {FormatBytes(librariesSize)}"; 
                    _otherUsageText.Text = $"Other - {FormatBytes(otherSize)}";
                    _totalUsageText.Text = FormatBytes(totalActualDisplayedSize);


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
                        catch (UnauthorizedAccessException) {  }
                        catch (System.IO.IOException) {  }
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

            double scaling = this.RenderScaling;
            double logicalWidth = width / scaling;
            double logicalHeight = height / scaling;

            var previewWindow = new Window
            {
                Title = "Resolution Preview",
                SystemDecorations = SystemDecorations.None,
                Width = logicalWidth,
                Height = logicalHeight,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                CanResize = false,
                Topmost = true,
                Background = new SolidColorBrush(Color.Parse("#CC101010")),
                TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent },
                ExtendClientAreaToDecorationsHint = true,
                ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome
            };

            var container = new Border
            {
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

            previewWindow.PointerPressed += (_, __) => previewWindow.Close();

            previewWindow.Deactivated += (_, __) => previewWindow.Close();

            previewWindow.Show();
        }



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
                transform.Y = 0; 
                _launchFailureBanner.Opacity = 1;
                return;
            }

            transform.Y = -100; 
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
                    double eased = 1 - Math.Pow(1 - progress, 3); 

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_launchFailureBanner == null || ct.IsCancellationRequested) return;
                        transform.Y = -100 + (100 * eased); 
                        _launchFailureBanner.Opacity = eased; 
                    });

                    if (i < steps)
                        await Task.Delay(delayMs, ct);
                }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(5000, ct); 
                        if (!ct.IsCancellationRequested)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() => HideLaunchFailureBanner());
                        }
                    }
                    catch (OperationCanceledException) {  }
                }, ct);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async void HideLaunchFailureBanner()
        {
            if (_launchFailureBanner == null) return;

            _launchFailureBannerCts?.Cancel();
            _launchFailureBannerShownForCurrentLaunch = false;

            TranslateTransform? transform = _launchFailureBanner.RenderTransform as TranslateTransform;
            if (transform == null)
            {
                _launchFailureBanner.IsVisible = false;
                return;
            }

            if (!AreAnimationsEnabled())
            {
                transform.Y = -100;
                _launchFailureBanner.Opacity = 0;
                _launchFailureBanner.IsVisible = false;
                return;
            }

            // Capture current position so we animate from wherever the banner actually is
            double startY = transform.Y;
            double startOpacity = _launchFailureBanner.Opacity;

            if (startOpacity < 0.01)
            {
                _launchFailureBanner.IsVisible = false;
                return;
            }

            var hideCts = new CancellationTokenSource();
            var ct = hideCts.Token;

            const int durationMs = 380;
            const int steps = 24;
            const int delayMs = durationMs / steps;

            try
            {
                for (int i = 0; i <= steps; i++)
                {
                    if (ct.IsCancellationRequested) break;

                    double progress = (double)i / steps;
                    double eased = progress * progress; // ease-in: slow start then fast exit

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_launchFailureBanner == null || ct.IsCancellationRequested) return;
                        transform.Y      = startY + (-100.0 - startY) * eased;
                        _launchFailureBanner.Opacity = startOpacity * (1.0 - eased);
                    });

                    if (i < steps)
                        await Task.Delay(delayMs, ct);
                }
            }
            catch (OperationCanceledException) { }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_launchFailureBanner != null)
                {
                    transform.Y = -100;
                    _launchFailureBanner.Opacity  = 0;
                    _launchFailureBanner.IsVisible = false;
                }
            });
        }

        private async void ShowGameStartingBanner(string message)
        {
            if (_gameStartingBanner == null || _gameStartingBannerText == null) return;

            if (_gameStartingBannerShownForCurrentLaunch) return;

            _gameStartingBannerText.Text = message;
            _gameStartingBanner.IsVisible = true;
            _gameStartingBannerShownForCurrentLaunch = true; 

            _gameStartingBannerCts?.Cancel(); 
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
                transform.Y = 0; 
                _gameStartingBanner.Opacity = 1;
                return;
            }

            transform.Y = -100; 
            _gameStartingBanner.Opacity = 0;

            const int durationMs = 500;
            const int steps = 30;
            const int delayMs = durationMs / steps;

            for (int i = 0; i <= steps; i++)
            {
                if (ct.IsCancellationRequested) break;

                double progress = (double)i / steps;
                double eased = 1 - Math.Pow(1 - progress, 3); 

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_gameStartingBanner == null || ct.IsCancellationRequested) return;
                    transform.Y = -100 + (100 * eased); 
                    _gameStartingBanner.Opacity = eased * 0.8; 
                });

                if (i < steps)
                    await Task.Delay(delayMs, ct);
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(5000, ct); 
                    if (!ct.IsCancellationRequested)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() => HideGameStartingBanner());
                    }
                }
                catch (OperationCanceledException) {  }
            }, ct);
        }


        private void HideGameStartingBanner_Click(object? sender, RoutedEventArgs e)
        {
            HideGameStartingBanner();
        }

        private async void HideGameStartingBanner()
        {
            if (_gameStartingBanner == null) return;

            _gameStartingBannerCts?.Cancel(); 

            TranslateTransform? transform = _gameStartingBanner.RenderTransform as TranslateTransform;
            if (transform == null) return;

            if (!AreAnimationsEnabled())
            {
                transform.Y = -100; 
                _gameStartingBanner.Opacity = 0;
                _gameStartingBanner.IsVisible = false;
                return;
            }

            const int durationMs = 500;
            const int steps = 30;
            const int delayMs = durationMs / steps;

            for (int i = 0; i <= steps; i++)
            {
                double progress = (double)i / steps;
                double eased = 1 - Math.Pow(1 - progress, 3); 

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_gameStartingBanner == null) return;
                    transform.Y = 0 - (100 * eased); 
                    _gameStartingBanner.Opacity = (1 - eased) * 0.8; 
                });

                if (i < steps)
                    await Task.Delay(delayMs);
            }

            _gameStartingBanner.IsVisible = false;
        }

        // ─── Launch Animation ─────────────────────────────────────────────────────

        private Image?   _centerLeaf;
        private Ellipse? _leafGlowOrb;
        private ProgressBar? _launchAnimProgressBar;

        private void InitLaunchAnimation()
        {
            var rootGrid = this.FindControl<Grid>("MainRootGrid");
            if (rootGrid == null) return;

            var rng = new Random(42);
            // Canvas 400×360 — centered scene, no text
            const double cw = 400, ch = 360;
            double cx = cw / 2, cy = ch / 2;

            _blockCanvas = new Canvas
            {
                Width = cw, Height = ch, ClipToBounds = false,
                RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                RenderTransform = new ScaleTransform(1.0, 1.0),
            };

            // ── Deep radial background glow ───────────────────────────────────
            var bgGlow = new Ellipse
            {
                Width = 320, Height = 280,
                Fill = new RadialGradientBrush
                {
                    GradientStops =
                    {
                        new GradientStop { Color = Color.FromArgb(90, 20, 90, 35),  Offset = 0.0 },
                        new GradientStop { Color = Color.FromArgb(30, 10, 50, 15),  Offset = 0.6 },
                        new GradientStop { Color = Color.FromArgb(0,   0,  0,  0),  Offset = 1.0 },
                    }
                }
            };
            Canvas.SetLeft(bgGlow, cx - 160);
            Canvas.SetTop(bgGlow,  cy - 140);
            _blockCanvas.Children.Add(bgGlow);

            // ── 3 faint orbit path rings (flattened ellipses for 3-D feel) ────
            _orbitPathRings.Clear();
            var orbitRadiiPairs = new (double rx, double ry)[] { (70, 49), (115, 80), (160, 112) };
            foreach (var (rx, ry) in orbitRadiiPairs)
            {
                var ring = new Ellipse
                {
                    Width  = rx * 2, Height = ry * 2,
                    Stroke = new SolidColorBrush(Color.FromArgb(28, 74, 222, 128)),
                    StrokeThickness = 1.0,
                };
                Canvas.SetLeft(ring, cx - rx);
                Canvas.SetTop(ring,  cy - ry);
                _blockCanvas.Children.Add(ring);
                _orbitPathRings.Add(ring);
            }

            // ── Soft pulsing glow orb behind the leaf ─────────────────────────
            _leafGlowOrb = new Ellipse
            {
                Width  = 120, Height = 120,
                Fill = new RadialGradientBrush
                {
                    GradientStops =
                    {
                        new GradientStop { Color = Color.FromArgb(190, 50, 210, 85),  Offset = 0.00 },
                        new GradientStop { Color = Color.FromArgb(70,  20, 130, 45),  Offset = 0.55 },
                        new GradientStop { Color = Color.FromArgb(0,    0,   0,  0),  Offset = 1.00 },
                    }
                },
                Effect = new DropShadowEffect { BlurRadius = 55, Color = Color.Parse("#22C55E"), OffsetX = 0, OffsetY = 0, Opacity = 0.95 }
            };
            Canvas.SetLeft(_leafGlowOrb, cx - 60);
            Canvas.SetTop(_leafGlowOrb,  cy - 60);
            _blockCanvas.Children.Add(_leafGlowOrb);

            // ── 24 orbiting particles across 3 rings ──────────────────────────
            // Ring 0: 8 inner  (rx=70,  ry=49),  clockwise,         warm green
            // Ring 1: 8 middle (rx=115, ry=80),  counter-clockwise, bright green
            // Ring 2: 8 outer  (rx=160, ry=112), clockwise,         pale/cyan green
            _orbiterStates.Clear();
            _orbiterControls.Clear();

            var orbiterRings = new (int count, double rx, double ry, double speed, byte r, byte g, byte b, double szMin, double szMax)[]
            {
                ( 8,  70,  49, +0.012, 120, 255, 140, 4.0, 6.5),
                ( 8, 115,  80, -0.008,  74, 222, 128, 4.5, 7.0),
                ( 8, 160, 112, +0.006, 150, 240, 180, 3.5, 5.5),
            };
            foreach (var (count, rx, ry, speed, cr, cg, cb, szMin, szMax) in orbiterRings)
            {
                for (int i = 0; i < count; i++)
                {
                    double angle = Math.PI * 2.0 * i / count;
                    double sz    = szMin + rng.NextDouble() * (szMax - szMin);
                    var orb = new Ellipse
                    {
                        Width  = sz, Height = sz,
                        Fill   = new SolidColorBrush(Color.FromArgb(220, cr, cg, cb)),
                        Effect = new DropShadowEffect
                        {
                            BlurRadius = 12,
                            Color      = Color.FromArgb(200, cr, cg, cb),
                            OffsetX = 0, OffsetY = 0, Opacity = 0.95
                        },
                    };
                    _blockCanvas.Children.Add(orb);
                    _orbiterControls.Add(orb);
                    _orbiterStates.Add(new OrbiterState
                    {
                        Angle     = angle,
                        Speed     = speed * (0.8 + rng.NextDouble() * 0.4),
                        Rx        = rx,
                        Ry        = ry,
                        GlowPhase = rng.NextDouble() * Math.PI * 2,
                        GlowSpeed = 1.5 + rng.NextDouble() * 1.5,
                    });
                }
            }

            // ── 30 rising dust motes ──────────────────────────────────────────
            _sparkleStates.Clear();
            _sparkleControls.Clear();
            Color[] dustCols = { Color.Parse("#4ADE80"), Color.Parse("#86EFAC"), Color.Parse("#22C55E"), Color.Parse("#A3E635") };
            for (int i = 0; i < 30; i++)
            {
                double spawnX = cx + (rng.NextDouble() - 0.5) * 220;
                double spawnY = cy + 50 + rng.NextDouble() * 70;
                double sz     = 1.5 + rng.NextDouble() * 2.5;
                var sq = new Rectangle
                {
                    Width = sz, Height = sz,
                    Fill  = new SolidColorBrush(dustCols[i % dustCols.Length]),
                    Opacity = 0,
                };
                Canvas.SetLeft(sq, spawnX - sz / 2);
                Canvas.SetTop(sq,  spawnY - sz / 2);
                _blockCanvas.Children.Add(sq);
                _sparkleControls.Add(sq);
                _sparkleStates.Add(new SparkleState
                {
                    X = spawnX, Y = spawnY,
                    Vx = (rng.NextDouble() - 0.5) * 0.4,
                    Vy = -0.28 - rng.NextDouble() * 0.42,
                    Life    = rng.NextDouble() * 110,   // staggered starts
                    MaxLife = 100 + rng.NextDouble() * 80,
                    Size    = sz,
                    Angle = 0, AngularV = 0,
                });
            }

            // ── Leaf logo — exact canvas center, on top of everything ─────────
            _centerLeaf = new Image
            {
                Width  = 84, Height = 84,
                Source = new Avalonia.Media.Imaging.Bitmap(
                    Avalonia.Platform.AssetLoader.Open(new Uri("avares://LeafClient/Assets/LeafRebrandedTransparent.png"))),
                RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                RenderTransform = new ScaleTransform(1.0, 1.0),
                Effect = new DropShadowEffect { BlurRadius = 30, Color = Color.Parse("#4ADE80"), OffsetX = 0, OffsetY = 0, Opacity = 0.95 }
            };
            Canvas.SetLeft(_centerLeaf, cx - 42);
            Canvas.SetTop(_centerLeaf,  cy - 42);
            _blockCanvas.Children.Add(_centerLeaf);

            // ── Shimmer "LAUNCHING" text ──────────────────────────────────────
            _launchAnimText = new TextBlock
            {
                Text          = "LAUNCHING",
                FontSize      = 21,
                FontWeight    = FontWeight.Bold,
                LetterSpacing = 8,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.Parse("#2A6A2A")),
                Effect = new DropShadowEffect { BlurRadius = 14, Color = Color.Parse("#4ADE80"), OffsetX = 0, OffsetY = 0, Opacity = 0.55 }
            };
            _launchAnimSubText = new TextBlock
            {
                Text          = "LEAF CLIENT",
                FontSize      = 9,
                FontWeight    = FontWeight.Medium,
                LetterSpacing = 5,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromArgb(70, 74, 222, 128)),
                Margin = new Thickness(0, 6, 0, 0)
            };
            var contentStack = new StackPanel
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Center,
                Spacing = 0
            };
            contentStack.Children.Add(_blockCanvas);
            contentStack.Children.Add(_launchAnimText);
            contentStack.Children.Add(_launchAnimSubText);

            // ── Full-screen overlay ───────────────────────────────────────────
            _launchAnimOverlay = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(252, 2, 7, 3)),
                ZIndex     = 999,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Stretch,
                IsVisible  = false,
                Child      = contentStack
            };
            Grid.SetRowSpan(_launchAnimOverlay, 99);
            Grid.SetColumnSpan(_launchAnimOverlay, 99);
            rootGrid.Children.Add(_launchAnimOverlay);

            // ── 60 fps animation timer ────────────────────────────────────────
            _launchAnimTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _launchAnimTimer.Tick += TickLaunchAnimation;

            // ── Placeholder text timer (no-op — no text in new animation) ─────
            _launchAnimTextTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(60000) };
            _launchAnimTextTimer.Tick += (_, __) => { };
        }

        // Repositions all three polygons of one isometric block to centre (bx, by)
        private static void PositionBlock(Polygon top, Polygon left, Polygon right, double bx, double by, double s)
        {
            top.Points = new Points(new[]
            {
                new Point(bx,     by - s / 2),
                new Point(bx + s, by),
                new Point(bx,     by + s / 2),
                new Point(bx - s, by),
            });
            left.Points = new Points(new[]
            {
                new Point(bx - s, by),
                new Point(bx,     by + s / 2),
                new Point(bx,     by + s),
                new Point(bx - s, by + s / 2),
            });
            right.Points = new Points(new[]
            {
                new Point(bx + s, by),
                new Point(bx,     by + s / 2),
                new Point(bx,     by + s),
                new Point(bx + s, by + s / 2),
            });
        }

        private void TickLaunchAnimation(object? sender, EventArgs e)
        {
            if (_blockCanvas == null) return;
            _animTime += 0.016;
            const double cw = 400, ch = 360;
            double cx = cw / 2, cy = ch / 2;

            // ── Auto-start fade-out at 5 s ────────────────────────────────────
            if (!_animFadingOut && _animTime >= 5.0)
            {
                _animFadingOut = true;
                _animFadeTime  = 0;
            }

            // ── Handle fade-out phase ─────────────────────────────────────────
            if (_animFadingOut)
            {
                _animFadeTime += 0.016;
                const double fadeDur = 0.85;
                double fp    = Math.Min(1.0, _animFadeTime / fadeDur);
                double eased = fp * fp;                         // ease-in: slow → fast
                _launchAnimOverlay!.Opacity = 1.0 - eased;
                // Canvas breathes outward slightly as it fades — feels like entering the game
                if (_blockCanvas.RenderTransform is ScaleTransform cs)
                {
                    cs.ScaleX = 1.0 + 0.12 * eased;
                    cs.ScaleY = 1.0 + 0.12 * eased;
                }
                if (fp >= 1.0)
                {
                    _launchAnimTimer?.Stop();
                    _launchAnimOverlay.IsVisible = false;
                    _launchAnimOverlay.Opacity   = 1.0;
                    if (_blockCanvas.RenderTransform is ScaleTransform cs2) { cs2.ScaleX = 1; cs2.ScaleY = 1; }
                    if (_mainContentGrid != null) _mainContentGrid.Effect = null;
                    return;
                }
            }

            // ── Orbiting particles ────────────────────────────────────────────
            for (int i = 0; i < _orbiterStates.Count; i++)
            {
                var o = _orbiterStates[i];
                o.Angle     += o.Speed;
                o.GlowPhase += o.GlowSpeed * 0.016;

                // Elliptical orbit gives 3-D feel (full horizontal, foreshortened vertical)
                double ox = cx + Math.Cos(o.Angle) * o.Rx;
                double oy = cy + Math.Sin(o.Angle) * o.Ry;

                // Depth cue: back-half particles (sin > 0 in screen-space) appear
                // smaller and dimmer, giving them a sense of going behind the orb
                double depthT  = (Math.Sin(o.Angle) + 1.0) * 0.5; // 0=front, 1=back
                double scale   = 1.0 - depthT * 0.40;
                double opacity = (0.45 + (1.0 - depthT) * 0.55)
                               * (0.75 + 0.25 * Math.Sin(o.GlowPhase));

                var orb = _orbiterControls[i];
                double sz = orb.Width * scale;
                orb.Opacity = Math.Clamp(opacity, 0.05, 1.0);
                Canvas.SetLeft(orb, ox - sz * 0.5);
                Canvas.SetTop(orb,  oy - sz * 0.5);

                _orbiterStates[i] = o;
            }

            // ── Rising dust motes ─────────────────────────────────────────────
            for (int i = 0; i < _sparkleStates.Count; i++)
            {
                var p = _sparkleStates[i];
                p.Life += 1;
                if (p.Life > p.MaxLife)
                {
                    // Respawn below canvas centre, spread horizontally
                    double spawnX = cx + (((i * 17) % 220) - 110);
                    double spawnY = cy + 55 + (i % 45);
                    p.X = spawnX;
                    p.Y = spawnY;
                    p.Vx = (((i * 7) % 11) - 5) * 0.08;
                    p.Vy = -0.26 - ((i * 3) % 5) * 0.10;
                    p.Life    = 0;
                    p.MaxLife = 100 + (i * 13) % 80;
                }
                else
                {
                    p.X += p.Vx;
                    p.Y += p.Vy;
                }

                double lifeRatio = p.Life / p.MaxLife;
                double alpha = lifeRatio < 0.20 ? lifeRatio / 0.20
                             : lifeRatio > 0.72 ? 1.0 - (lifeRatio - 0.72) / 0.28
                             : 1.0;

                var sq = _sparkleControls[i];
                sq.Opacity = Math.Clamp(alpha * 0.72, 0, 1);
                Canvas.SetLeft(sq, p.X - p.Size * 0.5);
                Canvas.SetTop(sq,  p.Y - p.Size * 0.5);

                _sparkleStates[i] = p;
            }

            // ── Pulse glow orb ────────────────────────────────────────────────
            if (_leafGlowOrb != null)
            {
                double gs = 120.0 * (1.0 + 0.18 * Math.Sin(_animTime * 2.1));
                _leafGlowOrb.Width  = gs;
                _leafGlowOrb.Height = gs;
                Canvas.SetLeft(_leafGlowOrb, cx - gs * 0.5);
                Canvas.SetTop(_leafGlowOrb,  cy - gs * 0.5);
            }

            // ── Breathe leaf logo ─────────────────────────────────────────────
            if (_centerLeaf?.RenderTransform is ScaleTransform lst)
            {
                double breathe = 1.0 + 0.06 * Math.Sin(_animTime * 1.8);
                lst.ScaleX = breathe;
                lst.ScaleY = breathe;
            }

            // ── Shimmer sweep through "LAUNCHING" text ────────────────────────
            if (_launchAnimText != null)
            {
                double sweep = ((_animTime * 0.38) % 1.7) - 0.35;
                double s0 = Math.Clamp(sweep - 0.18, 0.0, 1.0);
                double s1 = Math.Clamp(sweep,         0.0, 1.0);
                double s2 = Math.Clamp(sweep + 0.18, 0.0, 1.0);
                _launchAnimText.Foreground = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                    EndPoint   = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop { Color = Color.Parse("#2A6A2A"), Offset = 0.0 },
                        new GradientStop { Color = Color.Parse("#2A6A2A"), Offset = s0  },
                        new GradientStop { Color = Color.Parse("#CCFFE0"), Offset = s1  },
                        new GradientStop { Color = Color.Parse("#2A6A2A"), Offset = s2  },
                        new GradientStop { Color = Color.Parse("#2A6A2A"), Offset = 1.0 },
                    }
                };
            }
        }

        public void ShowLaunchAnimation()
        {
            if (_launchAnimOverlay == null) return;
            _animTime      = 0;
            _animFadingOut = false;
            _animFadeTime  = 0;
            _launchAnimOverlay.Opacity = 1.0;
            if (_blockCanvas?.RenderTransform is ScaleTransform cs) { cs.ScaleX = 1; cs.ScaleY = 1; }
            if (_launchAnimText    != null) _launchAnimText.Text    = "LAUNCHING";
            if (_launchAnimSubText != null) _launchAnimSubText.Text = "Building your world...";
            if (_mainContentGrid   != null) _mainContentGrid.Effect = new BlurEffect { Radius = 8 };

            // Reset all blocks to their start positions so they fly in fresh
            for (int i = 0; i < _blockStates.Count; i++)
            {
                var b = _blockStates[i];
                b.X        = b.StartX;
                b.Y        = b.StartY;
                b.Progress = 0;
                b.Arrived  = false;
                _blockStates[i] = b;
                if (i < _blockControls.Count)
                {
                    var (pt, pl, pr) = _blockControls[i];
                    PositionBlock(pt, pl, pr, b.StartX, b.StartY, b.Size);
                }
            }

            _launchAnimOverlay.IsVisible = true;
            _launchAnimTimer?.Start();
            _launchAnimTextTimer?.Start();
        }

        public void HideLaunchAnimation()
        {
            if (_launchAnimOverlay == null || !_launchAnimOverlay.IsVisible) return;
            _launchAnimTextTimer?.Stop();
            // Trigger smooth fade-out instead of instant hide
            if (!_animFadingOut)
            {
                _animFadingOut = true;
                _animFadeTime  = 0;
                _launchAnimTimer?.Start(); // ensure tick is running for the fade
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Mods page sub-tab switcher (Mods / Resource Packs).  The Resource Packs
        // page is now embedded inside ModsPage instead of being its own sidebar
        // entry — this handler toggles which sub-view is visible and keeps the
        // pill-tab visual state in sync.
        private void OnModsTabTapped(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Border pill || pill.Tag is not string tag) return;

            var tabMods   = this.FindControl<Border>("ModsTab_Mods");
            var tabRp     = this.FindControl<Border>("ModsTab_Rp");
            var modsScroll = this.FindControl<ScrollViewer>("ModsContent");
            var title     = this.FindControl<TextBlock>("ModsPageTitle");
            var subtitle  = this.FindControl<TextBlock>("ModsPageSubtitle");
            var addLocal  = this.FindControl<Button>("ModsAddLocalBtn");
            var browse    = this.FindControl<Button>("ModsBrowseBtn");

            bool showMods = tag == "mods";

            if (modsScroll != null) modsScroll.IsVisible = showMods;
            if (_resourcePacksPage != null)
            {
                _resourcePacksPage.IsVisible = !showMods;
                if (!showMods) _resourcePacksPage.LoadResourcePacksPage();
            }

            // Hide the Mods-specific action buttons when viewing Resource Packs
            if (addLocal != null) addLocal.IsVisible = showMods;
            if (browse != null)   browse.IsVisible   = showMods;

            if (title != null)    title.Text    = showMods ? "MODS" : "RESOURCE PACKS";
            if (subtitle != null) subtitle.Text = showMods
                ? "Manage installed mods and resource packs"
                : "Install and browse resource packs";

            // Pill visual state
            if (tabMods != null)
            {
                tabMods.Background = showMods
                    ? SolidColorBrush.Parse("#9333EA")
                    : SolidColorBrush.Parse("#0F1A24");
                tabMods.BorderBrush = showMods
                    ? null
                    : SolidColorBrush.Parse("#1C2A38");
                tabMods.BorderThickness = showMods ? new Thickness(0) : new Thickness(1);
                if (tabMods.Child is TextBlock tm)
                    tm.Foreground = showMods ? Brushes.White : SolidColorBrush.Parse("#9CA3AF");
            }
            if (tabRp != null)
            {
                tabRp.Background = !showMods
                    ? SolidColorBrush.Parse("#9333EA")
                    : SolidColorBrush.Parse("#0F1A24");
                tabRp.BorderBrush = !showMods
                    ? null
                    : SolidColorBrush.Parse("#1C2A38");
                tabRp.BorderThickness = !showMods ? new Thickness(0) : new Thickness(1);
                if (tabRp.Child is TextBlock tr)
                    tr.Foreground = !showMods ? Brushes.White : SolidColorBrush.Parse("#9CA3AF");
            }
        }

        // ──────────────────────────────────────────────────────────────────────

        private async void AddLocalMods_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return;

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = "Select Mods",
                    AllowMultiple = true,
                    FileTypeFilter = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType("Minecraft Mods") { Patterns = new[] { "*.jar", "*.zip" } },
                        new Avalonia.Platform.Storage.FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                    }
                });

                if (files == null || files.Count == 0) return;

                // Determine the mods directory for the active profile
                string modsDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    ".minecraft", "mods");
                Directory.CreateDirectory(modsDir);

                int copied = 0;
                foreach (var file in files)
                {
                    var path = file.Path.IsFile ? file.Path.LocalPath : null;
                    if (path == null) continue;

                    string destName = System.IO.Path.GetFileName(path);
                    string destPath = System.IO.Path.Combine(modsDir, destName);

                    // Handle zip files - extract .jar files from inside
                    if (path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            using var archive = System.IO.Compression.ZipFile.OpenRead(path);
                            foreach (var entry in archive.Entries)
                            {
                                if (entry.FullName.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) && !entry.FullName.Contains('/'))
                                {
                                    string jarDest = System.IO.Path.Combine(modsDir, entry.Name);
                                    entry.ExtractToFile(jarDest, overwrite: true);
                                    copied++;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Mods] Error extracting zip {path}: {ex.Message}");
                            // Fall back to just copying the zip
                            File.Copy(path, destPath, overwrite: true);
                            copied++;
                        }
                    }
                    else
                    {
                        File.Copy(path, destPath, overwrite: true);
                        copied++;
                    }
                }

                Console.WriteLine($"[Mods] Copied {copied} mod file(s) to {modsDir}");
                LoadUserMods();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Mods] Error adding local mods: {ex.Message}");
            }
        }

        private async void OpenModBrowser(object? sender, RoutedEventArgs e)
        {
            if (_modBrowserOverlay == null || _modBrowserPanel == null) return;

            _modBrowserPanel.Opacity = 0;
            _modBrowserPanel.RenderTransform = new ScaleTransform(0.92, 0.92);
            if (_modBrowserBackdrop != null) _modBrowserBackdrop.Opacity = 0;

            _modBrowserOverlay.IsVisible = true;

            if (_modSearchBox != null) _modSearchBox.Text = "";
            if (_modSearchClearBtn != null) _modSearchClearBtn.IsVisible = false;
            if (_modDetailsSidebar != null) _modDetailsSidebar.IsVisible = false;
            _currentModQuery = "";
            _modOffset = 0;
            _ = SearchMods("", false);

            if (!AreAnimationsEnabled())
            {
                _modBrowserPanel.Opacity = 1;
                _modBrowserPanel.RenderTransform = new ScaleTransform(1, 1);
                if (_modBrowserBackdrop != null) _modBrowserBackdrop.Opacity = 1;
                return;
            }

            await Task.Delay(16);
            _modBrowserPanel.Opacity = 1;
            _modBrowserPanel.RenderTransform = TransformOperations.Parse("scale(1,1)");
            if (_modBrowserBackdrop != null) _modBrowserBackdrop.Opacity = 1;
        }

        private async void CloseModBrowser(object? sender, RoutedEventArgs e)
        {
            if (_modBrowserOverlay == null || _modBrowserPanel == null) return;

            if (!AreAnimationsEnabled())
            {
                _modBrowserOverlay.IsVisible = false;
                return;
            }

            _modBrowserPanel.Opacity = 0;
            _modBrowserPanel.RenderTransform = TransformOperations.Parse("scale(0.92,0.92)");
            if (_modBrowserBackdrop != null) _modBrowserBackdrop.Opacity = 0;

            await Task.Delay(380);
            _modBrowserOverlay.IsVisible = false;
        }

        private async void OnModSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            _modSearchCts?.Cancel();
            _modSearchCts = new CancellationTokenSource();

            var searchText = _modSearchBox?.Text ?? "";
            if (_modSearchClearBtn != null) _modSearchClearBtn.IsVisible = !string.IsNullOrEmpty(searchText);

            try
            {
                await Task.Delay(400, _modSearchCts.Token);
                _currentModQuery = searchText;
                _modOffset = 0;
                await SearchMods(searchText, false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void OnModSearchClear(object? sender, RoutedEventArgs e)
        {
            if (_modSearchBox != null) _modSearchBox.Text = "";
            if (_modSearchClearBtn != null) _modSearchClearBtn.IsVisible = false;
            _currentModQuery = "";
            _modOffset = 0;
            _ = SearchMods("", false);
        }

        private void OnLoaderChipTapped(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Border chip) return;
            var loader = chip.Tag?.ToString() ?? "all";
            if (_selectedLoader == loader) return;
            _selectedLoader = loader;
            ApplyLoaderSelection();
            _modOffset = 0;
            _ = SearchMods(_currentModQuery, false);
        }

        private void ApplyLoaderSelection()
        {
            var chips = new[] { _modLoaderAllChip, _modLoaderFabricChip };
            foreach (var chip in chips)
            {
                if (chip == null) continue;
                var chipTag = chip.Tag?.ToString() ?? "";
                bool active = chipTag == _selectedLoader;
                if (active)
                {
                    if (!chip.Classes.Contains("Active")) chip.Classes.Add("Active");
                    chip.Background = new SolidColorBrush(Color.Parse("#9333EA"));
                    chip.BorderBrush = new SolidColorBrush(Color.Parse("#A855F7"));
                }
                else
                {
                    chip.Classes.Remove("Active");
                    chip.Background = Avalonia.Media.Brushes.Transparent;
                    chip.BorderBrush = Avalonia.Media.Brushes.Transparent;
                }
                if (chip.Child is TextBlock tb)
                {
                    if (active)
                    {
                        if (!tb.Classes.Contains("Active")) tb.Classes.Add("Active");
                        tb.Foreground = new SolidColorBrush(Color.Parse("#FFFFFF"));
                    }
                    else
                    {
                        tb.Classes.Remove("Active");
                        tb.Foreground = new SolidColorBrush(Color.Parse("#94A3B8"));
                    }
                }
            }
        }

        private void OnCategoryTapped(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Border item) return;
            var cat = item.Tag as string;
            if (_selectedCategory == cat) return;
            _selectedCategory = cat;
            ApplyCategorySelection();
            _modOffset = 0;
            _ = SearchMods(_currentModQuery, false);
        }

        private void ApplyCategorySelection()
        {
            if (_modCategorySidebar == null) return;
            foreach (var child in _modCategorySidebar.Children)
            {
                if (child is not Border border) continue;
                var tag = border.Tag as string;
                bool active = tag == _selectedCategory;
                if (active)
                {
                    if (!border.Classes.Contains("Active")) border.Classes.Add("Active");
                    border.Background = new SolidColorBrush(Color.Parse("#1E1338"));
                }
                else
                {
                    border.Classes.Remove("Active");
                    border.Background = Avalonia.Media.Brushes.Transparent;
                }

                if (border.Child is StackPanel sp && sp.Children.Count >= 2 && sp.Children[1] is TextBlock tb)
                {
                    if (active)
                    {
                        if (!tb.Classes.Contains("Active")) tb.Classes.Add("Active");
                        tb.Foreground = new SolidColorBrush(Color.Parse("#C084FC"));
                    }
                    else
                    {
                        tb.Classes.Remove("Active");
                        tb.Foreground = new SolidColorBrush(Color.Parse("#94A3B8"));
                    }
                }
            }
        }

        private void OnModFilterChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_modVersionDropdown?.SelectedItem is ComboBoxItem vItem)
            {
                _selectedMcVersion = vItem.Tag as string;
            }
            if (_modSortDropdown?.SelectedItem is ComboBoxItem sItem)
            {
                _selectedSort = sItem.Tag as string ?? "downloads";
            }
            _modOffset = 0;
            _ = SearchMods(_currentModQuery, false);
        }

        private void OnModsResetFilters(object? sender, RoutedEventArgs e)
        {
            _selectedLoader = "all";
            _selectedCategory = null;
            _selectedSort = "downloads";
            _selectedMcVersion = null;
            _currentModQuery = "";
            if (_modSearchBox != null) _modSearchBox.Text = "";
            if (_modSearchClearBtn != null) _modSearchClearBtn.IsVisible = false;
            if (_modVersionDropdown != null) _modVersionDropdown.SelectedIndex = 0;
            if (_modSortDropdown != null) _modSortDropdown.SelectedIndex = 0;
            ApplyLoaderSelection();
            ApplyCategorySelection();
            _modOffset = 0;
            _ = SearchMods("", false);
        }

        private async void OnModsLoadMore(object? sender, RoutedEventArgs e)
        {
            await SearchMods(_currentModQuery, true);
        }

        private string BuildModrinthUrl(string query, int offset, int limit)
        {
            return BuildModrinthSearchUrl(query, _selectedMcVersion ?? "", offset, limit);
        }

        private string BuildModrinthSearchUrl(string query, string version, int offset, int limit)
        {
            var facets = new List<string>();
            facets.Add("[\"project_type:mod\"]");
            if (_selectedLoader != "all")
            {
                facets.Add($"[\"categories:{_selectedLoader}\"]");
            }
            if (!string.IsNullOrEmpty(_selectedCategory))
            {
                facets.Add($"[\"categories:{_selectedCategory}\"]");
            }
            if (!string.IsNullOrEmpty(version))
            {
                facets.Add($"[\"versions:{version}\"]");
            }
            var facetsJson = "[" + string.Join(",", facets) + "]";
            var sb = new System.Text.StringBuilder("https://api.modrinth.com/v2/search?");
            sb.Append($"limit={limit}");
            sb.Append($"&offset={offset}");
            sb.Append($"&index={Uri.EscapeDataString(_selectedSort)}");
            sb.Append($"&facets={Uri.EscapeDataString(facetsJson)}");
            if (!string.IsNullOrWhiteSpace(query))
            {
                sb.Append($"&query={Uri.EscapeDataString(query)}");
            }
            return sb.ToString();
        }

        private async Task SearchMods(string query, bool append)
        {
            if (_modsResultsGrid == null || _modsEmptyPanel == null || _modsSkeletonPanel == null || _modsLoadMoreBtn == null) return;

            const int pageSize = 30;
            if (!append)
            {
                _modOffset = 0;
                _modsResultsGrid.ItemsSource = null;
                _modsResultsGrid.Items.Clear();
                _modsResultsGrid.IsVisible = false;
                _modsEmptyPanel.IsVisible = false;
                _modsLoadMoreBtn.IsVisible = false;
                _modsSkeletonPanel.IsVisible = true;
                if (_modsResultCount != null) _modsResultCount.Text = "Searching Modrinth…";
            }
            else
            {
                _modsLoadMoreBtn.Content = "Loading…";
                _modsLoadMoreBtn.IsEnabled = false;
            }

            try
            {
                var apiUrl = BuildModrinthUrl(query, _modOffset, pageSize);
                var response = await _modrinthClient.GetStringAsync(apiUrl);
                var searchResponse = JsonSerializer.Deserialize(response, JsonContext.Default.ModrinthSearchResponse);

                string? usedFallbackVersion = null;

                if (searchResponse?.hits == null || (searchResponse.hits.Count == 0 && !append))
                {
                    if (!string.IsNullOrEmpty(_selectedMcVersion))
                    {
                        var fallback = ModrinthVersionFallback.GetFallbackVersion(_selectedMcVersion);
                        if (fallback != null)
                        {
                            var fallbackUrl = BuildModrinthSearchUrl(query, fallback, _modOffset, pageSize);
                            var fallbackJson = await _modrinthClient.GetStringAsync(fallbackUrl);
                            var fallbackResponse = JsonSerializer.Deserialize(fallbackJson, JsonContext.Default.ModrinthSearchResponse);
                            if (fallbackResponse?.hits != null && fallbackResponse.hits.Count > 0)
                            {
                                usedFallbackVersion = fallback;
                                searchResponse = fallbackResponse;
                            }
                        }
                    }
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _modsSkeletonPanel.IsVisible = false;

                    if (!append)
                    {
                        _modsResultsGrid.Items.Clear();
                    }

                    if (searchResponse?.hits == null || (searchResponse.hits.Count == 0 && !append))
                    {
                        if (_modsBrowserVersionNotice != null) _modsBrowserVersionNotice.IsVisible = false;
                        _modsEmptyPanel.IsVisible = true;
                        _modsResultsGrid.IsVisible = false;
                        _modsLoadMoreBtn.IsVisible = false;
                        if (_modsResultCount != null) _modsResultCount.Text = "No results found";
                        return;
                    }

                    if (usedFallbackVersion != null)
                    {
                        if (_modsBrowserVersionNotice != null) _modsBrowserVersionNotice.IsVisible = true;
                        if (_modsBrowserVersionNoticeText != null)
                            _modsBrowserVersionNoticeText.Text =
                                $"No mods found for {_selectedMcVersion} yet — showing compatible {usedFallbackVersion} mods instead";
                    }
                    else
                    {
                        if (_modsBrowserVersionNotice != null) _modsBrowserVersionNotice.IsVisible = false;
                    }

                    _modTotalHits = searchResponse.total_hits;
                    foreach (var mod in searchResponse.hits)
                    {
                        var modCard = CreateModCard(mod);
                        _modsResultsGrid.Items.Add(modCard);
                    }

                    _modOffset += searchResponse.hits.Count;
                    _modsResultsGrid.IsVisible = true;
                    _modsEmptyPanel.IsVisible = false;

                    if (_modsResultCount != null)
                    {
                        var scope = string.IsNullOrEmpty(query) ? "mods" : $"results for \"{query}\"";
                        _modsResultCount.Text = $"Showing {_modOffset} of {FormatCount(_modTotalHits)} {scope}";
                    }

                    _modsLoadMoreBtn.Content = "Load more";
                    _modsLoadMoreBtn.IsEnabled = true;
                    _modsLoadMoreBtn.IsVisible = _modOffset < _modTotalHits;
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _modsSkeletonPanel.IsVisible = false;
                    _modsLoadMoreBtn.Content = "Load more";
                    _modsLoadMoreBtn.IsEnabled = true;
                    if (!append)
                    {
                        _modsEmptyPanel.IsVisible = true;
                        _modsResultsGrid.IsVisible = false;
                        _modsLoadMoreBtn.IsVisible = false;
                        if (_modsBrowserVersionNotice != null) _modsBrowserVersionNotice.IsVisible = false;
                        if (_modsResultCount != null) _modsResultCount.Text = "Unable to load mods";
                    }
                    Console.WriteLine($"[Mod Browser] Error searching mods: {ex.Message}");
                });
            }
        }

        private Border CreateModCard(ModrinthProject mod)
        {
            var card = new Border
            {
                Height = 300,
                CornerRadius = new CornerRadius(14),
                BorderBrush = new SolidColorBrush(Color.Parse("#1A2332")),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(6),
                Cursor = new Cursor(StandardCursorType.Hand),
                ClipToBounds = true,
            };
            card.Classes.Add("ModCard");

            var bgGradient = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            };
            bgGradient.GradientStops.Add(new GradientStop(Color.Parse("#101826"), 0));
            bgGradient.GradientStops.Add(new GradientStop(Color.Parse("#0A1018"), 1));
            card.Background = bgGradient;

            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(128) });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var bannerGradient = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
            };
            bannerGradient.GradientStops.Add(new GradientStop(Color.Parse("#1E1338"), 0));
            bannerGradient.GradientStops.Add(new GradientStop(Color.Parse("#120A24"), 1));
            var banner = new Border
            {
                Background = bannerGradient,
                ClipToBounds = true,
            };
            Grid.SetRow(banner, 0);
            rootGrid.Children.Add(banner);

            var iconHolder = new Border
            {
                Width = 76,
                Height = 76,
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(Color.Parse("#0D1520")),
                BorderBrush = new SolidColorBrush(Color.Parse("#2E1B56")),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Padding = new Thickness(8),
                BoxShadow = BoxShadows.Parse("0 12 30 0 #80000000"),
            };
            var modIcon = new Image
            {
                Stretch = Avalonia.Media.Stretch.Uniform,
            };
            if (!string.IsNullOrEmpty(mod.icon_url)) _ = LoadModIcon(modIcon, mod.icon_url);
            else
                modIcon.Source = new Avalonia.Media.Imaging.Bitmap(
                    AssetLoader.Open(new Uri("avares://LeafClient/Assets/minecraft.png")));
            iconHolder.Child = modIcon;
            Grid.SetRow(iconHolder, 0);
            rootGrid.Children.Add(iconHolder);

            if (mod.loaders != null && mod.loaders.Count > 0)
            {
                var primaryLoader = mod.loaders.FirstOrDefault(l =>
                    l.Equals("fabric", StringComparison.OrdinalIgnoreCase) ||
                    l.Equals("forge", StringComparison.OrdinalIgnoreCase) ||
                    l.Equals("quilt", StringComparison.OrdinalIgnoreCase) ||
                    l.Equals("neoforge", StringComparison.OrdinalIgnoreCase)) ?? mod.loaders[0];
                var loaderBadge = new Border
                {
                    Background = new SolidColorBrush(Color.Parse("#CC0D1520")),
                    BorderBrush = new SolidColorBrush(Color.Parse("#7C3AED")),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(7),
                    Padding = new Thickness(8, 3),
                    Margin = new Thickness(10),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                };
                loaderBadge.Child = new TextBlock
                {
                    Text = CapitalizeFirst(primaryLoader),
                    FontSize = 10,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse("#C4B5FD")),
                };
                Grid.SetRow(loaderBadge, 0);
                rootGrid.Children.Add(loaderBadge);
            }

            var titleStack = new StackPanel
            {
                Spacing = 2,
                Margin = new Thickness(16, 14, 16, 0),
            };
            titleStack.Children.Add(new TextBlock
            {
                Text = mod.title ?? "Untitled mod",
                Foreground = new SolidColorBrush(Color.Parse("#F8FAFC")),
                FontWeight = FontWeight.Bold,
                FontSize = 14,
                TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
            });
            titleStack.Children.Add(new TextBlock
            {
                Text = $"by {mod.author ?? "unknown"}",
                Foreground = new SolidColorBrush(Color.Parse("#64748B")),
                FontSize = 11,
                TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
            });
            Grid.SetRow(titleStack, 1);
            rootGrid.Children.Add(titleStack);

            var descText = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(mod.description) ? "No description available." : mod.description,
                Foreground = new SolidColorBrush(Color.Parse("#94A3B8")),
                FontSize = 11,
                LineHeight = 16,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
                MaxLines = 3,
                Margin = new Thickness(16, 8, 16, 12),
            };
            Grid.SetRow(descText, 2);
            rootGrid.Children.Add(descText);

            var statsRow = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#080D14")),
                BorderBrush = new SolidColorBrush(Color.Parse("#1A2332")),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(16, 10),
            };
            var statsGrid = new Grid();
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            var dl = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 4 };
            dl.Children.Add(new TextBlock { Text = "⬇", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#A855F7")), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
            dl.Children.Add(new TextBlock { Text = FormatCount(mod.downloads), FontSize = 11, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Color.Parse("#CBD5E1")), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
            Grid.SetColumn(dl, 0);
            statsGrid.Children.Add(dl);
            var fl = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 4, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
            fl.Children.Add(new TextBlock { Text = "♥", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#F472B6")), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
            fl.Children.Add(new TextBlock { Text = FormatCount(mod.follows), FontSize = 11, FontWeight = FontWeight.SemiBold, Foreground = new SolidColorBrush(Color.Parse("#CBD5E1")), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
            Grid.SetColumn(fl, 1);
            statsGrid.Children.Add(fl);
            statsRow.Child = statsGrid;
            Grid.SetRow(statsRow, 3);
            rootGrid.Children.Add(statsRow);

            card.Child = rootGrid;
            card.PointerPressed += (s, e) => ShowModDetails(mod, modIcon.Source);
            return card;
        }

        private static string CapitalizeFirst(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return char.ToUpperInvariant(value[0]) + value.Substring(1).ToLowerInvariant();
        }

        private void ShowModDetails(ModrinthProject mod, IImage? iconSource)
        {
            _selectedMod = mod;

            if (_modDetailsSidebar != null) _modDetailsSidebar.IsVisible = true;
            if (_modDetailsTitle != null) _modDetailsTitle.Text = mod.title;
            if (_modDetailsDescription != null)
                _modDetailsDescription.Text = string.IsNullOrWhiteSpace(mod.description) ? "No description available." : mod.description;

            if (_modDetailsStats != null)
            {
                var author = string.IsNullOrWhiteSpace(mod.author) ? "unknown" : mod.author;
                _modDetailsStats.Text = $"by {author}   •   ⬇ {FormatCount(mod.downloads)}   •   ♥ {FormatCount(mod.follows)}";
            }

            if (_modDetailsIcon != null)
            {
                if (iconSource != null) _modDetailsIcon.Source = iconSource;
                else if (!string.IsNullOrEmpty(mod.icon_url)) _ = LoadModIcon(_modDetailsIcon, mod.icon_url);
            }

            if (_modDetailsDownloadButton != null)
            {
                bool isBundled = !string.IsNullOrEmpty(mod.project_id) && BundledModIds.Contains(mod.project_id);
                bool supportsFabric = mod.loaders == null || mod.loaders.Count == 0 ||
                    mod.loaders.Any(l => l.Equals("fabric", StringComparison.OrdinalIgnoreCase));

                if (isBundled)
                {
                    _modDetailsDownloadButton.IsEnabled = false;
                    _modDetailsDownloadButton.Content = "Already included with LeafClient";
                    _modDetailsDownloadButton.Background = new SolidColorBrush(Color.Parse("#1A2332"));
                }
                else if (!supportsFabric)
                {
                    _modDetailsDownloadButton.IsEnabled = false;
                    _modDetailsDownloadButton.Content = "Fabric only — not supported";
                    _modDetailsDownloadButton.Background = new SolidColorBrush(Color.Parse("#1A2332"));
                }
                else
                {
                    _modDetailsDownloadButton.IsEnabled = true;
                    _modDetailsDownloadButton.Content = "Download";
                    _modDetailsDownloadButton.Background = new SolidColorBrush(Color.Parse("#9333EA"));
                }
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
                var versionsUrl = $"https://api.modrinth.com/v2/project/{mod.project_id}/version";
                var versionsResponse = await _modrinthClient.GetStringAsync(versionsUrl);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                try
                {

                    var versions = JsonSerializer.Deserialize(versionsResponse, JsonContext.Default.ListModrinthVersionDetailed);


                    var currentVersion = _currentSettings.SelectedSubVersion;
                    var compatibleVersion = versions?.FirstOrDefault(v =>
                        v.GameVersions.Contains(currentVersion) &&
                        v.loaders.Contains("fabric"));

                    if (compatibleVersion == null)
                    {
                        ToastService.Show($"Installation failed\nNo compatible version for Minecraft {currentVersion} + Fabric.", ToastType.Error);
                        return;
                    }

                    var modFile = compatibleVersion.files.FirstOrDefault(f =>
                        f.filename.EndsWith(".jar", StringComparison.OrdinalIgnoreCase));

                    if (modFile == null)
                    {
                        ToastService.Show("Installation failed\nNo valid .jar file found.", ToastType.Error);
                        return;
                    }

                    var installedMod = new InstalledMod
                    {
                        ModId = mod.project_id,
                        Name = mod.title,
                        Description = mod.description ?? "No description available",
                        Version = compatibleVersion.version_number,
                        MinecraftVersion = currentVersion, 
                        FileName = modFile.filename,
                        DownloadUrl = modFile.url,
                        Enabled = true, 
                        InstallDate = DateTime.Now,
                        IconUrl = mod.icon_url ?? ""
                    };

                    await DownloadAndInstallMod(modFile.url, modFile.filename, mod.title, installedMod);

                    if (!_currentSettings.InstalledMods.Any(m => m.ModId == installedMod.ModId && m.MinecraftVersion == installedMod.MinecraftVersion))
                    {
                        _currentSettings.InstalledMods.Add(installedMod);
                        Console.WriteLine($"[User Mods] Added '{mod.title}' to settings, now {_currentSettings.InstalledMods.Count} mods total");
                        await _settingsService.SaveSettingsAsync(_currentSettings);
                        Console.WriteLine($"[User Mods] Settings saved successfully");
                    }
                    else
                    {
                        Console.WriteLine($"[User Mods] Mod '{mod.title}' for MC {installedMod.MinecraftVersion} already exists in settings. Updating existing entry.");
                        var existingMod = _currentSettings.InstalledMods.First(m => m.ModId == installedMod.ModId && m.MinecraftVersion == installedMod.MinecraftVersion);
                        existingMod.Name = installedMod.Name;
                        existingMod.Description = installedMod.Description;
                        existingMod.Version = installedMod.Version;
                        existingMod.FileName = installedMod.FileName;
                        existingMod.DownloadUrl = installedMod.DownloadUrl;
                        existingMod.Enabled = true; 
                        existingMod.InstallDate = DateTime.Now;
                        existingMod.IconUrl = installedMod.IconUrl;
                        await _settingsService.SaveSettingsAsync(_currentSettings);
                        Console.WriteLine($"[User Mods] Settings updated for '{mod.title}'.");
                    }


                    await Dispatcher.UIThread.InvokeAsync(() => { LoadUserMods(); });

                    ToastService.Show($"'{mod.title}' installed\nFind it in Installed Mods.", ToastType.Success);

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
                ToastService.Show($"Install failed\nJSON error: {jsonEx.Message}", ToastType.Error);
            }
            catch (Exception ex)
            {
                ToastService.Show($"Install failed\n{ex.Message}", ToastType.Error);
            }
        }

        private async Task InstallUserModsAsync(string mcVersion)
        {
            string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
            Directory.CreateDirectory(modsFolder);

            // Migration: prune any legacy "leafclient" entries from InstalledMods.
            // The Leaf core mod is NEVER installed via the user mods path — it's injected
            // through -Dfabric.addMods pointing at the offline/test-mode build. Having a
            // stale leafclient row in settings causes this loop to download the old GitHub
            // jar on every launch, leaving TWO leafclient mods on Fabric's classpath and
            // non-deterministically loading the wrong one. See LaunchDiag reports.
            int removedLegacy = _currentSettings.InstalledMods.RemoveAll(
                m => m.ModId != null && m.ModId.Equals("leafclient", StringComparison.OrdinalIgnoreCase));
            if (removedLegacy > 0)
            {
                _gameOutputWindow?.AppendLog(
                    $"[LaunchDiag] Pruned {removedLegacy} legacy 'leafclient' entry/entries from InstalledMods (handled via -Dfabric.addMods instead).",
                    "WARN");
                Console.WriteLine($"[User Mods] Removed {removedLegacy} legacy leafclient entries from settings.");
                await _settingsService.SaveSettingsAsync(_currentSettings);
            }

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
                // Extra safety: never install anything identified as leafclient via this path.
                // The legacy entry is already pruned above, but a future bug elsewhere that
                // re-adds it should still not break the launch.
                if (mod.ModId != null && mod.ModId.Equals("leafclient", StringComparison.OrdinalIgnoreCase))
                {
                    _gameOutputWindow?.AppendLog(
                        $"[LaunchDiag] Skipping install of '{mod.Name}' (ModId=leafclient) — core mod is injected via -Dfabric.addMods, not the mods folder.",
                        "WARN");
                    continue;
                }

                try
                {
                    ShowProgress(true, $"Ensuring '{mod.Name}' is installed...");

                    string targetFilePath = GetModFilePath(modsFolder, mod, isDisabled: false); 
                    string disabledFilePath = GetModFilePath(modsFolder, mod, isDisabled: true); 

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
                            continue; 
                        }
                    }

                    if (mod.DownloadUrl.Equals("internal", StringComparison.OrdinalIgnoreCase))
                    {
                        string? launcherDir = AppContext.BaseDirectory;
                        if (string.IsNullOrEmpty(launcherDir))
                        {
                            string? launcherExePath = Environment.ProcessPath;
                            if (!string.IsNullOrEmpty(launcherExePath))
                            {
                                launcherDir = System.IO.Path.GetDirectoryName(launcherExePath);
                            }
                        }

                        if (string.IsNullOrEmpty(launcherDir))
                        {
                            Console.Error.WriteLine($"[User Mods] Could not determine launcher directory for internal mod '{mod.Name}'.");
                            ShowLaunchErrorBanner($"Internal mod '{mod.Name}' could not be located. Launch might fail.");
                            continue; 
                        }

                        string sourceLeafClientJarPath = System.IO.Path.Combine(launcherDir, mod.FileName);

                        if (System.IO.File.Exists(sourceLeafClientJarPath))
                        {
                            try
                            {
                                System.IO.File.Copy(sourceLeafClientJarPath, targetFilePath, true);
                                Console.WriteLine($"[User Mods] Copied internal mod '{mod.Name}' from launcher dir to mods folder.");
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine($"[User Mods] Failed to copy internal mod '{mod.Name}': {ex.Message}");
                                ShowLaunchErrorBanner($"Failed to copy internal mod '{mod.Name}'. It might be in use.");
                            }
                        }
                        else
                        {
                            Console.Error.WriteLine($"[User Mods] Internal mod '{mod.Name}' ({mod.FileName}) not found next to launcher EXE: {sourceLeafClientJarPath}");
                            ShowLaunchErrorBanner($"Internal mod '{mod.Name}' not found. Launch might fail.");
                        }
                        continue; 
                    }
                    if (File.Exists(targetFilePath))
                    {
                        Console.WriteLine($"[User Mods] '{mod.Name}' already exists, skipping download.");
                        continue; 
                    }

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
            if (_feedbackOverlay == null || _feedbackPanel == null)
            {
                Console.Error.WriteLine("[CRITICAL ERROR] FeedbackOverlay or FeedbackPanel were not initialized. Check MainWindow.axaml and InitializeControls.");
                return;
            }

            if (_feedbackOverlay.IsVisible || _isFeedbackAnimating) return;

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

            _osVersionTextBox.Text = Environment.OSVersion.ToString();

            Version? currentAppVersion = GetCurrentAppVersion();
            _leafClientVersionTextBox.Text = currentAppVersion != null ?
                                            $"Launcher v{currentAppVersion.Major}.{currentAppVersion.Minor}.{currentAppVersion.Build}" :
                                            "N/A";

            SuggestionType initialType = SuggestionType.Feature;
            if (sender is Button button && button.Tag is string buttonTag)
            {
                if (Enum.TryParse(buttonTag, out SuggestionType parsedType)) initialType = parsedType;
            }
            else if (sender is MenuItem menuItem && menuItem.Tag is string menuTag)
            {
                if (Enum.TryParse(menuTag, out SuggestionType parsedType)) initialType = parsedType;
            }
            SetInitialFeedbackType(initialType);

            _suggestionTextBox.Text = string.Empty;
            _expectedBehaviorTextBox.Text = string.Empty;
            _actualBehaviorTextBox.Text = string.Empty;
            _stepsToReproduceTextBox.Text = string.Empty;
            _attachedLogFileContent = null;
            _attachedLogFileName.Text = "No file attached";
            _statusMessageTextBlock.Text = string.Empty;

            _feedbackLogFolderPath = _logFolderPath;

            try
            {
                var backdrop = this.FindControl<Border>("FeedbackBackdrop");
                await AnimateOverlayOpenAsync(_feedbackOverlay, _feedbackPanel, backdrop);
            }
            finally { _isFeedbackAnimating = false; }
        }

        /// <summary>
        /// Closes the feedback overlay.
        /// </summary>
        private async void CloseFeedbackOverlay(object? sender, RoutedEventArgs e)
        {
            if (_feedbackOverlay == null || _feedbackPanel == null) return;
            if (!_feedbackOverlay.IsVisible || _isFeedbackAnimating) return;

            _isFeedbackAnimating = true;
            try
            {
                var backdrop = this.FindControl<Border>("FeedbackBackdrop");
                await AnimateOverlayCloseAsync(_feedbackOverlay, _feedbackPanel, backdrop);
            }
            finally { _isFeedbackAnimating = false; }
        }

        /// <summary>
        /// Handles the selection change in the feedback type ComboBox.
        /// The ComboBox itself is hidden in the redesigned overlay and is
        /// driven by the pill-tab buttons via <see cref="OnFeedbackTabTapped"/>,
        /// but the existing handler still toggles panel visibility so we keep it.
        /// </summary>
        private void FeedbackTypeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_feedbackTypeComboBox?.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is string tag)
            {
                if (_featureSuggestionPanel != null) _featureSuggestionPanel.IsVisible = (tag == "Feature");
                if (_bugReportPanel != null) _bugReportPanel.IsVisible = (tag == "Bug");
                if (_statusMessageTextBlock != null) _statusMessageTextBlock.Text = "";
                UpdateFeedbackTabVisuals(tag);
            }
        }

        /// <summary>
        /// Click/pointer handler for the redesigned pill tabs in the Feedback
        /// overlay. Reads the source Border's Tag ("Feature" or "Bug") and
        /// flips the hidden ComboBox — which in turn triggers panel toggling
        /// via <see cref="FeedbackTypeComboBox_SelectionChanged"/>.
        /// </summary>
        private void OnFeedbackTabTapped(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if (sender is not Avalonia.Controls.Border border || border.Tag is not string tag) return;
            if (_feedbackTypeComboBox?.Items is not { } items) return;

            var target = items.OfType<ComboBoxItem>().FirstOrDefault(i => (i.Tag as string) == tag);
            if (target != null)
            {
                _feedbackTypeComboBox.SelectedItem = target;
            }
            e.Handled = true;
        }

        /// <summary>
        /// Applies the "active" gradient to the selected feedback pill tab and
        /// reverts the other one to its idle (transparent) look.
        /// </summary>
        private void UpdateFeedbackTabVisuals(string activeTag)
        {
            var featureTab = this.FindControl<Avalonia.Controls.Border>("FeedbackFeatureTab");
            var bugTab     = this.FindControl<Avalonia.Controls.Border>("FeedbackBugTab");
            if (featureTab == null || bugTab == null) return;

            var activeBrush = new Avalonia.Media.LinearGradientBrush
            {
                StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative),
                EndPoint   = new Avalonia.RelativePoint(1, 1, Avalonia.RelativeUnit.Relative),
                GradientStops =
                {
                    new Avalonia.Media.GradientStop(Avalonia.Media.Color.Parse("#9333EA"), 0),
                    new Avalonia.Media.GradientStop(Avalonia.Media.Color.Parse("#7B2CBF"), 0.55),
                    new Avalonia.Media.GradientStop(Avalonia.Media.Color.Parse("#6B21A8"), 1),
                }
            };
            var idleBrush = Avalonia.Media.Brushes.Transparent;

            var activeText = Avalonia.Media.Brush.Parse("#FFFFFF");
            var idleText   = Avalonia.Media.Brush.Parse("#9CA3AF");

            bool featureActive = activeTag == "Feature";
            featureTab.Background = featureActive ? activeBrush : idleBrush;
            bugTab.Background     = featureActive ? idleBrush   : activeBrush;

            // Recolour the label + icon inside each pill.
            PaintPillChildren(featureTab, featureActive ? activeText : idleText);
            PaintPillChildren(bugTab,     featureActive ? idleText   : activeText);
        }

        /// <summary>
        /// Helper that walks a feedback pill Border and colours its TextBlock
        /// and Path children to match the active/idle palette.
        /// </summary>
        private static void PaintPillChildren(Avalonia.Controls.Border pill, Avalonia.Media.IBrush brush)
        {
            foreach (var tb in pill.GetVisualDescendants().OfType<Avalonia.Controls.TextBlock>())
            {
                tb.Foreground = brush;
            }
            foreach (var path in pill.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>())
            {
                path.Stroke = brush;
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
            else 
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
                await Task.Delay(2000); 
                CloseFeedbackOverlay(null, new RoutedEventArgs()); 
            }
        }

        /// <summary>
        /// Handles the Cancel button click in the feedback overlay.
        /// </summary>
        private void CancelFeedbackButton_Click(object? sender, RoutedEventArgs e)
        {
            CloseFeedbackOverlay(null, new RoutedEventArgs());
        }


        // ─── Crash Report Overlay ────────────────────────────────────────────

        /// <summary>
        /// Shows the crash report overlay with slide-in animation.
        /// Call this from App.axaml.cs after capturing the screenshot.
        /// </summary>
        public async void ShowCrashReportOverlay(Exception ex, byte[]? screenshotBytes)
        {
            if (_crashReportOverlay == null || _crashReportPanel == null) return;
            if (_crashReportOverlay.IsVisible || _isCrashAnimating) return;

            _currentCrashException = ex;
            _currentScreenshotBytes = screenshotBytes;
            _lastCrashSendResult = null;

            // Reset state
            if (_crashSendButton != null) _crashSendButton.IsEnabled = true;
            if (_crashDismissButton != null) _crashDismissButton.IsEnabled = true;
            if (_crashStatusLine != null) { _crashStatusLine.Text = ""; _crashStatusLine.IsVisible = false; }

            // Hide the copy / open-folder fallback buttons until the send
            // attempt finishes and we know whether a local copy exists.
            var copyBtnReset = this.FindControl<Button>("CrashCopyButton");
            var openBtnReset = this.FindControl<Button>("CrashOpenFolderButton");
            if (copyBtnReset != null) copyBtnReset.IsVisible = false;
            if (openBtnReset != null) openBtnReset.IsVisible = false;
            if (_crashScreenshotLine != null)
                _crashScreenshotLine.Text = screenshotBytes != null
                    ? "A screenshot was taken when this happened."
                    : "No screenshot was captured.";

            _isCrashAnimating = true;
            _crashReportOverlay.IsVisible = true;

            var tt = _crashReportPanel.RenderTransform as TranslateTransform ?? new TranslateTransform();
            _crashReportPanel.RenderTransform = tt;

            if (!AreAnimationsEnabled())
            {
                tt.Y = 0;
                _crashReportPanel.Opacity = 1;
                _isCrashAnimating = false;
                return;
            }

            const int durationMs = 500;
            const int steps = 30;
            tt.Y = -700;
            _crashReportPanel.Opacity = 0;

            try
            {
                for (int i = 0; i <= steps; i++)
                {
                    double eased = 1 - Math.Pow(1 - (double)i / steps, 3);
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_crashReportPanel == null) return;
                        tt.Y = -700 + 700 * eased;
                        _crashReportPanel.Opacity = eased;
                    });
                    if (i < steps) await Task.Delay(durationMs / steps);
                }
            }
            finally
            {
                _isCrashAnimating = false;
            }
        }

        private async void CloseCrashOverlay()
        {
            if (_crashReportOverlay == null || _crashReportPanel == null) return;
            if (!_crashReportOverlay.IsVisible || _isCrashAnimating) return;

            _isCrashAnimating = true;

            if (!AreAnimationsEnabled())
            {
                _crashReportPanel.Opacity = 0;
                _crashReportOverlay.IsVisible = false;
                _isCrashAnimating = false;
                return;
            }

            // Reset any translation from the entry animation
            if (_crashReportPanel.RenderTransform is TranslateTransform tt2)
                tt2.Y = 0;

            const int durationMs = 320;
            const int steps = 22;

            var blurEffect = new Avalonia.Media.BlurEffect { Radius = 0 };
            _crashReportPanel.Effect = blurEffect;

            try
            {
                for (int i = 0; i <= steps; i++)
                {
                    double t = (double)i / steps;
                    double eased = t * t; // ease-in: slow start, fast end
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_crashReportPanel == null) return;
                        _crashReportPanel.Opacity = 1 - eased;
                        blurEffect.Radius = eased * 18;
                    });
                    if (i < steps) await Task.Delay(durationMs / steps);
                }
                _crashReportOverlay.IsVisible = false;
            }
            finally
            {
                _isCrashAnimating = false;
                _currentCrashException = null;
                _currentScreenshotBytes = null;
                if (_crashReportPanel != null)
                {
                    _crashReportPanel.Effect = null;
                    _crashReportPanel.Opacity = 1;
                }
            }
        }

        // Holds the most recent crash-send outcome so the user can copy or
        // open the locally-saved report even after the send finishes.
        private LeafClient.Services.CrashReportService.SendResult? _lastCrashSendResult;

        private async void CrashSendButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_crashSendButton != null) _crashSendButton.IsEnabled = false;
            if (_crashDismissButton != null) _crashDismissButton.IsEnabled = false;

            if (_crashStatusLine != null)
            {
                _crashStatusLine.Text = "Sending report...";
                _crashStatusLine.IsVisible = true;
                _crashStatusLine.Foreground = GetBrush("SecondaryForegroundBrush");
            }

            LeafClient.Services.CrashReportService.SendResult? result = null;
            try
            {
                var ex = _currentCrashException;
                var screenshot = _currentScreenshotBytes;
                if (ex != null)
                {
                    var settingsService = new LeafClient.Services.SettingsService();
                    var settings = await settingsService.LoadSettingsAsync();
                    result = await LeafClient.Services.CrashReportService.SendAsync(ex, screenshot, settings);
                }
            }
            catch { /* never propagate from the crash reporter */ }

            _lastCrashSendResult = result;

            // Reveal the fallback actions only when we actually have a saved
            // copy on disk — if both the network AND the local save failed
            // there's nothing for the user to open.
            var copyBtn = this.FindControl<Button>("CrashCopyButton");
            var openBtn = this.FindControl<Button>("CrashOpenFolderButton");
            bool haveSaved = result is { SavedLocally: true, SavedPath: not null };
            if (copyBtn != null) copyBtn.IsVisible = haveSaved;
            if (openBtn != null) openBtn.IsVisible = haveSaved;

            if (_crashStatusLine != null)
            {
                if (result is { NetworkSent: true })
                {
                    _crashStatusLine.Text = haveSaved
                        ? "Report sent. Thank you! ✅  (Saved a copy locally too.)"
                        : "Report sent. Thank you! ✅";
                    _crashStatusLine.Foreground = GetBrush("SuccessBrush");
                }
                else
                {
                    string reason = result?.Failure switch
                    {
                        LeafClient.Services.CrashReportService.FailureKind.IspBlockOrTlsIntercept
                            => "Your network is blocking our upload endpoint (ISP/firewall/TLS intercept).",
                        LeafClient.Services.CrashReportService.FailureKind.Timeout
                            => "The upload timed out.",
                        LeafClient.Services.CrashReportService.FailureKind.NetworkUnreachable
                            => "Couldn't reach the upload endpoint. Check your connection.",
                        LeafClient.Services.CrashReportService.FailureKind.ServerRejected
                            => "The server rejected the report.",
                        _ => "Couldn't send the report.",
                    };

                    _crashStatusLine.Text = haveSaved
                        ? $"{reason}\nSaved locally — use the buttons below to copy or open it and send via Discord."
                        : reason;
                    _crashStatusLine.Foreground = GetBrush("ErrorBrush");
                }
            }

            if (_crashSendButton != null) _crashSendButton.IsEnabled = true;
            if (_crashDismissButton != null) _crashDismissButton.IsEnabled = true;

            // Only auto-close on success. When the send failed we leave the
            // overlay open so the user has time to read the error and use
            // the Copy / Open Folder buttons.
            if (result is { NetworkSent: true })
            {
                await Task.Delay(1800);
                CloseCrashOverlay();
            }
        }

        /// <summary>
        /// Copies the JSON crash payload from the most recent send attempt
        /// to the clipboard, so the user can paste it into Discord/email.
        /// </summary>
        private async void CrashCopyButton_Click(object? sender, RoutedEventArgs e)
        {
            var result = _lastCrashSendResult;
            if (result?.PayloadJson == null) return;

            try
            {
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard != null)
                {
                    await clipboard.SetTextAsync(result.PayloadJson);
                    if (_crashStatusLine != null)
                    {
                        _crashStatusLine.Text = "Copied crash JSON to clipboard — paste it into Discord and we'll take a look.";
                        _crashStatusLine.Foreground = GetBrush("SuccessBrush");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[CrashReport] Copy to clipboard failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Opens the %APPDATA%\LeafClient\CrashReports folder in Explorer so
        /// the user can grab the saved .json / .txt and share it manually.
        /// </summary>
        private void CrashOpenFolderButton_Click(object? sender, RoutedEventArgs e)
        {
            var folder = _lastCrashSendResult?.SavedFolder;
            if (string.IsNullOrEmpty(folder) || !System.IO.Directory.Exists(folder)) return;

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[CrashReport] Open folder failed: {ex.Message}");
            }
        }

        private void CrashDismissButton_Click(object? sender, RoutedEventArgs e)
        {
            CloseCrashOverlay();
        }

        // ─────────────────────────────────────────────────────────────────────

        // ─────────────────────────────────────────────────────────────────
        // Smooth overlay open/close helpers.
        //
        // The old frame-stepped animations (30 × Task.Delay(16ms)) stuttered
        // badly because the timer granularity on Windows is only ~15ms, so
        // every step ended up 15-30ms instead of 16.7ms. We now rely on the
        // Avalonia transition system — the panel itself has a DoubleTransition
        // for Opacity and a TransformOperationsTransition for RenderTransform
        // configured in XAML, and we just set the target values here. The
        // compositor interpolates at render-tick cadence so it stays glassy
        // smooth regardless of UI thread load.
        //
        // The pattern:
        //   1. Make the overlay IsVisible=true so the panel is laid out.
        //   2. Yield one background-priority tick so the starting state
        //      (scale 0.92, opacity 0) actually gets rendered.
        //   3. Set the target state (scale 1.0, opacity 1) — the Transition
        //      kicks in and animates to it.
        //   4. For close: set the target to the hidden state, wait the
        //      transition duration, then set IsVisible=false.
        // ─────────────────────────────────────────────────────────────────
        private const int OverlayAnimDurationMs = 320;

        private async Task AnimateOverlayOpenAsync(
            Grid? overlay, Border? panel, Border? backdrop)
        {
            if (overlay == null || panel == null) return;

            overlay.IsVisible = true;

            // Initial (closed) state. Do this explicitly so re-opening after
            // a previous close animation starts from the right values.
            panel.Opacity = 0;
            if (panel.RenderTransform is ScaleTransform st0)
            {
                st0.ScaleX = 0.92;
                st0.ScaleY = 0.92;
            }
            else
            {
                panel.RenderTransform = new ScaleTransform { ScaleX = 0.92, ScaleY = 0.92 };
            }
            if (backdrop != null) backdrop.Opacity = 0;

            if (!AreAnimationsEnabled())
            {
                panel.Opacity = 1;
                if (panel.RenderTransform is ScaleTransform stI)
                {
                    stI.ScaleX = 1.0;
                    stI.ScaleY = 1.0;
                }
                if (backdrop != null) backdrop.Opacity = 1;
                return;
            }

            // Wait one background-priority tick so the initial state actually
            // renders before we set the target, otherwise the transition
            // snaps straight to the end.
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

            if (backdrop != null) backdrop.Opacity = 1;
            panel.Opacity = 1;
            if (panel.RenderTransform is ScaleTransform stT)
            {
                stT.ScaleX = 1.0;
                stT.ScaleY = 1.0;
            }

            await Task.Delay(OverlayAnimDurationMs);
        }

        private async Task AnimateOverlayCloseAsync(
            Grid? overlay, Border? panel, Border? backdrop)
        {
            if (overlay == null || panel == null || !overlay.IsVisible) return;

            if (!AreAnimationsEnabled())
            {
                panel.Opacity = 0;
                if (panel.RenderTransform is ScaleTransform stI)
                {
                    stI.ScaleX = 0.92;
                    stI.ScaleY = 0.92;
                }
                if (backdrop != null) backdrop.Opacity = 0;
                overlay.IsVisible = false;
                return;
            }

            if (backdrop != null) backdrop.Opacity = 0;
            panel.Opacity = 0;
            if (panel.RenderTransform is ScaleTransform st)
            {
                st.ScaleX = 0.92;
                st.ScaleY = 0.92;
            }

            await Task.Delay(OverlayAnimDurationMs);
            overlay.IsVisible = false;
        }

        private async void OpenAboutLeafClient(object? sender, RoutedEventArgs e)
        {
            if (_aboutLeafClientOverlay == null || _aboutLeafClientPanel == null) return;
            if (_aboutLeafClientOverlay.IsVisible || _isAboutLeafClientAnimating) return;

            _isAboutLeafClientAnimating = true;
            try
            {
                var backdrop = this.FindControl<Border>("AboutLeafClientBackdrop");
                await AnimateOverlayOpenAsync(_aboutLeafClientOverlay, _aboutLeafClientPanel, backdrop);
            }
            finally { _isAboutLeafClientAnimating = false; }
        }

        private async void CloseAboutLeafClient(object? sender, RoutedEventArgs e)
        {
            if (_aboutLeafClientOverlay == null || _aboutLeafClientPanel == null) return;
            if (!_aboutLeafClientOverlay.IsVisible || _isAboutLeafClientAnimating) return;

            _isAboutLeafClientAnimating = true;
            try
            {
                var backdrop = this.FindControl<Border>("AboutLeafClientBackdrop");
                await AnimateOverlayCloseAsync(_aboutLeafClientOverlay, _aboutLeafClientPanel, backdrop);
            }
            finally { _isAboutLeafClientAnimating = false; }
        }


        private async void OpenCheckout(string url)
        {
            if (_checkoutOverlay == null || _checkoutPanel == null) return;

            if (_checkoutOverlay.IsVisible || _isCheckoutAnimating) return;

            _isCheckoutAnimating = true;
            _checkoutPurchaseDetected = false;
            _checkoutPreOwnedIds = new HashSet<string>(_ownedCosmeticIds);
            _checkoutPollCts?.Cancel();
            _checkoutPollCts = new CancellationTokenSource();
            _ = PollForCheckoutPurchaseAsync(_checkoutPollCts.Token);
            _checkoutOverlay.IsVisible = true;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Checkout] Failed to open browser: {ex.Message}");
            }

            var tt = _checkoutPanel.RenderTransform as TranslateTransform;
            if (tt == null)
            {
                tt = new TranslateTransform { Y = 900 };
                _checkoutPanel.RenderTransform = tt;
            }

            if (!AreAnimationsEnabled())
            {
                tt.Y = 0;
                _isCheckoutAnimating = false;
                return;
            }

            const int durationMs = 300;
            const int steps = 20;
            const int delayMs = durationMs / steps;

            try
            {
                for (int i = 0; i <= steps; i++)
                {
                    double progress = (double)i / steps;
                    double eased = 1 - Math.Pow(1 - progress, 3);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_checkoutPanel == null) return;
                        tt.Y = 900 * (1 - eased);
                    });

                    if (i < steps)
                        await Task.Delay(delayMs);
                }
            }
            finally
            {
                _isCheckoutAnimating = false;
            }
        }

        private async void CloseCheckout()
        {
            if (_checkoutOverlay == null || _checkoutPanel == null) return;
            if (!_checkoutOverlay.IsVisible || _isCheckoutAnimating) return;

            _isCheckoutAnimating = true;

            _checkoutPollCts?.Cancel();
            _checkoutPollCts = null;

            var tt = _checkoutPanel.RenderTransform as TranslateTransform;
            if (tt == null)
            {
                tt = new TranslateTransform();
                _checkoutPanel.RenderTransform = tt;
            }

            if (!AreAnimationsEnabled())
            {
                tt.Y = 900;
                _checkoutOverlay.IsVisible = false;
                _isCheckoutAnimating = false;
                return;
            }

            const int durationMs = 250;
            const int steps = 16;
            const int delayMs = durationMs / steps;

            try
            {
                for (int i = 0; i <= steps; i++)
                {
                    double progress = (double)i / steps;
                    double eased = Math.Pow(progress, 2);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_checkoutPanel == null) return;
                        tt.Y = 900 * eased;
                    });

                    if (i < steps)
                        await Task.Delay(delayMs);
                }

                _checkoutOverlay.IsVisible = false;
            }
            finally
            {
                _isCheckoutAnimating = false;
            }

            if (!_checkoutPurchaseDetected)
            {
                _ = SyncAfterCheckoutAsync();
            }
        }

        private async Task SyncAfterCheckoutAsync()
        {
            try
            {
                var beforeIds = new HashSet<string>(_ownedCosmeticIds);
                await SyncOwnedCosmeticsFromApiAsync();
                var newIds = _ownedCosmeticIds.Where(id => !beforeIds.Contains(id)).ToList();
                if (newIds.Count > 0)
                {
                    var firstNewId = newIds[0];
                    var catalog = LeafClient.Views.Pages.StorePageView.StoreCatalog;
                    var item = System.Array.Find(catalog, c => c.Id == firstNewId);
                    if (item.Id != null)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            ShowPurchaseCelebration(item.Id, item.Name, item.Preview, item.Rarity);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PostCheckout] Sync failed: {ex.Message}");
            }
        }

        private async Task PollForCheckoutPurchaseAsync(CancellationToken ct)
        {
            try
            {
                await Task.Delay(3000, ct);

                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        Console.WriteLine("[CheckoutPoll] Checking for new cosmetics...");
                        await SyncOwnedCosmeticsFromApiAsync();

                        if (_checkoutPreOwnedIds != null)
                        {
                            var newIds = _ownedCosmeticIds
                                .Where(id => !_checkoutPreOwnedIds.Contains(id))
                                .ToList();

                            if (newIds.Count > 0)
                            {
                                Console.WriteLine($"[CheckoutPoll] New cosmetic detected: {string.Join(", ", newIds)}");
                                _checkoutPurchaseDetected = true;

                                var firstNewId = newIds[0];
                                var catalog = LeafClient.Views.Pages.StorePageView.StoreCatalog;
                                var item = System.Array.Find(catalog, c => c.Id == firstNewId);

                                await Dispatcher.UIThread.InvokeAsync(() =>
                                {
                                    CloseCheckout();

                                    if (item.Id != null)
                                    {
                                        ShowPurchaseCelebration(item.Id, item.Name, item.Preview, item.Rarity);
                                    }
                                });

                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[CheckoutPoll] Poll error: {ex.Message}");
                    }

                    await Task.Delay(2000, ct);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"[CheckoutPoll] Unexpected error: {ex.Message}");
            }
        }

        private void OnCheckoutClose(object? sender, TappedEventArgs e) => CloseCheckout();

        private void OnCheckoutBackdropTapped(object? sender, TappedEventArgs e) => CloseCheckout();

        private async void ShowPurchaseChoice(string itemId, string itemName, string preview, string rarity, string priceText, int coinPrice, string checkoutUrl)
        {
            if (_purchasePopupOverlay != null && _purchasePopupOverlay.IsVisible) return;
            if (_isPurchasePopupAnimating) return;

            _pendingPurchaseItemId = itemId;

            var rarityColor = rarity switch
            {
                "Legendary" => Color.Parse("#FFD700"),
                "Epic"      => Color.Parse("#A855F7"),
                "Rare"      => Color.Parse("#3B82F6"),
                _           => Color.Parse("#9CA3AF"),
            };

            bool canAffordCoins = _currentCoinBalance >= coinPrice && coinPrice > 0;

            if (_purchasePopupOverlay != null)
            {
                foreach (var child in _purchasePopupOverlay.Children)
                {
                    if (child is Border panel) child.IsVisible = false;
                }
                _purchasePopupOverlay.Children.Clear();
            }

            var overlay = new Grid
            {
                Background = new SolidColorBrush(Color.Parse("#CC000000")),
                IsVisible = false,
                ZIndex = 900,
            };

            overlay.Tapped += (_, _) => ClosePurchasePopup();

            var card = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#1A1A2E")),
                CornerRadius = new CornerRadius(16),
                BorderBrush = new SolidColorBrush(Color.Parse("#2A2A4A")),
                BorderThickness = new Thickness(1),
                Width = 380,
                Padding = new Thickness(0),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                RenderTransform = new ScaleTransform { ScaleX = 0.85, ScaleY = 0.85 },
                BoxShadow = BoxShadows.Parse("0 20 60 0 #80000000"),
            };
            card.Tapped += (_, e) => e.Handled = true;

            var mainStack = new StackPanel { Spacing = 0 };

            var headerGrid = new Grid
            {
                Background = new SolidColorBrush(Color.Parse("#12122A")),
                Height = 100,
            };
            headerGrid.Children.Add(new Border
            {
                CornerRadius = new CornerRadius(16, 16, 0, 0),
                ClipToBounds = true,
                Child = new Border
                {
                    Background = new LinearGradientBrush
                    {
                        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                        EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                        GradientStops =
                        {
                            new GradientStop(Color.Parse($"#20{rarityColor.ToString().Substring(3)}"), 0),
                            new GradientStop(Color.Parse("#00000000"), 1),
                        },
                    },
                },
            });

            var previewEmoji = new TextBlock
            {
                Text = preview,
                FontSize = 42,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            };
            headerGrid.Children.Add(previewEmoji);

            var closeBtn = new Border
            {
                Width = 32, Height = 32,
                CornerRadius = new CornerRadius(16),
                Background = new SolidColorBrush(Color.Parse("#30FFFFFF")),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                Margin = new Thickness(0, 12, 12, 0),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                Child = new TextBlock
                {
                    Text = "\u2715",
                    FontSize = 14,
                    Foreground = Brushes.White,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                },
            };
            closeBtn.Tapped += (_, _) => ClosePurchasePopup();
            headerGrid.Children.Add(closeBtn);

            mainStack.Children.Add(headerGrid);

            var contentStack = new StackPanel { Spacing = 16, Margin = new Thickness(24, 20, 24, 24) };

            var nameBlock = new TextBlock
            {
                Text = itemName,
                FontSize = 22,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            };
            contentStack.Children.Add(nameBlock);

            var rarityBadge = new Border
            {
                Background = new SolidColorBrush(new Color(0x25, rarityColor.R, rarityColor.G, rarityColor.B)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 4),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Child = new TextBlock
                {
                    Text = rarity,
                    FontSize = 12,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = new SolidColorBrush(rarityColor),
                },
            };
            contentStack.Children.Add(rarityBadge);

            var detailsBox = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#15FFFFFF")),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16, 12),
                Margin = new Thickness(0, 4, 0, 0),
            };
            var detailsGrid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("*, Auto") };
            detailsGrid.Children.Add(new TextBlock
            {
                Text = itemName,
                FontSize = 14,
                Foreground = SolidColorBrush.Parse("#CCFFFFFF"),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            });
            var priceBlock = new TextBlock
            {
                Text = priceText,
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(rarityColor),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            };
            Grid.SetColumn(priceBlock, 1);
            detailsGrid.Children.Add(priceBlock);
            detailsBox.Child = detailsGrid;
            contentStack.Children.Add(detailsBox);

            var methodLabel = new TextBlock
            {
                Text = "Choose payment method:",
                FontSize = 13,
                Foreground = SolidColorBrush.Parse("#80FFFFFF"),
                Margin = new Thickness(0, 4, 0, 0),
            };
            contentStack.Children.Add(methodLabel);

            var coinBtnBorder = new Border
            {
                Background = canAffordCoins ? SolidColorBrush.Parse("#15228B45") : SolidColorBrush.Parse("#10FFFFFF"),
                BorderBrush = canAffordCoins ? SolidColorBrush.Parse("#4ADE80") : SolidColorBrush.Parse("#30FFFFFF"),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16, 14),
                Cursor = canAffordCoins ? new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand) : new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow),
                Opacity = canAffordCoins ? 1.0 : 0.45,
                IsEnabled = canAffordCoins,
            };
            var coinStack = new StackPanel { Spacing = 4 };
            coinStack.Children.Add(new TextBlock
            {
                Text = "\U0001f343  Pay with Leaf Points",
                FontSize = 15,
                FontWeight = FontWeight.SemiBold,
                Foreground = canAffordCoins ? SolidColorBrush.Parse("#4ADE80") : SolidColorBrush.Parse("#60FFFFFF"),
            });
            var balanceLine = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 6 };
            balanceLine.Children.Add(new TextBlock
            {
                Text = $"{coinPrice:N0} Leaf Points",
                FontSize = 12,
                Foreground = canAffordCoins ? SolidColorBrush.Parse("#80FFFFFF") : SolidColorBrush.Parse("#40FFFFFF"),
            });
            if (!canAffordCoins)
            {
                balanceLine.Children.Add(new TextBlock
                {
                    Text = $"(You have {_currentCoinBalance:N0})",
                    FontSize = 12,
                    Foreground = SolidColorBrush.Parse("#FF6B6B"),
                });
            }
            coinStack.Children.Add(balanceLine);
            coinBtnBorder.Child = coinStack;

            if (canAffordCoins)
            {
                coinBtnBorder.PointerEntered += (_, _) =>
                {
                    coinBtnBorder.Background = SolidColorBrush.Parse("#25228B45");
                    coinBtnBorder.BorderBrush = SolidColorBrush.Parse("#6ADE80");
                };
                coinBtnBorder.PointerExited += (_, _) =>
                {
                    coinBtnBorder.Background = SolidColorBrush.Parse("#15228B45");
                    coinBtnBorder.BorderBrush = SolidColorBrush.Parse("#4ADE80");
                };
                coinBtnBorder.Tapped += async (_, e) =>
                {
                    e.Handled = true;
                    await HandleCoinPurchaseFromPopup(itemId, itemName, preview, rarity, coinPrice);
                };
            }
            contentStack.Children.Add(coinBtnBorder);

            var cardBtnBorder = new Border
            {
                Background = SolidColorBrush.Parse("#15FFFFFF"),
                BorderBrush = SolidColorBrush.Parse("#40FFFFFF"),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16, 14),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
            };
            var cardStack = new StackPanel { Spacing = 4 };
            cardStack.Children.Add(new TextBlock
            {
                Text = "\U0001f4b3  Pay with Card / PayPal",
                FontSize = 15,
                FontWeight = FontWeight.SemiBold,
                Foreground = Brushes.White,
            });
            cardStack.Children.Add(new TextBlock
            {
                Text = priceText,
                FontSize = 12,
                Foreground = SolidColorBrush.Parse("#80FFFFFF"),
            });
            cardBtnBorder.Child = cardStack;
            cardBtnBorder.PointerEntered += (_, _) =>
            {
                cardBtnBorder.Background = SolidColorBrush.Parse("#25FFFFFF");
                cardBtnBorder.BorderBrush = SolidColorBrush.Parse("#60FFFFFF");
            };
            cardBtnBorder.PointerExited += (_, _) =>
            {
                cardBtnBorder.Background = SolidColorBrush.Parse("#15FFFFFF");
                cardBtnBorder.BorderBrush = SolidColorBrush.Parse("#40FFFFFF");
            };
            cardBtnBorder.Tapped += (_, e) =>
            {
                e.Handled = true;
                ClosePurchasePopup();

                var userId = DecodeJwtSub(_currentSettings?.LeafApiJwt);
                var url = checkoutUrl;
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    var sep = url.Contains('?') ? "&" : "?";
                    url = $"{url}{sep}checkout[custom][minecraft_uuid]={Uri.EscapeDataString(userId)}";
                }
                OpenCheckout(url);
            };
            contentStack.Children.Add(cardBtnBorder);

            mainStack.Children.Add(contentStack);
            card.Child = mainStack;
            overlay.Children.Add(card);

            _purchasePopupOverlay = overlay;
            _purchasePopupPanel = card;

            var rootGrid = this.FindControl<Grid>("RootGrid") ?? (Content as Grid);
            if (rootGrid != null)
            {
                rootGrid.Children.Add(overlay);
            }

            _isPurchasePopupAnimating = true;
            overlay.IsVisible = true;

            if (AreAnimationsEnabled())
            {
                var st = card.RenderTransform as ScaleTransform ?? new ScaleTransform();
                card.RenderTransform = st;
                card.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
                card.Opacity = 0;
                overlay.Opacity = 0;

                const int dur = 200;
                const int frames = 12;
                const int delay = dur / frames;

                for (int i = 0; i <= frames; i++)
                {
                    double t = (double)i / frames;
                    double eased = 1 - Math.Pow(1 - t, 3);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        overlay.Opacity = eased;
                        card.Opacity = eased;
                        st.ScaleX = 0.85 + 0.15 * eased;
                        st.ScaleY = 0.85 + 0.15 * eased;
                    });

                    if (i < frames) await Task.Delay(delay);
                }
            }
            else
            {
                card.Opacity = 1;
                overlay.Opacity = 1;
                var st = card.RenderTransform as ScaleTransform;
                if (st != null) { st.ScaleX = 1; st.ScaleY = 1; }
            }

            _isPurchasePopupAnimating = false;
        }

        private async void ClosePurchasePopup()
        {
            if (_purchasePopupOverlay == null || !_purchasePopupOverlay.IsVisible) return;
            if (_isPurchasePopupAnimating) return;
            _isPurchasePopupAnimating = true;

            if (AreAnimationsEnabled() && _purchasePopupPanel != null)
            {
                var st = _purchasePopupPanel.RenderTransform as ScaleTransform ?? new ScaleTransform();
                _purchasePopupPanel.RenderTransform = st;

                const int dur = 150;
                const int frames = 10;
                const int delay = dur / frames;

                for (int i = 0; i <= frames; i++)
                {
                    double t = (double)i / frames;
                    double eased = t * t;

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_purchasePopupOverlay != null) _purchasePopupOverlay.Opacity = 1 - eased;
                        if (_purchasePopupPanel != null) _purchasePopupPanel.Opacity = 1 - eased;
                        st.ScaleX = 1 - 0.1 * eased;
                        st.ScaleY = 1 - 0.1 * eased;
                    });

                    if (i < frames) await Task.Delay(delay);
                }
            }

            _purchasePopupOverlay.IsVisible = false;
            var rootGrid = this.FindControl<Grid>("RootGrid") ?? (Content as Grid);
            if (rootGrid != null && _purchasePopupOverlay != null)
                rootGrid.Children.Remove(_purchasePopupOverlay);
            _purchasePopupOverlay = null;
            _purchasePopupPanel = null;
            _isPurchasePopupAnimating = false;
        }

        private async Task HandleCoinPurchaseFromPopup(string itemId, string itemName, string preview, string rarity, int coinPrice)
        {
            var token = await EnsureLeafJwtAsync();
            if (string.IsNullOrEmpty(token)) return;

            var (success, newCoins, error) = await LeafApiService.PurchaseCosmeticWithCoinsAsync(token, itemId);

            if (success)
            {
                ClosePurchasePopup();
                _ownedCosmeticIds.Add(itemId);
                SaveOwnedJson();
                _cosmeticsPage?.RefreshOwnedList(_ownedCosmeticIds);
                _currentCoinBalance = newCoins;
                Dispatcher.UIThread.Post(() =>
                {
                    _leafsBalanceText ??= this.FindControl<TextBlock>("LeafsBalanceText");
                    if (_leafsBalanceText != null)
                        _leafsBalanceText.Text = newCoins.ToString("N0");
                });
                ShowPurchaseCelebration(itemId, itemName, preview, rarity);
                _storePage?.RefreshAfterPurchase(itemId);
            }
            else
            {
                ClosePurchasePopup();
                ToastService.Show(error ?? "Purchase failed.", ToastType.Error);
            }
        }

        // ─── Monthly Pass Popup ─────────────────────────────────────────────
        private bool _isMonthlyPassAnimating;

        public async void ShowMonthlyPassPopup()
        {
            var overlay = this.FindControl<Grid>("MonthlyPassOverlay");
            var panel   = this.FindControl<Border>("MonthlyPassPanel");
            var backdrop= this.FindControl<Border>("MonthlyPassBackdrop");
            if (overlay == null || panel == null) return;
            if (_isMonthlyPassAnimating || overlay.IsVisible) return;

            _isMonthlyPassAnimating = true;

            // Reset the tier selector to Monthly on every open so repeat shows
            // don't remember the previous click.
            if (this.FindControl<Border>("LeafPlusTier_Monthly") is { } monthlyPill)
            {
                OnLeafPlusTierTapped(monthlyPill, null!);
            }

            overlay.IsVisible = true;
            if (backdrop != null) backdrop.Opacity = 0;
            panel.Opacity = 0;

            var st = panel.RenderTransform as ScaleTransform ?? new ScaleTransform(0.85, 0.85);
            panel.RenderTransform       = st;
            panel.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            st.ScaleX = 0.85;
            st.ScaleY = 0.85;

            if (!AreAnimationsEnabled())
            {
                if (backdrop != null) backdrop.Opacity = 1;
                panel.Opacity = 1;
                st.ScaleX = 1; st.ScaleY = 1;
                _isMonthlyPassAnimating = false;
                return;
            }

            const int steps = 18;
            const int durationMs = 260;
            for (int i = 0; i <= steps; i++)
            {
                double t    = (double)i / steps;
                double ease = 1 - Math.Pow(1 - t, 3);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (backdrop != null) backdrop.Opacity = ease;
                    panel.Opacity = ease;
                    st.ScaleX = 0.85 + 0.15 * ease;
                    st.ScaleY = 0.85 + 0.15 * ease;
                });
                if (i < steps) await Task.Delay(durationMs / steps);
            }

            _isMonthlyPassAnimating = false;
        }

        private async void CloseMonthlyPassPopup()
        {
            var overlay = this.FindControl<Grid>("MonthlyPassOverlay");
            var panel   = this.FindControl<Border>("MonthlyPassPanel");
            var backdrop= this.FindControl<Border>("MonthlyPassBackdrop");
            if (overlay == null || panel == null) return;
            if (!overlay.IsVisible || _isMonthlyPassAnimating) return;

            _isMonthlyPassAnimating = true;

            var st = panel.RenderTransform as ScaleTransform ?? new ScaleTransform(1, 1);
            panel.RenderTransform = st;

            if (!AreAnimationsEnabled())
            {
                overlay.IsVisible = false;
                _isMonthlyPassAnimating = false;
                return;
            }

            const int steps = 14;
            const int durationMs = 200;
            for (int i = 0; i <= steps; i++)
            {
                double t    = (double)i / steps;
                double ease = Math.Pow(t, 2);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (backdrop != null) backdrop.Opacity = 1 - ease;
                    panel.Opacity = 1 - ease;
                    st.ScaleX = 1.0 - 0.15 * ease;
                    st.ScaleY = 1.0 - 0.15 * ease;
                });
                if (i < steps) await Task.Delay(durationMs / steps);
            }

            overlay.IsVisible = false;
            _isMonthlyPassAnimating = false;
        }

        private void OnMonthlyPassClose(object? sender, TappedEventArgs e) => CloseMonthlyPassPopup();
        private void OnMonthlyPassBackdropTapped(object? sender, TappedEventArgs e) => CloseMonthlyPassPopup();

        // Currently selected Leaf+ billing tier. Defaults to "monthly" — we
        // reset the visual highlight to the monthly pill every time the popup
        // is shown so repeat opens don't remember the previous selection.
        private string _selectedLeafPlusTier = "monthly";

        private void OnLeafPlusTierTapped(object? sender, TappedEventArgs e)
        {
            if (sender is not Border pill || pill.Tag is not string tier) return;
            _selectedLeafPlusTier = tier;

            var monthly   = this.FindControl<Border>("LeafPlusTier_Monthly");
            var quarterly = this.FindControl<Border>("LeafPlusTier_Quarterly");
            var yearly    = this.FindControl<Border>("LeafPlusTier_Yearly");

            void Paint(Border? b, bool selected)
            {
                if (b == null) return;
                if (selected)
                {
                    b.Background = SolidColorBrush.Parse("#1A1E44");
                    b.BorderBrush = SolidColorBrush.Parse("#7C3AED");
                    b.BorderThickness = new Thickness(1.5);
                }
                else
                {
                    b.Background = SolidColorBrush.Parse("#0F1A24");
                    b.BorderBrush = SolidColorBrush.Parse("#1C2A38");
                    b.BorderThickness = new Thickness(1);
                }
            }

            Paint(monthly,   tier == "monthly");
            Paint(quarterly, tier == "quarterly");
            Paint(yearly,    tier == "yearly");
        }

        // Lemon Squeezy checkout URLs — one per billing tier.
        private const string LeafPlusCheckoutMonthly   = "https://leafclient.lemonsqueezy.com/checkout/buy/1cde7c5c-c4fb-4a32-9a81-ed54cafc04ac";
        private const string LeafPlusCheckoutQuarterly = "https://leafclient.lemonsqueezy.com/checkout/buy/305dfdd4-40fd-4e00-946a-e6246b8636ee";
        private const string LeafPlusCheckoutYearly    = "https://leafclient.lemonsqueezy.com/checkout/buy/d3042d32-0b28-49ae-b12f-f897c308b9e0";

        private const int LeafPlusCoinsMonthly   = 500;
        private const int LeafPlusCoinsQuarterly = 1400;
        private const int LeafPlusCoinsYearly    = 5000;

        private bool _isLeafPlusPaymentAnimating;

        private void OnMonthlyPassSubscribeTapped(object? sender, TappedEventArgs e)
        {
            CloseMonthlyPassPopup();
            ShowLeafPlusPaymentPopup();
        }

        private async void ShowLeafPlusPaymentPopup()
        {
            var overlay  = this.FindControl<Grid>("LeafPlusPaymentOverlay");
            var panel    = this.FindControl<Border>("LeafPlusPaymentPanel");
            var backdrop = this.FindControl<Border>("LeafPlusPaymentBackdrop");
            if (overlay == null || panel == null) return;
            if (_isLeafPlusPaymentAnimating || overlay.IsVisible) return;

            _isLeafPlusPaymentAnimating = true;

            var tierLabel    = this.FindControl<TextBlock>("LeafPlusPaymentTierLabel");
            var pointsCost   = this.FindControl<TextBlock>("LeafPointsPayCost");
            var pointsBal    = this.FindControl<TextBlock>("LeafPointsPayBalance");
            var cardPrice    = this.FindControl<TextBlock>("LeafCardPayPrice");

            (string tierName, int coins, string cardText) = _selectedLeafPlusTier switch
            {
                "monthly"   => ("Monthly",   LeafPlusCoinsMonthly,   "€4.99/month"),
                "quarterly" => ("Quarterly", LeafPlusCoinsQuarterly, "€13.99/quarter"),
                "yearly"    => ("Yearly",    LeafPlusCoinsYearly,    "€49.99/year"),
                _           => ("Yearly",    LeafPlusCoinsYearly,    "€49.99/year"),
            };

            if (tierLabel  != null) tierLabel.Text  = $"Leaf+ {tierName}";
            if (pointsCost != null) pointsCost.Text = $"{coins:N0} 🍃";
            if (cardPrice  != null) cardPrice.Text  = cardText;

            int balance = _currentCoinBalance;
            if (pointsBal != null) pointsBal.Text = $"(you have {balance:N0})";

            var pointsOption = this.FindControl<Border>("LeafPointsPayOption");
            bool canAfford = balance >= coins;
            if (pointsOption != null)
            {
                pointsOption.Opacity = canAfford ? 1.0 : 0.45;
                pointsOption.Cursor = canAfford ? new Cursor(StandardCursorType.Hand) : new Cursor(StandardCursorType.No);
            }

            overlay.IsVisible = true;
            if (backdrop != null) backdrop.Opacity = 0;
            panel.Opacity = 0;

            var st = panel.RenderTransform as ScaleTransform ?? new ScaleTransform(0.85, 0.85);
            panel.RenderTransform       = st;
            panel.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            st.ScaleX = 0.85;
            st.ScaleY = 0.85;

            if (!AreAnimationsEnabled())
            {
                if (backdrop != null) backdrop.Opacity = 1;
                panel.Opacity = 1;
                st.ScaleX = 1; st.ScaleY = 1;
                _isLeafPlusPaymentAnimating = false;
                return;
            }

            const int steps = 18;
            const int durationMs = 260;
            for (int i = 0; i <= steps; i++)
            {
                double t    = (double)i / steps;
                double ease = 1 - Math.Pow(1 - t, 3);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (backdrop != null) backdrop.Opacity = ease;
                    panel.Opacity = ease;
                    st.ScaleX = 0.85 + 0.15 * ease;
                    st.ScaleY = 0.85 + 0.15 * ease;
                });
                if (i < steps) await Task.Delay(durationMs / steps);
            }

            _isLeafPlusPaymentAnimating = false;
        }

        private async void CloseLeafPlusPaymentPopup()
        {
            var overlay  = this.FindControl<Grid>("LeafPlusPaymentOverlay");
            var panel    = this.FindControl<Border>("LeafPlusPaymentPanel");
            var backdrop = this.FindControl<Border>("LeafPlusPaymentBackdrop");
            if (overlay == null || panel == null) return;
            if (!overlay.IsVisible || _isLeafPlusPaymentAnimating) return;

            _isLeafPlusPaymentAnimating = true;

            var st = panel.RenderTransform as ScaleTransform ?? new ScaleTransform(1, 1);
            panel.RenderTransform = st;

            if (!AreAnimationsEnabled())
            {
                overlay.IsVisible = false;
                _isLeafPlusPaymentAnimating = false;
                return;
            }

            const int steps = 14;
            const int durationMs = 200;
            for (int i = 0; i <= steps; i++)
            {
                double t    = (double)i / steps;
                double ease = Math.Pow(t, 2);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (backdrop != null) backdrop.Opacity = 1 - ease;
                    panel.Opacity = 1 - ease;
                    st.ScaleX = 1.0 - 0.15 * ease;
                    st.ScaleY = 1.0 - 0.15 * ease;
                });
                if (i < steps) await Task.Delay(durationMs / steps);
            }

            overlay.IsVisible = false;
            _isLeafPlusPaymentAnimating = false;
        }

        private void OnLeafPlusPaymentClose(object? sender, TappedEventArgs e) => CloseLeafPlusPaymentPopup();
        private void OnLeafPlusPaymentBackdropTapped(object? sender, TappedEventArgs e) => CloseLeafPlusPaymentPopup();

        private async void OnLeafPlusPayWithPoints(object? sender, TappedEventArgs e)
        {
            int coins = _selectedLeafPlusTier switch
            {
                "monthly"   => LeafPlusCoinsMonthly,
                "quarterly" => LeafPlusCoinsQuarterly,
                "yearly"    => LeafPlusCoinsYearly,
                _           => LeafPlusCoinsYearly,
            };

            if (_currentCoinBalance < coins) return;

            CloseLeafPlusPaymentPopup();

            var jwt = _currentSettings?.LeafApiJwt;
            if (string.IsNullOrEmpty(jwt)) return;

            try
            {
                var result = await LeafApiService.PurchaseLeafPlusWithCoinsAsync(jwt, _selectedLeafPlusTier);
                if (result?.Ok == true)
                {
                    if (_currentSettings != null)
                        await UpdateLeafsBalanceAsync();
                    string tierLabel = _selectedLeafPlusTier switch
                    {
                        "quarterly" => "LEAF+ Quarterly",
                        "yearly"    => "LEAF+ Yearly",
                        _           => "LEAF+ Monthly",
                    };
                    ShowPurchaseCelebration("leafplus", tierLabel, "", "exclusive");
                }
                else
                {
                    Console.WriteLine($"[LeafPlus] Coin purchase failed: {result?.Error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LeafPlus] Coin purchase error: {ex.Message}");
            }
        }

        private void OnLeafPlusPayWithCard(object? sender, TappedEventArgs e)
        {
            string url = _selectedLeafPlusTier switch
            {
                "monthly"   => LeafPlusCheckoutMonthly,
                "quarterly" => LeafPlusCheckoutQuarterly,
                "yearly"    => LeafPlusCheckoutYearly,
                _           => LeafPlusCheckoutYearly,
            };

            Console.WriteLine($"[LeafPlus] Card checkout — tier={_selectedLeafPlusTier}");
            CloseLeafPlusPaymentPopup();
            var leafPlusUserId = DecodeJwtSub(_currentSettings?.LeafApiJwt);
            if (!string.IsNullOrWhiteSpace(leafPlusUserId))
            {
                var sep = url.Contains('?') ? "&" : "?";
                url = $"{url}{sep}checkout[custom][minecraft_uuid]={Uri.EscapeDataString(leafPlusUserId)}";
            }
            OpenCheckout(url);
        }

        private void CloseCelebrationOverlay()
        {
            var overlay = this.FindControl<Grid>("CelebrationOverlay");
            if (overlay != null) overlay.IsVisible = false;
            if (TutorialService.Instance.IsHiddenForAction)
                TutorialService.Instance.ResumeAfterAction();
        }

        private void OnCelebrationBackdropTapped(object? sender, TappedEventArgs e) => CloseCelebrationOverlay();

        private void OnCelebClose(object? sender, TappedEventArgs e) => CloseCelebrationOverlay();

        private void OnCelebEquipNow(object? sender, TappedEventArgs e)
        {
            CloseCelebrationOverlay();
            SwitchToPage(6); // cosmetics page index
        }


        private async void OpenCommonQuestions(object? sender, RoutedEventArgs e)
        {
            if (_commonQuestionsOverlay == null || _commonQuestionsPanel == null) return;
            if (_commonQuestionsOverlay.IsVisible || _isCommonQuestionsAnimating) return;

            _isCommonQuestionsAnimating = true;
            try
            {
                var backdrop = this.FindControl<Border>("CommonQuestionsBackdrop");
                await AnimateOverlayOpenAsync(_commonQuestionsOverlay, _commonQuestionsPanel, backdrop);
            }
            finally { _isCommonQuestionsAnimating = false; }
        }

        private async void CloseCommonQuestions(object? sender, RoutedEventArgs e)
        {
            if (_commonQuestionsOverlay == null || _commonQuestionsPanel == null) return;
            if (!_commonQuestionsOverlay.IsVisible || _isCommonQuestionsAnimating) return;

            _isCommonQuestionsAnimating = true;
            try
            {
                var backdrop = this.FindControl<Border>("CommonQuestionsBackdrop");
                await AnimateOverlayCloseAsync(_commonQuestionsOverlay, _commonQuestionsPanel, backdrop);
            }
            finally { _isCommonQuestionsAnimating = false; }
        }




        private void OpenLauncherLogsFolder(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (System.IO.Directory.Exists(_logFolderPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _logFolderPath,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
                else
                {
                    Console.WriteLine($"[Logs] Log folder not found: {_logFolderPath}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] Failed to open logs folder: {ex.Message}");
            }
        }

        private void OpenModsFolder(object? sender, RoutedEventArgs e)
        {
            try
            {
                var modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
                System.IO.Directory.CreateDirectory(modsFolder);
                Process.Start(new ProcessStartInfo
                {
                    FileName = modsFolder,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] Failed to open mods folder: {ex.Message}");
            }
        }

        private void OpenShadersFolder(object? sender, RoutedEventArgs e)
        {
            try
            {
                var shadersFolder = System.IO.Path.Combine(_minecraftFolder, "shaderpacks");
                System.IO.Directory.CreateDirectory(shadersFolder);
                Process.Start(new ProcessStartInfo
                {
                    FileName = shadersFolder,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] Failed to open shaderpacks folder: {ex.Message}");
            }
        }

        private void OpenResourcePacksFolder(object? sender, RoutedEventArgs e)
        {
            try
            {
                var resourcePacksFolder = System.IO.Path.Combine(_minecraftFolder, "resourcepacks");
                System.IO.Directory.CreateDirectory(resourcePacksFolder);
                Process.Start(new ProcessStartInfo
                {
                    FileName = resourcePacksFolder,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] Failed to open resourcepacks folder: {ex.Message}");
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
                    var status = await _serverChecker.GetServerStatusAsync(s.Address, s.Port);
                    if (!string.IsNullOrEmpty(status.IconData))
                    {
                        s.IconBase64 = status.IconData;
                    }
                }
                catch {  }
                finally
                {
                    throttler.Release();
                }
            });

            await Task.WhenAll(tasks); 
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
            var allServers = _currentSettings.CustomServers.Concat(_featuredServers).ToList();
            var tasks = allServers.Select(async server =>
            {
                bool isFeatured = server.Id?.StartsWith("featured_") == true;
                try
                {
                    var status = await _serverChecker.GetServerStatusAsync(server.Address, server.Port);

                    server.IsOnline = status.IsOnline;
                    server.CurrentPlayers = status.CurrentPlayers;
                    server.MaxPlayers = status.MaxPlayers;
                    server.Motd = status.Motd;

                    server.StatusText = status.IsOnline ? "Online" : "Offline";
                    server.StatusColor = status.IsOnline ? Brushes.Green : Brushes.Red;

                    if (!string.IsNullOrEmpty(status.IconData))
                        server.IconBase64 = status.IconData;

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_serversPage?.IsVisible == true)
                        {
                            if (isFeatured) UpdateFeaturedServerCardUI(server);
                            else UpdateServerCardUI(server);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Server Status] Error checking {server.Name}: {ex.Message}");

                    server.IsOnline = false;
                    server.StatusText = "Offline";
                    server.StatusColor = Brushes.Red;
                    server.Motd = "Connection Failed";
                    server.CurrentPlayers = 0;
                    server.MaxPlayers = 0;

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_serversPage?.IsVisible == true)
                        {
                            if (isFeatured) UpdateFeaturedServerCardUI(server);
                            else UpdateServerCardUI(server);
                        }
                    });
                }
            });

            await Task.WhenAll(tasks);

            await _settingsService.SaveSettingsAsync(_currentSettings);

            await Dispatcher.UIThread.InvokeAsync(() => RefreshQuickPlayBar());

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
                            Foreground = GetBrush("SuccessBrush"), 
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

        private async Task ShowInfoDialog(string title, string message)
        {
            var dialog = new Window
            {
                Title = title,
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

            if (dialog.Content is StackPanel sp && sp.Children.OfType<Button>().LastOrDefault() is Button okBtn)
                okBtn.Click += (_, __) => dialog.Close();

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
                    return; 
                }

                string? newUsername = await ShowTextInputDialog("Change Username", "Enter your new Minecraft username:", _currentSettings.SessionUsername);

                if (string.IsNullOrWhiteSpace(newUsername) || newUsername == _currentSettings.SessionUsername)
                {
                    Console.WriteLine("[Account] Username change cancelled or same name entered.");
                    return;
                }

                bool confirmed = await ShowConfirmationDialog("Confirm Name Change", $"Are you sure you want to change your username to '{newUsername}'? This can only be done once every 30 days.");
                if (!confirmed)
                {
                    Console.WriteLine("[Account] Username change cancelled by user.");
                    return;
                }

                Console.WriteLine($"[Account] Attempting to change username to: {newUsername}");
                MojangApiService.PlayerProfileResponse profile = await _mojangApiService.ChangeName(_session.AccessToken, newUsername);

                _session.Username = profile.Name ?? newUsername; 
                _currentSettings.SessionUsername = profile.Name ?? newUsername;
                _currentSettings.SessionUuid = profile.Id ?? _session.UUID; 
                await _settingsService.SaveSettingsAsync(_currentSettings);

                Console.WriteLine($"[Account] Username changed successfully to: {profile.Name ?? newUsername}");
                await ShowAccountActionSuccessDialog($"Your username has been changed to '{profile.Name ?? newUsername}'.");

                await LoadUserInfoAsync(); 
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

            // Migrate: remove featured servers from personal list (they now live in the Featured section)
            var featuredAddresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "mc.hypixel.net", "2b2t.org", "play.pvplegacy.net" };

            int removed = _currentSettings.CustomServers.RemoveAll(s =>
                featuredAddresses.Contains(s.Address));

            if (removed > 0)
                await _settingsService.SaveSettingsAsync(_currentSettings);
        }



        private void LoadServerData()
        {

            _serverStatusRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };

            _serverStatusRefreshTimer.Tick += async (s, e) =>
            {
                try
                {
                    await RefreshAllServerStatusesAsync(); 
                }
                catch (Exception ex)
                {
                    try
                    {
                    }
                    catch { }
                }
            };
        }


        private void ShowOfflineMode()
        {
            Dispatcher.UIThread.Post(() =>
            {
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

            int maxParallel = 3;
            var allServers = _currentSettings.CustomServers.Concat(_featuredServers).ToList();
            await Parallel.ForEachAsync(allServers, new ParallelOptions { MaxDegreeOfParallelism = maxParallel }, async (server, ct) =>
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

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    server.IsOnline = result.IsOnline;
                    server.CurrentPlayers = result.CurrentPlayers;
                    server.MaxPlayers = result.MaxPlayers;
                    server.Motd = result.Motd;
                    server.IconBase64 = result.IconData;

                    bool isFeatured = server.Id?.StartsWith("featured_") == true;
                    if (isFeatured)
                        UpdateFeaturedServerCardUI(server);
                    else
                        _ = UpdateServerStatusUIAsync(server, accentBrush, hoverBackgroundBrush, accentButtonForegroundBrush, disabledForegroundBrush);
                });
            });
        }



        private async Task UpdateServerStatusUIAsync(
            LeafClient.Models.ServerInfo server, 
            IBrush accentBrush,
            IBrush hoverBackgroundBrush,
            IBrush accentButtonForegroundBrush,
            IBrush disabledForegroundBrush)
        {
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
            catch {  }
        }

        private void UpdateStatusText(TextBlock? statusText, string text)
        {
            if (statusText == null) return;

            try
            {
                if (statusText.Text != text)
                    statusText.Text = text;
            }
            catch {  }
        }

        private void UpdateMotdText(TextBlock? motdText, string text)
        {
            if (motdText == null) return;

            try
            {
                if (motdText.Text != text)
                    motdText.Text = text;
            }
            catch {  }
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
            catch {  }
        }


        private async void ShowSettingsSaveBanner()
        {
            if (_settingsSaveBanner == null) return;

            _settingsSaveBanner.IsVisible = true;

            TranslateTransform? transform = _settingsSaveBanner.RenderTransform as TranslateTransform;
            if (transform == null)
            {
                transform = new TranslateTransform();
                _settingsSaveBanner.RenderTransform = transform;
            }

            if (!AreAnimationsEnabled())
            {
                transform.Y = 0; 
                _settingsSaveBanner.Opacity = 1; 
                return; 
            }

            transform.Y = 80;
            _settingsSaveBanner.Opacity = 0;

            const int durationMs = 300;
            const int steps = 20;
            const int delayMs = durationMs / steps;

            for (int i = 0; i <= steps; i++)
            {
                double progress = (double)i / steps;
                double eased = 1 - Math.Pow(1 - progress, 3); 

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_settingsSaveBanner == null) return;
                    transform.Y = 80 - (80 * eased); 
                    _settingsSaveBanner.Opacity = eased; 
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

            _settingsDirty = false;

            if (!AreAnimationsEnabled())
            {
                transform.Y = 80;
                _settingsSaveBanner.Opacity = 0;
                _settingsSaveBanner.IsVisible = false;
                return;
            }

            const int durationMs = 300;
            const int steps = 20;
            const int delayMs = durationMs / steps;

            for (int i = 0; i <= steps; i++)
            {
                double progress = (double)i / steps;
                double eased = 1 - Math.Pow(1 - progress, 3); 

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_settingsSaveBanner == null) return;
                    transform.Y = 0 + (80 * eased); 
                    _settingsSaveBanner.Opacity = 1 - eased; 
                });

                if (i < steps)
                    await Task.Delay(delayMs);
            }

            _settingsSaveBanner.IsVisible = false;
        }

        private void MarkSettingsDirty()
        {
            if (_isApplyingSettings) return;
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


        private bool _isShuttingDown = false; 
        private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
        {
            bool isAppSwappingToLogin = (Application.Current as App)?.IsSwapToLogin ?? false;

            if (_isShuttingDown)
            {
                e.Cancel = false;
                return;
            }

            if (isAppSwappingToLogin)
            {
                Console.WriteLine($"[MainWindow] OnWindowClosing: Detected app is swapping windows. Cancelling close for this window.");
                e.Cancel = false; 

                if (Application.Current is App app)
                {
                    app.IsSwapToLogin = false;
                }
                return;
            }

            if (_currentSettings.MinimizeToTray && !_isExitingApp)
            {
                Console.WriteLine("[MainWindow] OnWindowClosing: Minimize to tray enabled. Hiding window.");
                e.Cancel = true; 
                MinimizeToTray();
                return;
            }

            _gameOutputWindow?.Close();
            _isShuttingDown = true; 
            e.Cancel = true; 

            this.Hide(); 
            Console.WriteLine("[MainWindow] Window hidden. Starting graceful application shutdown...");

            if (_onlineCountService != null)
            {
                try
                {
                    Console.WriteLine("[MainWindow] Attempting to decrement online count before shutdown...");
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)); 
                    await _onlineCountService.UpdateCount(false, cts.Token);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[MainWindow ERROR] Failed to decrement online count during shutdown: {ex.Message}");
                }
            }

            StopRichPresence();
            KillMinecraftProcess(); 

            if (_trayIcon != null)
            {
                _trayIcon.IsVisible = false;
                _trayIcon.Dispose();
            }

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
                Console.WriteLine("[MainWindow] Graceful shutdown complete. Terminating process.");
                desktop.Shutdown();
            }
        }


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

                var iconStream = AssetLoader.Open(new Uri("avares://LeafClient/Assets/logo_icon.png"));
                _trayIcon.Icon = new WindowIcon(iconStream);
                _trayIcon.ToolTipText = "Leaf Client";
                _trayIcon.IsVisible = false;

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

                _trayIcon.Clicked += (s, e) => RestoreFromTray();

                var trayIconsCollection = new Avalonia.Controls.TrayIcons();
                trayIconsCollection.Add(_trayIcon); 

                TrayIcon.SetIcons(Application.Current, trayIconsCollection);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TrayIcon] Failed to initialize: {ex.Message}");
            }
        }


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

        private void RestoreFromTray()
        {
            if (_isShuttingDown) return; 

            Dispatcher.UIThread.Post(() =>
            {
                if (_isShuttingDown) return; 

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
                }
            });
        }


        private void MinimizeToTray()
        {
            this.Hide();
            if (_trayIcon != null)
                _trayIcon.IsVisible = true;

            if (_currentSettings.ClosingNotificationsPreference != NotificationPreference.Never)
            {
                NotificationWindow.Show("Leaf Client", "Running in background", "Restore", () => RestoreFromTray());
            }
        }



        private void KillMinecraftProcess()
        {
            if (_gameProcess != null && !_gameProcess.HasExited)
            {
                try
                {
                    _userTerminatedGame = true;
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
            string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");

            if (!Directory.Exists(modsFolder))
            {
                try { Directory.CreateDirectory(modsFolder); } catch { }
                Console.WriteLine("[Launcher] Mods folder did not exist, created fresh.");
                _gameOutputWindow?.AppendLog("[LaunchDiag] Mods folder did not exist, created fresh.", "INFO");
                return;
            }

            Console.WriteLine("[Launcher] Clearing mods folder for a fresh launch...");
            _gameOutputWindow?.AppendLog("[LaunchDiag] Clearing mods folder for a fresh launch...", "INFO");

            const int maxAttempts = 3;
            Exception? lastEx = null;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    Directory.Delete(modsFolder, true);
                    Directory.CreateDirectory(modsFolder);
                    Console.WriteLine($"[Launcher] Mods folder cleared successfully (attempt {attempt}).");
                    _gameOutputWindow?.AppendLog($"[LaunchDiag] Mods folder cleared successfully (attempt {attempt}).", "INFO");
                    return;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    Console.Error.WriteLine($"[Launcher ERROR] Failed to clear mods folder (attempt {attempt}/{maxAttempts}): {ex.Message}");
                    _gameOutputWindow?.AppendLog(
                        $"[LaunchDiag] Failed to clear mods folder (attempt {attempt}/{maxAttempts}): {ex.GetType().Name}: {ex.Message}",
                        "ERROR");
                    try { System.Threading.Thread.Sleep(200); } catch { }
                }
            }

            // Retries exhausted — try to delete individual files and surface remaining leafclient jars.
            try
            {
                if (!Directory.Exists(modsFolder)) Directory.CreateDirectory(modsFolder);

                var remaining = Directory.GetFiles(modsFolder, "*.*", SearchOption.AllDirectories);
                foreach (var f in remaining)
                {
                    try { File.Delete(f); } catch { /* ignore individual failures */ }
                }

                var stillThere = Directory.GetFiles(modsFolder, "*.jar", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(modsFolder, "*.jar.disabled", SearchOption.AllDirectories))
                    .ToArray();

                var stillLeaf = stillThere
                    .Where(p => System.IO.Path.GetFileName(p).Contains("leaf", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (stillLeaf.Length > 0)
                {
                    _gameOutputWindow?.AppendLog(
                        $"[LaunchDiag] WARNING: {stillLeaf.Length} leafclient-like jar(s) SURVIVED the clear attempt!",
                        "ERROR");
                    foreach (var p in stillLeaf)
                    {
                        try
                        {
                            var fi = new FileInfo(p);
                            _gameOutputWindow?.AppendLog(
                                $"[LaunchDiag]   SURVIVED: {p} (size={fi.Length} bytes, mtime={fi.LastWriteTime:yyyy-MM-dd HH:mm:ss})",
                                "ERROR");
                        }
                        catch
                        {
                            _gameOutputWindow?.AppendLog($"[LaunchDiag]   SURVIVED: {p}", "ERROR");
                        }
                    }
                    ShowLaunchErrorBanner("A leafclient jar survived the mods folder clear. A stale mod may load.");
                }
                else if (stillThere.Length > 0)
                {
                    _gameOutputWindow?.AppendLog(
                        $"[LaunchDiag] WARNING: {stillThere.Length} non-leaf jar(s) survived the mods folder clear.",
                        "WARN");
                }
            }
            catch (Exception scanEx)
            {
                _gameOutputWindow?.AppendLog(
                    $"[LaunchDiag] Post-clear scan failed: {scanEx.GetType().Name}: {scanEx.Message}",
                    "ERROR");
            }

            if (lastEx != null)
            {
                Console.Error.WriteLine($"[Launcher ERROR] Failed to clear mods folder after {maxAttempts} attempts: {lastEx.Message}");
                ShowLaunchErrorBanner("Failed to clear the mods folder after retries. It might be in use.");
            }
        }

        // =====================================================================
        // Launch diagnostics helpers for the leafclient.jar loading pipeline.
        // All helpers are defensive and must never throw — they only log.
        // =====================================================================

        private static string ComputeShortSha256(string filePath)
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                using var sha = SHA256.Create();
                byte[] hash = sha.ComputeHash(stream);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString(0, Math.Min(16, sb.Length));
            }
            catch (Exception ex)
            {
                return $"hash-err:{ex.GetType().Name}";
            }
        }

        private void LogJarFileInfo(string label, string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    _gameOutputWindow?.AppendLog($"[LaunchDiag] {label}: MISSING -> {path}", "INFO");
                    return;
                }
                var fi = new FileInfo(path);
                string hash = ComputeShortSha256(path);
                _gameOutputWindow?.AppendLog(
                    $"[LaunchDiag] {label}: {path} | size={fi.Length / 1024}KB | mtime={fi.LastWriteTime:yyyy-MM-dd HH:mm:ss} | sha16={hash}",
                    "INFO");
            }
            catch (Exception ex)
            {
                _gameOutputWindow?.AppendLog($"[LaunchDiag] {label}: EXCEPTION inspecting {path}: {ex.GetType().Name}: {ex.Message}", "ERROR");
            }
        }

        private void ScanForStaleLeafJars(string directory, string scanLabel, string? expectedJarPath)
        {
            try
            {
                if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                {
                    _gameOutputWindow?.AppendLog($"[LaunchDiag] {scanLabel}: directory does not exist -> {directory}", "INFO");
                    return;
                }

                string? normalizedExpected = null;
                try
                {
                    if (!string.IsNullOrEmpty(expectedJarPath))
                        normalizedExpected = System.IO.Path.GetFullPath(expectedJarPath);
                }
                catch { /* ignore */ }

                // Scan only top-level AND subdirectories within THIS specific directory — never escape scope.
                var candidates = new List<string>();
                try
                {
                    candidates.AddRange(Directory.GetFiles(directory, "*leaf*.jar", SearchOption.AllDirectories));
                }
                catch { }
                try
                {
                    candidates.AddRange(Directory.GetFiles(directory, "leafclient*.jar", SearchOption.AllDirectories));
                }
                catch { }
                // Deduplicate via canonical paths.
                var unique = candidates
                    .Select(p => { try { return System.IO.Path.GetFullPath(p); } catch { return p; } })
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (unique.Count == 0)
                {
                    _gameOutputWindow?.AppendLog($"[LaunchDiag] {scanLabel}: no leafclient jars found under {directory}", "INFO");
                    return;
                }

                _gameOutputWindow?.AppendLog($"[LaunchDiag] {scanLabel}: found {unique.Count} leafclient-like jar(s) under {directory}", "INFO");
                foreach (var p in unique)
                {
                    bool isExpected = normalizedExpected != null &&
                                      p.Equals(normalizedExpected, StringComparison.OrdinalIgnoreCase);
                    string tag = isExpected ? "expected" : "STALE?";
                    try
                    {
                        var fi = new FileInfo(p);
                        string hash = ComputeShortSha256(p);
                        string level = isExpected ? "INFO" : "WARN";
                        _gameOutputWindow?.AppendLog(
                            $"[LaunchDiag]   [{tag}] {p} | size={fi.Length / 1024}KB | mtime={fi.LastWriteTime:yyyy-MM-dd HH:mm:ss} | sha16={hash}",
                            level);
                    }
                    catch (Exception ex)
                    {
                        _gameOutputWindow?.AppendLog(
                            $"[LaunchDiag]   [{tag}] {p} (inspect failed: {ex.GetType().Name}: {ex.Message})",
                            "ERROR");
                    }
                }
            }
            catch (Exception ex)
            {
                _gameOutputWindow?.AppendLog($"[LaunchDiag] {scanLabel}: scan failed: {ex.GetType().Name}: {ex.Message}", "ERROR");
            }
        }

        private void LogLaunchPreBuildDiagnostics(string leafModPath, string mcVersion)
        {
            try
            {
                _gameOutputWindow?.AppendLog("[LaunchDiag] ===== PRE-BUILD JAR DIAGNOSTIC =====", "INFO");
                _gameOutputWindow?.AppendLog($"[LaunchDiag] leafModPath target = {leafModPath}", "INFO");
                LogJarFileInfo("leafModPath (before build)", leafModPath);

                string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
                string leafRuntimeRoot = System.IO.Path.Combine(_minecraftFolder, "leaf-runtime");
                string leafOfflineRoot = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    ".leafclient", "offline");

                ScanForStaleLeafJars(modsFolder, "pre-build scan: .minecraft/mods", null);
                ScanForStaleLeafJars(leafRuntimeRoot, "pre-build scan: .minecraft/leaf-runtime", null);
                ScanForStaleLeafJars(leafOfflineRoot, "pre-build scan: .leafclient/offline", leafModPath);

                _gameOutputWindow?.AppendLog("[LaunchDiag] ===== END PRE-BUILD JAR DIAGNOSTIC =====", "INFO");
            }
            catch (Exception ex)
            {
                _gameOutputWindow?.AppendLog($"[LaunchDiag] pre-build diagnostics failed: {ex.GetType().Name}: {ex.Message}", "ERROR");
            }
        }

        private void LogLaunchPreLaunchDiagnostics(string leafModPath)
        {
            try
            {
                _gameOutputWindow?.AppendLog("[LaunchDiag] ===== PRE-LAUNCH FINAL CHECK =====", "INFO");
                string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
                ScanForStaleLeafJars(modsFolder, "pre-launch scan: .minecraft/mods (should be clean)", null);
                LogJarFileInfo("leafModPath (final)", leafModPath);
                _gameOutputWindow?.AppendLog($"[LaunchDiag] -Dfabric.addMods target = {leafModPath}", "INFO");
                _gameOutputWindow?.AppendLog("[LaunchDiag] ===== END PRE-LAUNCH FINAL CHECK =====", "INFO");
            }
            catch (Exception ex)
            {
                _gameOutputWindow?.AppendLog($"[LaunchDiag] pre-launch diagnostics failed: {ex.GetType().Name}: {ex.Message}", "ERROR");
            }
        }

        private async Task LoadSessionAsync()
        {
            try
            {
                _currentSettings = await _settingsService.LoadSettingsAsync();
                WipeCosmeticFilesIfStale();
                LoadOwnedJson();
                MigrateActiveAccountEntry();
                ValidateEquippedCosmetics();

                if (_currentSettings.IsLoggedIn && _currentSettings.AccountType == "microsoft")
                {
                    // FAST PATH: validate the stored Minecraft access token first.
                    // This avoids any OAuth round-trip for the common case where the token
                    // is still valid (~24-hour window from last refresh).
                    if (!string.IsNullOrWhiteSpace(_currentSettings.SessionAccessToken) &&
                        !string.IsNullOrWhiteSpace(_currentSettings.SessionUsername) &&
                        !string.IsNullOrWhiteSpace(_currentSettings.SessionUuid))
                    {
                        Console.WriteLine("[Launch] Validating cached Minecraft token...");
                        bool tokenValid = await ValidateMinecraftTokenAsync(_currentSettings.SessionAccessToken);
                        if (tokenValid)
                        {
                            _session = new MSession
                            {
                                Username = _currentSettings.SessionUsername,
                                UUID = _currentSettings.SessionUuid,
                                AccessToken = _currentSettings.SessionAccessToken,
                                Xuid = _currentSettings.SessionXuid
                            };
                            Console.WriteLine("[Launch] Cached token is still valid — skipping OAuth.");
                            return;
                        }
                        Console.WriteLine("[Launch] Cached token expired. Attempting MSAL silent refresh...");
                    }

                    // SLOW PATH: Minecraft token expired — refresh via MSAL silent flow.
                    // Only attempt if MSAL actually has a cached account (avoids a guaranteed
                    // MsalUiRequiredException that produces noisy logs with no benefit).
                    try
                    {
                        var app = await MsalClientHelper.BuildApplicationWithCache("499c8d36-be2a-4231-9ebd-ef291b7bb64c");
                        var msalAccounts = (await app.GetAccountsAsync()).ToList();
                        Console.WriteLine($"[Launch] MSAL cache contains {msalAccounts.Count} account(s).");

                        if (msalAccounts.Count > 0)
                        {
                            // BuildDefault() uses the same file-backed account manager as LoginWindow,
                            // so CreateAuthenticatorWithDefaultAccount() finds the saved loginHint.
                            var loginHandler = JELoginHandlerBuilder.BuildDefault();
                            var authenticator = loginHandler.CreateAuthenticatorWithDefaultAccount();
                            authenticator.AddMsalOAuth(app, msal => msal.Silent());
                            authenticator.AddXboxAuthForJE(xbox => xbox.Basic());
                            authenticator.AddForceJEAuthenticator();

                            var refreshedSession = await authenticator.ExecuteForLauncherAsync();
                            if (refreshedSession != null && refreshedSession.CheckIsValid())
                            {
                                _session = refreshedSession;
                                _currentSettings.SessionUsername = _session.Username;
                                _currentSettings.SessionUuid = _session.UUID;
                                _currentSettings.SessionAccessToken = _session.AccessToken;
                                _currentSettings.SessionXuid = _session.Xuid;
                                await _settingsService.SaveSettingsAsync(_currentSettings);
                                Console.WriteLine("[Launch] MSAL silent refresh succeeded.");
                                return;
                            }
                            Console.WriteLine("[Launch] MSAL silent refresh returned invalid session.");
                        }
                        else
                        {
                            Console.WriteLine("[Launch] MSAL has no cached accounts — silent refresh skipped.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Launch] MSAL silent refresh failed: {ex.GetType().Name}: {ex.Message}");
                    }

                    // FALLBACK: direct HTTP refresh using our own stored refresh token.
                    // This works even if the MSAL file cache is missing/corrupted because we
                    // captured the refresh token separately during login.
                    if (!string.IsNullOrWhiteSpace(_currentSettings.MicrosoftRefreshToken))
                    {
                        Console.WriteLine("[Launch] Trying direct HTTP refresh token exchange...");
                        var directSession = await TryDirectTokenRefreshAsync(_currentSettings.MicrosoftRefreshToken);
                        if (directSession != null)
                        {
                            _session = directSession;
                            Console.WriteLine("[Launch] Direct token refresh succeeded.");
                            return;
                        }
                        Console.WriteLine("[Launch] Direct token refresh failed — refresh token may be expired.");
                    }
                    else
                    {
                        Console.WriteLine("[Launch] No stored refresh token available (user logged in before this fix was deployed).");
                    }

                    _session = null;
                    Console.WriteLine("[Launch] All silent authentication methods exhausted.");
                    return;
                }

                if (_currentSettings.IsLoggedIn && _currentSettings.AccountType == "offline" &&
                    !string.IsNullOrWhiteSpace(_currentSettings.OfflineUsername))
                {
                    _session = MSession.CreateOfflineSession(_currentSettings.OfflineUsername);
                    return;
                }

                _session = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Launch] Failed to load session: {ex.GetType().Name}: {ex.Message}");
                _session = null;
            }
        }

        /// <summary>
        /// Opens LoginWindow for a transparent re-authentication when the session expires.
        /// Returns true if the user successfully re-authenticated, false otherwise.
        /// </summary>
        private async Task<bool> TryInteractiveReAuthAsync()
        {
            try
            {
                Console.WriteLine("[Launch] Session expired — prompting interactive re-authentication...");
                var tcs = new TaskCompletionSource<bool>();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var loginWindow = new LoginWindow();
                    loginWindow.LoginCompleted += (success) => tcs.TrySetResult(success);
                    loginWindow.Closed += (_, __) => tcs.TrySetResult(loginWindow.LoginSuccessful);
                    loginWindow.Show();
                });

                bool success = await tcs.Task;
                Console.WriteLine($"[Launch] Interactive re-auth result: {success}");
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Launch] Interactive re-auth failed: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Validates a Minecraft access token by calling the Minecraft profile API.
        /// Returns true if the token is still accepted by Mojang's servers.
        /// </summary>
        private static async Task<bool> ValidateMinecraftTokenAsync(string accessToken)
        {
            try
            {
                using var http = new System.Net.Http.HttpClient();
                http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                var resp = await http.GetAsync("https://api.minecraftservices.com/minecraft/profile");
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private Task<MSession?> TryDirectTokenRefreshAsync(string refreshToken)
            => DirectRefreshService.TryRefreshAsync(refreshToken, _currentSettings, _settingsService);

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
                        form.Add(new StringContent(variant), "variant");

                        byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                        var imageContent = new ByteArrayContent(fileBytes);
                        imageContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("image/png");

                        form.Add(imageContent, "file", "skin.png");

                        var response = await client.PostAsync(url, form);

                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine("[Skin Upload] Success! Skin updated.");

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
            // Control the container visibility
            if (_launchProgressPanel != null)
                _launchProgressPanel.IsVisible = show;

            // Update text if showing
            if (show && _launchProgressText != null)
                _launchProgressText.Text = text;

            // Mirror status into the launch overlay sub-text while it's visible
            if (_launchAnimOverlay?.IsVisible == true && _launchAnimSubText != null)
            {
                _launchAnimSubText.Text = show && !string.IsNullOrEmpty(text) ? text : "Preparing your game...";
            }

            if (show)
            {
                _currentOperationText = text.ToUpper();
                // Determine color based on text content for flavor
                if (text.Contains("Fabric")) _currentOperationColor = "DeepSkyBlue";
                else if (text.Contains("Sodium") || text.Contains("Lithium")) _currentOperationColor = "SeaGreen";
                else _currentOperationColor = "DeepSkyBlue";

                _isInstalling = true;
                UpdateLaunchButton(_currentOperationText, _currentOperationColor);
            }
            else
            {
                _isInstalling = false;
                ApplyLaunchButtonState();
            }
        }

        private bool IsOptiFineForFabricSupported(string version)
        {
            var supportedVersions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "1.21.10", "1.21.9", "1.21.8", "1.21.7", "1.21.6", "1.21.5", "1.21.4", "1.21.3", "1.21.1", 
    "1.20.6", "1.20.5", "1.20.4", "1.20.1", "1.19.2", "1.18.2", "1.17.1", "1.16.5"
};

            bool isSupported = supportedVersions.Contains(version);

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

                string exactVersionUrl = $"{baseApiUrl}?game_versions=[\"{mcVersion}\"]&loaders=[\"fabric\"]";
                var json = await client.GetStringAsync(exactVersionUrl);
                var versions = JsonSerializer.Deserialize(json, JsonContext.Default.ListModrinthVersion);

                if (versions != null && versions.Any())
                {
                    var latestVersion = versions.First();
                    var mrpackFile = latestVersion.files?.FirstOrDefault(f => !string.IsNullOrEmpty(f?.filename) && f.filename.EndsWith(".mrpack", StringComparison.OrdinalIgnoreCase));
                    if (mrpackFile?.url != null)
                    {
                        return mrpackFile.url;
                    }
                }

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

                Console.WriteLine("[OptiFineForFabric] No direct API filter match. Falling back to manual search of all versions.");
                json = await client.GetStringAsync(baseApiUrl); 
                var allVersions = JsonSerializer.Deserialize(json, JsonContext.Default.ListModrinthVersion);

                if (allVersions != null && allVersions.Any())
                {
                    if (Version.TryParse(mcVersion, out Version? targetSemver))
                    {
                        Models.ModrinthVersion? bestFallbackMatch = null; 
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
                                        bestFallbackMatch = v; 
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

        private void InitializeLauncher()
        {
            var path = new MinecraftPath(_minecraftFolder);
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(_minecraftFolder, "versions"));
            _launcher = new MinecraftLauncher(path);
        }


        private async Task DownloadFileWithProgressAsync(string url, string destinationPath, string fileName)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "LeafClient-Launcher");
            client.Timeout = TimeSpan.FromMinutes(10);

            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = totalBytes != -1;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var totalRead = 0L;
            var buffer = new byte[8192];
            var isMoreToRead = true;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_launchProgressPanel != null) _launchProgressPanel.IsVisible = true;
                if (_launchProgressBar != null)
                {
                    _launchProgressBar.IsIndeterminate = !canReportProgress;
                    _launchProgressBar.Minimum = 0;
                    _launchProgressBar.Maximum = 100;
                    _launchProgressBar.Value = 0;
                }
                if (_launchProgressText != null)
                    _launchProgressText.Text = $"Downloading {fileName}...";
            });

            do
            {
                var read = await contentStream.ReadAsync(buffer, 0, buffer.Length);
                if (read == 0)
                {
                    isMoreToRead = false;
                }
                else
                {
                    await fileStream.WriteAsync(buffer, 0, read);
                    totalRead += read;

                    if (canReportProgress)
                    {
                        var percent = (double)totalRead / totalBytes * 100;
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (_launchProgressBar != null) _launchProgressBar.Value = percent;
                            if (_launchProgressText != null) _launchProgressText.Text = $"Downloading {fileName} ({percent:F0}%)";
                        });
                    }
                }
            }
            while (isMoreToRead);
        }

        private async Task<bool> InstallSodiumIfNeededAsync(string mcVersion)
        {
            string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
            System.IO.Directory.CreateDirectory(modsFolder);

            try
            {
                ShowProgress(true, $"Fetching Sodium for {mcVersion}...");

                using var client = new HttpClient();
                var apiUrl = $"https://api.modrinth.com/v2/project/AANobbMI/version?game_versions=[\"{mcVersion}\"]&loaders=[\"fabric\"]";
                var response = await client.GetStringAsync(apiUrl);

                if (string.IsNullOrWhiteSpace(response)) throw new Exception("Empty response from Modrinth API");

                var versions = JsonSerializer.Deserialize(response, JsonContext.Default.ListModrinthVersion);
                var latest = versions?.FirstOrDefault();

                if (latest?.files == null || latest.files.Count == 0) throw new Exception("No Sodium files found");

                var file = latest.files.FirstOrDefault(f => f.filename?.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) == true) ?? latest.files.First();

                string fileName = $"sodium-fabric-{mcVersion}.jar";
                string sodiumPath = System.IO.Path.Combine(modsFolder, fileName);

                // USE TRACKED DOWNLOAD
                await DownloadFileWithProgressAsync(file.url, sodiumPath, "Sodium");

                Console.WriteLine($"[Sodium] Installed: {sodiumPath}");
                await RegisterAutoInstalledMod("sodium", "Sodium", latest.versionNumber, mcVersion, fileName, file.url);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Sodium] Install failed: {ex.Message}");
                ShowLaunchErrorBanner($"Failed to install Sodium: {ex.Message}");
                return false;
            }
            finally { ShowProgress(false); }
        }

        private async Task<bool> InstallLithiumIfNeededAsync(string mcVersion)
        {
            string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
            System.IO.Directory.CreateDirectory(modsFolder);

            try
            {
                ShowProgress(true, $"Fetching Lithium for {mcVersion}...");

                using var client = new HttpClient();
                var apiUrl = $"https://api.modrinth.com/v2/project/gvQqBUqZ/version?game_versions=[\"{mcVersion}\"]&loaders=[\"fabric\"]";
                var response = await client.GetStringAsync(apiUrl);

                var versions = JsonSerializer.Deserialize(response, JsonContext.Default.ListModrinthVersion);
                if (versions == null || versions.Count == 0) throw new Exception("No Lithium versions found");

                var latest = versions.FirstOrDefault();
                var downloadUrl = latest?.files?[0].url;

                if (string.IsNullOrWhiteSpace(downloadUrl)) throw new Exception("Invalid download URL");

                string fileName = $"lithium-{mcVersion}.jar";
                string lithiumPath = System.IO.Path.Combine(modsFolder, fileName);

                // USE TRACKED DOWNLOAD
                await DownloadFileWithProgressAsync(downloadUrl, lithiumPath, "Lithium");

                Console.WriteLine($"[Lithium] Installed: {lithiumPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Lithium] Install failed: {ex.Message}");
                return false;
            }
            finally { ShowProgress(false); }
        }

        private async Task<bool> InstallFerriteCorIfNeededAsync(string mcVersion)
        {
            string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
            System.IO.Directory.CreateDirectory(modsFolder);

            try
            {
                ShowProgress(true, $"Fetching FerriteCore for {mcVersion}...");

                using var client = new HttpClient();
                var apiUrl = $"https://api.modrinth.com/v2/project/uXXizFIs/version?game_versions=[\"{mcVersion}\"]&loaders=[\"fabric\"]";
                var response = await client.GetStringAsync(apiUrl);

                var versions = JsonSerializer.Deserialize(response, JsonContext.Default.ListModrinthVersion);
                if (versions == null || versions.Count == 0)
                {
                    Console.WriteLine($"[FerriteCore] No version found for {mcVersion}, skipping.");
                    return true; // Not a fatal error — just skip
                }

                var latest = versions.FirstOrDefault();
                var file = latest?.files?.FirstOrDefault(f => f.filename?.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) == true)
                           ?? latest?.files?.FirstOrDefault();

                if (file?.url == null) { Console.WriteLine("[FerriteCore] No download URL found, skipping."); return true; }

                string fileName = $"ferritecore-{mcVersion}.jar";
                string destPath = System.IO.Path.Combine(modsFolder, fileName);

                await DownloadFileWithProgressAsync(file.url, destPath, "FerriteCore");
                Console.WriteLine($"[FerriteCore] Installed: {destPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FerriteCore] Install failed: {ex.Message}");
                return false;
            }
            finally { ShowProgress(false); }
        }

        private async Task<bool> InstallImmediatelyFastIfNeededAsync(string mcVersion)
        {
            string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
            System.IO.Directory.CreateDirectory(modsFolder);

            try
            {
                ShowProgress(true, $"Fetching ImmediatelyFast for {mcVersion}...");

                using var client = new HttpClient();
                var apiUrl = $"https://api.modrinth.com/v2/project/5ZwThgaL/version?game_versions=[\"{mcVersion}\"]&loaders=[\"fabric\"]";
                var response = await client.GetStringAsync(apiUrl);

                var versions = JsonSerializer.Deserialize(response, JsonContext.Default.ListModrinthVersion);
                if (versions == null || versions.Count == 0)
                {
                    Console.WriteLine($"[ImmediatelyFast] No version found for {mcVersion}, skipping.");
                    return true;
                }

                var latest = versions.FirstOrDefault();
                var file = latest?.files?.FirstOrDefault(f => f.filename?.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) == true)
                           ?? latest?.files?.FirstOrDefault();

                if (file?.url == null) { Console.WriteLine("[ImmediatelyFast] No download URL found, skipping."); return true; }

                string fileName = $"immediatelyfast-{mcVersion}.jar";
                string destPath = System.IO.Path.Combine(modsFolder, fileName);

                await DownloadFileWithProgressAsync(file.url, destPath, "ImmediatelyFast");
                Console.WriteLine($"[ImmediatelyFast] Installed: {destPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ImmediatelyFast] Install failed: {ex.Message}");
                return false;
            }
            finally { ShowProgress(false); }
        }

        private async Task<bool> InstallEntityCullingIfNeededAsync(string mcVersion)
        {
            string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
            System.IO.Directory.CreateDirectory(modsFolder);

            try
            {
                ShowProgress(true, $"Fetching EntityCulling for {mcVersion}...");

                using var client = new HttpClient();
                var apiUrl = $"https://api.modrinth.com/v2/project/NNAgCjsB/version?game_versions=[\"{mcVersion}\"]&loaders=[\"fabric\"]";
                var response = await client.GetStringAsync(apiUrl);

                var versions = JsonSerializer.Deserialize(response, JsonContext.Default.ListModrinthVersion);
                if (versions == null || versions.Count == 0)
                {
                    Console.WriteLine($"[EntityCulling] No version found for {mcVersion}, skipping.");
                    return true;
                }

                var latest = versions.FirstOrDefault();
                var file = latest?.files?.FirstOrDefault(f => f.filename?.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) == true)
                           ?? latest?.files?.FirstOrDefault();

                if (file?.url == null) { Console.WriteLine("[EntityCulling] No download URL found, skipping."); return true; }

                string fileName = $"entityculling-{mcVersion}.jar";
                string destPath = System.IO.Path.Combine(modsFolder, fileName);

                await DownloadFileWithProgressAsync(file.url, destPath, "EntityCulling");
                Console.WriteLine($"[EntityCulling] Installed: {destPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EntityCulling] Install failed: {ex.Message}");
                return false;
            }
            finally { ShowProgress(false); }
        }

        private async Task<bool> InstallModernFixIfNeededAsync(string mcVersion)
        {
            string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
            System.IO.Directory.CreateDirectory(modsFolder);

            try
            {
                ShowProgress(true, $"Fetching ModernFix for {mcVersion}...");

                using var client = new HttpClient();
                var apiUrl = $"https://api.modrinth.com/v2/project/nmDe0x5F/version?game_versions=[\"{mcVersion}\"]&loaders=[\"fabric\"]";
                var response = await client.GetStringAsync(apiUrl);

                var versions = JsonSerializer.Deserialize(response, JsonContext.Default.ListModrinthVersion);
                if (versions == null || versions.Count == 0)
                {
                    Console.WriteLine($"[ModernFix] No version found for {mcVersion}, skipping.");
                    return true;
                }

                var latest = versions.FirstOrDefault();
                var file = latest?.files?.FirstOrDefault(f => f.filename?.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) == true)
                           ?? latest?.files?.FirstOrDefault();

                if (file?.url == null) { Console.WriteLine("[ModernFix] No download URL found, skipping."); return true; }

                string fileName = $"modernfix-{mcVersion}.jar";
                string destPath = System.IO.Path.Combine(modsFolder, fileName);

                await DownloadFileWithProgressAsync(file.url, destPath, "ModernFix");
                Console.WriteLine($"[ModernFix] Installed: {destPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ModernFix] Install failed: {ex.Message}");
                return false;
            }
            finally { ShowProgress(false); }
        }

        private async Task BuildAndCopyLocalModAsync(string mcVersion, string destJarPath)
        {
            string projectDir = _currentSettings.TestModeModProjectPath;
            string versionFolder = System.IO.Path.Combine(projectDir, "versions", $"{mcVersion}-fabric");

            if (!System.IO.Directory.Exists(versionFolder))
                throw new Exception($"[Test Mode] No version folder found: {versionFolder}");

            _gameOutputWindow?.AppendLog($"[Test Mode] Building local JAR for {mcVersion}...", "INFO");
            ShowProgress(true, $"Building LeafClient {mcVersion}...");

            // Determine gradle wrapper path and command
            string gradlewBat = System.IO.Path.Combine(projectDir, "gradlew.bat");
            string gradlewSh  = System.IO.Path.Combine(projectDir, "gradlew");

            string fileName;
            string arguments;

            if (System.OperatingSystem.IsWindows())
            {
                // On Windows, .bat files cannot be launched directly with UseShellExecute=false.
                // We must invoke cmd.exe /c to execute the batch file.
                if (!System.IO.File.Exists(gradlewBat))
                    throw new Exception("[Test Mode] gradlew.bat not found in project directory.");
                fileName  = "cmd.exe";
                arguments = $"/c \"{gradlewBat}\" :{mcVersion}-fabric:clean :{mcVersion}-fabric:remapJar";
            }
            else
            {
                if (!System.IO.File.Exists(gradlewSh))
                    throw new Exception("[Test Mode] gradlew not found in project directory.");
                fileName  = gradlewSh;
                arguments = $":{mcVersion}-fabric:clean :{mcVersion}-fabric:remapJar";
            }

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = projectDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Capture build start time BEFORE the build runs so we can filter stale artifacts out later.
            DateTime buildStartUtc = DateTime.UtcNow;
            _gameOutputWindow?.AppendLog($"[LaunchDiag] Gradle build start (UTC): {buildStartUtc:yyyy-MM-dd HH:mm:ss}", "INFO");

            using var proc = new System.Diagnostics.Process { StartInfo = psi };
            proc.OutputDataReceived += (_, e) => { if (e.Data != null) _gameOutputWindow?.AppendLog(e.Data, "INFO"); };
            proc.ErrorDataReceived  += (_, e) => { if (e.Data != null) _gameOutputWindow?.AppendLog(e.Data, "WARN"); };
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            await Task.Run(() => proc.WaitForExit());

            if (proc.ExitCode != 0)
                throw new Exception($"[Test Mode] Gradle build failed with exit code {proc.ExitCode}");

            // Robust jar discovery: look in build/libs AND build/devlibs, filter out dev/sources/javadoc jars,
            // require mtime >= buildStartUtc (minus 10s tolerance), prefer the newest remapped jar.
            string libsDir = System.IO.Path.Combine(versionFolder, "build", "libs");
            string devLibsDir = System.IO.Path.Combine(versionFolder, "build", "devlibs");

            _gameOutputWindow?.AppendLog($"[LaunchDiag] Scanning for built jar in: {libsDir}", "INFO");
            _gameOutputWindow?.AppendLog($"[LaunchDiag] (also checking devlibs for visibility): {devLibsDir}", "INFO");

            static string[] SafeListJars(string dir)
            {
                try { return Directory.Exists(dir) ? Directory.GetFiles(dir, "*.jar", SearchOption.TopDirectoryOnly) : Array.Empty<string>(); }
                catch { return Array.Empty<string>(); }
            }

            var libsJars = SafeListJars(libsDir);
            var devLibsJars = SafeListJars(devLibsDir);

            // Log everything we found in both folders for full transparency.
            _gameOutputWindow?.AppendLog($"[LaunchDiag] build/libs    contains {libsJars.Length} jar(s):", "INFO");
            foreach (var f in libsJars) LogJarFileInfo("  build/libs", f);
            _gameOutputWindow?.AppendLog($"[LaunchDiag] build/devlibs contains {devLibsJars.Length} jar(s):", "INFO");
            foreach (var f in devLibsJars) LogJarFileInfo("  build/devlibs", f);

            // Pick the right jar. Rules:
            //   1. MUST be in build/libs (remapJar output), NEVER build/devlibs.
            //   2. Name should start with "leafclient" (loom default or our override).
            //   3. Exclude -dev.jar, -sources.jar, -javadoc.jar.
            //   4. Prefer mtime >= buildStartUtc (fresh build product); allow a 10s tolerance
            //      for clock drift between our UtcNow and the filesystem timestamp.
            //   5. If multiple candidates, pick the newest.
            var buildThresholdUtc = buildStartUtc.AddSeconds(-10);

            var libsCandidates = libsJars
                .Select(p => new FileInfo(p))
                .Where(fi =>
                {
                    string name = fi.Name;
                    if (!name.EndsWith(".jar", StringComparison.OrdinalIgnoreCase)) return false;
                    if (name.EndsWith("-dev.jar",     StringComparison.OrdinalIgnoreCase)) return false;
                    if (name.EndsWith("-sources.jar", StringComparison.OrdinalIgnoreCase)) return false;
                    if (name.EndsWith("-javadoc.jar", StringComparison.OrdinalIgnoreCase)) return false;
                    if (!name.StartsWith("leafclient", StringComparison.OrdinalIgnoreCase)) return false;
                    return true;
                })
                .ToList();

            var freshCandidates = libsCandidates
                .Where(fi => fi.LastWriteTimeUtc >= buildThresholdUtc)
                .OrderByDescending(fi => fi.LastWriteTimeUtc)
                .ToList();

            FileInfo? chosen = freshCandidates.FirstOrDefault();

            if (chosen == null && libsCandidates.Count > 0)
            {
                // Gradle may have been "UP-TO-DATE" (incremental build). Accept the newest stale candidate
                // but surface a prominent warning so we can detect this scenario in the logs.
                chosen = libsCandidates.OrderByDescending(fi => fi.LastWriteTimeUtc).First();
                _gameOutputWindow?.AppendLog(
                    $"[LaunchDiag] WARN: No jar in build/libs was modified after build start. " +
                    $"Accepting STALE candidate {chosen.Name} (mtime={chosen.LastWriteTime:yyyy-MM-dd HH:mm:ss}). " +
                    $"The Gradle :clean task may have been a no-op, or the build was UP-TO-DATE.",
                    "WARN");
            }

            if (chosen == null)
            {
                var libsNames   = libsJars.Length   > 0 ? string.Join(", ", libsJars.Select(System.IO.Path.GetFileName))   : "(empty)";
                var devLibsNames = devLibsJars.Length > 0 ? string.Join(", ", devLibsJars.Select(System.IO.Path.GetFileName)) : "(empty)";
                throw new Exception(
                    $"[Test Mode] No remapped leafclient jar found after Gradle build for {mcVersion}.\n" +
                    $"  build/libs:    {libsNames}\n" +
                    $"  build/devlibs: {devLibsNames}\n" +
                    $"  Expected a *.jar in build/libs whose name starts with 'leafclient' and does not end with '-dev.jar', '-sources.jar', or '-javadoc.jar'.");
            }

            string builtJar = chosen.FullName;
            _gameOutputWindow?.AppendLog($"[LaunchDiag] Chosen source jar: {builtJar}", "INFO");
            LogJarFileInfo("source jar (chosen)", builtJar);

            // Ensure destination directory exists, then delete any stale destination before copying.
            string? destDir = System.IO.Path.GetDirectoryName(destJarPath);
            if (!string.IsNullOrEmpty(destDir)) Directory.CreateDirectory(destDir);
            try { if (File.Exists(destJarPath)) File.Delete(destJarPath); } catch { /* best-effort */ }

            System.IO.File.Copy(builtJar, destJarPath, overwrite: true);

            // Verify the copy by comparing hashes of source and destination.
            string srcHash = ComputeShortSha256(builtJar);
            string dstHash = ComputeShortSha256(destJarPath);
            long srcLen = new FileInfo(builtJar).Length;
            long dstLen = new FileInfo(destJarPath).Length;

            _gameOutputWindow?.AppendLog("[LaunchDiag] ===== POST-BUILD COPY VERIFICATION =====", "INFO");
            LogJarFileInfo("src", builtJar);
            LogJarFileInfo("dst", destJarPath);

            if (srcHash != dstHash || srcLen != dstLen)
            {
                _gameOutputWindow?.AppendLog(
                    $"[LaunchDiag] ERROR: src/dst mismatch after copy! srcHash={srcHash} dstHash={dstHash} srcLen={srcLen} dstLen={dstLen}",
                    "ERROR");
                throw new Exception("[Test Mode] Built JAR copy verification failed (hash/size mismatch between src and dst).");
            }
            _gameOutputWindow?.AppendLog($"[LaunchDiag] OK: src/dst match (sha16={srcHash}, size={srcLen / 1024}KB)", "INFO");
            _gameOutputWindow?.AppendLog("[LaunchDiag] ===== END POST-BUILD COPY VERIFICATION =====", "INFO");

            var jarInfo = new System.IO.FileInfo(destJarPath);
            string builtAt = jarInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
            long sizeKb = jarInfo.Length / 1024;
            _gameOutputWindow?.AppendLog("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", "INFO");
            _gameOutputWindow?.AppendLog($"  TEST MODE BUILD READY", "INFO");
            _gameOutputWindow?.AppendLog($"  JAR  : {destJarPath}", "INFO");
            _gameOutputWindow?.AppendLog($"  Built: {builtAt}  ({sizeKb} KB)  sha16={dstHash}", "INFO");
            _gameOutputWindow?.AppendLog("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", "INFO");
        }

        private async Task<bool> InstallPhosphorIfNeededAsync(string mcVersion)
        {
            string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
            System.IO.Directory.CreateDirectory(modsFolder);

            try
            {
                ShowProgress(true, $"Fetching Phosphor for {mcVersion}...");

                using var client = new HttpClient();
                // Phosphor (Modrinth: hEOCdOgW) — may only be available for older MC versions
                var apiUrl = $"https://api.modrinth.com/v2/project/hEOCdOgW/version?game_versions=[\"{mcVersion}\"]&loaders=[\"fabric\"]";
                var response = await client.GetStringAsync(apiUrl);

                var versions = JsonSerializer.Deserialize(response, JsonContext.Default.ListModrinthVersion);
                if (versions == null || versions.Count == 0)
                {
                    Console.WriteLine($"[Phosphor] Not available for {mcVersion}, skipping.");
                    return true; // not a failure — just not available for this version
                }

                var latest = versions.First();
                var file = latest.files?.FirstOrDefault(f => f.filename?.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) == true) ?? latest.files?.First();
                if (file == null) return true;

                string fileName = $"phosphor-fabric-{mcVersion}.jar";
                string destPath = System.IO.Path.Combine(modsFolder, fileName);
                await DownloadFileWithProgressAsync(file.url, destPath, "Phosphor");
                Console.WriteLine($"[Phosphor] Installed: {destPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Phosphor] Install failed (may not support this version): {ex.Message}");
                return true; // non-fatal
            }
            finally { ShowProgress(false); }
        }

        private async Task<bool> InstallIrisShadersIfNeededAsync(string mcVersion)
        {
            string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
            System.IO.Directory.CreateDirectory(modsFolder);

            try
            {
                ShowProgress(true, $"Fetching Iris Shaders for {mcVersion}...");

                using var client = new HttpClient();
                // Iris Shaders (Modrinth: YL57xq9U)
                var apiUrl = $"https://api.modrinth.com/v2/project/YL57xq9U/version?game_versions=[\"{mcVersion}\"]&loaders=[\"fabric\"]";
                var response = await client.GetStringAsync(apiUrl);

                var versions = JsonSerializer.Deserialize(response, JsonContext.Default.ListModrinthVersion);
                if (versions == null || versions.Count == 0)
                {
                    Console.WriteLine($"[Iris] Not available for {mcVersion}, skipping.");
                    return true;
                }

                var latest = versions.First();
                var file = latest.files?.FirstOrDefault(f => f.filename?.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) == true) ?? latest.files?.First();
                if (file == null) return true;

                string fileName = $"iris-fabric-{mcVersion}.jar";
                string destPath = System.IO.Path.Combine(modsFolder, fileName);
                await DownloadFileWithProgressAsync(file.url, destPath, "Iris Shaders");
                Console.WriteLine($"[Iris] Installed: {destPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Iris] Install failed: {ex.Message}");
                return true;
            }
            finally { ShowProgress(false); }
        }

        private async Task<bool> InstallDynamicLightsIfNeededAsync(string mcVersion)
        {
            string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
            System.IO.Directory.CreateDirectory(modsFolder);

            try
            {
                ShowProgress(true, $"Fetching Dynamic Lights for {mcVersion}...");

                using var client = new HttpClient();
                // LambDynamicLights (Modrinth: H8CuhNKC)
                var apiUrl = $"https://api.modrinth.com/v2/project/H8CuhNKC/version?game_versions=[\"{mcVersion}\"]&loaders=[\"fabric\"]";
                var response = await client.GetStringAsync(apiUrl);

                var versions = JsonSerializer.Deserialize(response, JsonContext.Default.ListModrinthVersion);
                if (versions == null || versions.Count == 0)
                {
                    Console.WriteLine($"[DynamicLights] Not available for {mcVersion}, skipping.");
                    return true;
                }

                var latest = versions.First();
                var file = latest.files?.FirstOrDefault(f => f.filename?.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) == true) ?? latest.files?.First();
                if (file == null) return true;

                string fileName = $"lambdynamiclights-fabric-{mcVersion}.jar";
                string destPath = System.IO.Path.Combine(modsFolder, fileName);
                await DownloadFileWithProgressAsync(file.url, destPath, "Dynamic Lights");
                Console.WriteLine($"[DynamicLights] Installed: {destPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DynamicLights] Install failed: {ex.Message}");
                return true;
            }
            finally { ShowProgress(false); }
        }

        private async Task<bool> InstallBetterFpsIfNeededAsync(string mcVersion)
        {
            string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
            System.IO.Directory.CreateDirectory(modsFolder);

            try
            {
                ShowProgress(true, $"Fetching BetterFps for {mcVersion}...");

                using var client = new HttpClient();
                // Better Fps - Render Distance (Modrinth: mfzaZK3z)
                var apiUrl = $"https://api.modrinth.com/v2/project/mfzaZK3z/version?game_versions=[\"{mcVersion}\"]&loaders=[\"fabric\"]";
                var response = await client.GetStringAsync(apiUrl);

                var versions = JsonSerializer.Deserialize(response, JsonContext.Default.ListModrinthVersion);
                if (versions == null || versions.Count == 0)
                {
                    Console.WriteLine($"[BetterFps] Not available for {mcVersion}, skipping.");
                    return true;
                }

                var latest = versions.First();
                var file = latest.files?.FirstOrDefault(f => f.filename?.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) == true) ?? latest.files?.First();
                if (file == null) return true;

                string fileName = $"betterfps-fabric-{mcVersion}.jar";
                string destPath = System.IO.Path.Combine(modsFolder, fileName);
                await DownloadFileWithProgressAsync(file.url, destPath, "BetterFps");
                Console.WriteLine($"[BetterFps] Installed: {destPath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BetterFps] Install failed: {ex.Message}");
                return true;
            }
            finally { ShowProgress(false); }
        }

        // 4. REPLACES InstallFabricApiIfNeededAsync
        private async Task<bool> InstallFabricApiIfNeededAsync(string mcVersion, string versionFolderName)
        {
            string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
            System.IO.Directory.CreateDirectory(modsFolder);
            string fabricApiPath = System.IO.Path.Combine(modsFolder, $"fabric-api-{mcVersion}.jar");

            try
            {
                if (System.IO.File.Exists(fabricApiPath) && _currentSettings.InstalledMods.Any(m => m.ModId == "fabric-api" && m.MinecraftVersion == mcVersion))
                {
                    return true;
                }

                ShowProgress(true, $"Fetching Fabric API for {mcVersion}...");

                using var client = new HttpClient();
                var apiUrl = $"https://api.modrinth.com/v2/project/P7dR8mSH/version?game_versions=[\"{mcVersion}\"]&loaders=[\"fabric\"]";
                var response = await client.GetStringAsync(apiUrl);
                var versions = JsonSerializer.Deserialize(response, JsonContext.Default.ListModrinthVersion);
                var latest = versions?.FirstOrDefault();

                if (latest?.files == null || latest.files.Count == 0) throw new Exception("No Fabric API file found");

                var file = latest.files.FirstOrDefault(f => f.filename?.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) == true) ?? latest.files.First();

                // USE TRACKED DOWNLOAD
                await DownloadFileWithProgressAsync(file.url, fabricApiPath, "Fabric API");

                await RegisterAutoInstalledMod("fabric-api", "Fabric API", latest.versionNumber, mcVersion, $"fabric-api-{mcVersion}.jar", file.url);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Fabric API] Install failed: {ex.Message}");
                return false;
            }
            finally { ShowProgress(false); }
        }

        // 5. REPLACES InstallOptiFineForFabricIfNeededAsync
        private async Task<bool> InstallOptiFineForFabricIfNeededAsync(string mcVersion, string profileName, bool isFabric)
        {
            if (!isFabric || !_currentSettings.IsOptiFineEnabled)
            {
                if (!isFabric) Console.WriteLine("[OptiFine] Not Fabric profile.");
                else ManageOptiFineForFabricMods(mcVersion, enable: false);
                return true;
            }

            string? mrpackUrl = await GetOptiFineMrpackUrlForVersion(mcVersion);
            if (string.IsNullOrEmpty(mrpackUrl))
            {
                ShowLaunchErrorBanner($"OptiFine pack not found for {mcVersion}.");
                ManageOptiFineForFabricMods(mcVersion, enable: false);
                return false;
            }

            string mrpackPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"OptiFineForFabric_{mcVersion}.mrpack");

            try
            {
                // USE TRACKED DOWNLOAD
                await DownloadFileWithProgressAsync(mrpackUrl, mrpackPath, "OptiFine Pack");
            }
            catch (Exception ex)
            {
                ShowLaunchErrorBanner($"Failed to download OptiFine pack: {ex.Message}");
                return false;
            }

            bool success = false;
            try
            {
                success = await ProcessModrinthPackInstallation(mrpackPath, System.IO.Path.Combine(_minecraftFolder, "mods"), mcVersion);
                ManageOptiFineForFabricMods(mcVersion, enable: success);
            }
            catch (Exception ex)
            {
                ShowLaunchErrorBanner($"Error installing OptiFine pack: {ex.Message}");
                ManageOptiFineForFabricMods(mcVersion, enable: false);
            }
            finally
            {
                if (File.Exists(mrpackPath)) File.Delete(mrpackPath);
            }

            return success;
        }

        private async Task DownloadAndInstallMod(string downloadUrl, string fileName, string modName, InstalledMod installedMod)
        {
            var modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
            Directory.CreateDirectory(modsFolder);
            var filePath = System.IO.Path.Combine(modsFolder, fileName);

            ShowProgress(true, $"Downloading {modName}...");

            try
            {
                await DownloadFileWithProgressAsync(downloadUrl, filePath, modName);
                installedMod.FileName = fileName;
                Console.WriteLine($"[Mod Install] Successfully installed {modName}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Mod Install ERROR] Failed to download {modName}: {ex.Message}");
                ShowLaunchErrorBanner($"Failed to download {modName}. Check your internet connection.");
                throw;
            }
            finally
            {
                ShowProgress(false);
            }
        }


        private async Task<bool> DownloadFileAsync(string url, string destinationPath)
        {
            try
            {
                await DownloadFileWithProgressAsync(url, destinationPath, System.IO.Path.GetFileName(destinationPath));
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
                    IconUrl = "",
                    IsAutoInstalled = true,
                });
                Console.WriteLine($"[Mod Tracker] Registered {modName} for MC {mcVersion} in settings (auto-installed).");
            }
            else
            {
                existing.FileName = fileName;
                existing.DownloadUrl = url;
                existing.Version = version;
                existing.Enabled = true;
                existing.IsAutoInstalled = true; // Backfill the flag for rows saved before the field existed
            }

            await _settingsService.SaveSettingsAsync(_currentSettings);
        }

        private async Task ApplyModPresetForLaunchAsync(string mcVersion, string fabricProfile)
        {
            // Get the active profile and its mod preset
            var activeProfile = _currentSettings.Profiles?.FirstOrDefault(p => p.Id == _currentSettings.ActiveProfileId)
                             ?? _currentSettings.Profiles?.FirstOrDefault();
            if (activeProfile == null)
            {
                Console.WriteLine("[Mod Preset] No active profile found.");
                return;
            }

            string preset = activeProfile.ModPreset ?? "none";
            Console.WriteLine($"[Mod Preset] Applying preset '{preset}' for MC {mcVersion}");

            // Always-required performance mods — install regardless of preset
            var alwaysRequired = new[] { "sodium", "lithium", "ferritecore", "immediatelyfast", "entityculling", "modernfix" };
            foreach (var modKey in alwaysRequired)
            {
                try
                {
                    switch (modKey)
                    {
                        case "sodium":        await InstallSodiumIfNeededAsync(mcVersion); break;
                        case "lithium":       await InstallLithiumIfNeededAsync(mcVersion); break;
                        case "ferritecore":   await InstallFerriteCorIfNeededAsync(mcVersion); break;
                        case "immediatelyfast": await InstallImmediatelyFastIfNeededAsync(mcVersion); break;
                        case "entityculling": await InstallEntityCullingIfNeededAsync(mcVersion); break;
                        case "modernfix":     await InstallModernFixIfNeededAsync(mcVersion); break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Required Mod] Failed to install {modKey}: {ex.Message}");
                }
            }

            // Map presets to mod lists (sodium/lithium already installed above)
            var presetMap = new Dictionary<string, string[]>
            {
                ["none"]     = Array.Empty<string>(),
                ["lite"]     = new[] { "fabric-api" },
                ["balanced"] = new[] { "fabric-api", "phosphor" },
                ["enhanced"] = new[] { "fabric-api", "phosphor", "iris", "dynamiclights", "betterfps" }
            };

            if (!presetMap.TryGetValue(preset, out var modsToInstall))
            {
                Console.WriteLine($"[Mod Preset] Unknown preset '{preset}', defaulting to none.");
                return;
            }

            if (modsToInstall.Length == 0)
            {
                Console.WriteLine("[Mod Preset] No mods to install for preset 'none'.");
                return;
            }

            // Install mods based on preset
            foreach (var modKey in modsToInstall)
            {
                try
                {
                    switch (modKey)
                    {
                        case "fabric-api":
                            Console.WriteLine("[Mod Preset] Installing Fabric API...");
                            await InstallFabricApiIfNeededAsync(mcVersion, fabricProfile);
                            break;
                        case "phosphor":
                            Console.WriteLine("[Mod Preset] Installing Phosphor...");
                            await InstallPhosphorIfNeededAsync(mcVersion);
                            break;
                        case "iris":
                            Console.WriteLine("[Mod Preset] Installing Iris Shaders...");
                            await InstallIrisShadersIfNeededAsync(mcVersion);
                            break;
                        case "dynamiclights":
                            Console.WriteLine("[Mod Preset] Installing Dynamic Lights...");
                            await InstallDynamicLightsIfNeededAsync(mcVersion);
                            break;
                        case "betterfps":
                            Console.WriteLine("[Mod Preset] Installing BetterFps...");
                            await InstallBetterFpsIfNeededAsync(mcVersion);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Mod Preset] Failed to install {modKey}: {ex.Message}");
                }
            }

            Console.WriteLine($"[Mod Preset] Finished applying preset '{preset}'");
        }

        private async Task<bool> ProcessModrinthPackInstallation(string mrpackPath, string modsFolder, string mcVersion)
        {
            Console.WriteLine($"[Modpack] Starting to process .mrpack installation from {mrpackPath} for MC {mcVersion}...");

            string tempExtractionFolder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ModrinthPackExtract_" + Guid.NewGuid());
            System.IO.Directory.CreateDirectory(tempExtractionFolder);
            Console.WriteLine($"[Modpack] Created temporary extraction folder: {tempExtractionFolder}");

            try
            {
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

                string indexPath = System.IO.Path.Combine(tempExtractionFolder, "modrinth.index.json");
                if (!System.IO.File.Exists(indexPath))
                {
                    Console.Error.WriteLine($"[Modpack ERROR] modrinth.index.json not found at {indexPath}. Cannot install mods.");
                    return false;
                }

                string indexJson = await System.IO.File.ReadAllTextAsync(indexPath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var pack = JsonSerializer.Deserialize(indexJson, JsonContext.Default.ModrinthPack);

                if (pack == null || pack.Files == null || pack.Files.Count == 0)
                {
                    Console.Error.WriteLine("[Modpack ERROR] ModrinthPack deserialized to null or contains no files.");
                    return false;
                }

                using (var httpClient = new HttpClient()) 
                {
                    foreach (var file in pack.Files)
                    {
                        if (file == null)
                        {
                            Console.WriteLine("[Modpack] Skipping null file entry in modrinth.index.json.");
                            continue;
                        }

                        if (string.IsNullOrEmpty(file.Path) || file.Path.Contains("..") || System.IO.Path.IsPathRooted(file.Path))
                        {
                            Console.Error.WriteLine($"[Modpack ERROR] Unsafe or invalid file path '{file.Path}', skipping download.");
                            continue;
                        }

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
                        string downloadUrl = file.Downloads[0]; 

                        Console.WriteLine($"[Modpack] Downloading mod: {fileName} from {downloadUrl}");
                        ShowProgress(true, $"Downloading {fileName}...");

                        try
                        {
                            byte[] modData = await httpClient.GetByteArrayAsync(downloadUrl);

                            if (file.Hashes != null && file.Hashes.TryGetValue("sha512", out var expectedHash))
                            {
                                using var sha512 = System.Security.Cryptography.SHA512.Create();
                                var actualHashBytes = sha512.ComputeHash(modData);
                                var actualHashString = BitConverter.ToString(actualHashBytes).Replace("-", "").ToLowerInvariant();

                                if (!actualHashString.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
                                {
                                    Console.Error.WriteLine($"[Modpack ERROR] Hash mismatch for {fileName}! Expected: {expectedHash}, Got: {actualHashString}. Skipping file.");
                                    continue; 
                                }
                                Console.WriteLine($"[Modpack] Hash validated for {fileName}.");
                            }

                            await System.IO.File.WriteAllBytesAsync(destPath, modData);
                            Console.WriteLine($"[Modpack] Successfully installed mod: {fileName} to {destPath}");

                            var installedMod = new InstalledMod
                            {
                                ModId = System.IO.Path.GetFileNameWithoutExtension(fileName), 
                                Name = fileName, 
                                Description = "Installed from Modrinth pack",
                                Version = "N/A", 
                                MinecraftVersion = mcVersion, 
                                FileName = fileName,
                                DownloadUrl = downloadUrl,
                                Enabled = true, 
                                InstallDate = DateTime.Now,
                                IconUrl = "" 
                            };

                            var existingMod = _currentSettings.InstalledMods.FirstOrDefault(m => m.ModId == installedMod.ModId && m.MinecraftVersion == installedMod.MinecraftVersion);
                            if (existingMod == null)
                            {
                                _currentSettings.InstalledMods.Add(installedMod);
                                Console.WriteLine($"[Modpack] Added '{installedMod.Name}' to settings.");
                            }
                            else
                            {
                                existingMod.Name = installedMod.Name;
                                existingMod.Description = installedMod.Description;
                                existingMod.Version = installedMod.Version;
                                existingMod.FileName = installedMod.FileName;
                                existingMod.DownloadUrl = installedMod.DownloadUrl;
                                existingMod.Enabled = true; 
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
                ShowProgress(false); 
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


        private async void LaunchFromVersionsSidebar(object? sender, RoutedEventArgs e)
        {
            AnimateSelectionIndicator(0);
            _currentSelectedIndex = 0;
            SwitchToPage(0);

            await Task.Delay(300);

            var launchBtn = this.FindControl<Button>("LaunchGameButton");
            if (launchBtn != null)
            {
                if (_isLaunching)
                {
                    if (_gameProcess != null && !_gameProcess.HasExited)
                    {
                        _userTerminatedGame = true;
                        _gameProcess.Kill();
                    }
                    _launchCancellationTokenSource?.Cancel();
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
            if (_isLaunching || _launcher == null)
            {
                Console.WriteLine("[Launch] Game is already launching or launcher is not initialized. Aborting new launch request.");
                return;
            }

            _currentSettings.QuickJoinServerAddress = server.Address;
            _currentSettings.QuickJoinServerPort = server.Port.ToString();
            await _settingsService.SaveSettingsAsync(_currentSettings);

            Console.WriteLine($"[Server] Preparing to join {server.Name} at {server.Address}:{server.Port}");

            AnimateSelectionIndicator(0);
            _currentSelectedIndex = 0;
            SwitchToPage(0);

            await Task.Delay(300);

            var launchBtn = this.FindControl<Button>("LaunchGameButton");
            if (launchBtn != null)
            {
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

                if (_quickPlayServersContainer != null)
                {
                    foreach (var child in _quickPlayServersContainer.Children)
                    {
                        if (child is Button qpBtn && qpBtn.Tag is ServerInfo qpServer)
                        {
                            qpBtn.IsEnabled = true; 

                            qpBtn.Opacity = (isAnyLaunchInProgress || !qpServer.IsOnline) ? 0.3 : 1.0;
                        }
                    }
                }

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
                                        joinButton.IsEnabled = !isAnyLaunchInProgress && server.IsOnline;
                                        joinButton.Background = server.IsOnline
                                            ? GetBrush("PrimaryAccentBrush")
                                            : GetBrush("HoverBackgroundBrush");
                                        joinButton.Foreground = server.IsOnline
                                            ? GetBrush("AccentButtonForegroundBrush")
                                            : GetBrush("DisabledForegroundBrush");
                                        joinButton.Opacity = 1.0; 
                                    }
                                    else
                                    {
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
            if (_isLaunching) return;

            Console.WriteLine($"[Launch] Starting game: version={version}, fabric={isFabric}, user={_currentUsername ?? "(none)"}");
            InitializeLauncher();

            _isLaunching = true;
            _userTerminatedGame = false;
            _gameStartingBannerShownForCurrentLaunch = false;
            _launchFailureBannerShownForCurrentLaunch = false;

            ShowLaunchAnimation();

            await UpdateServerButtonStates();

            ShowProgress(false);

            _launchCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _launchCancellationTokenSource.Token;

            HideLaunchErrorBanner();
            HideLaunchFailureBanner();

            await LoadSessionAsync();

            if (_session == null)
            {
                // For Microsoft accounts whose session expired, attempt transparent re-auth
                // before giving up — users should never need to manually log out and back in.
                if (_currentSettings?.IsLoggedIn == true && _currentSettings.AccountType == "microsoft")
                {
                    bool reAuthed = await TryInteractiveReAuthAsync();
                    if (reAuthed)
                    {
                        // Re-load session from freshly saved settings after successful re-auth.
                        await LoadSessionAsync();
                    }
                }

                if (_session == null)
                {
                    HideLaunchAnimation();
                    UpdateLaunchButton("LOGIN REQUIRED", "OrangeRed");
                    ShowLaunchErrorBanner("Session expired. Please log out and log back in.");
                    _isLaunching = false;
                    await UpdateServerButtonStates();
                    return;
                }
            }

            bool isNetworkAvailable = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
            if (!isNetworkAvailable && _currentSettings.AccountType != "offline")
            {
                HideLaunchAnimation();
                UpdateLaunchButton("YOU'RE OFFLINE", "Gray");
                ShowLaunchErrorBanner("No internet connection. Cannot launch online game.");
                _isLaunching = false;
                await UpdateServerButtonStates();
                return;
            }

            string versionToLaunch = version;

            try
            {
                if (_gameOutputWindow == null)
                {
                    _gameOutputWindow = new GameOutputWindow();

                    _gameOutputWindow.Closing += (s, args) =>
                    {
                        if (_gameProcess != null && !_gameProcess.HasExited)
                        {
                            args.Cancel = true;
                            _gameOutputWindow.Hide();
                        }
                    };

                    _gameOutputWindow.Closed += (s, e) => _gameOutputWindow = null;

                    _gameOutputWindow.KillGameRequested += (s, e) =>
                    {
                        if (_gameProcess != null && !_gameProcess.HasExited)
                        {
                            try
                            {
                                _userTerminatedGame = true;
                                _gameProcess.Kill();
                                _gameOutputWindow?.AppendLog("Kill signal sent to game process.", "WARN");
                            }
                            catch (Exception ex)
                            {
                                _gameOutputWindow?.AppendLog($"Failed to kill process: {ex.Message}", "ERROR");
                            }
                        }
                    };
                }


                _gameOutputWindow.ClearLog();
                _gameOutputWindow.SetSessionInfo(versionToLaunch, _session.Username, _logFolderPath);
                _gameOutputWindow.Show();
                _gameOutputWindow.Activate();
                _gameOutputWindow.AppendLog($"Initializing launch sequence for {version}...", "INFO");

                UpdateLaunchButton("PREPARING...", "DeepSkyBlue");
                ClearModsFolder();

                string leafModPath = GetLeafOfflineJarPath(version);

                bool leafSupported = IsLeafRuntimeVersion(version);

                _gameOutputWindow?.AppendLog($"[LaunchDiag] Mode: {(_currentSettings.IsTestMode ? "TEST (local build)" : "NORMAL (github download)")}", "INFO");
                _gameOutputWindow?.AppendLog($"[LaunchDiag] LeafClient mod support for {version}: {(leafSupported ? "YES" : "NO (launching vanilla)")}", "INFO");
                if (leafSupported)
                    LogLaunchPreBuildDiagnostics(leafModPath, version);

                bool isModern = Version.TryParse(version, out var v) && v >= new Version(1, 17);
                if (isModern && leafSupported)
                {
                    try
                    {
                        if (_currentSettings.IsTestMode)
                        {
                            if (File.Exists(leafModPath)) File.Delete(leafModPath);
                            await BuildAndCopyLocalModAsync(version, leafModPath);
                        }
                        else
                        {
                            var manifest = await LeafApiService.GetModManifestAsync(version);
                            if (manifest != null && !string.IsNullOrWhiteSpace(manifest.Sha256) && !string.IsNullOrWhiteSpace(manifest.JarUrl))
                            {
                                _gameOutputWindow?.AppendLog($"[LaunchDiag] Using verified CDN manifest (v{manifest.Version}) for Leaf Core.", "INFO");
                                if (File.Exists(leafModPath)) File.Delete(leafModPath);
                                await LeafApiService.DownloadVerifiedModJarAsync(manifest, leafModPath);
                                LogJarFileInfo("leafModPath (post-download)", leafModPath);
                            }
                            else
                            {
                                _gameOutputWindow?.AppendLog($"LeafClient doesn't support {version} yet — launching vanilla.", "WARN");
                                leafSupported = false;
                            }
                        }
                    }
                    catch (InvalidOperationException integrityEx) when (integrityEx.Message == "Jar integrity check failed")
                    {
                        _gameOutputWindow?.AppendLog("[JarVerify] Integrity check FAILED — aborting launch.", "ERROR");
                        ShowProgress(false);
                        await ShowAccountActionErrorDialog("Could not verify client files. Please restart the launcher.");
                        return;
                    }
                    catch (Exception ex)
                    {
                        _gameOutputWindow?.AppendLog($"Failed to prepare core mod: {ex.Message}", "ERROR");
                        ShowProgress(false);
                        if (_currentSettings.IsTestMode)
                        {
                            await ShowAccountActionErrorDialog($"Developer test mode build failed — launch aborted.\n\n{ex.Message}");
                            return;
                        }
                        leafSupported = false;
                        _gameOutputWindow?.AppendLog($"[LaunchDiag] LeafClient jar unavailable for {version} — falling back to vanilla launch.", "WARN");
                    }

                    if (leafSupported && (!File.Exists(leafModPath) || new FileInfo(leafModPath).Length < 1024))
                    {
                        leafSupported = false;
                        _gameOutputWindow?.AppendLog($"[LaunchDiag] LeafClient jar not present on disk after prep for {version} — falling back to vanilla launch.", "WARN");
                    }

                    ShowProgress(false);
                }

                if (GetSelectedAddon(version).Equals("Fabric", StringComparison.OrdinalIgnoreCase))
                {
                    versionToLaunch = await EnsureFabricProfileAsync(version);

                    if (string.IsNullOrEmpty(versionToLaunch))
                    {
                        throw new Exception("Fabric installation failed.");
                    }

                    if (_currentSettings.IsOptiFineEnabled)
                        await InstallOptiFineForFabricIfNeededAsync(version, versionToLaunch, true);

                    // Apply the active profile's mod preset during launch
                    await ApplyModPresetForLaunchAsync(version, versionToLaunch);

                    // Install user-managed mods from settings
                    await InstallUserModsAsync(version);

                    ShowProgress(false);
                }

                SyncLauncherManagedMods(version);

                // Belt-and-suspenders: actively delete any *leaf*.jar left in the mods folder
                // just before launch. The Leaf core mod is injected via -Dfabric.addMods, so
                // any file matching leafclient*.jar in mods/ is ALWAYS stale and would cause
                // Fabric to load two mods with id 'leafclient' and pick one non-deterministically.
                try
                {
                    string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
                    if (Directory.Exists(modsFolder))
                    {
                        var rogueLeafJars = Directory.GetFiles(modsFolder, "*.jar", SearchOption.TopDirectoryOnly)
                            .Concat(Directory.GetFiles(modsFolder, "*.jar.disabled", SearchOption.TopDirectoryOnly))
                            .Where(p =>
                            {
                                var n = System.IO.Path.GetFileName(p);
                                return n.StartsWith("leafclient", StringComparison.OrdinalIgnoreCase) ||
                                       n.StartsWith("leaf-", StringComparison.OrdinalIgnoreCase) ||
                                       n.Equals("leaf.jar", StringComparison.OrdinalIgnoreCase);
                            })
                            .ToList();
                        foreach (var rogue in rogueLeafJars)
                        {
                            try
                            {
                                File.Delete(rogue);
                                _gameOutputWindow?.AppendLog(
                                    $"[LaunchDiag] Deleted rogue leaf jar from mods folder: {System.IO.Path.GetFileName(rogue)}",
                                    "WARN");
                            }
                            catch (Exception ex)
                            {
                                _gameOutputWindow?.AppendLog(
                                    $"[LaunchDiag] FAILED to delete rogue leaf jar '{System.IO.Path.GetFileName(rogue)}': {ex.Message}",
                                    "ERROR");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _gameOutputWindow?.AppendLog($"[LaunchDiag] Rogue leaf jar sweep failed: {ex.Message}", "ERROR");
                }

                if (leafSupported)
                    LogLaunchPreLaunchDiagnostics(leafModPath);

                var jvmArguments = new List<MArgument>
        {
            new($"-Dleaf.testmode={(_currentSettings.IsTestMode ? "true" : "false")}"),
            new("-XX:+UseG1GC"),
            new("-XX:+UnlockExperimentalVMOptions"),
            new("-XX:G1NewSizePercent=20"),
            new("-XX:G1ReservePercent=20"),
            new("-XX:MaxGCPauseMillis=50"),
            new("-XX:G1HeapRegionSize=32M"),
            new("-XX:+DisableExplicitGC"),
            new("-XX:+AlwaysPreTouch"),
            new("-XX:+ParallelRefProcEnabled"),
        };

                if (leafSupported)
                {
                    jvmArguments.Insert(0, new MArgument($"-Dfabric.addMods={leafModPath}"));
                    jvmArguments.Insert(0, new MArgument("-Dleaf.version=1.5.0"));
                    jvmArguments.Insert(0, new MArgument("-Dleaf.client=true"));
                }

                // Combine global + active-profile JVM argument overrides so per-profile
                // tuning (e.g. a "benchmark" profile with aggressive GC flags) actually
                // takes effect at launch.
                var effectiveExtraJvmArgs = GetEffectiveExtraJvmArgs();
                if (!string.IsNullOrWhiteSpace(effectiveExtraJvmArgs))
                {
                    var customArgs = effectiveExtraJvmArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var arg in customArgs)
                        jvmArguments.Add(new MArgument(arg));
                }

                // Log the final JVM arguments list so we can see exactly what Fabric will receive.
                try
                {
                    _gameOutputWindow?.AppendLog($"[LaunchDiag] ===== FINAL JVM ARGS ({jvmArguments.Count}) =====", "INFO");
                    for (int i = 0; i < jvmArguments.Count; i++)
                    {
                        string? s = jvmArguments[i]?.ToString();
                        _gameOutputWindow?.AppendLog($"[LaunchDiag]   [{i}] {s}", "INFO");
                    }
                    _gameOutputWindow?.AppendLog("[LaunchDiag] ===== END FINAL JVM ARGS =====", "INFO");
                }
                catch (Exception ex)
                {
                    _gameOutputWindow?.AppendLog($"[LaunchDiag] Failed to dump JVM args: {ex.GetType().Name}: {ex.Message}", "ERROR");
                }

                // Resolve profile-aware launch values. An active profile can override
                // RAM allocation, screen resolution, and quick-join server.
                var (resUseCustom, resWidth, resHeight) = GetEffectiveResolution();
                var (qjAddress, qjPort) = GetEffectiveQuickJoin();

                var launchOption = new MLaunchOption
                {
                    Session = _session,
                    MaximumRamMb = GetEffectiveMaxRamMb(),
                    MinimumRamMb = GetEffectiveMinRamMb(),
                    JvmArgumentOverrides = jvmArguments,
                    ScreenHeight = resUseCustom ? resHeight : 0,
                    ScreenWidth  = resUseCustom ? resWidth  : 0,
                    ServerIp     = qjAddress,
                    ServerPort   = qjPort,
                };

                UpdateLaunchButton("LAUNCHING...", "Purple");
                ShowProgress(false);

                var process = await _launcher.CreateProcessAsync(versionToLaunch, launchOption);

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.EnableRaisingEvents = true;
                process.Exited += OnGameExited;

                var launchVersionInfo = _allVersions.FirstOrDefault(v => v.FullVersion == _currentSettings.SelectedSubVersion);
                _leafModExpected = leafSupported && launchVersionInfo?.IsLeafClientModSupported == true;
                _leafModLoaded = false;
                _lastLaunchVersion = _currentSettings.SelectedSubVersion;

                process.OutputDataReceived += (s, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;
                    _gameOutputWindow?.AppendLog(e.Data, "INFO");
                    // Detect Leaf Client mod loaded — Fabric logs mod initialization
                    if (_leafModExpected && !_leafModLoaded &&
                        (e.Data.Contains("leafclient", StringComparison.OrdinalIgnoreCase) &&
                         (e.Data.Contains("loaded", StringComparison.OrdinalIgnoreCase) ||
                          e.Data.Contains("initialized", StringComparison.OrdinalIgnoreCase) ||
                          e.Data.Contains("onInitialize", StringComparison.OrdinalIgnoreCase))))
                    {
                        _leafModLoaded = true;
                        Console.WriteLine("[Launch] Leaf Client mod successfully loaded.");
                    }
                };
                process.ErrorDataReceived += (s, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;
                    _gameOutputWindow?.AppendLog(e.Data, "ERROR");
                    // Also check error stream for mod load confirmation
                    if (_leafModExpected && !_leafModLoaded &&
                        (e.Data.Contains("leafclient", StringComparison.OrdinalIgnoreCase) &&
                         (e.Data.Contains("loaded", StringComparison.OrdinalIgnoreCase) ||
                          e.Data.Contains("initialized", StringComparison.OrdinalIgnoreCase) ||
                          e.Data.Contains("onInitialize", StringComparison.OrdinalIgnoreCase))))
                    {
                        _leafModLoaded = true;
                        Console.WriteLine("[Launch] Leaf Client mod successfully loaded (from stderr).");
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                _gameProcess = process;

                // Playtime tracking: record launch time, increment counter, persist.
                // Also increment the active profile's own LaunchCount / LastUsed so the
                // profile card can show "last used 3 days ago" / total launches.
                _sessionStartUtc = DateTime.UtcNow;
                if (_currentSettings != null)
                {
                    _currentSettings.LastLaunchTime  = DateTime.Now;
                    _currentSettings.TotalLaunchCount++;

                    var mcVer = _currentSettings.SelectedSubVersion;
                    if (!string.IsNullOrEmpty(mcVer))
                    {
                        _currentSettings.LaunchCountByVersion.TryGetValue(mcVer, out int lc);
                        _currentSettings.LaunchCountByVersion[mcVer] = lc + 1;
                    }

                    var activeProfile = GetActiveProfile();
                    if (activeProfile != null)
                    {
                        activeProfile.LaunchCount++;
                        activeProfile.LastUsed = DateTime.Now;
                    }

                    try { _ = _settingsService.SaveSettingsAsync(_currentSettings); } catch { }
                }

                // Keep animation visible briefly so the user sees "LAUNCHING MINECRAFT", then fade out
                _ = Task.Delay(1200).ContinueWith(_ => Dispatcher.UIThread.Post(HideLaunchAnimation));

                _isLaunching = false;
                UpdateLaunchButton("PLAYING ON LEAF CLIENT", "DeepSkyBlue");
                await UpdateServerButtonStates();

                if (_currentSettings.LauncherVisibilityOnGameLaunch == LauncherVisibility.Hide)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        MinimizeToTray();
                        NotificationWindow.Show("Game Starting", "Launcher hidden while game runs", "Restore", () => RestoreFromTray());
                    });
                }
            }
            catch (OperationCanceledException)
            {
                // User cancelled the launch — do not show LAUNCH FAILED
                HideLaunchAnimation();
                _isLaunching = false;
                ShowProgress(false);
                UpdateLaunchButton("LAUNCH GAME", "SeaGreen");
                await UpdateServerButtonStates();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Launch Error: {ex}");
                HideLaunchAnimation();
                UpdateLaunchButton("LAUNCH FAILED", "Red");
                ShowLaunchErrorBanner(ex.Message);
                _isLaunching = false;
                ShowProgress(false);
                await UpdateServerButtonStates();

                _ = Task.Delay(3000).ContinueWith(_ => Dispatcher.UIThread.Post(() => UpdateLaunchButton("LAUNCH GAME", "SeaGreen")));
            }
            finally
            {
                _launchCancellationTokenSource?.Dispose();
                _launchCancellationTokenSource = null;
            }
        }


        private async Task ManageOptiFineForFabricMods(string mcVersion, bool enable)
        {
            string[] optifineModNames = new[] {
                "optifine", "optifabric", "modmenu", "sodium", "iris", "lithium", "starlight"
            };

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

            // REMOVED: The block that was disabling leafclient when OptiFine was disabled.
            /* 
            var leafClientMod = _currentSettings.InstalledMods.FirstOrDefault(m => m.ModId == "leafclient" && m.MinecraftVersion == mcVersion);
            if (leafClientMod != null && leafClientMod.Enabled != enable)
            {
                leafClientMod.Enabled = enable; 
                settingsChanged = true;
                Console.WriteLine($"[OptiFineForFabric] Setting 'Leaf Client Runtime' to enabled={enable} in settings.");
            }
            */

            if (settingsChanged)
            {
                await _settingsService.SaveSettingsAsync(_currentSettings);
            }
        }

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

            // Playtime tracking: compute session length & persist
            // (done first so it works even if the rest of the handler bails)
            try
            {
                if (_sessionStartUtc.HasValue && _currentSettings != null)
                {
                    long sessionSeconds = (long)(DateTime.UtcNow - _sessionStartUtc.Value).TotalSeconds;
                    if (sessionSeconds < 0) sessionSeconds = 0;
                    if (sessionSeconds > 86400) sessionSeconds = 86400; // clamp at 24h

                    _currentSettings.CurrentSessionSeconds = sessionSeconds;
                    _currentSettings.TotalPlaytimeSeconds += sessionSeconds;
                    _currentSettings.LastExitTime = DateTime.Now;

                    // Per-version tracking
                    var mcVersion = _currentSettings.SelectedSubVersion;
                    if (!string.IsNullOrEmpty(mcVersion))
                    {
                        _currentSettings.PlaytimeByVersion.TryGetValue(mcVersion, out long cur);
                        _currentSettings.PlaytimeByVersion[mcVersion] = cur + sessionSeconds;
                    }

                    // Per-server tracking (if quick-join was used)
                    var server = _currentSettings.QuickJoinServerAddress;
                    if (!string.IsNullOrEmpty(server))
                    {
                        _currentSettings.PlaytimeByServer.TryGetValue(server, out long curS);
                        _currentSettings.PlaytimeByServer[server] = curS + sessionSeconds;
                    }

                    // Per-profile tracking — accumulate time against whichever profile
                    // was active when the game launched.
                    var activeProfile = GetActiveProfile();
                    if (activeProfile != null)
                    {
                        activeProfile.PlaytimeSeconds += sessionSeconds;
                    }

                    _sessionStartUtc = null;
                    _ = _settingsService.SaveSettingsAsync(_currentSettings);
                    Console.WriteLine($"[Playtime] Session: {sessionSeconds}s, total: {_currentSettings.TotalPlaytimeSeconds}s");

                    // Refresh the footer stats strip now — it's visible on every page
                    // so we want it updated immediately after the game exits.
                    Dispatcher.UIThread.Post(RefreshPlaytimeStatsCard);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Playtime] Failed to record session: {ex.Message}");
            }

            Dispatcher.UIThread.Post(async () =>
            {
                if (_isShuttingDown) return;
                string logoutSignalPath = System.IO.Path.Combine(_minecraftFolder, "logout.signal");
                if (System.IO.File.Exists(logoutSignalPath))
                {
                    try
                    {
                        System.IO.File.Delete(logoutSignalPath);
                        Console.WriteLine("[Launcher] Logout signal detected from game. Performing logout...");

                        LogoutButton_Click(null, new RoutedEventArgs());
                        return;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[Launcher] Error processing logout signal: {ex.Message}");
                    }
                }


                int exitCode = _gameProcess?.ExitCode ?? -1;
                _gameOutputWindow?.AppendLog($"Game exited with code {exitCode}.", exitCode == 0 ? "INFO" : "ERROR");

                // Auto-restart: if Leaf Client mod was expected but didn't load, and we haven't
                // already tried restarting, kill and re-launch automatically.
                if (exitCode != 0 && _leafModExpected && !_leafModLoaded && !_autoRestartAttempted && !_userTerminatedGame)
                {
                    Console.WriteLine("[Launch] Leaf Client mod failed to load. Auto-restarting...");
                    _gameOutputWindow?.AppendLog("Leaf Client mod did not load. Auto-restarting game...", "WARN");
                    _autoRestartAttempted = true;
                    _gameProcess = null;
                    _isLaunching = false;

                    // Brief delay then re-launch
                    await Task.Delay(1500);
                    UpdateLaunchButton("RESTARTING...", "Orange");
                    _ = LaunchGameAsync(_lastLaunchVersion ?? _currentSettings.SelectedSubVersion, true);
                    return;
                }
                // Reset the auto-restart flag for next launch
                _autoRestartAttempted = false;

                if (exitCode != 0)
                {
                    Console.Error.WriteLine($"[Launcher] Game process exited with error code: {exitCode}");

                    if (!_userTerminatedGame)
                    {
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
                }
                else
                {
                    // Check if game exited cleanly but Leaf mod never loaded — auto-restart once
                    if (_leafModExpected && !_leafModLoaded && !_autoRestartAttempted && !_userTerminatedGame)
                    {
                        Console.WriteLine("[Launch] Game exited cleanly but Leaf Client mod was not detected. Auto-restarting...");
                        _gameOutputWindow?.AppendLog("Leaf Client mod was not detected. Auto-restarting game...", "WARN");
                        _autoRestartAttempted = true;
                        _gameProcess = null;
                        _isLaunching = false;

                        await Task.Delay(1500);
                        UpdateLaunchButton("RESTARTING...", "Orange");
                        _ = LaunchGameAsync(_lastLaunchVersion ?? _currentSettings.SelectedSubVersion, true);
                        return;
                    }
                    _autoRestartAttempted = false;

                    UpdateLaunchButton("LAUNCH GAME", "SeaGreen");
                    HideLaunchFailureBanner();

                    if (_currentSettings.LauncherVisibilityOnGameLaunch == LauncherVisibility.Hide)
                    {
                        Console.WriteLine("[Launch] Game closed - restoring launcher from tray");
                        RestoreFromTray();
                    }
                }

                _ = Task.Run(async () =>
                {
                    try { await ReportSessionPlaytimeAsync(); } catch (Exception ex) { Console.WriteLine($"[PlaytimeReport] {ex.Message}"); }
                });

                _gameProcess = null;
                _isLaunching = false;
                _isInstalling = false;
                _gameStartingBannerShownForCurrentLaunch = false;

                await UpdateServerButtonStates();
                HideGameStartingBanner();
                HideLaunchAnimation();

                if (_currentSettings.LockGameAspectRatio)
                {
                    try
                    {
                        await Task.Delay(1000); 

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

                                _currentSettings.GameResolutionWidth = width.Value;
                                _currentSettings.GameResolutionHeight = height.Value;
                                await _settingsService.SaveSettingsAsync(_currentSettings);

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

                if (_currentSettings.LauncherVisibilityOnGameLaunch == LauncherVisibility.Hide)
                {
                    Console.WriteLine("[Launch] Game closed - restoring launcher from tray");
                    RestoreFromTray();
                }
            });
        }


        private int GetMaxRam()
        {
            if (_maxRamSlider != null)
                return (int)_maxRamSlider.Value;
            return 4096;
        }

        private int GetMinRam()
        {
            if (_minRamSlider != null)
                return (int)_minRamSlider.Value;
            return 1024;
        }

        // ─────────────────────────────────────────────────────────────────
        // PROFILE RESOLUTION HELPERS
        //
        // These read from the active profile first, then fall back to global
        // LauncherSettings. The idea is that profile fields like AllocatedMemoryGb,
        // JvmArgumentsOverride, GameResolutionWidthOverride, etc. ACTUALLY
        // take effect at launch time rather than being dead data.
        // ─────────────────────────────────────────────────────────────────

        private Models.LauncherProfile? GetActiveProfile()
        {
            if (_currentSettings?.Profiles == null || _currentSettings.Profiles.Count == 0)
                return null;
            return _currentSettings.Profiles.FirstOrDefault(p => p.Id == _currentSettings.ActiveProfileId)
                ?? _currentSettings.Profiles[0];
        }

        /// <summary>
        /// Returns the effective max-RAM in MB for launch. Prefers the active profile's
        /// <see cref="Models.LauncherProfile.AllocatedMemoryGb"/>, falling back to the slider/global setting.
        /// </summary>
        private int GetEffectiveMaxRamMb()
        {
            var profile = GetActiveProfile();
            if (profile != null && profile.AllocatedMemoryGb > 0)
                return (int)Math.Round(profile.AllocatedMemoryGb * 1024.0);
            return GetMaxRam();
        }

        private int GetEffectiveMinRamMb()
        {
            // Min RAM stays global — keep it simple.
            return GetMinRam();
        }

        /// <summary>
        /// Combines global JVM args with the active profile's override (appended after globals).
        /// Returns a whitespace-separated string, possibly empty.
        /// </summary>
        private string GetEffectiveExtraJvmArgs()
        {
            var global = _currentSettings?.JvmArguments ?? "";
            var profile = GetActiveProfile();
            var overrideArgs = profile?.JvmArgumentsOverride ?? "";
            if (string.IsNullOrWhiteSpace(global))   return overrideArgs.Trim();
            if (string.IsNullOrWhiteSpace(overrideArgs)) return global.Trim();
            return (global.Trim() + " " + overrideArgs.Trim()).Trim();
        }

        private (bool useCustom, int width, int height) GetEffectiveResolution()
        {
            var profile = GetActiveProfile();
            if (profile?.UseCustomResolutionOverride == true)
            {
                return (
                    true,
                    profile.GameResolutionWidthOverride  ?? _currentSettings.GameResolutionWidth,
                    profile.GameResolutionHeightOverride ?? _currentSettings.GameResolutionHeight
                );
            }
            return (
                _currentSettings?.UseCustomGameResolution ?? false,
                _currentSettings?.GameResolutionWidth  ?? 1280,
                _currentSettings?.GameResolutionHeight ?? 720
            );
        }

        private (string? address, int port) GetEffectiveQuickJoin()
        {
            var profile = GetActiveProfile();
            if (profile != null && !string.IsNullOrWhiteSpace(profile.QuickJoinServerAddressOverride))
            {
                int.TryParse(profile.QuickJoinServerPortOverride ?? "25565", out int pOv);
                if (pOv <= 0) pOv = 25565;
                return (profile.QuickJoinServerAddressOverride, pOv);
            }

            if (_currentSettings?.QuickLaunchEnabled == true && !string.IsNullOrWhiteSpace(_currentSettings.QuickJoinServerAddress))
            {
                int.TryParse(_currentSettings.QuickJoinServerPort ?? "25565", out int p);
                if (p <= 0) p = 25565;
                return (_currentSettings.QuickJoinServerAddress, p);
            }

            return (null, 25565);
        }

        private Color DarkenColor(Color color, float factor)
        {
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

            if (text == "LAUNCH GAME" || text.StartsWith("LAUNCH FAILED") || text == "SELECT VERSION" || text == "LOGIN REQUIRED" || text == "LAUNCH CANCELLED" || text == "YOU'RE OFFLINE" || text == "FABRIC INSTALL FAILED")
            {
                _isLaunching = false;
                _isInstalling = false;
            }

            ApplyLaunchButtonState(); 
        }

        private void ApplyLaunchButtonState()
        {
            if (this.FindControl<Button>("LaunchGameButton") is { } btn)
            {
                string displayText;
                string colorName;
                bool isButtonEnabled = true;
                bool isGameRunning = false;

                if (_cancelLaunchButton != null)
                {
                    _cancelLaunchButton.IsVisible = (_isLaunching || _isInstalling) && (_gameProcess == null || _gameProcess.HasExited);
                }

                if (_gameProcess != null && !_gameProcess.HasExited)
                {
                    displayText = "PLAYING ON LEAF CLIENT";
                    colorName = "DeepSkyBlue";
                    isButtonEnabled = true;
                    isGameRunning = true;

                    HideLaunchFailureBanner();
                }
                else if (_isLaunching || _isInstalling)
                {
                    displayText = _currentOperationText;
                    colorName = _currentOperationColor;
                    isButtonEnabled = false;
                    HideLaunchFailureBanner();
                }
                else if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                {
                    displayText = "YOU'RE OFFLINE";
                    colorName = "Gray";
                    isButtonEnabled = false;
                    HideLaunchFailureBanner();
                }
                else if (_currentOperationText == "LAUNCH CANCELLED" ||
                         _currentOperationText == "SELECT VERSION" ||
                         _currentOperationText == "LOGIN REQUIRED" ||
                         _currentOperationText == "FABRIC INSTALL FAILED" ||
                         _currentOperationText.StartsWith("LAUNCH FAILED"))
                {
                    displayText = _currentOperationText;
                    colorName = _currentOperationColor;
                    isButtonEnabled = true;

                    if (_currentOperationText.StartsWith("LAUNCH FAILED")) { }
                    else { HideLaunchFailureBanner(); }
                }
                else
                {
                    displayText = "LAUNCH GAME";
                    colorName = "SeaGreen";
                    isButtonEnabled = true;
                    HideLaunchFailureBanner();
                }

                if (_gameWindowPendingText != null)
                {
                    _gameWindowPendingText.IsVisible = isGameRunning;
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
                _launchButtonGlowColor = baseColor;
                btn.Effect = BuildLaunchGlow(baseColor, _launchButtonHovered);

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

        private static DropShadowEffect BuildLaunchGlow(Color baseColor, bool hovered)
        {
            byte hoverAlpha = 0xAA;
            byte idleAlpha = 0x2A;
            var glow = Color.FromArgb(hovered ? hoverAlpha : idleAlpha, baseColor.R, baseColor.G, baseColor.B);
            return new DropShadowEffect
            {
                BlurRadius = hovered ? 32 : 8,
                Color = glow,
                OffsetX = 0,
                OffsetY = hovered ? 8 : 0,
                Opacity = 1.0
            };
        }

        private static void WriteSessionJson(string? jwt)
        {
            try
            {
                var dir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "LeafClient");
                Directory.CreateDirectory(dir);
                var path = System.IO.Path.Combine(dir, "session.json");
                var json = $"{{\"jwt\":\"{jwt ?? ""}\"}}";

                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Session] Failed to write session.json: {ex.Message}");
            }
        }

        private string? CurrentAccountSub()
        {
            return DecodeJwtSub(_currentSettings?.LeafApiJwt);
        }

        private static void WriteEquippedJson(EquippedCosmetics? eq, string? sub)
        {
            try
            {
                var dir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "LeafClient");
                Directory.CreateDirectory(dir);
                var path = System.IO.Path.Combine(dir, "equipped.json");
                var snapshot = new EquippedCosmetics
                {
                    Sub = sub,
                    CapeId = eq?.CapeId,
                    HatId = eq?.HatId,
                    WingsId = eq?.WingsId,
                    BackItemId = eq?.BackItemId,
                    AuraId = eq?.AuraId
                };
                var json = System.Text.Json.JsonSerializer.Serialize(snapshot, JsonContext.Default.EquippedCosmetics);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Equipped] Failed to write equipped.json: {ex.Message}");
            }
        }

        private static void WriteEquippedJson(EquippedCosmetics? eq)
        {
            WriteEquippedJson(eq, eq?.Sub);
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
        private void OnSidebarButtonPointerEntered(object? sender, PointerEventArgs e)
        {
            if (sender is not Border b) return;

            // If this is the currently-selected button, scale the green selection
            // indicator to match the button's hover scale so they grow together.
            // Button scales to 1.08; indicator scales to 1.10 for a 0.5px-per-side
            // safety overshoot to fully cover the button under sub-pixel rounding
            // (avoids a visible dark sliver along the right/bottom edges).
            if (_selectionIndicator != null
                && b.Tag is string tagStr
                && int.TryParse(tagStr, out int tagIdx)
                && tagIdx == _currentSelectedIndex)
            {
                _selectionIndicator.RenderTransform =
                    Avalonia.Media.Transformation.TransformOperations.Parse("scale(1.10)");
            }

            _tooltipHideCts?.Cancel();

            if (_sidebarHoverTooltipText != null)
                _sidebarHoverTooltipText.Text = b.Tag switch
                {
                    "0" => "LAUNCH",
                    "1" => "PROFILES",
                    "2" => "SERVERS",
                    "3" => "CONTENT",
                    "6" => "COSMETICS",
                    "7" => "STORE",
                    "8" => "SCREENSHOTS",
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
                _sidebarHoverTooltip.Transitions = new Transitions(); 
                PositionTooltipNextTo(b);                              
                _sidebarHoverTooltip.IsVisible = true;                 
                _sidebarHoverTooltip.Opacity = 1;                      

                var restore = new Transitions();
                if (_savedTooltipTransitions != null)
                {
                    foreach (var t in _savedTooltipTransitions)
                        restore.Add(t);
                }
                _sidebarHoverTooltip.Transitions = restore;

                _tooltipHasShown = true;
                return; 
            }

            PositionTooltipNextTo(b);
            _sidebarHoverTooltip.IsVisible = true;
            _sidebarHoverTooltip.Opacity = 1;
        }

        private void OnSidebarButtonPointerExited(object? sender, PointerEventArgs e)
        {
            // Reset the selection indicator scale if we're leaving the currently-selected button.
            if (sender is Border b
                && _selectionIndicator != null
                && b.Tag is string tagStr
                && int.TryParse(tagStr, out int tagIdx)
                && tagIdx == _currentSelectedIndex)
            {
                _selectionIndicator.RenderTransform =
                    Avalonia.Media.Transformation.TransformOperations.Parse("scale(1.0)");
            }

            _tooltipHideCts?.Cancel();
            _tooltipHideCts = new CancellationTokenSource();
            var ct = _tooltipHideCts.Token;

            if (!AreAnimationsEnabled()) 
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
                    await Task.Delay(140, ct); 
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
                6 => "Cosmetics",
                7 => "Store",
                _ => "Game"
            };

            string detailsTop;
            if (_loggedIn && !string.IsNullOrWhiteSpace(_currentUsername))
            {
                if (_currentSettings.ShowUsernameInDiscordRichPresence)
                {
                    detailsTop = $"Playing as {_currentUsername}";
                }
                else
                {
                    string maskedUsername = MaskUsername(_currentUsername);
                    detailsTop = $"Playing as {maskedUsername}";
                }
            }
            else
            {
                detailsTop = "Leaf Client";
            }

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

        private void InitializeSkinsControls()
        {
            _skinsPage = this.FindControl<Grid>("SkinsPage");
            _skinsWrapPanel = this.FindControl<WrapPanel>("SkinsWrapPanel");
            _noSkinsMessage = this.FindControl<Border>("NoSkinsMessage");
        }

        // ─────────────────────────────────────────────────────────────────────────
        // COSMETICS PAGE (extracted to Views/Pages/CosmeticsPageView)
        // ─────────────────────────────────────────────────────────────────────────

        // ── Store page (extracted to UserControl) ──
        private LeafClient.Views.Pages.StorePageView? _storePage;


        /// <summary>
        /// Extracts the skin texture URL from Mojang's session server profile response.
        /// The response has a "properties" array with a base64-encoded "textures" value.
        /// </summary>
        private static string? ExtractSkinUrlFromProfile(string json)
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var properties = doc.RootElement.GetProperty("properties");
                foreach (var prop in properties.EnumerateArray())
                {
                    if (prop.GetProperty("name").GetString() == "textures")
                    {
                        string base64 = prop.GetProperty("value").GetString() ?? "";
                        string decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                        using var texDoc = System.Text.Json.JsonDocument.Parse(decoded);
                        return texDoc.RootElement
                            .GetProperty("textures")
                            .GetProperty("SKIN")
                            .GetProperty("url")
                            .GetString();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cosmetics] Failed to parse Mojang profile: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Extracts the skin texture URL from Ashcon API response.
        /// JSON path: textures.skin.url
        /// </summary>
        private static string? ExtractSkinUrlFromAshcon(string json)
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                return doc.RootElement
                    .GetProperty("textures")
                    .GetProperty("skin")
                    .GetProperty("url")
                    .GetString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cosmetics] Failed to parse Ashcon response: {ex.Message}");
            }
            return null;
        }

        private bool IsCosmeticEquipped(string cosId, string category)
        {
            if (_currentSettings?.Equipped == null) return false;
            return category switch
            {
                "capes" => _currentSettings.Equipped.CapeId == cosId,
                "hats"  => _currentSettings.Equipped.HatId == cosId,
                "wings" => _currentSettings.Equipped.WingsId == cosId,
                "auras" => _currentSettings.Equipped.AuraId == cosId,
                _       => false
            };
        }

        private void MigrateActiveAccountEntry()
        {
            if (_currentSettings == null) return;
            var entry = _currentSettings.SavedAccounts.FirstOrDefault(a => a.Id == _currentSettings.ActiveAccountId);
            if (entry == null) return;

            var expectedId = ExpectedJwtIdentifier(entry);

            if (!string.IsNullOrEmpty(entry.LeafApiJwt))
            {
                var jwtId = GetJwtMinecraftUsername(entry.LeafApiJwt);
                if (!string.IsNullOrEmpty(jwtId) && !string.Equals(jwtId, expectedId, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[Migrate] Cleared mismatched JWT from AccountEntry (expected={expectedId}, got={jwtId})");
                    entry.LeafApiJwt = null;
                    entry.LeafApiRefreshToken = null;
                    entry.OwnedCosmeticIds = new List<string>();
                    _ = _settingsService?.SaveSettingsAsync(_currentSettings);
                }
            }

            if (string.IsNullOrEmpty(entry.LeafApiJwt) && !string.IsNullOrEmpty(_currentSettings.LeafApiJwt))
            {
                var jwtId = GetJwtMinecraftUsername(_currentSettings.LeafApiJwt);
                if (!string.IsNullOrEmpty(jwtId) && string.Equals(jwtId, expectedId, StringComparison.OrdinalIgnoreCase))
                {
                    entry.LeafApiJwt = _currentSettings.LeafApiJwt;
                    entry.LeafApiRefreshToken = _currentSettings.LeafApiRefreshToken;
                    _ = _settingsService?.SaveSettingsAsync(_currentSettings);
                    Console.WriteLine("[Migrate] Copied matching LeafApiJwt into active AccountEntry.");
                }
            }

            if (entry.OwnedCosmeticIds.Count == 0 && !string.IsNullOrEmpty(entry.LeafApiJwt) && _ownedCosmeticIds.Count > 0)
            {
                entry.OwnedCosmeticIds = new List<string>(_ownedCosmeticIds);
            }
        }

        private static string ExpectedJwtIdentifier(AccountEntry entry)
        {
            if (entry.AccountType == "microsoft")
            {
                var clean = (entry.Uuid ?? "").Replace("-", "").ToLowerInvariant();
                if (clean.Length == 32)
                    return $"{clean[..8]}-{clean[8..12]}-{clean[12..16]}-{clean[16..20]}-{clean[20..]}";
                return entry.Uuid?.ToLowerInvariant() ?? "";
            }
            return entry.Username?.ToLowerInvariant() ?? "";
        }

        private static string? GetJwtMinecraftUsername(string jwt)
        {
            try
            {
                var parts = jwt.Split('.');
                if (parts.Length < 2) return null;
                var payload = parts[1];
                var padded = (payload.Length % 4) switch
                {
                    2 => payload + "==",
                    3 => payload + "=",
                    _ => payload
                };
                var bytes = Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/'));
                var json = System.Text.Encoding.UTF8.GetString(bytes);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("minecraft_username", out var prop))
                    return prop.GetString();
                return null;
            }
            catch
            {
                return null;
            }
        }

        private void LoadOwnedJson()
        {
            try
            {
                if (!System.IO.File.Exists(OwnedJsonPath)) return;
                var json = System.IO.File.ReadAllText(OwnedJsonPath);
                var currentSub = CurrentAccountSub();
                List<string>? ids = null;
                try
                {
                    var file = System.Text.Json.JsonSerializer.Deserialize(json, JsonContext.Default.OwnedCosmeticsFile);
                    if (file != null)
                    {
                        if (!string.IsNullOrEmpty(currentSub) && string.Equals(file.Sub, currentSub, StringComparison.Ordinal))
                            ids = file.Ids;
                        else
                            Console.WriteLine($"[Owned] sub mismatch (file='{file.Sub}', current='{currentSub}') — ignoring owned.json.");
                    }
                }
                catch
                {
                    try { ids = System.Text.Json.JsonSerializer.Deserialize(json, JsonContext.Default.ListString); }
                    catch { }
                    if (ids != null)
                        Console.WriteLine("[Owned] Migrated legacy List<string> owned.json — will re-stamp on next save.");
                }
                if (ids != null)
                    foreach (var id in ids)
                        _ownedCosmeticIds.Add(id);
                Console.WriteLine($"[Owned] Loaded {_ownedCosmeticIds.Count} owned cosmetics.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Owned] Failed to load owned.json: {ex.Message}");
            }
        }

        private void SaveOwnedJson()
        {
            try
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(OwnedJsonPath)!);
                var file = new OwnedCosmeticsFile
                {
                    Sub = CurrentAccountSub(),
                    Ids = new List<string>(_ownedCosmeticIds)
                };
                var json = System.Text.Json.JsonSerializer.Serialize(file, JsonContext.Default.OwnedCosmeticsFile);
                System.IO.File.WriteAllText(OwnedJsonPath, json);
                Console.WriteLine("[Owned] owned.json saved.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Owned] Failed to save owned.json: {ex.Message}");
            }
        }

        private void WipeCosmeticFilesIfStale()
        {
            try
            {
                var currentSub = CurrentAccountSub();
                var equippedPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "LeafClient", "equipped.json");
                if (System.IO.File.Exists(equippedPath))
                {
                    try
                    {
                        var raw = System.IO.File.ReadAllText(equippedPath);
                        var parsed = System.Text.Json.JsonSerializer.Deserialize(raw, JsonContext.Default.EquippedCosmetics);
                        var fileSub = parsed?.Sub;
                        if (string.IsNullOrEmpty(currentSub) || !string.Equals(fileSub, currentSub, StringComparison.Ordinal))
                        {
                            WriteEquippedJson(new EquippedCosmetics { Sub = currentSub });
                            Console.WriteLine($"[Cosmetics] Wiped stale equipped.json (file sub='{fileSub}', current='{currentSub}').");
                        }
                    }
                    catch
                    {
                        WriteEquippedJson(new EquippedCosmetics { Sub = currentSub });
                    }
                }
                if (System.IO.File.Exists(OwnedJsonPath))
                {
                    try
                    {
                        var raw = System.IO.File.ReadAllText(OwnedJsonPath);
                        string? fileSub = null;
                        try
                        {
                            var parsed = System.Text.Json.JsonSerializer.Deserialize(raw, JsonContext.Default.OwnedCosmeticsFile);
                            fileSub = parsed?.Sub;
                        }
                        catch { }
                        if (string.IsNullOrEmpty(currentSub) || !string.Equals(fileSub, currentSub, StringComparison.Ordinal))
                        {
                            _ownedCosmeticIds.Clear();
                            SaveOwnedJson();
                            Console.WriteLine($"[Cosmetics] Wiped stale owned.json (file sub='{fileSub}', current='{currentSub}').");
                        }
                    }
                    catch
                    {
                        _ownedCosmeticIds.Clear();
                        SaveOwnedJson();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cosmetics] WipeCosmeticFilesIfStale failed: {ex.Message}");
            }
        }

        // Clear any equipped cosmetics that are not in the owned set (e.g. from a previous session)
        private async void ValidateEquippedCosmetics()
        {
            if (_currentSettings?.Equipped == null) return;
            var eq = _currentSettings.Equipped;
            bool changed = false;

            if (!string.IsNullOrEmpty(eq.CapeId)  && !_ownedCosmeticIds.Contains(eq.CapeId))  { eq.CapeId  = null; changed = true; }
            if (!string.IsNullOrEmpty(eq.HatId)   && !_ownedCosmeticIds.Contains(eq.HatId))   { eq.HatId   = null; changed = true; }
            if (!string.IsNullOrEmpty(eq.WingsId) && !_ownedCosmeticIds.Contains(eq.WingsId)) { eq.WingsId = null; changed = true; }
            if (!string.IsNullOrEmpty(eq.AuraId)  && !_ownedCosmeticIds.Contains(eq.AuraId))  { eq.AuraId  = null; changed = true; }

            if (changed)
            {
                Console.WriteLine("[Owned] Cleared equipped cosmetics that are not owned.");
                await _settingsService.SaveSettingsAsync(_currentSettings);
            }
        }

        // ── Synthesized sound engine ──────────────────────────────────────────
        // Generates Minecraft note-block style WAV in-memory (sine + harmonics,
        // near-instant attack, exponential decay) and plays it via winmm.dll.
        // No NuGet packages, no external files — generated once and cached.

        [System.Runtime.InteropServices.DllImport("winmm.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern bool PlaySound(string pszSound, IntPtr hmod, uint fdwSound);

        private const uint SND_FILENAME  = 0x00020000;
        private const uint SND_ASYNC     = 0x0001;
        private const uint SND_NODEFAULT = 0x0002;

        private static string? _celebWavPath;
        private static string? _revealWavPath;

        /// <summary>
        /// Synthesizes a note-block style WAV. Each note is a decaying bell timbre:
        /// fundamental + 2nd + 3rd harmonics with exponential decay envelope.
        /// </summary>
        private static byte[] GenerateNoteBlockWav(params (double freq, double durMs)[] notes)
        {
            const int rate = 44100;
            const double tau = 0.22; // decay time constant in seconds

            int totalSamples = 0;
            foreach (var (_, d) in notes)
                totalSamples += (int)(rate * d / 1000.0);
            totalSamples += rate / 20; // 50 ms silence tail

            var pcm = new short[totalSamples];
            int pos = 0;

            foreach (var (freq, durMs) in notes)
            {
                int count = (int)(rate * durMs / 1000.0);
                for (int i = 0; i < count && pos < totalSamples; i++, pos++)
                {
                    double t = (double)i / rate;
                    double attack = i < 4 ? i / 4.0 : 1.0; // 4-sample ramp (~0.09 ms)
                    double env = attack * Math.Exp(-t / tau);
                    double s = env * (
                        0.70 * Math.Sin(2 * Math.PI * freq * t) +
                        0.20 * Math.Sin(2 * Math.PI * freq * 2 * t) +
                        0.10 * Math.Sin(2 * Math.PI * freq * 3 * t));
                    pcm[pos] = (short)Math.Clamp(s * 28000.0, -32767, 32767);
                }
            }

            int dataBytes = totalSamples * 2;
            var wav = new byte[44 + dataBytes];
            void W4(int p, int v) => System.Array.Copy(BitConverter.GetBytes(v), 0, wav, p, 4);
            void W2(int p, short v) => System.Array.Copy(BitConverter.GetBytes(v), 0, wav, p, 2);
            void Ws(int p, string s) { for (int i = 0; i < s.Length; i++) wav[p + i] = (byte)s[i]; }
            Ws(0, "RIFF"); W4(4, 36 + dataBytes); Ws(8, "WAVE");
            Ws(12, "fmt "); W4(16, 16); W2(20, 1); W2(22, 1);
            W4(24, rate); W4(28, rate * 2); W2(32, 2); W2(34, 16);
            Ws(36, "data"); W4(40, dataBytes);
            for (int i = 0; i < totalSamples; i++)
            {
                wav[44 + i * 2]     = (byte)(pcm[i] & 0xFF);
                wav[44 + i * 2 + 1] = (byte)((pcm[i] >> 8) & 0xFF);
            }
            return wav;
        }

        private static string EnsureCelebWav()
        {
            if (_celebWavPath != null && System.IO.File.Exists(_celebWavPath)) return _celebWavPath;
            _celebWavPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "leafclient_sfx_celeb.wav");
            // C5 → E5 → G5 → C6  (ascending major chord arpeggio)
            System.IO.File.WriteAllBytes(_celebWavPath, GenerateNoteBlockWav(
                (523.25, 85.0), (659.25, 85.0), (783.99, 85.0), (1046.50, 480.0)));
            return _celebWavPath;
        }

        private static string EnsureRevealWav()
        {
            if (_revealWavPath != null && System.IO.File.Exists(_revealWavPath)) return _revealWavPath;
            _revealWavPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "leafclient_sfx_reveal.wav");
            // G5 → B5 → D6 → F#6  (brighter, shimmery reveal chord)
            System.IO.File.WriteAllBytes(_revealWavPath, GenerateNoteBlockWav(
                (783.99, 65.0), (987.77, 65.0), (1174.66, 65.0), (1479.98, 420.0)));
            return _revealWavPath;
        }

        private static void PlayCelebrationSound()
        {
            _ = System.Threading.Tasks.Task.Run(() =>
            {
                try { PlaySound(EnsureCelebWav(), IntPtr.Zero, SND_FILENAME | SND_ASYNC | SND_NODEFAULT); }
                catch { }
            });
        }

        private static void PlayRevealSound()
        {
            _ = System.Threading.Tasks.Task.Run(() =>
            {
                try { PlaySound(EnsureRevealWav(), IntPtr.Zero, SND_FILENAME | SND_ASYNC | SND_NODEFAULT); }
                catch { }
            });
        }

        private void ShowCelebrationPanel(string id, string name, string preview, string rarity)
        {
            var overlay = this.FindControl<Grid>("CelebrationOverlay");
            if (overlay == null) return;

            var (rarityMain, _, _) = rarity switch
            {
                "Legendary" => ("#F59E0B", "#92400E", "#1A1408"),
                "Epic"      => ("#A855F7", "#6B21A8", "#110C1A"),
                "Rare"      => ("#3B82F6", "#1E3A8A", "#0C1320"),
                _           => ("#6B7280", "#374151", "#0F1318")
            };

            var giftIcon        = this.FindControl<TextBlock>("CelebGiftIcon");
            var itemRendHost    = this.FindControl<Border>("CelebItemRendererHost");
            var itemName        = this.FindControl<TextBlock>("CelebItemName");
            var rarityBadge     = this.FindControl<Border>("CelebRarityBadge");
            var rarityText      = this.FindControl<TextBlock>("CelebRarityText");
            var glowRing        = this.FindControl<Ellipse>("CelebGlowRing");
            var glowRingOuter   = this.FindControl<Ellipse>("CelebGlowRingOuter");
            var glowFill        = this.FindControl<Ellipse>("CelebGlowFill");
            var unlockedBanner  = this.FindControl<Border>("CelebUnlockedBanner");

            if (itemName != null)
            {
                itemName.Text       = name;
                itemName.Foreground = SolidColorBrush.Parse(rarityMain);
            }
            if (rarityText != null)
            {
                rarityText.Text       = rarity.ToUpper();
                rarityText.Foreground = SolidColorBrush.Parse(rarityMain);
            }
            if (rarityBadge != null)
            {
                var mc = Color.Parse(rarityMain);
                rarityBadge.Background  = new SolidColorBrush(new Color(0x28, mc.R, mc.G, mc.B));
                rarityBadge.BorderBrush = new SolidColorBrush(new Color(0x55, mc.R, mc.G, mc.B));
            }
            if (glowRing      != null) glowRing.Stroke      = SolidColorBrush.Parse(rarityMain);
            if (glowRingOuter != null) glowRingOuter.Stroke = SolidColorBrush.Parse(rarityMain);

            // Build a SkinRendererControl inside the host, load skin + cosmetic async
            Controls.SkinRendererControl? celebRenderer = null;
            if (itemRendHost != null)
            {
                celebRenderer = new Controls.SkinRendererControl
                {
                    Width  = 160,
                    Height = 200,
                };
                itemRendHost.Child = celebRenderer;

                var category = System.Array.Find(
                    Views.Pages.StorePageView.StoreCatalog, c => c.Id == id).Category ?? "";

                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    var skinBytes = await FetchSkinBytesAsync().ConfigureAwait(false);
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (skinBytes != null)
                            celebRenderer.UpdateSkinTexture(skinBytes);

                        // Apply the specific cosmetic being unlocked
                        Services.CosmeticHelpers.ApplyCosmeticPreviewToRenderer(
                            celebRenderer, id, category, _currentSettings);
                    });
                });
            }

            // Reset state
            if (giftIcon      != null) { giftIcon.IsVisible      = true;  giftIcon.Opacity      = 1; }
            if (itemRendHost  != null) { itemRendHost.IsVisible   = false; itemRendHost.Opacity  = 0; }
            if (unlockedBanner!= null) { unlockedBanner.IsVisible = false; unlockedBanner.Opacity= 0; }
            if (glowRingOuter != null) { glowRingOuter.IsVisible  = false; glowRingOuter.Opacity = 0; }
            if (glowFill      != null) { glowFill.IsVisible       = false; glowFill.Opacity      = 0; }
            var resultPanel = this.FindControl<StackPanel>("CelebResultPanel");
            if (resultPanel != null) { resultPanel.IsVisible = false; resultPanel.Opacity = 0; }
            if (glowRing    != null) { glowRing.IsVisible    = false; glowRing.Opacity    = 0; }

            if (overlay.RenderTransform is ScaleTransform ot) { ot.ScaleX = 0; ot.ScaleY = 0; }
            overlay.Opacity   = 0;
            overlay.IsVisible = true;

            // Phase timings via DispatcherTimer
            int phase = 0;
            var timer = new Avalonia.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            double elapsed = 0;
            bool revealSoundPlayed = false;

            var panelScale = new ScaleTransform(0, 0);
            var panel = this.FindControl<Border>("CelebPanel");
            if (panel != null)
            {
                panel.RenderTransform = panelScale;
                panel.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            }
            var giftScale = new ScaleTransform(0, 0);
            if (giftIcon != null)
            {
                giftIcon.RenderTransform = giftScale;
                giftIcon.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            }
            var itemScale = new ScaleTransform(0, 0);
            if (itemRendHost != null)
            {
                itemRendHost.RenderTransform = itemScale;
                itemRendHost.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            }

            static double Ease(double t) => t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;
            static double Bounce(double t)
            {
                var s = Ease(Math.Min(t, 1.0));
                if (t > 0.75) s = 1 + Math.Sin((t - 0.75) * Math.PI / 0.25) * 0.15 * (1 - (t - 0.75) / 0.25);
                return s;
            }

            timer.Tick += (_, _) =>
            {
                elapsed += 16;

                // Fade in overlay only during phase 0 (elapsed resets on each phase, so guard here)
                if (phase == 0 && elapsed <= 200)
                    overlay.Opacity = elapsed / 200.0;
                else
                    overlay.Opacity = 1;

                // Phase 0: Panel + gift scale in + banner fade in (0–600ms)
                if (phase == 0)
                {
                    double t = Math.Min(elapsed / 600.0, 1.0);
                    var s = Bounce(t);
                    panelScale.ScaleX = s; panelScale.ScaleY = s;
                    giftScale.ScaleX  = s; giftScale.ScaleY  = s;
                    if (t >= 0.3 && unlockedBanner != null)
                    {
                        unlockedBanner.IsVisible = true;
                        unlockedBanner.Opacity   = Math.Min((t - 0.3) / 0.4, 1.0);
                    }
                    if (t >= 1.0) { phase = 1; elapsed = 0; }
                }
                // Phase 1: Gift shake (0–400ms)
                else if (phase == 1)
                {
                    double t = elapsed / 400.0;
                    if (giftIcon != null)
                    {
                        var angle = Math.Sin(t * Math.PI * 6) * 8 * (1 - t);
                        var rt = new RotateTransform(angle);
                        giftIcon.RenderTransform = rt;
                        giftIcon.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
                    }
                    if (t >= 1.0) { phase = 2; elapsed = 0; }
                }
                // Phase 2: Gift fade out, renderer + glow rings fade in (0–500ms)
                else if (phase == 2)
                {
                    revealSoundPlayed = true;
                    double t = Math.Min(elapsed / 500.0, 1.0);
                    if (giftIcon != null) giftIcon.Opacity = 1 - t;
                    if (itemRendHost != null)
                    {
                        itemRendHost.IsVisible = true;
                        itemRendHost.Opacity   = t;
                        var s = Bounce(t);
                        itemScale.ScaleX = s; itemScale.ScaleY = s;
                    }
                    if (glowFill != null)
                    {
                        glowFill.IsVisible = true;
                        glowFill.Opacity   = t * 0.6;
                    }
                    if (glowRing != null)
                    {
                        glowRing.IsVisible = true;
                        glowRing.Opacity   = t * 0.85;
                    }
                    if (glowRingOuter != null && t > 0.2)
                    {
                        glowRingOuter.IsVisible = true;
                        glowRingOuter.Opacity   = ((t - 0.2) / 0.8) * 0.45;
                    }
                    if (t >= 1.0)
                    {
                        if (giftIcon != null) giftIcon.IsVisible = false;
                        phase = 3; elapsed = 0;
                    }
                }
                // Phase 3: Result panel fade in, rings pulse (0–500ms)
                else if (phase == 3)
                {
                    double t = Math.Min(elapsed / 500.0, 1.0);
                    if (resultPanel != null) { resultPanel.IsVisible = true; resultPanel.Opacity = t; }
                    double pulse = 1.0 + Math.Sin(elapsed / 500.0 * Math.PI) * 0.08;
                    if (glowRing      != null) glowRing.Opacity      = 0.85 * pulse;
                    if (glowRingOuter != null) glowRingOuter.Opacity = 0.45 * pulse;
                    if (t >= 1.0) { timer.Stop(); }
                }
            };

            timer.Start();
        }

        // ═══════════════════════════════════════════════════════════
        //  Randomized Explosion Celebration Animation
        //  Types: TNT, Creeper, Bed, End Crystal
        // ═══════════════════════════════════════════════════════════

        private enum ExplosionStyle
        {
            LongFuse,    // TNT/Creeper: 4s fuse charge + boom
            QuickBoom,   // Bed: brief buildup + instant boom
            HoverRotate, // End Crystal: hover + rotate, then burst
        }

        private sealed class ExplosionConfig
        {
            public required string BlockBitmapUri;
            public required string WavUri;
            public required string WavTempFilename;
            public required ExplosionStyle Style;
            public required long ExplosionTargetMs;
            public required Color GlowInner;
            public required Color GlowMid;
            public required Color[] ParticleColors;
            public double BlockSize = 140; // display size inside TntBlockGroup
        }

        private static readonly ExplosionConfig[] _explosionPresets = new[]
        {
            // TNT — orange glow, white/gray particles, 4s fuse
            new ExplosionConfig
            {
                BlockBitmapUri    = "avares://LeafClient/Assets/TNT.png",
                WavUri            = "avares://LeafClient/Assets/MinecraftTNT.wav",
                WavTempFilename   = "leafclient_sfx_tnt.wav",
                Style             = ExplosionStyle.LongFuse,
                ExplosionTargetMs = 4300,
                GlowInner         = Color.FromArgb(0xDD, 0xFF, 0x99, 0x00),
                GlowMid           = Color.FromArgb(0x88, 0xFF, 0x55, 0x00),
                BlockSize         = 170,
                ParticleColors    = new[]
                {
                    Color.FromRgb(255, 255, 255),
                    Color.FromRgb(255, 255, 255),
                    Color.FromRgb(255, 255, 255),
                    Color.FromRgb(238, 238, 238),
                    Color.FromRgb(220, 220, 220),
                    Color.FromRgb(200, 200, 200),
                    Color.FromRgb(180, 180, 180),
                },
            },
            // Creeper — green glow, white/gray/green particles, same 4s fuse
            new ExplosionConfig
            {
                BlockBitmapUri    = "avares://LeafClient/Assets/Creeper.png",
                WavUri            = "avares://LeafClient/Assets/MinecraftTNT.wav",
                WavTempFilename   = "leafclient_sfx_tnt.wav",
                Style             = ExplosionStyle.LongFuse,
                ExplosionTargetMs = 4300,
                GlowInner         = Color.FromArgb(0xDD, 0x88, 0xFF, 0x44),
                GlowMid           = Color.FromArgb(0x88, 0x44, 0xCC, 0x22),
                BlockSize         = 230,
                ParticleColors    = new[]
                {
                    Color.FromRgb(255, 255, 255),
                    Color.FromRgb(240, 240, 240),
                    Color.FromRgb(220, 220, 220),
                    Color.FromRgb(180, 180, 180),
                    Color.FromRgb(140, 220, 90),
                    Color.FromRgb(100, 190, 70),
                    Color.FromRgb( 70, 150, 50),
                },
            },
            // Bed — red glow, red/white particles, quick ~900ms buildup
            new ExplosionConfig
            {
                BlockBitmapUri    = "avares://LeafClient/Assets/Bed.png",
                WavUri            = "avares://LeafClient/Assets/BedExplosion.wav",
                WavTempFilename   = "leafclient_sfx_bed.wav",
                Style             = ExplosionStyle.QuickBoom,
                ExplosionTargetMs = 900,
                GlowInner         = Color.FromArgb(0xDD, 0xFF, 0x33, 0x33),
                GlowMid           = Color.FromArgb(0x88, 0xCC, 0x11, 0x11),
                BlockSize         = 210,
                ParticleColors    = new[]
                {
                    Color.FromRgb(255, 255, 255),
                    Color.FromRgb(255, 220, 220),
                    Color.FromRgb(255, 140, 140),
                    Color.FromRgb(255,  80,  80),
                    Color.FromRgb(220,  40,  40),
                    Color.FromRgb(180,  20,  20),
                    Color.FromRgb(140,  10,  10),
                },
            },
            // End Crystal — purple/pink glow, magenta particles, hover + rotate ~2000ms
            new ExplosionConfig
            {
                BlockBitmapUri    = "avares://LeafClient/Assets/EndCrystal.png",
                WavUri            = "avares://LeafClient/Assets/CrystalExplosion.wav",
                WavTempFilename   = "leafclient_sfx_crystal.wav",
                Style             = ExplosionStyle.HoverRotate,
                ExplosionTargetMs = 2000,
                GlowInner         = Color.FromArgb(0xDD, 0xDD, 0x77, 0xFF),
                GlowMid           = Color.FromArgb(0x88, 0x99, 0x33, 0xCC),
                BlockSize         = 200,
                ParticleColors    = new[]
                {
                    Color.FromRgb(255, 255, 255),
                    Color.FromRgb(255, 210, 255),
                    Color.FromRgb(230, 140, 255),
                    Color.FromRgb(190,  90, 230),
                    Color.FromRgb(150,  50, 190),
                    Color.FromRgb(110,  30, 150),
                    Color.FromRgb( 80,  20, 110),
                },
            },
        };

        // Cache wav temp paths (load once per session)
        private static readonly Dictionary<string, string> _wavCache = new();

        private static string? EnsureWavAsset(string avaresUri, string tempFileName)
        {
            if (_wavCache.TryGetValue(avaresUri, out var cached) && System.IO.File.Exists(cached))
                return cached;
            var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), tempFileName);
            try
            {
                using var stream = Avalonia.Platform.AssetLoader.Open(new Uri(avaresUri));
                using var ms = new System.IO.MemoryStream();
                stream.CopyTo(ms);
                System.IO.File.WriteAllBytes(tempPath, ms.ToArray());
                _wavCache[avaresUri] = tempPath;
                return tempPath;
            }
            catch { return null; }
        }

        private static void PlayWavSound(string? wavPath)
        {
            if (wavPath == null) return;
            _ = System.Threading.Tasks.Task.Run(() =>
            {
                try { PlaySound(wavPath, IntPtr.Zero, SND_FILENAME | SND_ASYNC | SND_NODEFAULT); }
                catch { }
            });
        }

        // Each particle is a cross-shaped Control (two overlapping Borders inside a Grid).
        private sealed class TntParticle
        {
            public required Control Element;
            public double X, Y;
            public double Vx, Vy;
            public double Opacity;
            public double FadeRate;
        }

        private static Control CreateCrossParticle(double size, Color color)
        {
            var brush = new SolidColorBrush(color);
            var grid  = new Grid { Width = size, Height = size };
            grid.Children.Add(new Border
            {
                Width               = size,
                Height              = size * 0.38,
                Background          = brush,
                VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            });
            grid.Children.Add(new Border
            {
                Width               = size * 0.38,
                Height              = size,
                Background          = brush,
                VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            });
            return grid;
        }

        private List<TntParticle> CreateTntParticles(Canvas canvas, Color[] colors)
        {
            var rng       = new Random();
            var particles = new List<TntParticle>();
            double cx     = this.ClientSize.Width  / 2.0;
            double cy     = this.ClientSize.Height / 2.0;

            // Three size bands matching the Minecraft screenshot density
            (double minSz, double maxSz, int count)[] bands =
            [
                (28, 52, 12),   // large chunks
                (14, 27, 24),   // medium
                (6,  13, 24),   // small
            ];

            foreach (var (minSz, maxSz, count) in bands)
            {
                for (int i = 0; i < count; i++)
                {
                    double angle = rng.NextDouble() * Math.PI * 2;
                    double speed = 4.0 + rng.NextDouble() * 13.0;
                    double size  = minSz + rng.NextDouble() * (maxSz - minSz);
                    var    color = colors[rng.Next(colors.Length)];

                    var el = CreateCrossParticle(size, color);
                    el.RenderTransform = new RotateTransform(rng.NextDouble() * 90);
                    el.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
                    el.Opacity = 1.0;

                    canvas.Children.Add(el);
                    Canvas.SetLeft(el, cx - size / 2);
                    Canvas.SetTop(el,  cy - size / 2);

                    particles.Add(new TntParticle
                    {
                        Element  = el,
                        X        = cx - size / 2,
                        Y        = cy - size / 2,
                        Vx       = Math.Cos(angle) * speed,
                        Vy       = Math.Sin(angle) * speed - 2.0,
                        Opacity  = 1.0,
                        FadeRate = 0.010 + rng.NextDouble() * 0.016,
                    });
                }
            }
            return particles;
        }

        // Bitmap cache per asset URI
        private static readonly Dictionary<string, Avalonia.Media.Imaging.Bitmap> _bitmapCache = new();

        private static Avalonia.Media.Imaging.Bitmap? LoadAssetBitmap(string avaresUri)
        {
            if (_bitmapCache.TryGetValue(avaresUri, out var cached)) return cached;
            try
            {
                using var s = Avalonia.Platform.AssetLoader.Open(new Uri(avaresUri));
                var bmp = new Avalonia.Media.Imaging.Bitmap(s);
                _bitmapCache[avaresUri] = bmp;
                return bmp;
            }
            catch { return null; }
        }

        // Entry point — randomly picks one of the 4 explosion presets and runs it.
        private async Task RunTntAnimationAsync()
        {
            var preset = _explosionPresets[new Random().Next(_explosionPresets.Length)];
            await RunExplosionAnimationAsync(preset);
        }

        // Debug: Ctrl+Shift+X triggers the crash report overlay with a fake exception.
        private async void OnTutorialStepChanged(LeafClient.Models.TutorialStep step)
        {
            if (_addServerOverlay?.IsVisible == true && step.TargetElementName != "AddServerModalCard")
                HideAddServerOverlay();

            if (_modBrowserOverlay?.IsVisible == true && step.TargetElementName != "ModBrowserPanel")
                _modBrowserOverlay.IsVisible = false;

            if (step.NavigateToPage.HasValue)
                SwitchToPage(step.NavigateToPage.Value);

            if (step.OpenAccountPanel)
                OpenAccountPanelClick();

            if (step.OnEnter == LeafClient.Models.TutorialOnEnter.SelectLeafCapeInStore)
            {
                await Task.Delay(600);
                _storePage?.SelectLeafCapeForTutorial();
            }
        }

        private void OnDebugKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            var mods = e.KeyModifiers;
            bool ctrlShift = mods.HasFlag(Avalonia.Input.KeyModifiers.Control)
                          && mods.HasFlag(Avalonia.Input.KeyModifiers.Shift);
            if (!ctrlShift) return;

            if (e.Key == Avalonia.Input.Key.X)
            {
                Console.WriteLine("[Debug] Triggering fake crash to test crash reporter");
                try
                {
                    throw new InvalidOperationException(
                        "Debug test crash — triggered via Ctrl+Shift+X. If you see this in the crash report backend, the pipeline works end-to-end.");
                }
                catch (Exception ex)
                {
                    ShowCrashReportOverlay(ex, null);
                }
                e.Handled = true;
            }
        }

        private async Task RunExplosionAnimationAsync(ExplosionConfig config)
        {
            var tntOverlay    = this.FindControl<Grid>("TntAnimOverlay");
            var tntBackdrop   = this.FindControl<Rectangle>("TntBackdrop");
            var tntFlash      = this.FindControl<Rectangle>("TntWhiteFlash");
            var tntCanvas     = this.FindControl<Canvas>("TntParticleCanvas");
            var tntGroup      = this.FindControl<Grid>("TntBlockGroup");
            var tntGlow       = this.FindControl<Ellipse>("TntGlow");
            var tntBlockFlash = this.FindControl<Rectangle>("TntBlockFlash");
            var tntBlockImage = this.FindControl<Image>("TntBlockImage");

            if (tntOverlay == null || tntGroup == null) return;

            var blockBitmap = LoadAssetBitmap(config.BlockBitmapUri);
            var wavPath     = EnsureWavAsset(config.WavUri, config.WavTempFilename);

            // Reset state + swap textures + set glow color
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                tntCanvas?.Children.Clear();
                if (tntBackdrop   != null) tntBackdrop.Opacity   = 0;
                if (tntFlash      != null) tntFlash.Opacity      = 0;
                if (tntGlow       != null) tntGlow.Opacity       = 0;
                if (tntBlockFlash != null) tntBlockFlash.Opacity = 0;

                if (tntBlockImage != null && blockBitmap != null)
                {
                    tntBlockImage.Source = blockBitmap;
                    tntBlockImage.Width  = config.BlockSize;
                    tntBlockImage.Height = config.BlockSize;
                }
                if (tntBlockFlash != null && blockBitmap != null)
                {
                    tntBlockFlash.Width  = config.BlockSize;
                    tntBlockFlash.Height = config.BlockSize;
                    // Match the Image control's default Stretch=Uniform so the mask
                    // aligns exactly with the visible pixels of non-square textures.
                    tntBlockFlash.OpacityMask = new ImageBrush
                    {
                        Source  = blockBitmap,
                        Stretch = Stretch.Uniform,
                    };
                }

                // Update glow color to match preset
                if (tntGlow != null)
                {
                    tntGlow.Fill = new RadialGradientBrush
                    {
                        GradientStops =
                        {
                            new GradientStop(config.GlowInner, 0),
                            new GradientStop(config.GlowMid,   0.45),
                            new GradientStop(Color.FromArgb(0, 0, 0, 0), 1),
                        }
                    };
                }

                tntGroup.IsVisible             = true;
                tntGroup.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
                tntGroup.Opacity               = 0;
                tntOverlay.IsVisible           = true;
                tntOverlay.Opacity             = 1;

                // Style-specific initial transform
                if (config.Style == ExplosionStyle.LongFuse)
                {
                    tntGroup.RenderTransform = new TranslateTransform(0, 620);
                }
                else if (config.Style == ExplosionStyle.HoverRotate)
                {
                    // TransformGroup: scale (fade-in), translate (hover), rotate (spin)
                    var tg = new TransformGroup();
                    tg.Children.Add(new ScaleTransform(0.2, 0.2));
                    tg.Children.Add(new TranslateTransform(0, 0));
                    tg.Children.Add(new RotateTransform(0));
                    tntGroup.RenderTransform = tg;
                }
                else // QuickBoom
                {
                    tntGroup.RenderTransform = new ScaleTransform(0.2, 0.2);
                }
            });

            var globalSw = System.Diagnostics.Stopwatch.StartNew();

            if (config.Style == ExplosionStyle.LongFuse)
            {
                // Long fuse: play WAV at t=0 (sound contains fuse sizzle + boom)
                PlayWavSound(wavPath);

                // Phase 0: slide up from below (~400ms)
                const int slideSteps = 25;
                for (int i = 0; i <= slideSteps; i++)
                {
                    double t    = (double)i / slideSteps;
                    double ease = 1 - Math.Pow(1 - t, 3);
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (tntGroup.RenderTransform is TranslateTransform tt2)
                            tt2.Y = 620 * (1 - ease);
                        tntGroup.Opacity = ease;
                        if (tntBackdrop != null) tntBackdrop.Opacity = ease * 0.80;
                    });
                    if (i < slideSteps) await Task.Delay(400 / slideSteps);
                }

                // Phase 1: charging flash + glow pulse (using Stopwatch for real-time)
                bool flashOn = false;
                while (globalSw.ElapsedMilliseconds < config.ExplosionTargetMs)
                {
                    long remaining = config.ExplosionTargetMs - globalSw.ElapsedMilliseconds;
                    double progress = 1.0 - Math.Clamp((double)remaining / (config.ExplosionTargetMs - 450), 0, 1);

                    double halfInterval;
                    if (progress < 0.80)
                        halfInterval = Math.Max(40.0, 160.0 - progress * 150.0);
                    else
                        halfInterval = 60.0; // slow flash near the end

                    flashOn = !flashOn;

                    double glowOpacity = 0.25 + progress * 0.70;
                    double blockWhite  = flashOn ? (0.30 + progress * 0.65) : 0.0;

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (tntGlow       != null) tntGlow.Opacity       = Math.Min(1.0, glowOpacity);
                        if (tntBlockFlash != null) tntBlockFlash.Opacity = blockWhite;
                    });

                    int waitMs = (int)Math.Min(halfInterval, Math.Max(16, remaining));
                    await Task.Delay(waitMs);
                }
            }
            else if (config.Style == ExplosionStyle.QuickBoom)
            {
                // Phase 0: quick scale-in (200ms)
                const int scaleSteps = 14;
                for (int i = 0; i <= scaleSteps; i++)
                {
                    double t    = (double)i / scaleSteps;
                    double ease = 1 - Math.Pow(1 - t, 2);
                    double scale = 0.3 + 0.7 * ease;
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (tntGroup.RenderTransform is ScaleTransform st)
                        {
                            st.ScaleX = scale;
                            st.ScaleY = scale;
                        }
                        tntGroup.Opacity = ease;
                        if (tntBackdrop != null) tntBackdrop.Opacity = ease * 0.80;
                    });
                    if (i < scaleSteps) await Task.Delay(200 / scaleSteps);
                }

                // Phase 1: brief pulse/glow ramp-up. Play WAV right before boom so
                // the ~few-ms wav delay lands exactly with the visual particles.
                const long wavLeadMs = 40;
                long playWavAt = config.ExplosionTargetMs - wavLeadMs;
                bool wavPlayed = false;

                while (globalSw.ElapsedMilliseconds < config.ExplosionTargetMs)
                {
                    long remaining = config.ExplosionTargetMs - globalSw.ElapsedMilliseconds;
                    double progress = 1.0 - Math.Clamp((double)remaining / (config.ExplosionTargetMs - 200), 0, 1);

                    double glowOpacity = 0.25 + progress * 0.75;
                    double pulse       = Math.Sin(globalSw.ElapsedMilliseconds / 70.0) * 0.15 + 0.85;
                    double blockWhite  = progress * 0.65 * pulse;

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (tntGlow       != null) tntGlow.Opacity       = Math.Min(1.0, glowOpacity);
                        if (tntBlockFlash != null) tntBlockFlash.Opacity = blockWhite;
                    });

                    if (!wavPlayed && globalSw.ElapsedMilliseconds >= playWavAt)
                    {
                        PlayWavSound(wavPath);
                        wavPlayed = true;
                    }

                    await Task.Delay(16);
                }

                if (!wavPlayed) PlayWavSound(wavPath);
            }
            else // HoverRotate: End Crystal — floats up/down + spins, then bursts
            {
                // Phase 0: fade-in with initial scale (250ms)
                const int scaleSteps = 16;
                for (int i = 0; i <= scaleSteps; i++)
                {
                    double t    = (double)i / scaleSteps;
                    double ease = 1 - Math.Pow(1 - t, 2);
                    double scale = 0.3 + 0.7 * ease;
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (tntGroup.RenderTransform is TransformGroup tgA
                            && tgA.Children.Count >= 1
                            && tgA.Children[0] is ScaleTransform st)
                        {
                            st.ScaleX = scale;
                            st.ScaleY = scale;
                        }
                        tntGroup.Opacity = ease;
                        if (tntBackdrop != null) tntBackdrop.Opacity = ease * 0.80;
                    });
                    if (i < scaleSteps) await Task.Delay(250 / scaleSteps);
                }

                // Phase 1: hover (vertical sine) + rotate (continuous spin) +
                // glow ramp-up. Play WAV right before the burst.
                const long wavLeadMs = 40;
                long playWavAt = config.ExplosionTargetMs - wavLeadMs;
                bool wavPlayed = false;

                long phase1Start = globalSw.ElapsedMilliseconds;

                while (globalSw.ElapsedMilliseconds < config.ExplosionTargetMs)
                {
                    long remaining = config.ExplosionTargetMs - globalSw.ElapsedMilliseconds;
                    long elapsedInPhase = globalSw.ElapsedMilliseconds - phase1Start;
                    double progress = 1.0 - Math.Clamp((double)remaining / (config.ExplosionTargetMs - 250), 0, 1);

                    // Hover: smooth sine wave on Y, amplitude 18px, slow 1600ms period
                    double hoverY = Math.Sin(elapsedInPhase / 1600.0 * Math.PI * 2) * 18.0;
                    // Glow + flash ramp
                    double glowOpacity = 0.30 + progress * 0.70;
                    double pulse       = Math.Sin(globalSw.ElapsedMilliseconds / 90.0) * 0.18 + 0.82;
                    double blockWhite  = progress * 0.55 * pulse;

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (tntGroup.RenderTransform is TransformGroup tgB
                            && tgB.Children.Count >= 2)
                        {
                            if (tgB.Children[1] is TranslateTransform tr) tr.Y = hoverY;
                        }
                        if (tntGlow       != null) tntGlow.Opacity       = Math.Min(1.0, glowOpacity);
                        if (tntBlockFlash != null) tntBlockFlash.Opacity = blockWhite;
                    });

                    if (!wavPlayed && globalSw.ElapsedMilliseconds >= playWavAt)
                    {
                        PlayWavSound(wavPath);
                        wavPlayed = true;
                    }

                    await Task.Delay(16);
                }

                if (!wavPlayed) PlayWavSound(wavPath);
            }

            // Phase 2: BOOM — trigger explosion and return immediately so the
            // celebration panel can show at the same time as the particles.
            List<TntParticle> particles = null!;
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (tntFlash  != null) tntFlash.Opacity = 1;
                tntGroup.IsVisible = false;
                if (tntCanvas != null) particles = CreateTntParticles(tntCanvas, config.ParticleColors);
            });

            // Run particle animation in the background — caller continues immediately.
            _ = Task.Run(async () =>
            {
                const int explodeSteps = 120;
                for (int i = 0; i <= explodeSteps; i++)
                {
                    double t = (double)i / explodeSteps;
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (tntFlash != null)
                            tntFlash.Opacity = Math.Max(0.0, 1.0 - t * 5.0);
                        if (tntBackdrop != null)
                            tntBackdrop.Opacity = Math.Max(0.0, 0.80 * (1.0 - Math.Max(0.0, (t - 0.25) / 0.75)));

                        foreach (var p in particles)
                        {
                            p.X  += p.Vx;
                            p.Y  += p.Vy;
                            p.Vy += 0.20;
                            p.Opacity = Math.Max(0, p.Opacity - p.FadeRate);
                            Canvas.SetLeft(p.Element, p.X);
                            Canvas.SetTop(p.Element,  p.Y);
                            p.Element.Opacity = p.Opacity;
                        }
                    });
                    if (i < explodeSteps) await Task.Delay(16);
                }

                // Cleanup after particles finish
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    tntOverlay.IsVisible = false;
                    tntCanvas?.Children.Clear();
                    tntGroup.IsVisible  = true;
                });
            });
        }

        // EquipCosmetic, UnequipCosmetic, SaveEquippedJson, OnCosmeticTabTapped,
        // OnPreviewLightToggle, OnCosmeticsSearchChanged moved to CosmeticsPageView.

        private void OpenSkinsPage(object? sender, RoutedEventArgs e)
        {
            CloseAccountPanelImmediate(null, new RoutedEventArgs());
            SwitchToPage(5); 
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
                        return;
                    }

                    if (string.IsNullOrEmpty(selectedFilePath))
                    {
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


        private void LoadSkins()
        {
            if (_skinsWrapPanel == null || _noSkinsMessage == null) return;

            _skinsWrapPanel.Children.Clear();
            _currentlySelectedSkinCard = null; 

            if (_currentSettings.CustomSkins.Count == 0)
            {
                _noSkinsMessage.IsVisible = true;
                _currentSettings.SelectedSkinId = null; 
                _ = _settingsService.SaveSettingsAsync(_currentSettings); 
                return;
            }

            _noSkinsMessage.IsVisible = false;

            foreach (var skin in _currentSettings.CustomSkins)
            {
                var skinCard = CreateSkinCard(skin);
                _skinsWrapPanel.Children.Add(skinCard);
            }

            _isProgrammaticallySelectingSkin = true;
            try
            {
                if (!string.IsNullOrEmpty(_currentSettings.SelectedSkinId))
                {
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
                Tag = skin.Id, 
                BorderBrush = skin.Id == _currentSettings.SelectedSkinId ? selectedBorderBrush : Brushes.Transparent,
                BorderThickness = new Thickness(skin.Id == _currentSettings.SelectedSkinId ? 3 : 0)
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var imageContainer = new Border
            {
                Background = GetBrush("HoverBackgroundBrush"),
                CornerRadius = new CornerRadius(8),
                ClipToBounds = true
            };
            _ = LoadSkinPreviewAsync(skin, imageContainer);
            Grid.SetRow(imageContainer, 0);
            grid.Children.Add(imageContainer);

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

            card.PointerEntered += (s, e) =>
            {
                menuButton.IsVisible = true;
                menuButton.Opacity = 1;
            };

            card.PointerExited += (s, e) =>
            {
                if (menuButton.Flyout == null || !menuButton.Flyout.IsOpen)
                {
                    menuButton.Opacity = 0;
                    menuButton.IsVisible = false;
                }
            };

            card.PointerPressed += (s, e) =>
            {
                if (e.GetCurrentPoint(s as Control).Properties.IsLeftButtonPressed)
                {
                    SelectSkin(skin.Id);
                    e.Handled = true; 
                }
            };



            card.Child = grid;

            if (skin.Id == _currentSettings.SelectedSkinId)
            {
                _currentlySelectedSkinCard = card;
            }

            return card;
        }


        private async void SelectSkin(string skinId)
        {

            if (_isProgrammaticallySelectingSkin)
            {
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
                    newSelectedCard.BorderBrush = new SolidColorBrush(Color.FromRgb(50, 205, 50)); 
                    newSelectedCard.BorderThickness = new Thickness(3);
                    _currentlySelectedSkinCard = newSelectedCard;
                }

                _currentSettings.SelectedSkinId = skinId;
                await _settingsService.SaveSettingsAsync(_currentSettings);

                return;
            }


            if (_currentlySelectedSkinCard != null)
            {
                _currentlySelectedSkinCard.BorderBrush = Brushes.Transparent;
                _currentlySelectedSkinCard.BorderThickness = new Thickness(0);
            }

            var card = _skinsWrapPanel?.Children.OfType<Border>()
                .FirstOrDefault(b => b.Tag as string == skinId);

            if (card != null)
            {
                card.BorderBrush = new SolidColorBrush(Color.FromRgb(50, 205, 50)); 
                card.BorderThickness = new Thickness(3);
                _currentlySelectedSkinCard = card;
            }

            _currentSettings.SelectedSkinId = skinId;
            await _settingsService.SaveSettingsAsync(_currentSettings);

            if (_currentSettings.AccountType == "offline")
            {
                Console.WriteLine("[Skin Upload] Skipped: User is in Offline Mode.");
                ShowSkinStatusBanner("⚠️ Offline Mode: Skins cannot be changed within Offline Mode.", SkinBannerStatus.Error);
                return;
            }

            var selectedSkin = _currentSettings.CustomSkins.FirstOrDefault(s => s.Id == skinId);
            if (selectedSkin == null || !System.IO.File.Exists(selectedSkin.FilePath))
            {
                Console.WriteLine("[Skin Upload] Skipped: File not found.");
                ShowSkinStatusBanner("❌ Error: Skin file not found.", SkinBannerStatus.Error);
                return;
            }

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

                var loadingText = new TextBlock
                {
                    Text = "⏳",
                    FontSize = 32,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                container.Child = loadingText;

                var pose = _skinRenderService.GetRandomLargePoseName();

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
                LoadSkins(); 
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

            if (!AreAnimationsEnabled()) 
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_launchErrorBanner != null)
                        {
                            _launchErrorBanner.IsVisible = false;
                            _launchErrorBanner.Opacity = 0; 
                        }
                    });
                });
                return;
            }

            _ = Task.Run(async () =>
            {
                await Task.Delay(3500);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_launchErrorBanner != null)
                    {
                        _launchErrorBanner.Opacity = 0;
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

            if (!AreAnimationsEnabled()) 
            {
                _launchErrorBanner.Opacity = 0;
                _launchErrorBanner.IsVisible = false;
                return;
            }
            _launchErrorBanner.Opacity = 0;
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

            if (!AreAnimationsEnabled()) 
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

            if (!AreAnimationsEnabled()) 
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
                    await Task.Delay(140, ct); 
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

            var origin = button.TranslatePoint(new Point(0, 0), this);
            if (!origin.HasValue) return;

            const double tooltipHeight = 26;
            const double gapAboveButton = 8;

            double tooltipWidth = _quickPlayTooltip.Bounds.Width > 0
                ? _quickPlayTooltip.Bounds.Width
                : 80; 

            double buttonCenterX = origin.Value.X + (button.Bounds.Width / 2.0);
            double left = buttonCenterX - (tooltipWidth / 2.0);

            double top = origin.Value.Y - tooltipHeight - gapAboveButton;

            Canvas.SetLeft(_quickPlayTooltip, Math.Max(0, left)); 
            Canvas.SetTop(_quickPlayTooltip, Math.Max(0, top));   

            Console.WriteLine($"[QuickPlay Tooltip] Button at ({origin.Value.X}, {origin.Value.Y}), Tooltip at ({left}, {top})");
        }

        // ─────────────────────────────────────────────────────────────────────────
        // PROFILE EDITOR OVERLAY
        // ─────────────────────────────────────────────────────────────────────────

        private void ShowProfileOverlay(LauncherProfile? profile)
        {
            _po_EditingProfile = profile;
            _po_ActiveTab = 0;
            _po_ShowAllVersionsFlag = false;
            var savedPreset = profile?.ModPreset ?? "balanced";
            _po_SelectedPreset = savedPreset == "none" ? "balanced" : savedPreset;
            _po_SelectedVersion = profile?.MinecraftVersion ?? "";

            if (_po_TitleText != null)
                _po_TitleText.Text = profile == null ? "New Profile" : $"Edit — {profile.Name}";

            // Update avatar letter
            if (_po_AvatarLetter != null)
            {
                string name = profile?.Name ?? "N";
                _po_AvatarLetter.Text = !string.IsNullOrWhiteSpace(name) ? name.Trim().Substring(0, 1).ToUpperInvariant() : "N";
            }

            if (_po_NameBox != null)
                _po_NameBox.Text = profile?.Name ?? "";

            if (_po_MemSlider != null)
                _po_MemSlider.Value = profile?.AllocatedMemoryGb ?? 3.0;

            if (_po_MemLabel != null)
                _po_MemLabel.Text = $"{(profile?.AllocatedMemoryGb ?? 3.0):0.0} GB";

            if (_po_ShowAllVersions != null)
                _po_ShowAllVersions.IsChecked = false;

            PO_PopulateAccountDropdown(profile?.AccountSetting ?? "active");
            PO_PopulateVersionDropdown();
            PO_SelectVersion(_po_SelectedVersion);
            PO_UpdateSupportBanner(_po_SelectedVersion);
            PO_UpdateModsSupportBanner(_po_SelectedVersion);
            PO_ApplyPresetHighlight(_po_SelectedPreset);

            // ── Advanced tab: populate override fields + stats ──
            if (_po_DescriptionBox != null)
                _po_DescriptionBox.Text = profile?.Description ?? "";
            if (_po_IconEmojiBox != null)
                _po_IconEmojiBox.Text = profile?.IconEmoji ?? "";
            if (_po_JvmArgsBox != null)
                _po_JvmArgsBox.Text = profile?.JvmArgumentsOverride ?? "";
            if (_po_UseCustomResolutionToggle != null)
                _po_UseCustomResolutionToggle.IsChecked = profile?.UseCustomResolutionOverride ?? false;
            if (_po_ResWidthBox != null)
                _po_ResWidthBox.Text = profile?.GameResolutionWidthOverride?.ToString() ?? "";
            if (_po_ResHeightBox != null)
                _po_ResHeightBox.Text = profile?.GameResolutionHeightOverride?.ToString() ?? "";
            if (_po_QuickJoinAddressBox != null)
                _po_QuickJoinAddressBox.Text = profile?.QuickJoinServerAddressOverride ?? "";
            if (_po_QuickJoinPortBox != null)
                _po_QuickJoinPortBox.Text = profile?.QuickJoinServerPortOverride ?? "";

            if (_po_StatLaunches != null)
                _po_StatLaunches.Text = (profile?.LaunchCount ?? 0).ToString();
            if (_po_StatPlaytime != null)
                _po_StatPlaytime.Text = FormatPlaytimeShort(profile?.PlaytimeSeconds ?? 0);
            if (_po_StatLastUsed != null)
            {
                if (profile != null && profile.LastUsed != DateTime.MinValue)
                {
                    var ago = DateTime.Now - profile.LastUsed;
                    _po_StatLastUsed.Text = ago.TotalDays >= 1 ? $"{(int)ago.TotalDays}d ago"
                                         :  ago.TotalHours >= 1 ? $"{(int)ago.TotalHours}h ago"
                                         :  $"{(int)Math.Max(1, ago.TotalMinutes)}m ago";
                }
                else
                {
                    _po_StatLastUsed.Text = "never";
                }
            }

            PO_SwitchTab(0);

            if (_profileEditorOverlay != null) _profileEditorOverlay.IsVisible = true;
            if (_mainContentGrid != null)
                _mainContentGrid.Effect = new BlurEffect { Radius = 7 };
        }

        private void HideProfileOverlay()
        {
            if (_profileEditorOverlay != null) _profileEditorOverlay.IsVisible = false;
            if (_mainContentGrid != null) _mainContentGrid.Effect = null;
        }

        private async void PO_SwitchTab(int idx)
        {
            var panels = new[] { _po_TabIdentity, _po_TabPerformance, _po_TabMods, _po_TabAdvanced };
            var navBtns = new[] { _po_NavBtnGeneral, _po_NavBtnPerformance, _po_NavBtnMods, _po_NavBtnAdvanced };

            // Fade out current tab
            if (_po_ActiveTab != idx && panels[_po_ActiveTab] != null)
                panels[_po_ActiveTab]!.Opacity = 0;

            // Update nav button styles
            foreach (var (btn, i) in navBtns.Select((b, i) => (b, i)))
            {
                if (btn == null) continue;
                btn.Classes.Remove("PONavActive");
                if (i == idx) btn.Classes.Add("PONavActive");
            }

            // Slide the nav indicator vertically (54px height + 8px spacing = 62px per step)
            const double navStep = 62;
            if (_po_NavIndicator != null)
                _po_NavIndicator.Margin = new Thickness(0, idx * navStep, 0, 0);
            if (_po_NavActiveBar != null)
                _po_NavActiveBar.Margin = new Thickness(-2, 15 + idx * navStep, 0, 0);

            if (_po_ActiveTab != idx)
            {
                await System.Threading.Tasks.Task.Delay(120);

                // Toggle visibility
                foreach (var (p, i) in panels.Select((p, i) => (p, i)))
                {
                    if (p == null) continue;
                    p.IsVisible = i == idx;
                    if (i == idx) p.Opacity = 0;
                }

                _po_ActiveTab = idx;

                await System.Threading.Tasks.Task.Delay(30);
                if (panels[idx] != null)
                    panels[idx]!.Opacity = 1;
            }
            else
            {
                _po_ActiveTab = idx;
            }
        }

        // ── Skin-head helpers ─────────────────────────────────────────────────

        /// <summary>
        /// Downloads a Minecraft player-head from mc-heads.net using a UUID or username.
        /// Result is cached; null is cached on failure so we don't retry endlessly.
        /// </summary>
        private async Task<Bitmap?> LoadSkinHeadAsync(string identifier)
        {
            if (_skinHeadCache.TryGetValue(identifier, out var cached))
                return cached;

            try
            {
                // mc-heads.net accepts both UUID and username, returns a PNG face render
                var url = $"https://mc-heads.net/avatar/{identifier}/48";
                var bytes = await _httpClient.GetByteArrayAsync(url);
                using var ms = new MemoryStream(bytes);
                var bmp = new Bitmap(ms);
                _skinHeadCache[identifier] = bmp;
                return bmp;
            }
            catch
            {
                _skinHeadCache[identifier] = null;
                return null;
            }
        }

        /// <summary>
        /// Creates an avatar Border sized <paramref name="size"/>×<paramref name="size"/>.
        /// Shows a letter placeholder immediately, then asynchronously swaps in the real
        /// Minecraft skin-head face for any account that has a UUID or username.
        /// </summary>
        private Border MakeSkinHeadAvatarBorder(AccountEntry? acct, int size, int cornerRadius,
                                                 string? fallbackInitial = null, string? fallbackAccentHex = null)
        {
            // Prefer UUID for lookup; fall back to username (works for online accounts via mc-heads.net)
            string? identifier = !string.IsNullOrWhiteSpace(acct?.Uuid) ? acct!.Uuid
                               : !string.IsNullOrWhiteSpace(acct?.Username) ? acct!.Username
                               : null;

            bool isMicrosoft = acct?.AccountType == "microsoft";
            string initial = fallbackInitial
                ?? (string.IsNullOrWhiteSpace(acct?.Username) ? "?" : acct!.Username[0].ToString().ToUpper());
            string accent = fallbackAccentHex ?? (isMicrosoft ? "#7C3AED" : "#374151");

            // Solid background for placeholder
            var avatarBorder = new Border
            {
                Width = size, Height = size,
                CornerRadius = new CornerRadius(cornerRadius),
                Background = new SolidColorBrush(Color.Parse(accent)),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                ClipToBounds = true
            };

            // Placeholder: letter initial
            avatarBorder.Child = new TextBlock
            {
                Text = initial,
                Foreground = Brushes.White,
                FontSize = size * 0.45,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            // Kick off async download and swap for any account we have an identifier for
            if (identifier != null)
            {
                var capturedIdentifier = identifier;
                _ = Task.Run(async () =>
                {
                    var bmp = await LoadSkinHeadAsync(capturedIdentifier);
                    if (bmp != null)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            avatarBorder.Background = Brushes.Transparent;
                            var img = new Image
                            {
                                Source = bmp,
                                Width = size,
                                Height = size,
                                Stretch = Avalonia.Media.Stretch.UniformToFill
                            };
                            RenderOptions.SetBitmapInterpolationMode(img, BitmapInterpolationMode.None);
                            avatarBorder.Child = img;
                        });
                    }
                });
            }

            return avatarBorder;
        }

        // ── Account combo-item ─────────────────────────────────────────────────

        private ComboBoxItem MakeAccountComboItem(string label, string tag,
                                                   AccountEntry? acct = null,
                                                   string? initial = null, string? accentHex = null)
        {
            var content = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10 };

            // Avatar: real skin head for Microsoft accounts, letter initial otherwise
            var avatar = MakeSkinHeadAvatarBorder(acct, 26, 7, initial, accentHex);
            content.Children.Add(avatar);

            content.Children.Add(new TextBlock
            {
                Text = label,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                FontSize = 13
            });

            return new ComboBoxItem { Content = content, Tag = tag };
        }

        private void PO_PopulateAccountDropdown(string selectedSetting)
        {
            if (_po_AccountCombo == null) return;
            _po_AccountCombo.Items.Clear();

            // First option: "Use active account" with a star avatar (no account entry)
            _po_AccountCombo.Items.Add(MakeAccountComboItem("Use active account", "active",
                acct: null, initial: "★", accentHex: "#374151"));

            // Add all saved accounts — Microsoft ones will get real skin head
            var accounts = _currentSettings?.SavedAccounts ?? new List<AccountEntry>();
            foreach (var acct in accounts)
            {
                string label = acct.AccountType == "microsoft"
                    ? $"{acct.Username}  (Microsoft)"
                    : $"{acct.Username}  (Offline)";
                _po_AccountCombo.Items.Add(MakeAccountComboItem(label, acct.Id, acct: acct));
            }

            // Select the matching item
            bool found = false;
            foreach (var item in _po_AccountCombo.Items.OfType<ComboBoxItem>())
            {
                if (item.Tag?.ToString() == selectedSetting)
                {
                    _po_AccountCombo.SelectedItem = item;
                    found = true;
                    break;
                }
            }
            if (!found && _po_AccountCombo.ItemCount > 0)
                _po_AccountCombo.SelectedIndex = 0;
        }

        private void PO_PopulateVersionDropdown()
        {
            if (_po_VersionCombo == null) return;
            _po_VersionCombo.SelectionChanged -= OnPO_VersionChanged;
            _po_VersionCombo.Items.Clear();

            IEnumerable<VersionInfo> versions = _po_ShowAllVersionsFlag
                ? _allVersions
                : _allVersions.Where(v => _po_RecommendedVersions.Contains(v.FullVersion));

            foreach (var v in versions.OrderByDescending(v => Version.TryParse(v.FullVersion, out var parsed) ? parsed : new Version(0, 0)))
            {
                var item = new ComboBoxItem { Content = v.FullVersion, Tag = v.FullVersion };
                _po_VersionCombo.Items.Add(item);
            }

            _po_VersionCombo.SelectionChanged += OnPO_VersionChanged;
            PO_SelectVersion(_po_SelectedVersion);
        }

        private void PO_SelectVersion(string version)
        {
            if (_po_VersionCombo == null) return;
            foreach (var item in _po_VersionCombo.Items.OfType<ComboBoxItem>())
            {
                if (item.Tag?.ToString() == version)
                {
                    _po_VersionCombo.SelectedItem = item;
                    return;
                }
            }
            if (_po_VersionCombo.ItemCount > 0)
                _po_VersionCombo.SelectedIndex = 0;
        }

        private void PO_UpdateSupportBanner(string version)
        {
            if (_po_SupportBanner == null) return;
            var info = _allVersions.FirstOrDefault(v => v.FullVersion == version);
            _po_SupportBanner.IsVisible = true;

            bool isFull   = info?.IsLeafClientModSupported == true;
            bool isFabric = Version.TryParse(version, out var sv) && sv >= new Version(1, 14);

            string bgHex, dotHex, titleHex, titleText, subText;
            if (isFull)
            {
                bgHex = "#0B1E0F"; dotHex = "#4CAF50"; titleHex = "#56D060";
                titleText = "Full Support";
                subText   = "All launcher features, mods and shaders are available.";
            }
            else if (isFabric)
            {
                bgHex = "#1A1500"; dotHex = "#FFC107"; titleHex = "#FFD040";
                titleText = "Partial Support";
                subText   = "Fabric mods work but some launcher features may be limited.";
            }
            else
            {
                bgHex = "#1A0800"; dotHex = "#FF5722"; titleHex = "#FF7040";
                titleText = "Vanilla Only";
                subText   = "No Fabric mod support on this version — vanilla gameplay only.";
            }

            _po_SupportBanner.Background = SolidColorBrush.Parse(bgHex);
            _po_SupportBanner.BorderBrush = SolidColorBrush.Parse(dotHex);
            _po_SupportBanner.BorderThickness = new Thickness(0, 0, 0, 0);

            // Dot + its container
            if (_po_SupportDot != null)
            {
                _po_SupportDot.Fill = SolidColorBrush.Parse(dotHex);
                // Tint the container border (parent of the dot) with 20% alpha version of dot color
                if (_po_SupportDot.Parent is Border dotContainer)
                {
                    try
                    {
                        var dc = Color.Parse(dotHex);
                        dotContainer.Background = new SolidColorBrush(
                            Color.FromArgb(50, dc.R, dc.G, dc.B));
                    }
                    catch { /* leave default */ }
                }
            }

            if (_po_SupportTitle != null)
            {
                _po_SupportTitle.Text       = titleText;
                _po_SupportTitle.Foreground = SolidColorBrush.Parse(titleHex);
            }
            if (_po_SupportSub != null)
                _po_SupportSub.Text = subText;
        }

        private void PO_UpdateModsSupportBanner(string version)
        {
            if (_po_ModsSupportBanner == null || _po_ModsSupportText == null) return;
            bool isFabric = Version.TryParse(version, out var sv) && sv >= new Version(1, 14);
            if (isFabric)
            {
                _po_ModsSupportBanner.Background = SolidColorBrush.Parse("#0D2010");
                _po_ModsSupportText.Text = $"Minecraft {version} supports Fabric mods";
                _po_ModsSupportText.Foreground = SolidColorBrush.Parse("#4CAF50");
            }
            else
            {
                _po_ModsSupportBanner.Background = SolidColorBrush.Parse("#1A1500");
                _po_ModsSupportText.Text = $"Minecraft {version} — vanilla only, Fabric mods not available";
                _po_ModsSupportText.Foreground = SolidColorBrush.Parse("#FFC107");
            }
        }

        private void PO_PopulatePresetBadges()
        {
            void FillBadges(WrapPanel? panel, string[] mods, string bgHex, string fgHex)
            {
                if (panel == null) return;
                panel.Children.Clear();
                foreach (var mod in mods)
                {
                    var b = new Border
                    {
                        CornerRadius = new CornerRadius(5),
                        Padding = new Thickness(7, 3),
                        Margin = new Thickness(0, 0, 5, 4)
                    };
                    try { b.Background = SolidColorBrush.Parse(bgHex); } catch { b.Background = GetBrush("HoverBackgroundBrush"); }
                    IBrush fg;
                    try { fg = SolidColorBrush.Parse(fgHex); } catch { fg = GetBrush("SecondaryForegroundBrush"); }
                    b.Child = new TextBlock { Text = mod, FontSize = 10, Foreground = fg };
                    panel.Children.Add(b);
                }
            }

            FillBadges(_po_BadgesBalanced, _po_PresetMods["balanced"], "#1E1030", "#B080FF");
            FillBadges(_po_BadgesEnhanced, _po_PresetMods["enhanced"], "#0D2018", "#60DDA0");
            FillBadges(_po_BadgesLite,     _po_PresetMods["lite"],     "#1E1E08", "#D4C84A");
        }

        private void PO_ApplyPresetHighlight(string preset)
        {
            var cards = new Dictionary<string, Border?>
            {
                ["balanced"] = _po_PresetBalanced,
                ["enhanced"] = _po_PresetEnhanced,
                ["lite"]     = _po_PresetLite
            };
            foreach (var kvp in cards)
            {
                if (kvp.Value == null) continue;
                if (kvp.Key == preset)
                {
                    kvp.Value.BorderBrush     = SolidColorBrush.Parse("#9333EA");
                    kvp.Value.BorderThickness = new Thickness(2);
                    kvp.Value.Background      = SolidColorBrush.Parse("#150D20");
                }
                else
                {
                    kvp.Value.BorderThickness = new Thickness(1.5);
                    kvp.Value.BorderBrush     = SolidColorBrush.Parse("#1C2A38");
                    kvp.Value.Background      = SolidColorBrush.Parse("#0F1A24");
                }
            }
        }

        // ── Overlay event handlers ──

        private void OnPO_NameChanged(object? sender, TextChangedEventArgs e)
        {
            if (_po_AvatarLetter != null && _po_NameBox != null)
            {
                string name = _po_NameBox.Text?.Trim() ?? "";
                _po_AvatarLetter.Text = !string.IsNullOrWhiteSpace(name) ? name.Substring(0, 1).ToUpperInvariant() : "N";
            }
        }

        private void OnProfileOverlayTabClick(object? sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string t && int.TryParse(t, out int idx))
                PO_SwitchTab(idx);
        }

        private void OnPO_VersionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_po_VersionCombo?.SelectedItem is ComboBoxItem item && item.Tag is string ver)
            {
                _po_SelectedVersion = ver;
                PO_UpdateSupportBanner(ver);
                PO_UpdateModsSupportBanner(ver);
            }
        }

        private void OnPO_ShowAllVersionsToggle(object? sender, RoutedEventArgs e)
        {
            _po_ShowAllVersionsFlag = _po_ShowAllVersions?.IsChecked == true;
            PO_PopulateVersionDropdown();
        }

        private void OnPO_MemoryChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            double val = Math.Round(e.NewValue * 2) / 2.0;
            if (_po_MemLabel != null) _po_MemLabel.Text = $"{val:0.0} GB";
        }

        private void OnPO_PresetTapped(object? sender, TappedEventArgs e)
        {
            if (sender is Border b && b.Tag is string preset)
            {
                _po_SelectedPreset = preset;
                PO_ApplyPresetHighlight(preset);
            }
        }

        private void OnProfileOverlayClose(object? sender, RoutedEventArgs e)  => HideProfileOverlay();
        private void OnProfileOverlayCancel(object? sender, RoutedEventArgs e) => HideProfileOverlay();

        private async void OnProfileOverlaySave(object? sender, RoutedEventArgs e)
        {
            string name = _po_NameBox?.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(name)) name = "New Profile";

            double memory = _po_MemSlider != null ? Math.Round(_po_MemSlider.Value * 2) / 2.0 : 3.0;

            string[] accentColors = { "#7B2CBF", "#00C853", "#2196F3", "#FF5722", "#E91E63", "#009688", "#FF9800" };
            string accent = _po_EditingProfile?.AccentColor
                ?? accentColors[new Random().Next(accentColors.Length)];

            var profile = new LauncherProfile
            {
                Id                = _po_EditingProfile?.Id ?? Guid.NewGuid().ToString(),
                Name              = name,
                MinecraftVersion  = _po_SelectedVersion,
                AccountSetting    = _po_AccountCombo?.SelectedItem is ComboBoxItem ai ? ai.Tag?.ToString() ?? "active" : "active",
                JavaSetting       = _po_JavaCombo?.SelectedItem is ComboBoxItem ji ? ji.Tag?.ToString() ?? "bundled" : "bundled",
                AllocatedMemoryGb = memory,
                ModPreset         = _po_SelectedPreset,
                AccentColor       = accent,
                CreatedDate       = _po_EditingProfile?.CreatedDate ?? DateTime.Now,

                // Preserve fields not exposed in the editor yet + runtime counters
                CustomJavaPath  = _po_EditingProfile?.CustomJavaPath,
                LaunchCount     = _po_EditingProfile?.LaunchCount ?? 0,
                PlaytimeSeconds = _po_EditingProfile?.PlaytimeSeconds ?? 0,
                LastUsed        = _po_EditingProfile?.LastUsed ?? DateTime.MinValue,

                // ── Advanced-tab override fields (editable) ──
                Description                    = _po_DescriptionBox?.Text?.Trim() ?? "",
                IconEmoji                      = _po_IconEmojiBox?.Text?.Trim() ?? "",
                JvmArgumentsOverride           = NullIfBlank(_po_JvmArgsBox?.Text),
                UseCustomResolutionOverride    = _po_UseCustomResolutionToggle?.IsChecked == true
                    ? true
                    : (bool?)null,
                GameResolutionWidthOverride    = ParseNullableInt(_po_ResWidthBox?.Text),
                GameResolutionHeightOverride   = ParseNullableInt(_po_ResHeightBox?.Text),
                QuickJoinServerAddressOverride = NullIfBlank(_po_QuickJoinAddressBox?.Text),
                QuickJoinServerPortOverride    = NullIfBlank(_po_QuickJoinPortBox?.Text),
            };

            // If the resolution toggle is off, clear the override dimensions so the
            // launcher falls back to global settings.
            if (profile.UseCustomResolutionOverride != true)
            {
                profile.GameResolutionWidthOverride  = null;
                profile.GameResolutionHeightOverride = null;
            }

            if (_currentSettings.Profiles == null)
                _currentSettings.Profiles = new List<LauncherProfile>();

            if (_po_EditingProfile == null)
            {
                _currentSettings.Profiles.Add(profile);
                if (_currentSettings.ActiveProfileId == null)
                    _currentSettings.ActiveProfileId = profile.Id;
            }
            else
            {
                int idx = _currentSettings.Profiles.FindIndex(p => p.Id == _po_EditingProfile.Id);
                if (idx >= 0) _currentSettings.Profiles[idx] = profile;
            }

            await _settingsService.SaveSettingsAsync(_currentSettings);
            HideProfileOverlay();
            RefreshProfilesPage();
            UpdateLaunchVersionText();
        }

        // ─────────────────────────────────────────────────────────────────────────
        // PROFILES PAGE
        // ─────────────────────────────────────────────────────────────────────────

        private void RefreshProfilesPage()
        {
            if (_profilesListPanel == null || _noProfilesMessage == null) return;

            _profilesListPanel.Children.Clear();

            var profiles = _currentSettings.Profiles;
            if (profiles == null || profiles.Count == 0)
            {
                _noProfilesMessage.IsVisible = true;
                return;
            }

            _noProfilesMessage.IsVisible = false;
            foreach (var profile in profiles)
            {
                var card = BuildProfileCard(profile);
                _profilesListPanel.Children.Add(card);
            }

            UpdateLaunchVersionText();
        }



        private Border BuildProfileCard(LauncherProfile profile)
        {
            bool isActive = _currentSettings.ActiveProfileId == profile.Id;

            // Parse accent color safely
            IBrush accentBrush;
            Color accentColor;
            try
            {
                accentColor = Color.Parse(profile.AccentColor ?? "#7B2CBF");
                accentBrush = new SolidColorBrush(accentColor);
            }
            catch
            {
                accentColor = Color.Parse("#7B2CBF");
                accentBrush = GetBrush("PrimaryAccentBrush");
            }

            // Subtle tinted background for active card
            IBrush cardBg;
            if (isActive)
            {
                var tinted = Color.FromArgb(22, accentColor.R, accentColor.G, accentColor.B);
                cardBg = new SolidColorBrush(tinted);
            }
            else
            {
                cardBg = GetBrush("CardBackgroundColor");
            }

            // ── Outer card ──
            var card = new Border
            {
                Background = cardBg,
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 0, 10),
                Tag = profile.Id,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                ClipToBounds = true,
                BorderBrush = isActive ? accentBrush : GetBrush("PrimaryBorderBrush"),
                BorderThickness = isActive ? new Thickness(1.5) : new Thickness(1)
            };

            // ── Root grid: 3 rows — content | separator | action bar ──
            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // ════════════════════ ROW 0 — main content ════════════════════
            var contentGrid = new Grid { Margin = new Thickness(18, 16, 18, 14) };
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });        // avatar
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // info
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });        // checkmark / set-active
            Grid.SetRow(contentGrid, 0);
            rootGrid.Children.Add(contentGrid);

            // ── Avatar ──
            string initials = string.IsNullOrWhiteSpace(profile.Name) ? "?" : profile.Name[0].ToString().ToUpper();
            var avatarBorder = new Border
            {
                Width = 54, Height = 54,
                CornerRadius = new CornerRadius(14),
                Margin = new Thickness(0, 0, 16, 0),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Background = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint   = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops =
                    {
                        new GradientStop(accentColor, 0),
                        new GradientStop(Color.FromArgb(170, accentColor.R, accentColor.G, accentColor.B), 1)
                    }
                }
            };
            if (isActive)
            {
                avatarBorder.BoxShadow = new BoxShadows(new BoxShadow
                {
                    Blur = 16,
                    Color = Color.FromArgb(90, accentColor.R, accentColor.G, accentColor.B)
                });
            }
            avatarBorder.Child = new TextBlock
            {
                Text = initials,
                Foreground = Brushes.White,
                FontSize = 22,
                FontWeight = FontWeight.Black,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(avatarBorder, 0);
            contentGrid.Children.Add(avatarBorder);

            // ── Info stack ──
            var infoStack = new StackPanel
            {
                Spacing = 4,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            // Name row (name + ACTIVE pill)
            var nameRow = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8 };
            nameRow.Children.Add(new TextBlock
            {
                Text = profile.Name,
                Foreground = GetBrush("PrimaryForegroundBrush"),
                FontWeight = FontWeight.Bold,
                FontSize = 15,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            });
            if (isActive)
            {
                var activePill = new Border
                {
                    Background = accentBrush,
                    CornerRadius = new CornerRadius(20),
                    Padding = new Thickness(8, 2),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                activePill.Child = new TextBlock
                {
                    Text = "ACTIVE",
                    Foreground = Brushes.White,
                    FontSize = 9,
                    FontWeight = FontWeight.Bold,
                    LetterSpacing = 1
                };
                nameRow.Children.Add(activePill);
            }
            infoStack.Children.Add(nameRow);

            // Account type line
            string accountTypeLabel = _currentSettings?.AccountType switch
            {
                "microsoft" => "Microsoft Account",
                "local"     => "Local / Offline",
                _           => "Local / Offline"
            };
            infoStack.Children.Add(new TextBlock
            {
                Text = accountTypeLabel,
                Foreground = GetBrush("SecondaryForegroundBrush"),
                FontSize = 11,
                Margin = new Thickness(0, 1, 0, 4)
            });

            // Badges row
            var badgesRow = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 5 };

            void AddBadge(string text, string bgHex, string fgHex)
            {
                var b = new Border { CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 3) };
                try { b.Background = SolidColorBrush.Parse(bgHex); } catch { b.Background = GetBrush("HoverBackgroundBrush"); }
                IBrush fg;
                try { fg = SolidColorBrush.Parse(fgHex); } catch { fg = GetBrush("SecondaryForegroundBrush"); }
                b.Child = new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeight.SemiBold, Foreground = fg };
                badgesRow.Children.Add(b);
            }

            AddBadge($"MC {profile.MinecraftVersion}", "#161C2E", "#7AABFF");
            string presetLabel = profile.ModPreset switch
            {
                "balanced" => "⚡ Balanced",
                "enhanced" => "🚀 Enhanced",
                "lite"     => "🌿 Lite",
                _          => "◯ Vanilla"
            };
            AddBadge(presetLabel, "#1C1830", "#A880FF");
            AddBadge($"{profile.AllocatedMemoryGb:0.#} GB RAM", "#0F2218", "#5DDFA0");

            // Per-profile stats badges: only shown once the profile has actually been used
            if (profile.LaunchCount > 0)
            {
                AddBadge($"🎮 {profile.LaunchCount} launches", "#1A1520", "#F0A4FF");
                if (profile.PlaytimeSeconds > 0)
                {
                    AddBadge($"⏱ {FormatPlaytimeShort(profile.PlaytimeSeconds)}", "#201A10", "#F5C56B");
                }
            }

            infoStack.Children.Add(badgesRow);

            Grid.SetColumn(infoStack, 1);
            contentGrid.Children.Add(infoStack);

            // ── Right col: checkmark circle (active) or "Set Active" button ──
            var rightCol = new StackPanel
            {
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Margin = new Thickness(12, 0, 0, 0)
            };

            if (isActive)
            {
                // Checkmark circle
                var checkCircle = new Border
                {
                    Width = 34, Height = 34,
                    CornerRadius = new CornerRadius(17),
                    Background = accentBrush
                };
                checkCircle.Child = new TextBlock
                {
                    Text = "✓",
                    Foreground = Brushes.White,
                    FontSize = 16,
                    FontWeight = FontWeight.Bold,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Center
                };
                rightCol.Children.Add(checkCircle);
            }
            else
            {
                var setBtn = new Button
                {
                    Content = "Set Active",
                    Background = GetBrush("HoverBackgroundBrush"),
                    Foreground = GetBrush("PrimaryForegroundBrush"),
                    BorderBrush = GetBrush("PrimaryBorderBrush"),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(14, 7),
                    FontSize = 12,
                    FontWeight = FontWeight.SemiBold,
                    Cursor = new Cursor(StandardCursorType.Hand)
                };
                setBtn.Click += (_, _) => OnSetActiveProfile(profile);
                rightCol.Children.Add(setBtn);
            }

            Grid.SetColumn(rightCol, 2);
            contentGrid.Children.Add(rightCol);

            // ════════════════════ ROW 1 — separator ════════════════════
            var separator = new Border
            {
                Height = 1,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                Background = isActive ? new SolidColorBrush(Color.FromArgb(50, accentColor.R, accentColor.G, accentColor.B))
                                      : GetBrush("PrimaryBorderBrush"),
                Margin = new Thickness(18, 0)
            };
            Grid.SetRow(separator, 1);
            rootGrid.Children.Add(separator);

            // ════════════════════ ROW 2 — action bar ════════════════════
            var actionBar = new Grid { Margin = new Thickness(8, 6, 8, 8) };
            actionBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            actionBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetRow(actionBar, 2);
            rootGrid.Children.Add(actionBar);

            // Edit button (left, text)
            var editBtn = new Button
            {
                Content = "✎  Edit",
                Background = Brushes.Transparent,
                Foreground = GetBrush("SecondaryForegroundBrush"),
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 7),
                FontSize = 12,
                FontWeight = FontWeight.SemiBold,
                Cursor = new Cursor(StandardCursorType.Hand),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
            };
            editBtn.Click += (_, _) => OnEditProfile(profile);
            Grid.SetColumn(editBtn, 0);
            actionBar.Children.Add(editBtn);

            // Icon button group (right side)
            var iconGroup = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            Border MakeIconChip(string icon, string tooltip, IBrush fg, Action onClick)
            {
                var iconText = new TextBlock
                {
                    Text = icon,
                    FontSize = 13,
                    Foreground = fg,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                };
                var chip = new Border
                {
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(12, 9, 12, 7),
                    Background = SolidColorBrush.Parse("#0F1A24"),
                    BorderBrush = SolidColorBrush.Parse("#1C2A38"),
                    BorderThickness = new Thickness(1),
                    Cursor = new Cursor(StandardCursorType.Hand),
                    MinWidth = 38,
                    MinHeight = 34,
                    Child = iconText
                };
                ToolTip.SetTip(chip, tooltip);
                chip.Tapped += (_, _) => onClick();
                return chip;
            }

            iconGroup.Children.Add(MakeIconChip("\U0001F4C1", "Open profile folder", GetBrush("SecondaryForegroundBrush"), () =>
            {
                try
                {
                    string folderPath = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        ".minecraft", "profiles", profile.Id);
                    System.IO.Directory.CreateDirectory(folderPath);
                    LeafClient.Utils.SystemUtil.OpenFolder(folderPath);
                }
                catch { }
            }));

            iconGroup.Children.Add(MakeIconChip("\U0001F5D1", "Delete profile", SolidColorBrush.Parse("#FF6B6B"), () => OnDeleteProfile(profile)));

            Grid.SetColumn(iconGroup, 1);
            actionBar.Children.Add(iconGroup);

            card.Child = rootGrid;
            return card;
        }

        private void OnNewProfileClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ShowProfileOverlay(null);
        }

        // Pointer-pressed variant used by the new Border-based "NEW PROFILE" pill.
        private void OnNewProfileClickBorder(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            e.Handled = true;
            ShowProfileOverlay(null);
        }

        private void OnEditProfile(LeafClient.Models.LauncherProfile profile)
        {
            ShowProfileOverlay(profile);
        }

        private async void OnDeleteProfile(LeafClient.Models.LauncherProfile profile)
        {
            _currentSettings.Profiles.RemoveAll(p => p.Id == profile.Id);
            if (_currentSettings.ActiveProfileId == profile.Id)
                _currentSettings.ActiveProfileId = _currentSettings.Profiles.Count > 0
                    ? _currentSettings.Profiles[0].Id : null;
            await _settingsService.SaveSettingsAsync(_currentSettings);
            RefreshProfilesPage();
        }

        private async void OnSetActiveProfile(LeafClient.Models.LauncherProfile profile)
        {
            _currentSettings.ActiveProfileId = profile.Id;
            _currentSettings.SelectedSubVersion = profile.MinecraftVersion;
            await _settingsService.SaveSettingsAsync(_currentSettings);
            RefreshProfilesPage();
            UpdateLaunchVersionText();
        }

        // ─────────────────────────────────────────────────────────────────────────

        private void LoadServers()
        {
            if (_serversWrapPanel == null || _noServersMessage == null) return;

            // Featured servers
            LoadFeaturedServers();

            // Personal servers
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
            _ = UpdateServerButtonStates();
        }

        private void LoadFeaturedServers()
        {
            if (_featuredServersPanel == null) return;
            _featuredServersPanel.Children.Clear();

            var grid = new Grid { HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var configs = new[]
            {
                (_featuredServers[0], Color.Parse("#2B1A5A"), Color.Parse("#1A1040"), Color.Parse("#7B2CBF"), 1),
                (_featuredServers[1], Color.Parse("#3A1E00"), Color.Parse("#1A0D00"), Color.Parse("#B87020"), 2),
                (_featuredServers[2], Color.Parse("#4A0000"), Color.Parse("#1A0000"), Color.Parse("#CC2222"), 3),
            };

            int col = 0;
            foreach (var (server, startColor, endColor, glowColor, rank) in configs)
            {
                var gradient = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint   = new RelativePoint(1, 1, RelativeUnit.Relative),
                };
                gradient.GradientStops.Add(new GradientStop(startColor, 0));
                gradient.GradientStops.Add(new GradientStop(endColor, 1));

                var card = CreateFeaturedServerCard(server, gradient, glowColor, rank);
                Grid.SetColumn(card, col);
                grid.Children.Add(card);
                col += 2;
            }

            _featuredServersPanel.Children.Add(grid);
        }

        private Border CreateFeaturedServerCard(ServerInfo server, LinearGradientBrush gradient, Color glowColor, int rank)
        {
            // Rank accent brush — used only for the rank badge and the box-shadow
            // glow that tints the card from underneath. The card body itself is
            // pure glass to match the MainWindow aesthetic.
            var rankAccentBrush = new SolidColorBrush(Color.FromArgb(220, glowColor.R, glowColor.G, glowColor.B));

            // Green gradient matching the LAUNCH GAME button / selection indicator.
            // Every JOIN button uses this so the brand color stays consistent across
            // all three cards instead of bleeding the per-rank red/orange/purple.
            LinearGradientBrush MakeJoinBrush()
            {
                var b = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                };
                b.GradientStops.Add(new GradientStop(Color.Parse("#5CCF6B"), 0));
                b.GradientStops.Add(new GradientStop(Color.Parse("#2E8B4A"), 1));
                return b;
            }

            var card = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#CC060C14")),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(20),
                Tag = server.Id,
                BorderBrush = new SolidColorBrush(Color.Parse("#30FFFFFF")),
                BorderThickness = new Thickness(1),
                // First shadow: rank-colored glow underneath the card — subtle
                // but gives each card a distinct tint (purple/orange/red) without
                // the old high-saturation gradient fill.
                // Second shadow: soft dark drop shadow for depth.
                BoxShadow = new BoxShadows(
                    new BoxShadow
                    {
                        Color = Color.FromArgb(85, glowColor.R, glowColor.G, glowColor.B),
                        Blur = 30, OffsetX = 0, OffsetY = 8, IsInset = false
                    },
                    new[]
                    {
                        new BoxShadow
                        {
                            Color = Color.FromArgb(120, 0, 0, 0),
                            Blur = 20, OffsetX = 0, OffsetY = 6, IsInset = false
                        },
                    }),
                ClipToBounds = false
            };

            var outerStack = new StackPanel { Spacing = 0 };

            // ── Row 1: Icon + Name/Address + Rank badge ──────────────────────
            var topGrid = new Grid();
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });          // icon
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // info
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });          // rank badge

            // Icon — glass-tinted holder matching the rest of the launcher.
            var iconBorder = new Border
            {
                Width = 60, Height = 60,
                CornerRadius = new CornerRadius(12),
                ClipToBounds = true,
                Background = new SolidColorBrush(Color.Parse("#1AFFFFFF")),
                BorderBrush = new SolidColorBrush(Color.Parse("#25FFFFFF")),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 14, 0),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
            };
            var serverIcon = new Image
            {
                Name = $"FeaturedIcon_{server.Id}",
                Width = 60, Height = 60,
                Stretch = Avalonia.Media.Stretch.UniformToFill
            };
            iconBorder.Child = serverIcon;
            Grid.SetColumn(iconBorder, 0);
            topGrid.Children.Add(iconBorder);

            // Name + address
            var infoStack = new StackPanel { Spacing = 3, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
            infoStack.Children.Add(new TextBlock
            {
                Text = server.Name,
                Foreground = Brushes.White,
                FontWeight = FontWeight.ExtraBold,
                FontSize = 20,
                Margin = new Thickness(0, 2, 0, 0),
                TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
            });
            infoStack.Children.Add(new TextBlock
            {
                Text = server.Address,
                Foreground = new SolidColorBrush(Color.Parse("#99FFFFFF")),
                FontSize = 12,
                TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis
            });
            Grid.SetColumn(infoStack, 1);
            topGrid.Children.Add(infoStack);

            // Rank badge (top-right) — the only place per-rank color lives.
            var rankBadge = new Border
            {
                Background = rankAccentBrush,
                CornerRadius = new CornerRadius(20),
                Width = 34, Height = 34,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                BoxShadow = new BoxShadows(new BoxShadow
                {
                    Color = Color.FromArgb(140, glowColor.R, glowColor.G, glowColor.B),
                    Blur = 14, OffsetX = 0, OffsetY = 0, IsInset = false
                })
            };
            rankBadge.Child = new TextBlock
            {
                Text = $"#{rank}",
                Foreground = Brushes.White,
                FontWeight = FontWeight.ExtraBold,
                FontSize = 12,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(rankBadge, 2);
            topGrid.Children.Add(rankBadge);

            outerStack.Children.Add(topGrid);

            // ── Row 2: MOTD ─────────────────────────────────────────────────
            var motdText = new TextBlock
            {
                Name = $"FeaturedMotd_{server.Id}",
                Text = "",
                Foreground = new SolidColorBrush(Color.Parse("#B2FFFFFF")),
                FontSize = 12,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                Margin = new Thickness(0, 14, 0, 0),
                MinHeight = 36,
                LineHeight = 17
            };
            outerStack.Children.Add(motdText);

            // ── Row 3: Separator ─────────────────────────────────────────────
            outerStack.Children.Add(new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.Parse("#20FFFFFF")),
                Margin = new Thickness(0, 14, 0, 14)
            });

            // ── Row 4: Status bar (dot + status | players) ──────────────────
            var footerGrid = new Grid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var statusRow = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            var dot = new Ellipse
            {
                Name = $"FeaturedDot_{server.Id}",
                Width = 9, Height = 9,
                Fill = new SolidColorBrush(Color.Parse("#6B7280")),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            statusRow.Children.Add(dot);
            var statusTxt = new TextBlock
            {
                Name = $"FeaturedStatus_{server.Id}",
                Text = "Checking...",
                Foreground = new SolidColorBrush(Color.Parse("#E6FFFFFF")),
                FontSize = 13, FontWeight = FontWeight.SemiBold
            };
            statusRow.Children.Add(statusTxt);
            Grid.SetColumn(statusRow, 0);
            footerGrid.Children.Add(statusRow);

            var playerRow = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 6, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            playerRow.Children.Add(new TextBlock
            {
                Text = "👤", FontSize = 12,
                Foreground = new SolidColorBrush(Color.Parse("#99FFFFFF")),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            });
            playerRow.Children.Add(new TextBlock
            {
                Name = $"FeaturedPlayers_{server.Id}",
                Text = "—",
                Foreground = new SolidColorBrush(Color.Parse("#CCFFFFFF")),
                FontSize = 12, FontWeight = FontWeight.SemiBold,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            });
            Grid.SetColumn(playerRow, 1);
            footerGrid.Children.Add(playerRow);
            outerStack.Children.Add(footerGrid);

            // ── Row 5: JOIN button ───────────────────────────────────────────
            // Thicker vertical padding (was 11, now 16) + brand-green gradient.
            // The GlowGreen class provides the green DropShadowEffect glow
            // (8-blur at rest, 30-blur on hover), so no manual BoxShadow is
            // needed here — and Button doesn't even expose BoxShadow anyway.
            var joinBtn = new Button
            {
                Name = $"FeaturedJoin_{server.Id}",
                Content = "JOIN SERVER",
                Background = MakeJoinBrush(),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(0, 13),
                MinHeight = 0,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                FontWeight = FontWeight.Bold,
                FontSize = 14,
                Cursor = new Cursor(StandardCursorType.Hand),
                IsEnabled = false,
                Margin = new Thickness(0, 18, 0, 0),
            };
            joinBtn.Classes.Add("ColorBtn");
            joinBtn.Classes.Add("LiftBtn");
            joinBtn.Classes.Add("GlowGreen");
            joinBtn.Click += (s, e) => JoinServer(server);
            outerStack.Children.Add(joinBtn);

            card.Child = outerStack;
            return card;
        }

        private void UpdateFeaturedServerCardUI(ServerInfo server)
        {
            if (_featuredServersPanel == null) return;

            var outerGrid = _featuredServersPanel.Children.OfType<Grid>().FirstOrDefault();
            if (outerGrid == null) return;

            var card = outerGrid.Children.OfType<Border>()
                .FirstOrDefault(b => b.Tag as string == server.Id);
            if (card?.Child is not StackPanel outerStack) return;

            // Row 0: topGrid (icon + info + rank)
            var topGrid = outerStack.Children.OfType<Grid>().FirstOrDefault();
            if (topGrid != null)
            {
                // Icon (column 0)
                var iconBorder = topGrid.Children.OfType<Border>().FirstOrDefault(b => Grid.GetColumn(b) == 0);
                if (iconBorder?.Child is Image img && !string.IsNullOrEmpty(server.IconBase64))
                {
                    try
                    {
                        var data = server.IconBase64;
                        var comma = data.IndexOf(',');
                        if (comma > 0) data = data.Substring(comma + 1);
                        var bytes = Convert.FromBase64String(data);
                        using var ms = new MemoryStream(bytes);
                        img.Source = new Avalonia.Media.Imaging.Bitmap(ms);
                    }
                    catch { }
                }
            }

            // Row 1: MOTD TextBlock (first plain TextBlock in outerStack)
            var motd = outerStack.Children.OfType<TextBlock>().FirstOrDefault();
            if (motd != null)
                motd.Text = string.IsNullOrEmpty(server.Motd) ? server.Address : server.Motd;

            // Row 3: footerGrid
            var footerGrid = outerStack.Children.OfType<Grid>().Skip(1).FirstOrDefault();
            if (footerGrid != null)
            {
                // Status row (column 0) — dot + text
                var statusRow = footerGrid.Children.OfType<StackPanel>()
                    .FirstOrDefault(sp => Grid.GetColumn(sp) == 0);
                if (statusRow != null)
                {
                    var dot = statusRow.Children.OfType<Ellipse>().FirstOrDefault();
                    if (dot != null) dot.Fill = server.IsOnline ? new SolidColorBrush(Color.Parse("#4CAF50")) : Brushes.Red;

                    var txt = statusRow.Children.OfType<TextBlock>().FirstOrDefault();
                    if (txt != null) txt.Text = server.IsOnline ? "Online" : "Offline";
                }

                // Players row (column 1)
                var playerRow = footerGrid.Children.OfType<StackPanel>()
                    .FirstOrDefault(sp => Grid.GetColumn(sp) == 1);
                if (playerRow != null)
                {
                    var playerTxt = playerRow.Children.OfType<TextBlock>().Skip(1).FirstOrDefault();
                    if (playerTxt != null)
                        playerTxt.Text = server.IsOnline ? $"{server.CurrentPlayers}/{server.MaxPlayers}" : "—";
                }
            }

            // Row 4: JOIN button
            var joinBtn = outerStack.Children.OfType<Button>().FirstOrDefault();
            if (joinBtn != null) joinBtn.IsEnabled = server.IsOnline;
        }




        private Border CreateServerCard(ServerInfo server)
        {
            var card = new Border
            {
                Background = GetBrush("CardBackgroundColor"),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 15),
                Tag = server.Id,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch 
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); 
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

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

            var infoStack = new StackPanel
            {
                Spacing = 5,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch 
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
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch 
            };
            infoStack.Children.Add(motdText);

            Grid.SetColumn(infoStack, 1);
            grid.Children.Add(infoStack);

            var actionStack = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 10,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right 
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
            joinButton.Classes.Add("ColorBtn");
            joinButton.Classes.Add("LiftBtn");
            joinButton.Classes.Add("GlowAccent");
            joinButton.Click += (s, e) => JoinServer(server);
            actionStack.Children.Add(joinButton);

            var menuButton = new Button
            {
                FontSize = 20, 
                Width = 40,
                Height = 40,
                Background = GetBrush("HoverBackgroundBrush"),
                CornerRadius = new CornerRadius(8),
                Cursor = new Cursor(StandardCursorType.Hand),
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Content = new TextBlock 
                {
                    Text = "⋮",
                    Foreground = GetBrush("PrimaryForegroundBrush"), 
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
            await RefreshAllServerStatusesAsync();
        }
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

            if ((dialog.Content as StackPanel)?.Children.OfType<Button>().Last() is Button okButton)
            {
                okButton.Click += (s, e) => dialog.Close();
            }

            await dialog.ShowDialog(this);
        }

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

            if ((dialog.Content as StackPanel)?.Children.OfType<Button>().Last() is Button okButton)
            {
                okButton.Click += (s, e) => dialog.Close();
            }

            await dialog.ShowDialog(this);
        }

        private void JoinServer(ServerInfo server)
        {
            if (_isLaunching || _isInstalling)
            {
                ShowGameAlreadyLaunchingDialog();
                return; 
            }

            if (!server.IsOnline)
            {
                ShowOfflineDialog(server.Name);
                return; 
            }

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

            if (!(card.Child is Grid grid))
            {
                Console.WriteLine($"[UI Update] Grid not found in card for {server.Name}");
                return;
            }

            var infoStack = grid.Children.OfType<StackPanel>()
                .FirstOrDefault(sp => Grid.GetColumn(sp) == 1);

            if (infoStack == null)
            {
                Console.WriteLine($"[UI Update] Info stack not found for {server.Name}");
                return;
            }

            var statusStack = infoStack.Children.OfType<StackPanel>()
                .FirstOrDefault(sp => sp.Orientation == Avalonia.Layout.Orientation.Horizontal);

            if (statusStack != null)
            {
                var statusDot = statusStack.Children.OfType<Ellipse>().FirstOrDefault();
                if (statusDot != null)
                {
                    statusDot.Fill = server.IsOnline ? Brushes.Green : Brushes.Red;
                }

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

            var motdText = infoStack.Children.OfType<TextBlock>()
                .Skip(1) 
                .FirstOrDefault();

            if (motdText != null)
            {
                motdText.Text = string.IsNullOrEmpty(server.Motd) ? "No description" : server.Motd;
            }

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

            var iconBorder = grid.Children.OfType<Border>()
                .FirstOrDefault(b => Grid.GetColumn(b) == 0);

            if (iconBorder != null)
            {
                var serverIcon = iconBorder.Child as Image;
                if (serverIcon != null && !string.IsNullOrEmpty(server.IconBase64))
                {
                    try
                    {
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

                // Show featured servers first, then user's custom servers
                var allQPServers = _featuredServers.Concat(_currentSettings.CustomServers);

                foreach (var server in allQPServers)
                {
                    var serverButton = new Button
                    {
                        Classes = { "IconButton" },
                        Width = 50,
                        Height = 40,
                        Tag = server, 
                        IsEnabled = true, 
                        Opacity = 1.0 
                    };

                    serverButton.Click += (s, e) => JoinServer(server);

                    serverButton.PointerEntered += OnQuickPlayServerPointerEntered;
                    serverButton.PointerExited += OnQuickPlayServerPointerExited;

                    var icon = new Image { Width = 29, Height = 29 };

                    if (!string.IsNullOrEmpty(server.IconBase64))
                    {
                        try
                        {
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
                        CornerRadius = new CornerRadius(8), 
                        ClipToBounds = true, 
                        Background = Brushes.Transparent, 
                        Child = icon
                    };

                    serverButton.Content = roundedIconBorder; 
                    quickPlayContainer.Children.Add(serverButton);
                }

                Console.WriteLine($"[QuickPlay] Refreshed bar with {_featuredServers.Count + _currentSettings.CustomServers.Count} servers");
                _ = UpdateServerButtonStates(); 
            });
        }

        private void AddServerButton_Click(object? sender, RoutedEventArgs e)
        {
            ShowAddServerOverlay();
        }

        private void ShowAddServerOverlay()
        {
            if (_addServerOverlay == null)
                BuildAddServerOverlay();

            if (_addServerNameBox != null) _addServerNameBox.Text = "";
            if (_addServerAddressBox != null) _addServerAddressBox.Text = "";
            if (_addServerPortBox != null) _addServerPortBox.Text = "25565";
            if (_addServerStatusText != null) _addServerStatusText.Text = "";
            if (_addServerSaveButton != null) _addServerSaveButton.IsEnabled = true;

            if (_addServerOverlay != null) _addServerOverlay.IsVisible = true;
            if (_mainContentGrid != null) _mainContentGrid.Effect = new BlurEffect { Radius = 7 };
        }

        private void HideAddServerOverlay()
        {
            if (_addServerOverlay != null) _addServerOverlay.IsVisible = false;
            if (_mainContentGrid != null) _mainContentGrid.Effect = null;
        }

        private void BuildAddServerOverlay()
        {
            var overlay = new Grid
            {
                Name = "AddServerOverlay",
                Background = SolidColorBrush.Parse("#AA000000"),
                ZIndex = 200
            };
            Grid.SetRowSpan(overlay, 99);
            Grid.SetColumnSpan(overlay, 99);

            var card = new Border
            {
                Name = "AddServerModalCard",
                Width = 420,
                CornerRadius = new CornerRadius(18),
                BorderBrush = SolidColorBrush.Parse("#162030"),
                BorderThickness = new Thickness(1),
                Background = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
                    GradientStops = new GradientStops
                    {
                        new GradientStop(Color.Parse("#0D1520"), 0),
                        new GradientStop(Color.Parse("#080C14"), 1)
                    }
                },
                BoxShadow = new BoxShadows(new BoxShadow
                {
                    Color = Color.Parse("#CC000000"),
                    Blur = 40,
                    OffsetX = 0,
                    OffsetY = 8
                }),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Padding = new Thickness(28, 24, 28, 28)
            };

            var stack = new StackPanel { Spacing = 14 };

            var titleRow = new Grid();
            titleRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            titleRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            var titleText = new TextBlock
            {
                Text = "Add Server",
                FontSize = 20,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };
            Grid.SetColumn(titleText, 0);

            var closeBtn = new Button
            {
                Content = "✕",
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = SolidColorBrush.Parse("#888888"),
                FontSize = 16,
                Padding = new Thickness(6),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            closeBtn.Click += (_, _) =>
            {
                HideAddServerOverlay();
                if (LeafClient.Services.TutorialService.Instance.IsRunning)
                    LeafClient.Services.TutorialService.Instance.Next();
            };
            Grid.SetColumn(closeBtn, 1);

            titleRow.Children.Add(titleText);
            titleRow.Children.Add(closeBtn);
            stack.Children.Add(titleRow);

            stack.Children.Add(new TextBlock { Text = "Server Name", Foreground = SolidColorBrush.Parse("#9BAFCA"), FontSize = 13 });
            _addServerNameBox = new TextBox
            {
                Watermark = "My Awesome Server",
                Background = SolidColorBrush.Parse("#0A1220"),
                Foreground = Brushes.White,
                BorderBrush = SolidColorBrush.Parse("#1E2D40"),
                CaretBrush = Brushes.White,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 10)
            };
            stack.Children.Add(_addServerNameBox);

            stack.Children.Add(new TextBlock { Text = "Server Address", Foreground = SolidColorBrush.Parse("#9BAFCA"), FontSize = 13 });
            _addServerAddressBox = new TextBox
            {
                Watermark = "play.example.com",
                Background = SolidColorBrush.Parse("#0A1220"),
                Foreground = Brushes.White,
                BorderBrush = SolidColorBrush.Parse("#1E2D40"),
                CaretBrush = Brushes.White,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 10)
            };
            stack.Children.Add(_addServerAddressBox);

            stack.Children.Add(new TextBlock { Text = "Port (optional, default 25565)", Foreground = SolidColorBrush.Parse("#9BAFCA"), FontSize = 13 });
            _addServerPortBox = new TextBox
            {
                Text = "25565",
                Background = SolidColorBrush.Parse("#0A1220"),
                Foreground = Brushes.White,
                BorderBrush = SolidColorBrush.Parse("#1E2D40"),
                CaretBrush = Brushes.White,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 10)
            };
            stack.Children.Add(_addServerPortBox);

            _addServerStatusText = new TextBlock
            {
                Text = "",
                Foreground = Brushes.Gray,
                FontSize = 12,
                Margin = new Thickness(0, 2, 0, 0)
            };
            stack.Children.Add(_addServerStatusText);

            var btnRow = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 10,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Margin = new Thickness(0, 6, 0, 0)
            };

            var cancelBtn = new Button
            {
                Content = "Cancel",
                Padding = new Thickness(20, 10),
                Background = SolidColorBrush.Parse("#0E1928"),
                Foreground = SolidColorBrush.Parse("#9BAFCA"),
                BorderBrush = SolidColorBrush.Parse("#1E2D40"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            cancelBtn.Click += (_, _) =>
            {
                HideAddServerOverlay();
                if (LeafClient.Services.TutorialService.Instance.IsRunning)
                    LeafClient.Services.TutorialService.Instance.Next();
            };

            _addServerSaveButton = new Button
            {
                Name = "AddServerModalSaveButton",
                Content = "Add Server",
                Padding = new Thickness(20, 10),
                Background = SolidColorBrush.Parse("#16532A"),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                CornerRadius = new CornerRadius(10),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
            };
            _addServerSaveButton.Click += AddServerOverlay_SaveClick;

            btnRow.Children.Add(cancelBtn);
            btnRow.Children.Add(_addServerSaveButton);
            stack.Children.Add(btnRow);

            card.Child = stack;
            overlay.Children.Add(card);

            overlay.PointerPressed += (s, e) =>
            {
                if (e.Source == overlay)
                    HideAddServerOverlay();
            };

            _addServerOverlay = overlay;
            overlay.IsVisible = false;

            var rootGrid = this.FindControl<Grid>("RootGrid") ?? (Content as Grid);
            rootGrid?.Children.Add(overlay);
        }

        private async void AddServerOverlay_SaveClick(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_addServerNameBox?.Text) || string.IsNullOrWhiteSpace(_addServerAddressBox?.Text))
            {
                if (_addServerStatusText != null)
                {
                    _addServerStatusText.Text = "Please enter both name and address";
                    _addServerStatusText.Foreground = Brushes.Red;
                }
                return;
            }

            if (!int.TryParse(_addServerPortBox?.Text, out int port))
                port = 25565;

            if (_addServerStatusText != null)
            {
                _addServerStatusText.Text = "Validating server...";
                _addServerStatusText.Foreground = Brushes.Orange;
            }
            if (_addServerSaveButton != null) _addServerSaveButton.IsEnabled = false;

            var serverStatus = await _serverChecker.GetServerStatusAsync(_addServerAddressBox!.Text, port);

            if (!serverStatus.IsOnline)
            {
                if (_addServerStatusText != null)
                {
                    _addServerStatusText.Text = "Server appears offline, but you can still add it";
                    _addServerStatusText.Foreground = Brushes.Orange;
                }
                await Task.Delay(1500);
            }

            var newServer = new ServerInfo
            {
                Name = _addServerNameBox!.Text,
                Address = _addServerAddressBox!.Text,
                Port = port,
                IconBase64 = serverStatus.IconData ?? ""
            };

            _currentSettings.CustomServers.Add(newServer);
            await _settingsService.SaveSettingsAsync(_currentSettings);

            HideAddServerOverlay();
            LoadServers();
            RefreshQuickPlayBar();
            await RefreshAllServerStatusesAsync();
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
                AcceptsReturn = true, 
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Top,
                TextWrapping = TextWrapping.Wrap,
                Height = 200, 
                Watermark = "e.g., -Xmx2G -Xms1G -XX:+UseG1GC",
                Text = _currentSettings.JvmArguments 
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
                MarkSettingsDirty(); 
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

        // PointerPressed variant used by the redesigned About overlay — the
        // gradient "VIEW GITHUB" pill is a Border, not a Button, so we need
        // the PointerPressed signature.
        private void OpenDeveloperGitHubBorder(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/voidZiAD",
                UseShellExecute = true
            });
            e.Handled = true;
        }

        private bool AreAnimationsEnabled()
        {
            return _currentSettings.AnimationsEnabled;
        }

        private void WireSettingsDirtyHandlers()
        {
            if (_natureThemeToggle != null)
            {
                _natureThemeToggle.Checked += (_, __) =>
                {
                    MarkSettingsDirty();
                    UpdateNatureThemeState();
                };
                _natureThemeToggle.Unchecked += (_, __) =>
                {
                    MarkSettingsDirty();
                    UpdateNatureThemeState();
                };
            }
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
                    if (_isApplyingSettings) return;
                    _currentSettings.IsOptiFineEnabled = true;
                    await _settingsService.SaveSettingsAsync(_currentSettings);
                };
                _optiFineToggle.Unchecked += async (_, __) =>
                {
                    if (_isApplyingSettings) return;
                    _currentSettings.IsOptiFineEnabled = false;
                    await _settingsService.SaveSettingsAsync(_currentSettings);
                };
            }
            if (_testModeToggle != null)
            {
                _testModeToggle.Checked += async (_, __) =>
                {
                    if (_isApplyingSettings) return;
                    _currentSettings.IsTestMode = true;
                    if (_testModePathPanel != null) _testModePathPanel.IsVisible = true;
                    await _settingsService.SaveSettingsAsync(_currentSettings);
                };
                _testModeToggle.Unchecked += async (_, __) =>
                {
                    if (_isApplyingSettings) return;
                    _currentSettings.IsTestMode = false;
                    if (_testModePathPanel != null) _testModePathPanel.IsVisible = false;
                    await _settingsService.SaveSettingsAsync(_currentSettings);
                };
            }
            if (_testModePathBox != null)
            {
                _testModePathBox.LostFocus += async (_, __) =>
                {
                    _currentSettings.TestModeModProjectPath = _testModePathBox.Text ?? _currentSettings.TestModeModProjectPath;
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
                    MarkSettingsDirty();
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
                    OnAnimationsEnabledChanged();
                };
                _animationsEnabledToggle.Unchecked += (_, __) =>
                {
                    MarkSettingsDirty();
                    OnAnimationsEnabledChanged();
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

            if (_minRamSlider != null)
            {
                _minRamSlider.ValueChanged += (s, e) => MarkSettingsDirty();
            }
            if (_maxRamSlider != null)
            {
                _maxRamSlider.ValueChanged += (s, e) => MarkSettingsDirty();
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

        private void CloseSkinStatusBanner(object? sender, RoutedEventArgs e)
        {
            if (_skinStatusBanner != null)
            {
                _skinStatusBanner.IsVisible = false;
            }
        }

        private void ShowSkinStatusBanner(string message, SkinBannerStatus status = SkinBannerStatus.Info)
        {
            if (_skinStatusBanner == null || _skinStatusBannerText == null) return;

            _skinStatusBanner.Background = status switch
            {
                SkinBannerStatus.Success => new SolidColorBrush(Color.FromRgb(34, 139, 34)), 
                SkinBannerStatus.Error => new SolidColorBrush(Color.FromRgb(220, 38, 38)), 
                SkinBannerStatus.Warning => new SolidColorBrush(Color.FromRgb(234, 179, 8)), 
                _ => new SolidColorBrush(Color.FromRgb(42, 42, 42)) 
            };

            _skinStatusBannerText.Text = message;
            _skinStatusBanner.IsVisible = true;

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
                    // 0 in the launcher UI means "Unlimited"; Minecraft uses 260 for that.
                    _optionsService.SetInt("maxFps", (int)e.NewValue == 0 ? 260 : (int)e.NewValue);
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
            if (_highContrastToggle != null)
            {
                _highContrastToggle.Checked += (s, e) => { _optionsService.SetBool("highContrast", true); _optionsService.Save(); };
                _highContrastToggle.Unchecked += (s, e) => { _optionsService.SetBool("highContrast", false); _optionsService.Save(); };
            }
            if (_playerHatToggle != null)
            {
                _playerHatToggle.Checked += (s, e) => { _optionsService.SetBool("modelPart_hat", true); _optionsService.Save(); };
                _playerHatToggle.Unchecked += (s, e) => { _optionsService.SetBool("modelPart_hat", false); _optionsService.Save(); };
            }
            if (_playerCapeToggle != null)
            {
                _playerCapeToggle.Checked += (s, e) => { _optionsService.SetBool("modelPart_cape", true); _optionsService.Save(); };
                _playerCapeToggle.Unchecked += (s, e) => { _optionsService.SetBool("modelPart_cape", false); _optionsService.Save(); };
            }
            if (_playerJacketToggle != null)
            {
                _playerJacketToggle.Checked += (s, e) => { _optionsService.SetBool("modelPart_jacket", true); _optionsService.Save(); };
                _playerJacketToggle.Unchecked += (s, e) => { _optionsService.SetBool("modelPart_jacket", false); _optionsService.Save(); };
            }
            if (_playerLeftSleeveToggle != null)
            {
                _playerLeftSleeveToggle.Checked += (s, e) => { _optionsService.SetBool("modelPart_left_sleeve", true); _optionsService.Save(); };
                _playerLeftSleeveToggle.Unchecked += (s, e) => { _optionsService.SetBool("modelPart_left_sleeve", false); _optionsService.Save(); };
            }
            if (_playerRightSleeveToggle != null)
            {
                _playerRightSleeveToggle.Checked += (s, e) => { _optionsService.SetBool("modelPart_right_sleeve", true); _optionsService.Save(); };
                _playerRightSleeveToggle.Unchecked += (s, e) => { _optionsService.SetBool("modelPart_right_sleeve", false); _optionsService.Save(); };
            }
            if (_playerLeftPantToggle != null)
            {
                _playerLeftPantToggle.Checked += (s, e) => { _optionsService.SetBool("modelPart_left_pants_leg", true); _optionsService.Save(); };
                _playerLeftPantToggle.Unchecked += (s, e) => { _optionsService.SetBool("modelPart_left_pants_leg", false); _optionsService.Save(); };
            }
            if (_playerRightPantToggle != null)
            {
                _playerRightPantToggle.Checked += (s, e) => { _optionsService.SetBool("modelPart_right_pants_leg", true); _optionsService.Save(); };
                _playerRightPantToggle.Unchecked += (s, e) => { _optionsService.SetBool("modelPart_right_pants_leg", false); _optionsService.Save(); };
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

        private async void OnAddonFabricClick(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentSettings.SelectedSubVersion)) return;

            var versionInfo = _allVersions.FirstOrDefault(v => v.FullVersion == _currentSettings.SelectedSubVersion);
            if (versionInfo != null)
            {
                versionInfo.Loader = "Fabric";
            }

            await SaveSelectedAddon(_currentSettings.SelectedSubVersion, "Fabric");
            UpdateAddonSelectionUI(_currentSettings.SelectedSubVersion);
            UpdateLaunchVersionText(); 
        }


        private async void OnAddonVanillaClick(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentSettings.SelectedSubVersion)) return;

            var versionInfo = _allVersions.FirstOrDefault(v => v.FullVersion == _currentSettings.SelectedSubVersion);
            if (versionInfo != null)
            {
                versionInfo.Loader = "Vanilla";
            }

            await SaveSelectedAddon(_currentSettings.SelectedSubVersion, "Vanilla");
            UpdateAddonSelectionUI(_currentSettings.SelectedSubVersion);
            UpdateLaunchVersionText(); 

            if (IsOptiFineForFabricSupported(_currentSettings.SelectedSubVersion))
            {
                ManageOptiFineForFabricMods(_currentSettings.SelectedSubVersion, false);
            }
        }


        private void OpenSettingsFromVersions(object? sender, RoutedEventArgs e)
        {
            _currentSelectedIndex = 4;
            SwitchToPage(4);
        }

        // ── Cosmetic preview baker (dev tool) ──────────────────────────────
        // Renders every cosmetic into 4 PNG angles (front/back/angled-L/R)
        // via SkinRendererControl.RenderFrame and writes them to the user's
        // Desktop.  Intended for producing banner/marketing artwork — not a
        // runtime-loaded asset.  Handler is wired up in Settings → About.
        private async void OnBakeCosmeticPreviewsClick(object? sender, RoutedEventArgs e)
        {
            var btn        = this.FindControl<Button>("BakeCosmeticPreviewsButton");
            var btnText    = this.FindControl<TextBlock>("BakeCosmeticPreviewsButtonText");
            var statusText = this.FindControl<TextBlock>("BakeCosmeticPreviewsStatus");

            if (btn == null || btnText == null || statusText == null) return;
            if (!btn.IsEnabled) return;

            btn.IsEnabled = false;
            btnText.Text  = "BAKING...";
            statusText.IsVisible = true;
            statusText.Text = "Fetching your Minecraft skin...";

            try
            {
                // Fetch the currently-logged-in user's skin via Mojang's public
                // session server (UUID path) with a minotar.net fallback.  This
                // same method is used by the 3D store preview, so it works on
                // any machine where the user is signed in to a Minecraft
                // account — not just the developer's PC.
                byte[]? userSkin = null;
                try { userSkin = await FetchSkinBytesAsync(); }
                catch (Exception ex) { Console.WriteLine($"[Bake] Failed to fetch user skin: {ex.Message}"); }

                statusText.Text = userSkin != null
                    ? "Rendering cosmetic previews on your skin — this can take a few seconds..."
                    : "Rendering cosmetic previews (skin fetch failed; using fallback mannequin)...";

                var result = await LeafClient.Services.CosmeticPreviewBaker.BakeAllAsync(
                    userSkinBytes: userSkin,
                    log: line => Console.WriteLine(line));

                statusText.Text =
                    $"Done — {result.ImagesWritten} PNGs written to {result.OutputDirectory}" +
                    (result.Skipped > 0 ? $"  ({result.Skipped} skipped)" : "") +
                    (result.Errors.Count > 0 ? $"  ({result.Errors.Count} errors)" : "");

                // Open the output folder in Explorer so the user can grab the
                // files immediately.
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = result.OutputDirectory,
                        UseShellExecute = true,
                    });
                }
                catch { /* best effort */ }
            }
            catch (Exception ex)
            {
                statusText.Text = $"Failed: {ex.Message}";
                Console.WriteLine($"[Bake] Failed: {ex}");
            }
            finally
            {
                btnText.Text  = "BAKE COSMETIC PREVIEWS";
                btn.IsEnabled = true;
            }
        }

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
            _minRamSlider = this.FindControl<Slider>("MinRamSlider");
            _minRamValueText = this.FindControl<TextBlock>("MinRamValueText");
            if (_minRamSlider != null)
            {
                _minRamSlider.ValueChanged += (s, e) =>
                {
                    if (_minRamValueText != null) _minRamValueText.Text = $"{e.NewValue:F0} MB";
                    MarkSettingsDirty();
                };
            }

            _maxRamSlider = this.FindControl<Slider>("MaxRamSlider");
            _maxRamValueText = this.FindControl<TextBlock>("MaxRamValueText");
            if (_maxRamSlider != null)
            {
                _maxRamSlider.ValueChanged += (s, e) =>
                {
                    double gb = e.NewValue / 1024.0;
                    if (_maxRamValueText != null) _maxRamValueText.Text = $"{gb:0.##} GB";
                    MarkSettingsDirty();
                };
            }
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
                _maxFpsSlider.Minimum = 0;
                _maxFpsSlider.Maximum = 260;

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
            _natureThemeToggle = this.FindControl<ToggleSwitch>("NatureThemeToggle");
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

        private async Task UpdateSkinPreviewsAsync(string? username, string? uuid)
        {
            if (string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(uuid))
                return;

            try
            {
                var smallPose = _skinRenderService.GetRandomSmallPoseName();
                var largePose = _skinRenderService.GetRandomLargePoseName();
                _lastSmallPose = smallPose;

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

        private async Task LoadUserInfoAsync()
        {
            try
            {
                await LoadSessionAsync();

                var settings = await _settingsService.LoadSettingsAsync();

                var username = _session?.Username ?? settings.SessionUsername;
                Console.WriteLine($"[UserInfo] Refreshing user info: username={username ?? "(none)"}, type={settings.AccountType}");
                var uuid = _session?.UUID ?? settings.SessionUuid;

                _currentUsername = username;
                _loggedIn = !string.IsNullOrWhiteSpace(uuid);

                if (_accountUsernameDisplay != null)
                    _accountUsernameDisplay.Text = (username ?? "Player").ToUpper();

                if (_accountUuidDisplay != null)
                    _accountUuidDisplay.Text = _loggedIn ? uuid : "N/A (Offline)";

                if (_playingAsUsername != null)
                    _playingAsUsername.Text = (username ?? "Player").ToUpper();

                await UpdateSkinPreviewsAsync(username ?? "Player", uuid);

                UpdateRichPresenceFromState();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading user info: {ex.Message}");
            }
        }


        private async void LinkWebsiteButton_Click(object? sender, RoutedEventArgs e)
        {
            if (!_currentSettings.IsLoggedIn || _currentSettings.AccountType != "microsoft")
            {
                await ShowAccountActionErrorDialog("Link Website is only available for Microsoft accounts.");
                return;
            }

            var uuid = _currentSettings.SessionUuid;
            var accessToken = _currentSettings.SessionAccessToken;
            if (string.IsNullOrEmpty(uuid) || string.IsNullOrEmpty(accessToken))
            {
                await ShowAccountActionErrorDialog("Your session is missing credentials. Please log out and log back in.");
                return;
            }

            if (WebLinkInlinePanel != null && AccountFooterButtonsPanel != null)
            {
                WebLinkCodeTextBox.Text = string.Empty;
                AccountFooterButtonsPanel.IsVisible = false;
                WebLinkInlinePanel.IsVisible = true;
                WebLinkCodeTextBox.Focus();
            }
        }

        private async void WebLinkConfirmButton_Click(object? sender, RoutedEventArgs e)
        {
            var code = WebLinkCodeTextBox?.Text?.Trim().ToUpperInvariant() ?? string.Empty;
            if (code.Length != 6)
            {
                ShowWebLinkResult(false, "Please enter the 6-character code from the website.");
                return;
            }

            var uuid = _currentSettings.SessionUuid;
            var accessToken = _currentSettings.SessionAccessToken;

            if (string.IsNullOrEmpty(uuid) || string.IsNullOrEmpty(accessToken))
            {
                ShowWebLinkResult(false, "Your session is missing credentials. Please log out and log back in.");
                return;
            }

            var tokenValid = await ValidateMinecraftTokenAsync(accessToken);
            if (!tokenValid)
            {
                if (!string.IsNullOrWhiteSpace(_currentSettings.MicrosoftRefreshToken))
                {
                    var refreshed = await TryDirectTokenRefreshAsync(_currentSettings.MicrosoftRefreshToken);
                    if (refreshed != null)
                    {
                        _currentSettings.SessionUuid = refreshed.UUID;
                        _currentSettings.SessionAccessToken = refreshed.AccessToken;
                        await _settingsService.SaveSettingsAsync(_currentSettings);
                        uuid = refreshed.UUID;
                        accessToken = refreshed.AccessToken;
                    }
                    else
                    {
                        ShowWebLinkResult(false, "Your session has expired. Please log out and log back in.");
                        return;
                    }
                }
                else
                {
                    ShowWebLinkResult(false, "Your session has expired. Please log out and log back in.");
                    return;
                }
            }

            var (success, error) = await LeafApiService.WebLinkCompleteAsync(code, uuid, accessToken);

            if (success)
                ShowWebLinkResult(true, "Account linked! You are now signed in on the website.");
            else
                ShowWebLinkResult(false, error ?? "Failed to link account. Check the code and try again.");
        }

        private void ShowWebLinkResult(bool success, string message)
        {
            if (WebLinkCodeEntryPanel != null) WebLinkCodeEntryPanel.IsVisible = false;
            if (WebLinkResultPanel != null) WebLinkResultPanel.IsVisible = true;
            if (WebLinkResultIcon != null)
            {
                WebLinkResultIcon.Text = success ? "✓" : "✕";
                WebLinkResultIcon.Foreground = success
                    ? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#6BDF9F"))
                    : new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FF6B6B"));
            }
            if (WebLinkResultText != null)
            {
                WebLinkResultText.Text = message;
                WebLinkResultText.Foreground = success
                    ? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#6BDF9F"))
                    : new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#FF6B6B"));
            }
        }

        private void WebLinkCancelButton_Click(object? sender, RoutedEventArgs e)
        {
            if (WebLinkInlinePanel != null) WebLinkInlinePanel.IsVisible = false;
            if (AccountFooterButtonsPanel != null) AccountFooterButtonsPanel.IsVisible = true;
            if (WebLinkCodeEntryPanel != null) WebLinkCodeEntryPanel.IsVisible = true;
            if (WebLinkResultPanel != null) WebLinkResultPanel.IsVisible = false;
            if (WebLinkCodeTextBox != null) WebLinkCodeTextBox.Text = string.Empty;
        }

        private async void LogoutButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("[MainWindow] Starting logout process...");

                string activeId = _currentSettings?.ActiveAccountId ?? "";

                // Remove the current account from saved accounts
                _currentSettings?.SavedAccounts.RemoveAll(a => a.Id == activeId);

                // Clear the current session via service
                await _sessionService.LogoutAsync();

                // Check if there are other accounts to switch to
                var remaining = _currentSettings?.SavedAccounts;
                if (remaining != null && remaining.Count > 0)
                {
                    // Switch to the first remaining account
                    var next = remaining[0];
                    Console.WriteLine($"[MainWindow] Switching to account: {next.Username} ({next.AccountType})");

                    if (_currentSettings != null)
                    {
                        _currentSettings.IsLoggedIn = true;
                        _currentSettings.AccountType = next.AccountType;
                        _currentSettings.SessionUsername = next.Username;
                        _currentSettings.SessionUuid = next.Uuid;
                        _currentSettings.SessionAccessToken = next.AccessToken;
                        _currentSettings.SessionXuid = next.Xuid;
                        _currentSettings.ActiveAccountId = next.Id;
                        _currentSettings.OfflineUsername = next.AccountType == "offline" ? next.Username : null;

                        LoadAccountState(next);

                        await _settingsService.SaveSettingsAsync(_currentSettings);
                    }

                    _session = next.AccountType == "offline"
                        ? MSession.CreateOfflineSession(next.Username)
                        : new MSession(next.Username, next.AccessToken ?? "", next.Uuid ?? "");

                    _loggedIn = true;
                    _currentUsername = next.Username;

                    PopulateAccountsListPanel();
                    await LoadUserInfoAsync();
                    _ = SyncOwnedCosmeticsFromApiAsync();
                    Console.WriteLine("[MainWindow] Switched to next account successfully");
                }
                else
                {
                    // No accounts left - show LoginWindow
                    Console.WriteLine("[MainWindow] No accounts remaining, showing LoginWindow...");

                    if (Application.Current is App app)
                    {
                        app.IsSwapToLogin = true;
                    }

                    if (_currentSettings != null)
                    {
                        _currentSettings.IsLoggedIn = false;
                        _currentSettings.AccountType = null;
                        _currentSettings.SessionUsername = null;
                        _currentSettings.SessionUuid = null;
                        _currentSettings.SessionAccessToken = null;
                        _currentSettings.SessionXuid = null;
                        _currentSettings.OfflineUsername = null;
                        _currentSettings.ActiveAccountId = null;
                        await _settingsService.SaveSettingsAsync(_currentSettings);
                    }

                    _loggedIn = false;
                    _currentUsername = null;
                    _lastSmallPose = null;
                    _session = null;

                    StopRichPresence();

                    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    {
                        this.Hide();

                        var loginWindow = new LoginWindow();

                        loginWindow.LoginCompleted += (success) =>
                        {
                            Console.WriteLine($"[MainWindow] Login completed with success: {success}");
                            if (success)
                            {
                                Dispatcher.UIThread.Post(() =>
                                {
                                    var newMainWindow = new MainWindow();
                                    newMainWindow.Show();
                                    desktop.MainWindow = newMainWindow;
                                    loginWindow.Close();
                                    this.Close();
                                });
                            }
                        };

                        loginWindow.Closed += (_, __) =>
                        {
                            if (!loginWindow.LoginSuccessful)
                            {
                                Dispatcher.UIThread.Post(() =>
                                {
                                    this.Close();
                                });
                            }
                        };

                        desktop.MainWindow = loginWindow;
                        loginWindow.Show();
                        loginWindow.Activate();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainWindow] Error during logout: {ex.Message}");
                Console.WriteLine($"[MainWindow] Stack trace: {ex.StackTrace}");
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
            if (!AreAnimationsEnabled() || !_currentSettings.EnableNewContentIndicators)
            {
                if (_promoBanner != null) { _promoBanner.Opacity = 1; if (_promoBanner.RenderTransform is TranslateTransform ttPromo) ttPromo.Y = 0; _promoBanner.IsVisible = true; }
                if (_launchSection != null) { _launchSection.Opacity = 1; if (_launchSection.RenderTransform is TranslateTransform ttLaunch) ttLaunch.Y = 0; _launchSection.IsVisible = true; }
                if (_newsSectionGrid != null) { _newsSectionGrid.Opacity = 1; if (_newsSectionGrid.RenderTransform is TranslateTransform ttNews) ttNews.Y = 0; _newsSectionGrid.IsVisible = true; }
                return;
            }

            await Task.Delay(100);
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

            if (!AreAnimationsEnabled())
            {
                _particleCts?.Cancel();
                _particles.Clear();
                _particleLayer?.Children.Clear(); 
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


        private void OnNavButtonClick(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border border && border.Tag is string tagStr && int.TryParse(tagStr, out var index))
            {
                AnimateSelectionIndicator(index);
                _currentSelectedIndex = index;
                SwitchToPage(index);

                // The pointer is still over the just-clicked button, and it's now the
                // selected one — so the indicator needs to match the button's hover
                // scale right away. Without this, the indicator sits at 1.0 while the
                // button is at 1.08 (visible gap) until the user un-hovers and re-hovers.
                if (_selectionIndicator != null && border.IsPointerOver)
                {
                    _selectionIndicator.RenderTransform =
                        Avalonia.Media.Transformation.TransformOperations.Parse("scale(1.10)");
                }
            }
        }

        private async Task<bool> EnsureMinecraftVersionInstalledAsync(string version)
        {
            try
            {
                ShowProgress(true, $"Verifying Minecraft {version}...");

                var files = await _launcher.ExtractFiles(version);
                bool allFilesPresent = files.All(f => System.IO.File.Exists(f.Path));

                if (allFilesPresent)
                {
                    Console.WriteLine($"[Install] Base Minecraft {version} already present. Skipping install.");
                    ShowProgress(false);
                    return true;
                }

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


        // Fade + subtle upward slide for every page transition.
        // Call this right after setting a page's IsVisible = true.
        private void AnimatePageIn(Avalonia.Controls.Control? page)
        {
            if (page == null) return;

            // Wire up transitions once per page (idempotent).
            if (page.Transitions == null || page.Transitions.Count == 0)
            {
                page.Transitions = new Avalonia.Animation.Transitions
                {
                    new Avalonia.Animation.DoubleTransition
                    {
                        Property = Avalonia.Visual.OpacityProperty,
                        Duration = TimeSpan.FromMilliseconds(240),
                        Easing = new Avalonia.Animation.Easings.CubicEaseOut()
                    },
                    new Avalonia.Animation.TransformOperationsTransition
                    {
                        Property = Avalonia.Visual.RenderTransformProperty,
                        Duration = TimeSpan.FromMilliseconds(280),
                        Easing = new Avalonia.Animation.Easings.CubicEaseOut()
                    }
                };
            }

            // Start state: invisible + 14px below target.
            page.Opacity = 0;
            page.RenderTransform = Avalonia.Media.Transformation.TransformOperations.Parse("translate(0px, 14px)");

            // Next UI tick: animate to final state (transitions play over the interpolation).
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                page.Opacity = 1;
                page.RenderTransform = Avalonia.Media.Transformation.TransformOperations.Parse("translate(0px, 0px)");
            }, Avalonia.Threading.DispatcherPriority.Render);
        }

        private static readonly string[] _pageNames = { "Home", "Versions", "Servers", "Mods", "Settings", "Skins", "Cosmetics", "Store", "Screenshots" };

        private async void SwitchToPage(int index)
        {
            var name = index >= 0 && index < _pageNames.Length ? _pageNames[index] : index.ToString();
            Console.WriteLine($"[Nav] Navigating to page: {name} (index {index})");
            int navToken = ++_navigationToken;

            int previousSelectedIndex = _currentSelectedIndex;
            bool isSkinsOverlay = index == 5;
            if (_gamePage != null) _gamePage.IsVisible = false;
            if (_versionsPage != null) _versionsPage.IsVisible = false;
            if (_serversPage != null) _serversPage.IsVisible = false;
            if (_modsPage != null) _modsPage.IsVisible = false;
            if (_settingsPage != null) _settingsPage.IsVisible = false;
            if (_skinsPage != null) _skinsPage.IsVisible = false;
            if (_cosmeticsPage != null) _cosmeticsPage.IsVisible = false;
            if (_storePage != null) _storePage.IsVisible = false;
            if (_screenshotsPage != null) _screenshotsPage.IsVisible = false;

            _serverStatusRefreshTimer?.Stop();

            switch (index)
            {
                case 0:
                    if (_gamePage != null) { _gamePage.IsVisible = true; AnimatePageIn(_gamePage); }
                    UpdateLaunchVersionText();
                    RefreshPlaytimeStatsCard();
                    break;

                case 1:
                    if (_versionsPage != null) { _versionsPage.IsVisible = true; AnimatePageIn(_versionsPage); }
                    RefreshProfilesPage();
                    break;

                case 2:
                    if (_serversPage != null) { _serversPage.IsVisible = true; AnimatePageIn(_serversPage); }

                    if (!_serversLoaded)
                    {
                        _serversLoaded = true;
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
                    }

                    _serverStatusRefreshTimer?.Start();
                    break;

                case 3:
                    if (_modsPage != null) { _modsPage.IsVisible = true; AnimatePageIn(_modsPage); }
                    LoadUserMods();
                    // Reset the Mods/Resource Packs sub-tab to "Mods" on entry
                    if (this.FindControl<Border>("ModsTab_Mods") is { } modsPill)
                    {
                        OnModsTabTapped(modsPill, null!);
                    }
                    break;

                case 4:
                    if (_settingsPage != null) { _settingsPage.IsVisible = true; AnimatePageIn(_settingsPage); }
                    if (!_settingsDirty) HideSettingsSaveBanner();
                    await CalculateDiskUsageAsync();
                    break;

                case 5:
                    if (_skinsPage != null) { _skinsPage.IsVisible = true; AnimatePageIn(_skinsPage); }
                    break;

                case 6:
                    if (_cosmeticsPage != null) { _cosmeticsPage.IsVisible = true; AnimatePageIn(_cosmeticsPage); }
                    _cosmeticsPage?.LoadCosmeticsPage();
                    _ = SyncOwnedCosmeticsFromApiAsync();
                    break;

                case 7:
                    if (_storePage != null) { _storePage.IsVisible = true; AnimatePageIn(_storePage); }
                    _storePage?.LoadStorePage();
                    break;

                case 8:
                    if (_screenshotsPage != null) { _screenshotsPage.IsVisible = true; AnimatePageIn(_screenshotsPage); }
                    _screenshotsPage?.LoadScreenshotsPage();
                    break;
            }

            if (navToken != _navigationToken) return;

            if (index != 4) HideSettingsSaveBanner();

            if (isSkinsOverlay)
            {
                _currentSelectedIndex = previousSelectedIndex;
                AnimateSelectionIndicator(previousSelectedIndex);
            }
            else
            {
                _currentSelectedIndex = index;
                AnimateSelectionIndicator(index);
            }
            UpdateRichPresenceFromState();
        }

        private async Task<string?> EnsureFabricProfileAsync(string version)
        {
            Console.WriteLine($"[DEBUG] EnsureFabricProfileAsync started with version: '{version}'");

            var baseOk = await EnsureMinecraftVersionInstalledAsync(version);
            if (!baseOk)
            {
                Console.WriteLine($"[Fabric] Base version {version} could not be verified.");
                return null;
            }

            string versionsPath = System.IO.Path.Combine(_minecraftFolder, "versions");
            string? foundProfile = null;

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

            try
            {
                UpdateLaunchButton("INSTALLING FABRIC...", "DeepSkyBlue");
                ShowProgress(true, $"Installing Fabric loader for Minecraft {version}...");

                foundProfile = await RunExclusiveInstall(async () =>
                    await new FabricInstaller(new HttpClient())
                        .Install(version, new MinecraftPath(_minecraftFolder)));

                ShowProgress(false);

                _currentSettings.SelectedFabricProfileName = foundProfile;
                await _settingsService.SaveSettingsAsync(_currentSettings);
                return foundProfile;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Fabric] Install failed: {ex}");
                if (ex.InnerException != null) Console.WriteLine($"[Fabric] Inner: {ex.InnerException}");

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
                    _versionDropdown.SelectionChanged -= OnSubVersionSelected; 
                    _versionDropdown.ItemsSource = subVersions.OrderByDescending(v => Version.TryParse(v.FullVersion, out var parsed) ? parsed : new Version(0, 0)).ToList();

                    if (_versionDropdown.ItemCount > 0)
                    {
                        var savedSub = _currentSettings.SelectedSubVersion;
                        var itemToSelect = _versionDropdown.Items.OfType<VersionInfo>()
                            .FirstOrDefault(item => item.FullVersion == savedSub);

                        if (itemToSelect != null)
                        {
                            _versionDropdown.SelectedItem = itemToSelect;
                        }
                        else
                        {
                            _versionDropdown.SelectedIndex = 0;
                        }

                        OnSubVersionSelected(null, null); 
                    }

                    _versionDropdown.SelectionChanged += OnSubVersionSelected; 
                }

                _currentSettings.SelectedMajorVersion = majorVersion;
            }
            UpdateRichPresenceFromState();
        }

        private async void OnSubVersionSelected(object? sender, SelectionChangedEventArgs? e)
        {
            if (_versionDropdown?.SelectedItem is VersionInfo selectedVersionInfo)
            {
                string selectedSubVersion = selectedVersionInfo.FullVersion;
                _currentSettings.SelectedSubVersion = selectedSubVersion;
                if (!_isApplyingSettings)
                    _currentSettings.ActiveProfileId = null;

                var currentVersionInfo = _allVersions.FirstOrDefault(v => v.FullVersion == selectedSubVersion);
                if (currentVersionInfo != null)
                {
                    _currentSettings.SelectedMajorVersion = currentVersionInfo.MajorVersion;

                    bool isFabricCompatible = Version.TryParse(selectedSubVersion, out Version? semver) &&
                                              semver >= new Version(1, 14);

                    string savedLoader = GetSelectedAddon(selectedSubVersion);

                    if (!isFabricCompatible)
                    {
                        savedLoader = "Vanilla";

                        if (_currentSettings.SelectedAddonByVersion == null)
                            _currentSettings.SelectedAddonByVersion = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                        _currentSettings.SelectedAddonByVersion[selectedSubVersion] = "Vanilla";
                    }

                    currentVersionInfo.Loader = savedLoader;
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
                var allVersions = JsonSerializer.Deserialize(response, JsonContext.Default.ListModrinthVersion);

                if (allVersions == null) return null;

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
                // Prefer active profile display
                var activeProfile = _currentSettings.Profiles?
                    .FirstOrDefault(p => p.Id == _currentSettings.ActiveProfileId);

                if (activeProfile != null)
                {
                    string loaderLabel = activeProfile.ModPreset switch
                    {
                        "none"     => "Vanilla",
                        "lite"     => "with Lite Mods",
                        "balanced" => "with Fabric",
                        "enhanced" => "with Enhanced Fabric",
                        _          => "with Fabric"
                    };
                    _launchVersionText.Text = $"Leaf Client {activeProfile.MinecraftVersion} {loaderLabel}";
                }
                else
                {
                    var selectedVersionInfo = _allVersions.FirstOrDefault(v => v.FullVersion == _currentSettings.SelectedSubVersion);
                    if (selectedVersionInfo != null)
                    {
                        _launchVersionText.Text = $"Leaf Client {selectedVersionInfo.FullVersion} with {selectedVersionInfo.Loader}";
                    }
                    else
                    {
                        _launchVersionText.Text = "Leaf Client";
                    }
                }
                if (!_isLaunching)
                    UpdateLaunchButton("LAUNCH GAME", "SeaGreen");
            }
            UpdateRichPresenceFromState();
        }


        private void PopulateAllVersionsData()
        {
            _allVersions.Add(new VersionInfo("1.21.11", "1.21", "Latest Release", "Fabric", "January 14, 2026", "Latest refinements and bug fixes for the Tricky Trials Update."));
            _allVersions.Add(new VersionInfo("1.21.10", "1.21", "Release", "Fabric", "December 26, 2025", "Further refinements and bug fixes for the Tricky Trials Update."));
            _allVersions.Add(new VersionInfo("1.21.9", "1.21", "Release", "Fabric", "December 19, 2025", "More bug fixes and optimizations for the Tricky Trials Update."));
            _allVersions.Add(new VersionInfo("1.21.8", "1.21", "Release", "Fabric", "December 12, 2025", "Additional bug fixes and performance improvements for the Tricky Trials Update."));
            _allVersions.Add(new VersionInfo("1.21.7", "1.21", "Release", "Fabric", "December 5, 2025", "Minor bug fixes for the Tricky Trials Update."));
            _allVersions.Add(new VersionInfo("1.21.6", "1.21", "Release", "Fabric", "November 28, 2025", "Performance and stability improvements for the Tricky Trials Update."));
            _allVersions.Add(new VersionInfo("1.21.5", "1.21", "Release", "Fabric", "November 21, 2025", "Further bug fixes for the Tricky Trials Update."));
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
            _allVersions.Add(new VersionInfo("1.8.9", "1.8", "Classic PvP", "Forge", "December 9, 2015", "Most popular PvP version, widely used for Hypixel and competitive play. Leaf Client for 1.8.9 runs on Forge."));
            _allVersions.Add(new VersionInfo("1.8.8", "1.8", "Legacy", "Vanilla", "September 28, 2015", "Bug fixes for 1.8."));
            _allVersions.Add(new VersionInfo("1.8.7", "1.8", "Legacy", "Vanilla", "July 1, 2015", "Bug fixes for 1.8."));
            _allVersions.Add(new VersionInfo("1.8.6", "1.8", "Legacy", "Vanilla", "June 25, 2015", "Bug fixes for 1.8."));
            _allVersions.Add(new VersionInfo("1.8.5", "1.8", "Legacy", "Vanilla", "June 23, 2015", "Bug fixes for 1.8."));
            _allVersions.Add(new VersionInfo("1.8.4", "1.8", "Legacy", "Vanilla", "May 21, 2015", "Bug fixes for 1.8."));
            _allVersions.Add(new VersionInfo("1.8.3", "1.8", "Legacy", "Vanilla", "February 20, 2015", "Bug fixes for 1.8."));
            _allVersions.Add(new VersionInfo("1.8.2", "1.8", "Legacy", "Vanilla", "February 19, 2015", "Bug fixes for 1.8."));
            _allVersions.Add(new VersionInfo("1.8.1", "1.8", "Legacy", "Vanilla", "November 24, 2014", "Bug fixes for 1.8."));
            _allVersions.Add(new VersionInfo("1.8", "1.8", "Release", "Vanilla", "September 2, 2014", "The Bountiful Update."));
            _allVersions.Add(new VersionInfo("1.7.10", "1.7", "Classic", "Vanilla", "June 26, 2014", "Classic modded version, widely used for legacy modpacks."));
            _allVersions.Add(new VersionInfo("1.7.9", "1.7", "Legacy", "Vanilla", "April 14, 2014", "Bug fixes for 1.7."));
            _allVersions.Add(new VersionInfo("1.7.5", "1.7", "Legacy", "Vanilla", "February 26, 2014", "Performance improvements."));
            _allVersions.Add(new VersionInfo("1.7.4", "1.7", "Legacy", "Vanilla", "December 10, 2013", "Bug fixes."));
            _allVersions.Add(new VersionInfo("1.7.2", "1.7", "Release", "Vanilla", "October 25, 2013", "The Update that Changed the World: new biomes and fishing mechanics."));

            var leafClientSupportedVersions = LeafSupportedVersions;

            foreach (var version in _allVersions)
            {
                if (leafClientSupportedVersions.Contains(version.FullVersion))
                {
                    version.IsLeafClientModSupported = true;
                    version.IsFullySupported = true;
                    version.SupportLevel = SupportLevel.FullSupport;
                }
                else
                {
                    // Set appropriate support level for non-fully-supported versions
                    version.SupportLevel = SupportLevel.VanillaOnly;
                }
            }
        }
        private void AnimateSelectionIndicator(int index)
        {
            if (_selectionIndicator == null) return;

            bool animationsEnabled = AreAnimationsEnabled();

            if (animationsEnabled && _savedSelectionIndicatorTransitions != null)
            {
                var newTransitions = new Transitions();
                foreach (var t in _savedSelectionIndicatorTransitions) newTransitions.Add(t);
                _selectionIndicator.Transitions = newTransitions;
            }
            else
            {
                _selectionIndicator.Transitions = new Transitions();
            }

            // Map page index (tag) to visual sidebar position.
            // Visual order (left→right): Game(0), Versions(1), Servers(2), Mods(3),
            // Cosmetics(tag6→4), Store(tag7→5), Screenshots(tag8→6), Settings(tag4→7).
            // Skins (index 5) has no sidebar slot — SwitchToPage preserves the
            // previous selection instead of calling us with 5.
            int visualPosition = index switch
            {
                6 => 4,  // Cosmetics
                7 => 5,  // Store
                8 => 6,  // Screenshots
                4 => 7,  // Settings
                _ => index
            };
            double leftOffset = visualPosition * 60;

            _selectionIndicator.Opacity = 0.55;
            _selectionIndicator.Margin = new Thickness(leftOffset, 0, 0, 0);
        }


        private void PositionTooltipNextTo(Border button)
        {
            if (_sidebarHoverTooltip == null) return;

            var origin = button.TranslatePoint(new Point(0, 0), this);
            if (!origin.HasValue) return;

            // The tooltip's Bounds.Width reflects the layout from the *previous* text.
            // When we switch from a long label like "RESOURCE PACKS" to a short one
            // like "STORE" (or vice versa), that stale width makes the centering math
            // wrong. Force a measure pass with the fresh text first, then use DesiredSize.
            _sidebarHoverTooltip.InvalidateMeasure();
            _sidebarHoverTooltip.Measure(Avalonia.Size.Infinity);
            double tooltipWidth = _sidebarHoverTooltip.DesiredSize.Width;
            if (tooltipWidth <= 0)
                tooltipWidth = _sidebarHoverTooltip.Bounds.Width > 0 ? _sidebarHoverTooltip.Bounds.Width : 80;

            double buttonCenterX = origin.Value.X + (button.Bounds.Width / 2.0);
            double left = buttonCenterX - (tooltipWidth / 2.0);

            // Position tooltip below the button (Button Y + Height + Spacing)
            double top = origin.Value.Y + button.Bounds.Height + 10;

            Canvas.SetLeft(_sidebarHoverTooltip, left);
            Canvas.SetTop(_sidebarHoverTooltip, top);

            // A second positioning pass once the actual Bounds are known after layout
            // catches any rounding drift. Avalonia resolves layout on the render thread,
            // so this runs after the text has been measured with the fresh content.
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (_sidebarHoverTooltip == null) return;
                double realWidth = _sidebarHoverTooltip.Bounds.Width;
                if (realWidth <= 0) return;
                double fixedLeft = buttonCenterX - (realWidth / 2.0);
                Canvas.SetLeft(_sidebarHoverTooltip, fixedLeft);
            }, Avalonia.Threading.DispatcherPriority.Render);
        }


        private void MinimizeWindow(object? s, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeWindow(object? s, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void CloseWindow(object? s, RoutedEventArgs e)
        {
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
            _parallaxCts?.Cancel();
            KillMinecraftProcess();
            try { _onlineCountService?.DeleteHeartbeatAsync().Wait(3000); } catch { }
            _onlineCountService?.Dispose();
            if (_trayIcon != null)
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

        // ── ColorBtn shimmer sweep ─────────────────────────────────────────────
        // AddClassHandler registers at the Avalonia class level so it fires for
        // every Button instance (AXAML or C#), even though PointerEntered is a
        // direct (non-bubbling) routed event.

        private static bool _shimmerRegistered;
        private static readonly Dictionary<Button, DispatcherTimer> _shimmerTimers = new();

        private static void RegisterColorBtnShimmer()
        {
            if (_shimmerRegistered) return;
            _shimmerRegistered = true;
            Button.PointerEnteredEvent.AddClassHandler<Button>((btn, e) =>
            {
                if (btn.Classes.Contains("ColorBtn"))
                    StartButtonShimmer(btn);
            });
        }

        private static void StartButtonShimmer(Button btn)
        {
            // Stop any existing timer so rapid re-hovers don't stack.
            if (_shimmerTimers.TryGetValue(btn, out var existing))
            {
                existing.Stop();
                _shimmerTimers.Remove(btn);
            }

            var shimmer = btn.GetVisualDescendants().OfType<Border>()
                .FirstOrDefault(b => b.Name == "PART_Shimmer");
            if (shimmer == null) return;

            // Full-width overlay approach: instead of translating a fixed-pixel stripe
            // (which always looks like a thin line), we sweep gradient stop OFFSETS across
            // the [0..1] coordinate space of the full-button-width overlay.
            // Offsets outside [0..1] are valid in Avalonia — the gradient pads with the
            // nearest stop colour (Transparent), so the shimmer naturally fades in from
            // off-screen left and out off-screen right.
            const double halfW  = 0.30;                  // shimmer half-width = 30% of button
            const double startC = -(halfW + 0.05);        // center begins just off the left edge
            const double endC   = 1.0 + halfW + 0.05;    // center ends just off the right edge

            var fadeLeft  = new GradientStop(Colors.Transparent, startC - halfW);
            var bright    = new GradientStop(Color.FromArgb(210, 255, 255, 255), startC);
            var fadeRight = new GradientStop(Colors.Transparent, startC + halfW);

            shimmer.Background = new LinearGradientBrush
            {
                StartPoint    = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint      = new RelativePoint(1, 0, RelativeUnit.Relative),
                GradientStops = new GradientStops { fadeLeft, bright, fadeRight }
            };
            shimmer.RenderTransform = null;

            void SetCenter(double center)
            {
                fadeLeft.Offset  = center - halfW;
                bright.Offset    = center;
                fadeRight.Offset = center + halfW;
                shimmer.InvalidateVisual();
            }

            SetCenter(startC);

            const int sweepMs     = 550;
            const int pauseMs     = 100;
            const int totalSweeps = 2;

            int  sweepCount = 0;
            bool inPause    = false;
            var  sw         = System.Diagnostics.Stopwatch.StartNew();

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _shimmerTimers[btn] = timer;

            timer.Tick += (_, _) =>
            {
                if (sweepCount >= totalSweeps)
                {
                    SetCenter(startC); // push gradient off-screen so it disappears
                    timer.Stop();
                    _shimmerTimers.Remove(btn);
                    return;
                }

                long elapsed = sw.ElapsedMilliseconds;

                if (inPause)
                {
                    if (elapsed >= pauseMs)
                    {
                        inPause = false;
                        sw.Restart();
                        SetCenter(startC);
                    }
                    return;
                }

                if (elapsed >= sweepMs)
                {
                    sweepCount++;
                    SetCenter(startC);
                    if (sweepCount < totalSweeps)
                    {
                        inPause = true;
                        sw.Restart();
                    }
                    return;
                }

                double t      = elapsed / (double)sweepMs;
                double eased  = t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2;
                SetCenter(startC + (endC - startC) * eased);
            };

            timer.Start();
        }

        private async void OnLaunchSectionPointerEntered(object? s, PointerEventArgs e) => await AnimateScaleTransform(_launchBgScale, 1.10, 220, (cts) => _launchBgCts = cts, () => _launchBgCts);
        private async void OnLaunchSectionPointerExited(object? s, PointerEventArgs e)
        {
            await AnimateScaleTransform(_launchBgScale, 1.08, 220, (cts) => _launchBgCts = cts, () => _launchBgCts);
            // Reset parallax target to center when mouse leaves
            _parallaxTargetX = 0;
            _parallaxTargetY = 0;
        }

        private void OnLaunchSectionPointerMoved(object? s, PointerEventArgs e)
        {
            if (_launchSection == null) return;
            var pos = e.GetPosition(_launchSection);
            double w = _launchSection.Bounds.Width;
            double h = _launchSection.Bounds.Height;
            if (w <= 0 || h <= 0) return;

            // Normalize to -1..1 from center
            double nx = (pos.X / w - 0.5) * 2.0;
            double ny = (pos.Y / h - 0.5) * 2.0;

            // Max parallax offset in pixels
            const double maxOffset = 18.0;
            _parallaxTargetX = -nx * maxOffset;
            _parallaxTargetY = -ny * maxOffset;
        }

        /// <summary>
        /// Runs a smooth lerp loop on a background thread that moves the bg image
        /// toward the latest parallax target. 60fps, lerp factor ~0.06 per frame.
        /// </summary>
        private void StartParallaxLoop()
        {
            _parallaxCts?.Cancel();
            _parallaxCts = new CancellationTokenSource();
            var token = _parallaxCts.Token;

            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(16, token).ContinueWith(_ => { }); // ~60 fps, swallow cancel
                    if (token.IsCancellationRequested) break;

                    double tx = _parallaxTargetX;
                    double ty = _parallaxTargetY;
                    double cx = _parallaxCurrentX;
                    double cy = _parallaxCurrentY;

                    const double lerpFactor = 0.06;
                    double nx = cx + (tx - cx) * lerpFactor;
                    double ny = cy + (ty - cy) * lerpFactor;

                    _parallaxCurrentX = nx;
                    _parallaxCurrentY = ny;

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (_launchBgTranslate != null)
                        {
                            _launchBgTranslate.X = nx;
                            _launchBgTranslate.Y = ny;
                        }
                    });
                }
            }, token);
        }

        private void OnAnimationsEnabledChanged()
        {
            bool animationsEnabled = AreAnimationsEnabled();
            Transitions emptyTransitions = new Transitions();

            void SetTransitions(Control? control, IList<ITransition>? savedTransitions)
            {
                if (control == null) return;

                if (animationsEnabled && savedTransitions != null)
                {
                    var newTransitions = new Transitions();
                    foreach (var t in savedTransitions)
                    {
                        // To avoid CS0122, we need to re-create the transition rather than copying directly
                        // and assuming 'Property' is accessible.
                        // However, for the account panel, we're now explicitly defining it below.
                        // For other controls, if 'savedTransitions' holds public properties, this might work.
                        // For now, let's assume 'savedTransitions' is correctly constructed in InitializeControls
                        // and contains transitions that are safe to re-add.
                        newTransitions.Add(t);
                    }
                    control.Transitions = newTransitions;
                }
                else
                {
                    control.Transitions = emptyTransitions;
                }
            }

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
                    st.ScaleX = st.ScaleY = 1.0;
                }
                control.Opacity = finalOpacity;
                control.IsVisible = (finalOpacity > 0);
            }

            SetTransitions(_selectionIndicator, _savedSelectionIndicatorTransitions);
            SetTransitions(_settingsIndicator, _savedSettingsIndicatorTransitions);
            AnimateSelectionIndicator(_currentSelectedIndex);

            SetTransitions(_gameStartingBanner, _gameStartingBanner?.Transitions?.ToList());
            SetToFinalState(_gameStartingBanner, finalY: -100, finalOpacity: 0);

            SetTransitions(_promoBanner, _savedPromoBannerTransitions);
            if (animationsEnabled && _currentSettings.EnableNewContentIndicators)
            {
                SetToFinalState(_promoBanner, finalY: 0, finalOpacity: 1.0);
            }
            else
            {
                SetToFinalState(_promoBanner, finalY: 80, finalOpacity: 0);
            }

            SetTransitions(_launchSection, _savedLaunchSectionTransitions);
            SetToFinalState(_launchSection, finalY: 0, finalOpacity: 1.0);

            SetTransitions(_newsSectionGrid, _savedNewsSectionGridTransitions);
            if (animationsEnabled && _currentSettings.EnableNewContentIndicators)
            {
                SetToFinalState(_newsSectionGrid, finalY: 0, finalOpacity: 1.0);
            }
            else
            {
                SetToFinalState(_newsSectionGrid, finalY: 80, finalOpacity: 0);
            }

            SetTransitions(_launchErrorBanner, _savedLaunchErrorBannerTransitions);
            SetToFinalState(_launchErrorBanner, finalOpacity: 0);

            SetTransitions(_settingsSaveBanner, _savedSettingsSaveBannerTransitions);
            if (!animationsEnabled && _settingsDirty && _settingsSaveBanner != null)
            {
                if (_settingsSaveBanner.RenderTransform is TranslateTransform tt) tt.Y = 0;
                _settingsSaveBanner.Opacity = 1;
                _settingsSaveBanner.IsVisible = true;
            }
            else if (!animationsEnabled && !_settingsDirty && _settingsSaveBanner != null)
            {
                if (_settingsSaveBanner.RenderTransform is TranslateTransform tt) tt.Y = 80;
                _settingsSaveBanner.Opacity = 0;
                _settingsSaveBanner.IsVisible = false;
            }


            if (_accountPanel?.RenderTransform is TranslateTransform accountPanelTt)
            {
                if (animationsEnabled)
                {
                    // Restore original transitions when animations are enabled
                    if (_savedAccountPanelTransitions != null)
                    {
                        var newTransitions = new Transitions();
                        foreach (var t in _savedAccountPanelTransitions)
                        {
                            newTransitions.Add(t);
                        }
                        _accountPanel.Transitions = newTransitions;
                    }
                }
                else
                {
                    // Clear transitions when animations are disabled
                    _accountPanel.Transitions = emptyTransitions;
                }
            }
            // When animations are disabled, explicitly set the final state to off-screen and invisible
            // This ensures it starts correctly if animations are enabled later.
            SetToFinalState(_accountPanel, finalX: (_accountPanel?.Bounds.Width ?? 400), finalOpacity: 0);


            SetTransitions(_sidebarHoverTooltip, _savedTooltipTransitions);
            SetToFinalState(_sidebarHoverTooltip, finalOpacity: 0);

            SetTransitions(_quickPlayTooltip, _savedQuickPlayTooltipTransitions);
            SetToFinalState(_quickPlayTooltip, finalOpacity: 0);

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


            if (!animationsEnabled)
            {
                _particleCts?.Cancel();
                _particles.Clear();
                _particleLayer?.Children.Clear();
            }
            else
            {
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

        // ══════════════════════════════════════════════════════
        //  MAJOR UPDATE badge — animated purple gradient
        // ══════════════════════════════════════════════════════
        //
        // The badge on the NewsItem1 (UI & UX Overhaul) card uses a two-stop
        // LinearGradientBrush whose stop colors are continuously rewritten
        // from a background task. Each stop walks a 3-anchor purple cycle
        // (dark → medium → light → dark) at a slight phase offset from the
        // other, so the gradient appears to drift smoothly across the badge
        // while always staying in the purple family.
        private void StartMajorUpdateBadgeAnimation()
        {
            if (_majorUpdateBadge == null) return;
            if (_majorUpdateBadge.Background is not LinearGradientBrush brush) return;
            if (brush.GradientStops == null || brush.GradientStops.Count < 2) return;

            var stop1 = brush.GradientStops[0];
            var stop2 = brush.GradientStops[1];

            _majorUpdateBadgeCts?.Cancel();
            _majorUpdateBadgeCts = new CancellationTokenSource();
            var token = _majorUpdateBadgeCts.Token;

            // Three purple anchor colors to cycle through.
            var anchors = new[]
            {
                Color.FromRgb(0x6A, 0x1B, 0x9A), // dark purple  (Material Purple 800)
                Color.FromRgb(0xAB, 0x47, 0xBC), // medium purple (Material Purple 400)
                Color.FromRgb(0xCE, 0x93, 0xD8), // light purple  (Material Purple 200)
            };

            Task.Run(async () =>
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                const double periodSec = 3.2; // full cycle duration

                while (!token.IsCancellationRequested)
                {
                    double t = (sw.Elapsed.TotalSeconds / periodSec) % 1.0;
                    // Walk across all 3 anchors over one period (multiply by anchor count).
                    double phase1 = t * anchors.Length;
                    double phase2 = (t + 0.33) * anchors.Length;

                    Color c1 = SampleColorCycle(anchors, phase1);
                    Color c2 = SampleColorCycle(anchors, phase2);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        stop1.Color = c1;
                        stop2.Color = c2;
                    });

                    try { await Task.Delay(16, token); }
                    catch (TaskCanceledException) { break; }
                }
            }, token);
        }

        // Samples a position along a closed cycle of color anchors via linear
        // interpolation. `phase` is in [0, anchors.Length); wrapping is handled.
        private static Color SampleColorCycle(Color[] anchors, double phase)
        {
            int n = anchors.Length;
            phase = ((phase % n) + n) % n; // positive modulo
            int i = (int)phase;
            double frac = phase - i;
            Color a = anchors[i];
            Color b = anchors[(i + 1) % n];
            return Color.FromRgb(
                (byte)(a.R + (b.R - a.R) * frac),
                (byte)(a.G + (b.G - a.G) * frac),
                (byte)(a.B + (b.B - a.B) * frac));
        }


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

            CancellationTokenSource? currentCts = getCtsFunc();

            if (!AreAnimationsEnabled())
            {
                st.ScaleX = st.ScaleY = target;
                currentCts?.Cancel();
                currentCts?.Dispose();
                setCtsAction(null); 
                return;
            }

            currentCts?.Cancel();
            currentCts?.Dispose(); 

            CancellationTokenSource newCts = new CancellationTokenSource();
            setCtsAction(newCts); 
            var ct = newCts.Token;

            double start = st.ScaleX;
            double delta = target - start;

            if (Math.Abs(delta) < 0.001)
            {
                st.ScaleX = st.ScaleY = target;
                newCts.Dispose();
                setCtsAction(null); 
                return;
            }

            const int steps = 16; 
            int delayMs = durationMs / steps;

            try
            {
                for (int i = 1; i <= steps; i++)
                {
                    ct.ThrowIfCancellationRequested(); 

                    double t = (double)i / steps;
                    double eased = 1 - Math.Pow(1 - t, 3); 

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (st != null) 
                        {
                            st.ScaleX = st.ScaleY = start + (delta * eased);
                        }
                    });

                    await Task.Delay(delayMs, ct);
                }
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (st != null)
                    {
                        st.ScaleX = st.ScaleY = target; 
                    }
                });
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                newCts.Dispose();
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

            _launchBgCts?.Cancel(); 
            _launchBgCts = new CancellationTokenSource(); 
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
                if (st.ScaleX != 1.00)
                {
                    double currentScale = st.ScaleX;
                    double resetDelta = 1.00 - currentScale;
                    for (int i = 1; i <= 8; i++) 
                    {
                        double t = (double)i / 8;
                        double eased = 1 - Math.Pow(1 - t, 3);
                        st.ScaleX = st.ScaleY = currentScale + (resetDelta * eased);
                        await Task.Delay(ms / 32); 
                    }
                    st.ScaleX = st.ScaleY = 1.00; 
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

            cts?.Cancel(); 
            cts = new CancellationTokenSource(); 
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
                if (st.ScaleX != 1.00)
                {
                    double currentScale = st.ScaleX;
                    double resetDelta = 1.00 - currentScale;
                    for (int i = 1; i <= 8; i++) 
                    {
                        double t = (double)i / 8;
                        double eased = 1 - Math.Pow(1 - t, 3);
                        st.ScaleX = st.ScaleY = currentScale + (resetDelta * eased);
                        await Task.Delay(ms / 32); 
                    }
                    st.ScaleX = st.ScaleY = 1.00; 
                }
            }
            finally
            {
                if (!ct.IsCancellationRequested) { st.ScaleX = st.ScaleY = target; }
            }
        }

        private void OpenAccountPanel(object? s, RoutedEventArgs e)
        {
            OpenAccountPanelClick();
            UpdateRichPresenceFromState();
        }


        private async Task RefreshAccountPanelPoseAsync()
        {
            try
            {
                var username = _session?.Username ?? _currentSettings?.SessionUsername;
                var uuid = _session?.UUID ?? _currentSettings?.SessionUuid;

                if (string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(uuid))
                {
                    await Task.Delay(250);
                    username = _session?.Username ?? _currentSettings?.SessionUsername;
                    uuid = _session?.UUID ?? _currentSettings?.SessionUuid;
                }

                var largePose = _skinRenderService.GetRandomLargePoseName();

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

        private bool _accountPanelOpen = false;

        private void OnAccountButtonClick(object? sender, RoutedEventArgs e)
        {
            if (_accountPanelOpen)
                CloseAccountPanel();
            else
                OpenAccountPanelClick();
        }

        private void OpenAccountPanelClick()
        {
            if (_accountPanelOverlay == null || _accountPanel == null) return;
            _accountPanelOpen = true;

            // Do all heavy UI work BEFORE making the overlay visible so it
            // doesn't block the first animation frame.
            PopulateAccountsListPanel();

            // Snap to invisible without transitions
            _accountPanel.Transitions = null;
            _accountPanel.Opacity = 0;
            _accountPanelOverlay.Opacity = 0;
            _accountPanelOverlay.IsVisible = true;

            // Next render frame: restore transition and fade in
            Dispatcher.UIThread.Post(() =>
            {
                _accountPanel.Transitions = new Transitions
                {
                    new DoubleTransition { Property = Visual.OpacityProperty, Duration = TimeSpan.FromMilliseconds(180), Easing = new CubicEaseOut() }
                };
                _accountPanelOverlay.Opacity = 1;
                _accountPanel.Opacity = 1;
            }, DispatcherPriority.Render);

            _ = RefreshAccountPanelPoseAsync();
        }

        private async void CloseAccountPanel()
        {
            if (_accountPanelOverlay == null || _accountPanel == null) return;
            _accountPanelOpen = false;

            _accountPanel.Opacity = 0;
            _accountPanelOverlay.Opacity = 0;

            await Task.Delay(180);
            if (!_accountPanelOpen)
                _accountPanelOverlay.IsVisible = false;
        }

        private void PopulateAccountsListPanel()
        {
            var panel = _accountsListPanel;
            if (panel == null) return;

            // Ensure current active account is in SavedAccounts
            EnsureActiveAccountInList();

            var accounts = _currentSettings?.SavedAccounts ?? new List<AccountEntry>();
            string activeId = _currentSettings?.ActiveAccountId ?? "";

            // Build all cards first, then swap in one shot to avoid repeated layout passes
            var newCards = new List<Control>();
            foreach (var account in accounts)
            {
                newCards.Add(CreateAccountCard(account, account.Id == activeId));
            }

            if (accounts.Count == 0)
            {
                newCards.Add(new TextBlock
                {
                    Text = "No accounts added yet",
                    Foreground = SolidColorBrush.Parse("#4B5563"),
                    FontSize = 12,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20)
                });
            }

            // Suspend layout while we swap children
            panel.IsVisible = false;
            panel.Children.Clear();
            foreach (var card in newCards)
                panel.Children.Add(card);
            panel.IsVisible = true;

            // Update account type label and dot (cached refs — no FindControl traversal)
            var activeAccount = accounts.FirstOrDefault(a => a.Id == activeId);
            if (_accountTypeLabel != null)
                _accountTypeLabel.Text = activeAccount?.AccountType == "microsoft" ? "Microsoft Account" : "Offline";
            if (_accountOnlineDot != null)
                _accountOnlineDot.Fill = SolidColorBrush.Parse("#9333EA");
            if (EditSkinButton != null)
                EditSkinButton.IsVisible = activeAccount?.AccountType != "offline";
        }

        private void EnsureActiveAccountInList()
        {
            if (_currentSettings == null) return;
            if (!_currentSettings.IsLoggedIn || string.IsNullOrWhiteSpace(_currentSettings.SessionUsername)) return;

            // Check if current session is already in the saved accounts list
            bool alreadyExists = _currentSettings.SavedAccounts.Any(a =>
                a.Username == _currentSettings.SessionUsername &&
                a.AccountType == _currentSettings.AccountType);

            if (!alreadyExists)
            {
                var entry = new AccountEntry
                {
                    AccountType = _currentSettings.AccountType ?? "offline",
                    Username = _currentSettings.SessionUsername ?? "Player",
                    Uuid = _currentSettings.SessionUuid,
                    AccessToken = _currentSettings.SessionAccessToken,
                    Xuid = _currentSettings.SessionXuid
                };
                _currentSettings.SavedAccounts.Add(entry);
                _currentSettings.ActiveAccountId = entry.Id;
                _ = _settingsService.SaveSettingsAsync(_currentSettings);
            }
            else if (string.IsNullOrWhiteSpace(_currentSettings.ActiveAccountId))
            {
                var existing = _currentSettings.SavedAccounts.First(a =>
                    a.Username == _currentSettings.SessionUsername &&
                    a.AccountType == _currentSettings.AccountType);
                _currentSettings.ActiveAccountId = existing.Id;
            }
        }

        private Border CreateAccountCard(AccountEntry account, bool isActive)
        {
            var card = new Border
            {
                Background = isActive ? SolidColorBrush.Parse("#150D20") : SolidColorBrush.Parse("#0F1A24"),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14, 10),
                BorderThickness = new Thickness(isActive ? 2 : 1.5),
                BorderBrush = isActive ? SolidColorBrush.Parse("#9333EA") : SolidColorBrush.Parse("#1C2A38"),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                Tag = account.Id
            };

            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Avatar — real skin head for Microsoft accounts, letter initial for offline
            var avatar = MakeSkinHeadAvatarBorder(account, 36, 10);
            avatar.Margin = new Thickness(0, 0, 12, 0);
            Grid.SetColumn(avatar, 0);
            row.Children.Add(avatar);

            // Info
            var info = new StackPanel { Spacing = 2, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            info.Children.Add(new TextBlock
            {
                Text = account.Username,
                Foreground = Brushes.White,
                FontWeight = FontWeight.SemiBold,
                FontSize = 13
            });
            info.Children.Add(new TextBlock
            {
                Text = account.AccountType == "microsoft" ? "Microsoft" : "Offline",
                Foreground = SolidColorBrush.Parse("#6B7280"),
                FontSize = 11
            });
            Grid.SetColumn(info, 1);
            row.Children.Add(info);

            // Active checkmark or remove button
            if (isActive)
            {
                var check = new Border
                {
                    Width = 24, Height = 24,
                    CornerRadius = new CornerRadius(12),
                    Background = SolidColorBrush.Parse("#9333EA"),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                check.Child = new TextBlock
                {
                    Text = "✓", Foreground = Brushes.White, FontSize = 12, FontWeight = FontWeight.Bold,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
                };
                Grid.SetColumn(check, 2);
                row.Children.Add(check);
            }
            else
            {
                // Remove button for non-active accounts
                var removeBtn = new Button
                {
                    Content = "✕",
                    Foreground = SolidColorBrush.Parse("#6B7280"),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    FontSize = 12,
                    Padding = new Thickness(6),
                    Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Tag = account.Id
                };
                removeBtn.Click += OnRemoveAccount;
                Grid.SetColumn(removeBtn, 2);
                row.Children.Add(removeBtn);
            }

            card.Child = row;

            // Clicking a non-active card switches to it
            if (!isActive)
            {
                card.Tapped += OnSwitchAccount;
            }

            return card;
        }

        private void SaveCurrentAccountState()
        {
            if (_currentSettings == null) return;
            var entry = _currentSettings.SavedAccounts.FirstOrDefault(a => a.Id == _currentSettings.ActiveAccountId);
            if (entry == null) return;
            entry.LeafApiJwt = _currentSettings.LeafApiJwt;
            entry.LeafApiRefreshToken = _currentSettings.LeafApiRefreshToken;
            entry.OwnedCosmeticIds = new List<string>(_ownedCosmeticIds);
            if (_currentSettings.Equipped != null)
                entry.Equipped = new EquippedCosmetics
                {
                    CapeId = _currentSettings.Equipped.CapeId,
                    HatId = _currentSettings.Equipped.HatId,
                    WingsId = _currentSettings.Equipped.WingsId,
                    BackItemId = _currentSettings.Equipped.BackItemId,
                    AuraId = _currentSettings.Equipped.AuraId
                };
        }

        private void LoadAccountState(AccountEntry account)
        {
            if (_currentSettings == null) return;
            _currentSettings.LeafApiJwt = account.LeafApiJwt;
            _currentSettings.LeafApiRefreshToken = account.LeafApiRefreshToken;
            _onlineCountService?.UpdateAccessToken(account.LeafApiJwt);
            WriteSessionJson(account.LeafApiJwt);
            var sub = DecodeJwtSub(account.LeafApiJwt);
            _currentSettings.Equipped = new EquippedCosmetics
            {
                Sub = sub,
                CapeId = account.Equipped.CapeId,
                HatId = account.Equipped.HatId,
                WingsId = account.Equipped.WingsId,
                BackItemId = account.Equipped.BackItemId,
                AuraId = account.Equipped.AuraId
            };
            WriteEquippedJson(_currentSettings.Equipped, sub);
            _ownedCosmeticIds.Clear();
            foreach (var id in account.OwnedCosmeticIds)
                _ownedCosmeticIds.Add(id);
            SaveOwnedJson();
            Dispatcher.UIThread.Post(() =>
            {
                _cosmeticsPage?.RefreshOwnedList(_ownedCosmeticIds);
                _storePage?.RefreshOwnedList(_ownedCosmeticIds);
                _cosmeticsPage?.OnAccountChanged();
                _storePage?.OnAccountChanged();
            });
        }

        private async void OnSwitchAccount(object? sender, TappedEventArgs e)
        {
            if (sender is not Border b || b.Tag is not string accountId) return;
            var account = _currentSettings?.SavedAccounts.FirstOrDefault(a => a.Id == accountId);
            if (account == null || _currentSettings == null) return;
            Console.WriteLine($"[Accounts] Switching to account: {account.Username} (type: {account.AccountType})");

            SaveCurrentAccountState();

            _currentSettings.ActiveAccountId = account.Id;
            _currentSettings.AccountType = account.AccountType;
            _currentSettings.SessionUsername = account.Username;
            _currentSettings.SessionUuid = account.Uuid;
            _currentSettings.SessionAccessToken = account.AccessToken;
            _currentSettings.SessionXuid = account.Xuid;
            _currentSettings.IsLoggedIn = true;

            LoadAccountState(account);

            if (account.AccountType == "offline")
            {
                _currentSettings.OfflineUsername = account.Username;
                _session = MSession.CreateOfflineSession(account.Username);
            }
            else
            {
                _session = new MSession
                {
                    Username = account.Username,
                    UUID = account.Uuid ?? "",
                    AccessToken = account.AccessToken ?? "",
                    Xuid = account.Xuid
                };
            }

            await _settingsService.SaveSettingsAsync(_currentSettings);
            PopulateAccountsListPanel();
            await LoadUserInfoAsync();
            SwitchToPage(_currentSelectedIndex);

            if (string.IsNullOrEmpty(account.LeafApiJwt) && account.AccountType == "microsoft" &&
                !string.IsNullOrWhiteSpace(account.Uuid) && !string.IsNullOrWhiteSpace(account.AccessToken))
            {
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        var apiResult = await LeafApiService.MicrosoftLoginAsync(account.Uuid!, account.AccessToken!);
                        if (apiResult != null && _currentSettings != null)
                        {
                            _currentSettings.LeafApiJwt = apiResult.AccessToken;
                            _currentSettings.LeafApiRefreshToken = apiResult.RefreshToken;
                            account.LeafApiJwt = apiResult.AccessToken;
                            account.LeafApiRefreshToken = apiResult.RefreshToken;
                            await _settingsService.SaveSettingsAsync(_currentSettings);
                            _onlineCountService?.UpdateAccessToken(apiResult.AccessToken);
                            WriteSessionJson(apiResult.AccessToken);
                            Console.WriteLine("[Accounts] Silently re-linked Microsoft account JWT on switch.");
                            await SyncOwnedCosmeticsFromApiAsync();
                        }
                    }
                    catch (Exception ex) { Console.WriteLine($"[Accounts] Silent re-link failed: {ex.Message}"); }
                });
            }
            else
            {
                _ = SyncOwnedCosmeticsFromApiAsync();
            }
        }

        private async void OnRemoveAccount(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string accountId) return;
            if (_currentSettings == null) return;

            _currentSettings.SavedAccounts.RemoveAll(a => a.Id == accountId);
            await _settingsService.SaveSettingsAsync(_currentSettings);
            PopulateAccountsListPanel();
        }

        private System.Threading.CancellationTokenSource? _msAuthCts;

        private async void AddMicrosoftAccountClick(object? sender, RoutedEventArgs e)
        {
            var statusPanel = this.FindControl<Border>("MsAuthStatusPanel");
            var statusText = this.FindControl<TextBlock>("MsAuthStatusText");
            var statusSub = this.FindControl<TextBlock>("MsAuthSubText");
            if (statusPanel != null) statusPanel.IsVisible = true;
            if (statusText != null) statusText.Text = "Opening browser for Microsoft login...";
            if (statusSub != null) statusSub.Text = "Complete the sign-in in your browser";

            _msAuthCts = new System.Threading.CancellationTokenSource();

            try
            {
                XboxAuthNet.Game.Msal.MsalSerializationConfig.DefaultSerializerOptions = LeafClient.Json.Options;
                XboxAuthNet.JsonConfig.DefaultOptions = LeafClient.Json.Options;

                var app = await MsalClientHelper.BuildApplicationWithCache("499c8d36-be2a-4231-9ebd-ef291b7bb64c");

                string? capturedRefreshToken = null;
                app.UserTokenCache.SetAfterAccess(notification =>
                {
                    if (notification.HasStateChanged && capturedRefreshToken == null)
                    {
                        try
                        {
                            var cacheBytes = notification.TokenCache.SerializeMsalV3();
                            using var cacheDoc = System.Text.Json.JsonDocument.Parse(cacheBytes);
                            if (cacheDoc.RootElement.TryGetProperty("RefreshToken", out var rtSection))
                            {
                                foreach (var rtEntry in rtSection.EnumerateObject())
                                {
                                    if (rtEntry.Value.TryGetProperty("secret", out var secret))
                                    {
                                        capturedRefreshToken = secret.GetString();
                                        break;
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                });

                var loginHandler = JELoginHandlerBuilder.BuildDefault();
                var authenticator = loginHandler.CreateAuthenticatorWithNewAccount();

                authenticator.AddMsalOAuth(app, msal => msal.Interactive());
                authenticator.AddXboxAuthForJE(xbox => xbox.Basic());
                authenticator.AddForceJEAuthenticator();

                if (statusText != null) statusText.Text = "Waiting for browser sign-in...";

                var newSession = await authenticator.ExecuteForLauncherAsync();

                if (newSession != null && newSession.CheckIsValid())
                {
                    var entry = new AccountEntry
                    {
                        AccountType = "microsoft",
                        Username = newSession.Username,
                        Uuid = newSession.UUID,
                        AccessToken = newSession.AccessToken,
                        Xuid = newSession.Xuid
                    };

                    _currentSettings?.SavedAccounts.RemoveAll(a =>
                        a.AccountType == "microsoft" && a.Uuid == newSession.UUID);

                    _currentSettings?.SavedAccounts.Add(entry);

                    if (_currentSettings != null)
                    {
                        _currentSettings.ActiveAccountId = entry.Id;
                        _currentSettings.IsLoggedIn = true;
                        _currentSettings.AccountType = "microsoft";
                        _currentSettings.SessionUsername = newSession.Username;
                        _currentSettings.SessionUuid = newSession.UUID;
                        _currentSettings.SessionAccessToken = newSession.AccessToken;
                        _currentSettings.SessionXuid = newSession.Xuid;
                        if (!string.IsNullOrWhiteSpace(capturedRefreshToken))
                            _currentSettings.MicrosoftRefreshToken = capturedRefreshToken;
                        _currentSettings.LeafApiJwt = null;
                        _currentSettings.LeafApiRefreshToken = null;
                        _currentSettings.Equipped = new EquippedCosmetics();
                        _ownedCosmeticIds.Clear();
                        WriteEquippedJson(new EquippedCosmetics(), null);
                        SaveOwnedJson();
                        WriteSessionJson(null);
                        await _settingsService.SaveSettingsAsync(_currentSettings);
                        Dispatcher.UIThread.Post(() =>
                        {
                            _cosmeticsPage?.RefreshOwnedList(_ownedCosmeticIds);
                            _storePage?.RefreshOwnedList(_ownedCosmeticIds);
                            _cosmeticsPage?.OnAccountChanged();
                            _storePage?.OnAccountChanged();
                        });
                    }

                    _ = System.Threading.Tasks.Task.Run(async () =>
                    {
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(newSession.UUID) && !string.IsNullOrWhiteSpace(newSession.AccessToken))
                            {
                                var apiResult = await LeafApiService.MicrosoftLoginAsync(newSession.UUID, newSession.AccessToken);
                                if (apiResult != null && _currentSettings != null)
                                {
                                    _currentSettings.LeafApiJwt = apiResult.AccessToken;
                                    _currentSettings.LeafApiRefreshToken = apiResult.RefreshToken;
                                    var activeEntry = _currentSettings.SavedAccounts.FirstOrDefault(a => a.Id == _currentSettings.ActiveAccountId);
                                    if (activeEntry != null)
                                    {
                                        activeEntry.LeafApiJwt = apiResult.AccessToken;
                                        activeEntry.LeafApiRefreshToken = apiResult.RefreshToken;
                                    }
                                    await _settingsService.SaveSettingsAsync(_currentSettings);
                                    _onlineCountService?.UpdateAccessToken(apiResult.AccessToken);
                                    WriteSessionJson(apiResult.AccessToken);
                                    Console.WriteLine("[Accounts] LeafClient API linked for Microsoft account.");
                                    _ownedCosmeticIds.Clear();
                                    WriteEquippedJson(_currentSettings?.Equipped ?? new EquippedCosmetics(), DecodeJwtSub(apiResult.AccessToken));
                                    SaveOwnedJson();
                                    await SyncOwnedCosmeticsFromApiAsync();
                                }
                            }
                        }
                        catch (Exception ex2)
                        {
                            Console.WriteLine($"[Accounts] LeafClient API link failed (non-critical): {ex2.Message}");
                        }
                    });

                    _session = newSession;
                    PopulateAccountsListPanel();
                    await LoadUserInfoAsync();
                    SwitchToPage(_currentSelectedIndex);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[Accounts] Microsoft auth cancelled.");
                if (statusText != null) statusText.Text = "Sign-in cancelled.";
                if (statusSub != null) statusSub.Text = "You can try again anytime.";
                await System.Threading.Tasks.Task.Delay(1500);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Accounts] Microsoft auth failed: {ex}");
                if (statusText != null) statusText.Text = "Authentication failed. Try again.";
                if (statusSub != null) statusSub.Text = ex.Message.Length > 80 ? ex.Message[..80] + "\u2026" : ex.Message;
                await System.Threading.Tasks.Task.Delay(3000);
            }
            finally
            {
                if (statusPanel != null) statusPanel.IsVisible = false;
                _msAuthCts = null;
            }
        }

        private void OnCancelMsAuth(object? sender, RoutedEventArgs e)
        {
            _msAuthCts?.Cancel();
            var statusPanel = this.FindControl<Border>("MsAuthStatusPanel");
            if (statusPanel != null) statusPanel.IsVisible = false;
        }

        // True = "Create account" tab is selected; False = "Sign in" tab is selected.
        // Both tabs are always visible to the user — no auto-switching.
        private bool _addOfflineCreateMode = false;

        private void AddLocalAccountClick(object? sender, RoutedEventArgs e)
        {
            var panel = this.FindControl<Border>("AddOfflinePanel");
            var nameBox = this.FindControl<TextBox>("OfflineUsernameBox");
            var pwBox = this.FindControl<TextBox>("OfflinePasswordBox");
            var pwConfirmBox = this.FindControl<TextBox>("OfflineConfirmPasswordBox");
            var errBox = this.FindControl<TextBlock>("OfflineAddError");
            if (panel != null) panel.IsVisible = true;
            if (nameBox != null) { nameBox.Text = ""; nameBox.Focus(); }
            if (pwBox != null) pwBox.Text = "";
            if (pwConfirmBox != null) pwConfirmBox.Text = "";
            if (errBox != null) { errBox.IsVisible = false; errBox.Text = ""; }
            _addOfflineCreateMode = false;
            ApplyAddOfflineMode();
        }

        private void OnSelectAddOfflineSignInTab(object? sender, RoutedEventArgs e)
        {
            _addOfflineCreateMode = false;
            ApplyAddOfflineMode();
            var errBox = this.FindControl<TextBlock>("OfflineAddError");
            if (errBox != null) { errBox.IsVisible = false; errBox.Text = ""; }
        }

        private void OnSelectAddOfflineCreateTab(object? sender, RoutedEventArgs e)
        {
            _addOfflineCreateMode = true;
            ApplyAddOfflineMode();
            var errBox = this.FindControl<TextBlock>("OfflineAddError");
            if (errBox != null) { errBox.IsVisible = false; errBox.Text = ""; }
        }

        private void ApplyAddOfflineMode()
        {
            var subtext = this.FindControl<TextBlock>("AddOfflineSubtext");
            var actionText = this.FindControl<TextBlock>("OfflineAddActionText");
            var confirmBox = this.FindControl<TextBox>("OfflineConfirmPasswordBox");
            var signInTab = this.FindControl<Button>("OfflineSignInTab");
            var createTab = this.FindControl<Button>("OfflineCreateTab");
            var signInTabText = this.FindControl<TextBlock>("OfflineSignInTabText");
            var createTabText = this.FindControl<TextBlock>("OfflineCreateTabText");

            // Highlight the active tab — purple background, white text. Inactive: transparent + muted.
            var activeBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#7C3AED"));
            var inactiveBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Transparent);
            var activeFg = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.White);
            var inactiveFg = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#94A3B8"));

            if (_addOfflineCreateMode)
            {
                if (signInTab != null) signInTab.Background = inactiveBrush;
                if (createTab != null) createTab.Background = activeBrush;
                if (signInTabText != null) signInTabText.Foreground = inactiveFg;
                if (createTabText != null) createTabText.Foreground = activeFg;
                if (subtext != null)    subtext.Text    = "Pick a password to create a new Leaf account for this cracked username.";
                if (actionText != null) actionText.Text = "Create account";
                if (confirmBox != null) confirmBox.IsVisible = true;
            }
            else
            {
                if (signInTab != null) signInTab.Background = activeBrush;
                if (createTab != null) createTab.Background = inactiveBrush;
                if (signInTabText != null) signInTabText.Foreground = activeFg;
                if (createTabText != null) createTabText.Foreground = inactiveFg;
                if (subtext != null)    subtext.Text    = "Sign in with your existing Leaf account credentials.";
                if (actionText != null) actionText.Text = "Sign in";
                if (confirmBox != null) confirmBox.IsVisible = false;
            }
        }

        private void ShowAddOfflineError(string msg)
        {
            var errBox = this.FindControl<TextBlock>("OfflineAddError");
            if (errBox != null) { errBox.Text = msg; errBox.IsVisible = true; }
        }

        private async void OnAddOfflineConfirm(object? sender, RoutedEventArgs e)
        {
            var nameBox = this.FindControl<TextBox>("OfflineUsernameBox");
            var pwBox = this.FindControl<TextBox>("OfflinePasswordBox");
            var pwConfirmBox = this.FindControl<TextBox>("OfflineConfirmPasswordBox");
            var panel = this.FindControl<Border>("AddOfflinePanel");

            string name = nameBox?.Text?.Trim() ?? "";
            string password = pwBox?.Text ?? "";

            if (string.IsNullOrWhiteSpace(name)) { ShowAddOfflineError("Please enter a username."); return; }
            if (name.Length < 3 || name.Length > 16 || !System.Text.RegularExpressions.Regex.IsMatch(name, "^[A-Za-z0-9_]+$"))
            {
                ShowAddOfflineError("Username must be 3–16 characters (letters, numbers, underscores only).");
                return;
            }
            if (string.IsNullOrEmpty(password)) { ShowAddOfflineError("Please enter a password."); return; }

            LeafApiAuthResult? apiResult;
            try
            {
                if (!_addOfflineCreateMode)
                {
                    // Sign-in tab: just attempt login, no auto-fallback to create.
                    Console.WriteLine($"[Accounts] Attempting sign-in for username: {name}");
                    var (loginResult, loginError) = await LeafApiService.LoginWithErrorAsync(name, password);
                    if (loginResult == null)
                    {
                        Console.WriteLine($"[Accounts] Sign-in failed for '{name}': {loginError}");
                        ShowAddOfflineError(loginError ?? "Sign-in failed. Check your username and password.");
                        return;
                    }
                    apiResult = loginResult;
                }
                else
                {
                    // Create tab: validate confirm + strength, then register.
                    string confirm = pwConfirmBox?.Text ?? "";
                    if (password != confirm) { ShowAddOfflineError("Passwords do not match."); return; }
                    var pwErr = LeafApiService.ValidatePasswordStrength(password);
                    if (pwErr != null) { ShowAddOfflineError(pwErr); return; }

                    Console.WriteLine($"[Accounts] Attempting registration for username: {name}");
                    var (registerResult, registerError) = await LeafApiService.RegisterWithErrorAsync(name, password);
                    if (registerResult == null)
                    {
                        Console.WriteLine($"[Accounts] Registration failed for '{name}': {registerError}");
                        ShowAddOfflineError(registerError ?? "Registration failed. Try a different username.");
                        return;
                    }
                    apiResult = registerResult;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AddOffline] Leaf API call failed: {ex.Message}");
                ShowAddOfflineError("Couldn't reach the LeafClient API. Check your connection.");
                return;
            }

            var offlineSession = MSession.CreateOfflineSession(name);
            var entry = new AccountEntry
            {
                AccountType = "offline",
                Username = name,
                Uuid = offlineSession.UUID,
                LeafApiJwt = apiResult.AccessToken,
                LeafApiRefreshToken = apiResult.RefreshToken,
            };

            _currentSettings?.SavedAccounts.RemoveAll(a =>
                a.AccountType == "offline" && a.Username.Equals(name, StringComparison.OrdinalIgnoreCase));
            _currentSettings?.SavedAccounts.Add(entry);

            if (_currentSettings != null)
            {
                _currentSettings.ActiveAccountId = entry.Id;
                _currentSettings.IsLoggedIn = true;
                _currentSettings.AccountType = "offline";
                _currentSettings.OfflineUsername = name;
                _currentSettings.SessionUsername = name;
                _currentSettings.SessionUuid = offlineSession.UUID;
                _currentSettings.SessionAccessToken = null;
                _currentSettings.SessionXuid = null;
                _currentSettings.LeafApiJwt = apiResult.AccessToken;
                _currentSettings.LeafApiRefreshToken = apiResult.RefreshToken;
                _currentSettings.Equipped = new EquippedCosmetics();
                _ownedCosmeticIds.Clear();
                await _settingsService.SaveSettingsAsync(_currentSettings);
            }

            _session = offlineSession;
            _onlineCountService?.UpdateAccessToken(apiResult.AccessToken);
            WriteSessionJson(apiResult.AccessToken);
            WriteEquippedJson(new EquippedCosmetics(), DecodeJwtSub(apiResult.AccessToken));
            SaveOwnedJson();
            Dispatcher.UIThread.Post(() =>
            {
                _cosmeticsPage?.RefreshOwnedList(_ownedCosmeticIds);
                _storePage?.RefreshOwnedList(_ownedCosmeticIds);
                _cosmeticsPage?.OnAccountChanged();
                _storePage?.OnAccountChanged();
            });

            Console.WriteLine($"[Accounts] Offline account '{name}' {(_addOfflineCreateMode ? "registered" : "signed in")} successfully");
            if (panel != null) panel.IsVisible = false;
            PopulateAccountsListPanel();
            await LoadUserInfoAsync();
            _ = SyncOwnedCosmeticsFromApiAsync();
            SwitchToPage(_currentSelectedIndex);
        }

        private void OnCancelAddOffline(object? sender, RoutedEventArgs e)
        {
            var panel = this.FindControl<Border>("AddOfflinePanel");
            if (panel != null) panel.IsVisible = false;
        }

        private void OnAccountBackdropClick(object? sender, PointerPressedEventArgs e)
        {
            CloseAccountPanel();
        }

        private void OnAccountPanelCloseClick(object? sender, RoutedEventArgs e)
        {
            CloseAccountPanel();
        }

        private void OnAccountPanelBackdropPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            CloseAccountPanel();
        }

        private void CloseAccountPanelImmediate(object? sender, RoutedEventArgs e)
        {
            _accountPanelCloseCts?.Cancel();
            if (_accountPanel != null)
            {
                var saved = _accountPanel.Transitions;
                _accountPanel.Transitions = null;
                _accountPanel.Opacity = 0;
                if (_accountPanel.RenderTransform is ScaleTransform st)
                    { st.ScaleX = 0.94; st.ScaleY = 0.94; }
                _accountPanel.Transitions = saved;
            }
            if (_accountPanelOverlay != null)
                _accountPanelOverlay.IsVisible = false;
            _accountPanelOpen = false;
        }

        private async void CopyUuidToClipboard(object? sender, PointerPressedEventArgs e)
        {
            if (_accountUuidDisplay?.Text is string uuid)
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel?.Clipboard != null)
                {
                    await topLevel.Clipboard.SetTextAsync(uuid);
                    ShowSkinStatusBanner("UUID copied to clipboard!", SkinBannerStatus.Success);
                }
            }
        }

        private static void OpenDiscord(object? s, RoutedEventArgs e) =>
            Process.Start(new ProcessStartInfo { FileName = "https://discord.gg/wvYqKpUNdb", UseShellExecute = true });
        private static void OpenWebsite(object? s, RoutedEventArgs e) =>
            Process.Start(new ProcessStartInfo { FileName = "https://leafclient.com", UseShellExecute = true });
        private static void OpenInstagram(object? s, RoutedEventArgs e) =>
            Process.Start(new ProcessStartInfo { FileName = "https://instagram.com/leafclient", UseShellExecute = true });


        private async void LoadAndApplySettings()
        {
            _currentSettings = await _settingsService.LoadSettingsAsync();

            _currentSettings.CustomServers ??= new List<ServerInfo>();
            _currentSettings.CustomSkins ??= new List<SkinInfo>();
            _currentSettings.Profiles ??= new List<LauncherProfile>();

            if (_currentSettings.Profiles.Count == 0)
            {
                var latestVersion = _allVersions.FirstOrDefault();
                var defaultProfile = new LauncherProfile
                {
                    Name = "Default",
                    MinecraftVersion = latestVersion?.FullVersion ?? "1.21.11"
                };
                _currentSettings.Profiles.Add(defaultProfile);
                _currentSettings.ActiveProfileId = defaultProfile.Id;
                await _settingsService.SaveSettingsAsync(_currentSettings);
            }

            await InitializeDefaultServersAsync();

            ApplySettingsToUi(_currentSettings);

            LoadServers();
            RefreshQuickPlayBar();

            // Populate the footer stats strip now that settings are loaded.
            // The footer is visible on every page so this must run at startup,
            // not just when navigating to the home page.
            RefreshPlaytimeStatsCard();

            StartRichPresenceIfEnabled();
            WriteSessionJson(_currentSettings.LeafApiJwt);
        }
        private string GetModFilePath(string modsFolder, InstalledMod mod, bool isDisabled = false)
        {
            string fileName = mod.FileName;
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
            if (_natureThemeToggle != null) _natureThemeToggle.IsChecked = settings.EnableNatureTheme;
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

            _ = CalculateDiskUsageAsync();

            if (_optiFineToggle != null) _optiFineToggle.IsChecked = settings.IsOptiFineEnabled;
            if (_testModeToggle != null) _testModeToggle.IsChecked = settings.IsTestMode;
            if (_testModePathBox != null) _testModePathBox.Text = settings.TestModeModProjectPath;
            if (_testModePathPanel != null) _testModePathPanel.IsVisible = settings.IsTestMode;

            if (_launchOnStartupToggle != null) _launchOnStartupToggle.IsChecked = settings.LaunchOnStartup;
            if (_minimizeToTrayToggle != null) _minimizeToTrayToggle.IsChecked = settings.MinimizeToTray;
            if (_discordRichPresenceToggle != null) _discordRichPresenceToggle.IsChecked = settings.DiscordRichPresence;

            if (_minRamSlider != null)
            {
                _minRamSlider.Value = settings.MinRamAllocationMb > 0 ? settings.MinRamAllocationMb : 1024;
            }

            if (_maxRamSlider != null)
            {
                double val = 4096;
                string maxRamStr = settings.MaxRamAllocationGb;
                if (!string.IsNullOrEmpty(maxRamStr))
                {
                    if (maxRamStr.EndsWith(" GB"))
                    {
                        if (double.TryParse(maxRamStr.Replace(" GB", ""), out double gb)) val = gb * 1024;
                    }
                    else if (int.TryParse(maxRamStr, out int mb))
                    {
                        val = mb;
                    }
                }
                _maxRamSlider.Value = val;
            }

            if (_quickLaunchEnabledToggle != null)
            {
                _quickLaunchEnabledToggle.IsChecked = settings.QuickLaunchEnabled;
            }

            if (_quickJoinServerAddressTextBox != null) _quickJoinServerAddressTextBox.Text = settings.QuickJoinServerAddress;
            if (_quickJoinServerPortTextBox != null) _quickJoinServerPortTextBox.Text = settings.QuickJoinServerPort;

            // Reload options.txt so we always reflect the current Minecraft state,
            // not just what the launcher last wrote (handles in-game changes).
            _optionsService.Load();

            if (_mouseSensitivitySlider != null) _mouseSensitivitySlider.Value = _optionsService.HasKey("mouseSensitivity") ? _optionsService.GetDouble("mouseSensitivity", settings.MouseSensitivity) : settings.MouseSensitivity;
            if (_scrollSensitivitySlider != null) _scrollSensitivitySlider.Value = _optionsService.HasKey("scrollSensitivity") ? _optionsService.GetDouble("scrollSensitivity", settings.ScrollSensitivity) : settings.ScrollSensitivity;
            if (_autoJumpToggle != null) _autoJumpToggle.IsChecked = _optionsService.HasKey("autoJump") ? _optionsService.GetBool("autoJump", settings.AutoJump) : settings.AutoJump;
            if (_touchscreenToggle != null) _touchscreenToggle.IsChecked = _optionsService.HasKey("touchscreen") ? _optionsService.GetBool("touchscreen", settings.Touchscreen) : settings.Touchscreen;
            if (_toggleSprintToggle != null) _toggleSprintToggle.IsChecked = _optionsService.HasKey("toggleSprint") ? _optionsService.GetBool("toggleSprint", settings.ToggleSprint) : settings.ToggleSprint;
            if (_toggleCrouchToggle != null) _toggleCrouchToggle.IsChecked = _optionsService.HasKey("toggleCrouch") ? _optionsService.GetBool("toggleCrouch", settings.ToggleCrouch) : settings.ToggleCrouch;
            if (_subtitlesToggle != null) _subtitlesToggle.IsChecked = _optionsService.HasKey("showSubtitles") ? _optionsService.GetBool("showSubtitles", settings.Subtitles) : settings.Subtitles;
            if (_renderDistanceSlider != null) _renderDistanceSlider.Value = _optionsService.HasKey("renderDistance") ? _optionsService.GetInt("renderDistance", (int)settings.RenderDistance) : settings.RenderDistance;
            if (_simulationDistanceSlider != null) _simulationDistanceSlider.Value = _optionsService.HasKey("simulationDistance") ? _optionsService.GetInt("simulationDistance", (int)settings.SimulationDistance) : settings.SimulationDistance;
            if (_entityDistanceSlider != null) _entityDistanceSlider.Value = _optionsService.HasKey("entityDistanceScaling") ? _optionsService.GetDouble("entityDistanceScaling", settings.EntityDistance) : settings.EntityDistance;
            if (_maxFpsSlider != null)
            {
                // options.txt stores 260 for "Unlimited"; the slider uses 0 for "Unlimited".
                var rawFps = _optionsService.HasKey("maxFps") ? _optionsService.GetInt("maxFps", (int)settings.MaxFps) : (int)settings.MaxFps;
                _maxFpsSlider.Value = rawFps >= 260 ? 0 : rawFps;
            }
            if (_vSyncToggle != null) _vSyncToggle.IsChecked = _optionsService.HasKey("enableVsync") ? _optionsService.GetBool("enableVsync", settings.VSync) : settings.VSync;
            if (_fullscreenToggle != null) _fullscreenToggle.IsChecked = _optionsService.HasKey("fullscreen") ? _optionsService.GetBool("fullscreen", settings.Fullscreen) : settings.Fullscreen;
            if (_entityShadowsToggle != null) _entityShadowsToggle.IsChecked = _optionsService.HasKey("entityShadows") ? _optionsService.GetBool("entityShadows", settings.EntityShadows) : settings.EntityShadows;
            if (_highContrastToggle != null) _highContrastToggle.IsChecked = _optionsService.HasKey("highContrast") ? _optionsService.GetBool("highContrast", settings.HighContrast) : settings.HighContrast;

            if (_renderCloudsComboBox != null)
            {
                var cloudsRaw = _optionsService.HasKey("renderClouds")
                    ? _optionsService.GetEnum("renderClouds", settings.RenderClouds ?? "fast")
                    : (settings.RenderClouds ?? "fast");
                // Capitalise to match ComboBoxItem content ("Off", "Fast", "Fancy")
                if (string.IsNullOrEmpty(cloudsRaw)) cloudsRaw = "fast";
                var cloudsDisplay = char.ToUpperInvariant(cloudsRaw[0]) + cloudsRaw.Substring(1).ToLowerInvariant();
                var itemToSelect = _renderCloudsComboBox.Items.OfType<ComboBoxItem>()
                    .FirstOrDefault(i => i.Content?.ToString()?.Equals(cloudsDisplay, StringComparison.OrdinalIgnoreCase) == true);
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

            if (_playerHatToggle != null) _playerHatToggle.IsChecked = _optionsService.HasKey("modelPart_hat") ? _optionsService.GetBool("modelPart_hat", settings.PlayerHat) : settings.PlayerHat;
            if (_playerCapeToggle != null) _playerCapeToggle.IsChecked = _optionsService.HasKey("modelPart_cape") ? _optionsService.GetBool("modelPart_cape", settings.PlayerCape) : settings.PlayerCape;
            if (_playerJacketToggle != null) _playerJacketToggle.IsChecked = _optionsService.HasKey("modelPart_jacket") ? _optionsService.GetBool("modelPart_jacket", settings.PlayerJacket) : settings.PlayerJacket;
            if (_playerLeftSleeveToggle != null) _playerLeftSleeveToggle.IsChecked = _optionsService.HasKey("modelPart_left_sleeve") ? _optionsService.GetBool("modelPart_left_sleeve", settings.PlayerLeftSleeve) : settings.PlayerLeftSleeve;
            if (_playerRightSleeveToggle != null) _playerRightSleeveToggle.IsChecked = _optionsService.HasKey("modelPart_right_sleeve") ? _optionsService.GetBool("modelPart_right_sleeve", settings.PlayerRightSleeve) : settings.PlayerRightSleeve;
            if (_playerLeftPantToggle != null) _playerLeftPantToggle.IsChecked = _optionsService.HasKey("modelPart_left_pants_leg") ? _optionsService.GetBool("modelPart_left_pants_leg", settings.PlayerLeftPant) : settings.PlayerLeftPant;
            if (_playerRightPantToggle != null) _playerRightPantToggle.IsChecked = _optionsService.HasKey("modelPart_right_pants_leg") ? _optionsService.GetBool("modelPart_right_pants_leg", settings.PlayerRightPant) : settings.PlayerRightPant;
            if (settings.CustomSkins == null)
            {
                settings.CustomSkins = new List<SkinInfo>();
            }
            if (_playerMainHandComboBox != null)
            {
                var handRaw = _optionsService.HasKey("mainHand")
                    ? _optionsService.GetEnum("mainHand", settings.PlayerMainHand ?? "right")
                    : (settings.PlayerMainHand ?? "right");
                if (string.IsNullOrEmpty(handRaw)) handRaw = "right";
                var handDisplay = char.ToUpperInvariant(handRaw[0]) + handRaw.Substring(1).ToLowerInvariant();
                var itemToSelect = _playerMainHandComboBox.Items.OfType<ComboBoxItem>()
                    .FirstOrDefault(i => i.Content?.ToString()?.Equals(handDisplay, StringComparison.OrdinalIgnoreCase) == true);
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

            if (_majorVersionsStackPanel != null)
            {
                var majorVersionBorder = _majorVersionsStackPanel.Children
                    .OfType<Border>()
                    .FirstOrDefault(b => b.Tag is string tag && tag == validMajorVersion);

                if (majorVersionBorder != null)
                {
                    if (_versionDropdown != null)
                    {
                        _versionDropdown.SelectionChanged -= OnSubVersionSelected;
                    }

                    OnMajorVersionClick(majorVersionBorder, new RoutedEventArgs()); 

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
                        _versionDropdown.SelectionChanged += OnSubVersionSelected;
                    }
                }
                else
                {
                    var firstMajor = _majorVersionsStackPanel.Children.OfType<Border>().FirstOrDefault();
                    if (firstMajor != null)
                    {
                        if (_versionDropdown != null)
                        {
                            _versionDropdown.SelectionChanged -= OnSubVersionSelected;
                        }
                        OnMajorVersionClick(firstMajor, new RoutedEventArgs());
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
                            _versionDropdown.SelectionChanged += OnSubVersionSelected;
                        }
                    }
                }
            }

            UpdateLaunchVersionText();
            UpdateAddonSelectionUI(validSubVersion);

            LoadServers();
            RefreshQuickPlayBar();

            _isApplyingSettings = false;

            // Snapshot the clean state so Cancel can revert to it
            try { _settingsSnapshotJson = System.Text.Json.JsonSerializer.Serialize(settings, JsonContext.Default.LauncherSettings); }
            catch { _settingsSnapshotJson = null; }
        }


        private static void OpenPrivacyPolicy(object? s, RoutedEventArgs e) =>
           Process.Start(new ProcessStartInfo { FileName = "https://leafclient.com/privacy.html", UseShellExecute = true });

        private static void OpenTermsOfService(object? s, RoutedEventArgs e) =>
            Process.Start(new ProcessStartInfo { FileName = "https://leafclient.com/terms.html", UseShellExecute = true });

        private static void OpenLicenses(object? s, RoutedEventArgs e) =>
            Process.Start(new ProcessStartInfo { FileName = "https://github.com/LeafClientMC/LeafClient/blob/main/LICENSE.md", UseShellExecute = true });

        private static void OpenContact(object? s, RoutedEventArgs e) =>
            Process.Start(new ProcessStartInfo { FileName = "https://leafclient.com/contact.html", UseShellExecute = true });


        private async void SaveSettingsFromUi()
        {
            _currentSettings.EnableNatureTheme = _natureThemeToggle?.IsChecked ?? true;
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

            _currentSettings.IsOptiFineEnabled = _optiFineToggle?.IsChecked ?? false;
            _currentSettings.IsTestMode = _testModeToggle?.IsChecked ?? false;
            if (_testModePathBox?.Text is string p && !string.IsNullOrWhiteSpace(p))
                _currentSettings.TestModeModProjectPath = p;
            _currentSettings.LaunchOnStartup = _launchOnStartupToggle?.IsChecked ?? false;
            _currentSettings.MinimizeToTray = _minimizeToTrayToggle?.IsChecked ?? false;
            _currentSettings.DiscordRichPresence = _discordRichPresenceToggle?.IsChecked ?? false;
            _currentSettings.MinRamAllocationMb = (int)(_minRamSlider?.Value ?? 1024);

            double maxRamMb = _maxRamSlider?.Value ?? 4096;
            _currentSettings.MaxRamAllocationGb = $"{(maxRamMb / 1024.0):0.##} GB";
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
            _currentSettings.MaxFps = _maxFpsSlider?.Value ?? 0; 
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
            _currentSettings.SelectedSubVersion = (_versionDropdown?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            var sub = _currentSettings.SelectedSubVersion;
            if (!string.IsNullOrWhiteSpace(sub))
            {
                var addon = GetSelectedAddon(sub);
                _currentSettings.SelectedAddonByVersion ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _currentSettings.SelectedAddonByVersion[sub] = addon;
            }
            await _settingsService.SaveSettingsAsync(_currentSettings);
        }

        private void SyncLauncherManagedMods(string mcVersion)
        {
            string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
            if (!Directory.Exists(modsFolder))
            {
                Directory.CreateDirectory(modsFolder);
                Console.WriteLine("[Mod Sync] Mods folder did not exist, created.");
                return; 
            }

            Console.WriteLine($"[Mod Sync] Synchronizing launcher-managed mods for MC {mcVersion}...");

            var allModFilesOnDisk = Directory.GetFiles(modsFolder, "*.jar*", SearchOption.TopDirectoryOnly)
                                             .ToList();

            var launcherManagedModsForCurrentVersion = _currentSettings.InstalledMods
                .Where(m => m.MinecraftVersion.Equals(mcVersion, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(m => GetModFilePath(modsFolder, m, isDisabled: false), m => m, StringComparer.OrdinalIgnoreCase); 

            var launcherManagedModsForOtherVersions = _currentSettings.InstalledMods
                .Where(m => !m.MinecraftVersion.Equals(mcVersion, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var mod in launcherManagedModsForCurrentVersion.Values)
            {
                string enabledJarPath = GetModFilePath(modsFolder, mod, isDisabled: false); 
                string disabledJarPath = GetModFilePath(modsFolder, mod, isDisabled: true); 

                if (mod.Enabled)
                {
                    if (File.Exists(disabledJarPath))
                    {
                        try
                        {
                            File.Move(disabledJarPath, enabledJarPath);
                            Console.WriteLine($"[Mod Sync] Enabled '{mod.Name}' for MC {mcVersion}.");
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"[Mod Sync ERROR] Failed to enable '{mod.Name}': {ex.Message}");
                        }
                    }
                }
                else 
                {
                    if (File.Exists(enabledJarPath))
                    {
                        try
                        {
                            File.Move(enabledJarPath, disabledJarPath);
                            Console.WriteLine($"[Mod Sync] Disabled '{mod.Name}' for MC {mcVersion}.");
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"[Mod Sync ERROR] Failed to disable '{mod.Name}': {ex.Message}");
                        }
                    }
                }
            }

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
                    _currentSettings.InstalledMods.Remove(mod);
                }
            }

            foreach (var fileOnDisk in allModFilesOnDisk)
            {
                if (fileOnDisk.EndsWith(".jar.disabled", StringComparison.OrdinalIgnoreCase))
                {
                    string baseFileName = System.IO.Path.GetFileName(fileOnDisk).Replace(".jar.disabled", ".jar");
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
            var patterns = new[]
            {
                @"-(\d+\.\d+\.\d+)\.jar$",           
                @"-(\d+\.\d+)\.jar$",                 
                @"-mc(\d+\.\d+\.\d+)\.jar$",         
                @"-mc(\d+\.\d+)\.jar$",               
                @"-fabric-(\d+\.\d+\.\d+)\.jar$", 
                @"-fabric-(\d+\.\d+)\.jar$"        
            };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(fileName, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return null; 
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

        // ─────────────────────────────────────────────────────────────────────
        // STORE PAGE  (extracted to Views/Pages/StorePageView)
        // ─────────────────────────────────────────────────────────────────────

        private static string? DecodeJwtMinecraftUsername(string? jwt)
        {
            if (string.IsNullOrWhiteSpace(jwt)) return null;
            try
            {
                var parts = jwt.Split('.');
                if (parts.Length < 2) return null;
                var payload = parts[1];
                var padded = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(padded));
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("minecraft_username", out var el))
                    return el.GetString();
            }
            catch { }
            return null;
        }

        private static string? DecodeJwtSub(string? jwt)
        {
            if (string.IsNullOrWhiteSpace(jwt)) return null;
            try
            {
                var parts = jwt.Split('.');
                if (parts.Length < 2) return null;
                var payload = parts[1];
                var padded = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(padded));
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("sub", out var el))
                    return el.GetString();
            }
            catch { }
            return null;
        }

        private async Task<byte[]?> FetchSkinBytesAsync()
        {
            string? uuid = _session?.UUID ?? _currentSettings?.SessionUuid;
            string? username = _session?.Username ?? _currentSettings?.SessionUsername;
            if (string.IsNullOrWhiteSpace(uuid) && string.IsNullOrWhiteSpace(username)) return null;

            using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("LeafClient/1.0");

            if (!string.IsNullOrWhiteSpace(uuid))
            {
                try
                {
                    string clean = uuid.Replace("-", "");
                    var json = await http.GetStringAsync($"https://sessionserver.mojang.com/session/minecraft/profile/{clean}");
                    var url  = ExtractSkinUrlFromProfile(json);
                    if (!string.IsNullOrWhiteSpace(url)) return await http.GetByteArrayAsync(url);
                }
                catch { /* fallthrough */ }
            }
            if (!string.IsNullOrWhiteSpace(username))
            {
                try { return await http.GetByteArrayAsync($"https://minotar.net/skin/{username}"); } catch { }
            }
            return null;
        }

        // Store methods (PopulateStoreGrid, CreateStoreCard, ApplyCosmeticPreviewToRenderer,
        // UpdateStorePreviewPanel, HighlightStoreTab, OnStoreTabTapped) moved to StorePageView.

    }

    public enum SupportLevel
    {
        FullSupport,
        PartialSupport,
        VanillaOnly
    }

    public class VersionInfo
    {
        public string FullVersion { get; set; }
        public string MajorVersion { get; set; }
        public string Type { get; set; }
        public string Loader { get; set; }
        public string ReleaseDate { get; set; }
        public string Description { get; set; }
        public bool IsLeafClientModSupported { get; set; }
        public bool IsFullySupported { get; set; }
        public SupportLevel SupportLevel { get; set; }

        public string DisplayVersion
        {
            get
            {
                return SupportLevel switch
                {
                    SupportLevel.FullSupport => $"{FullVersion} ✓ Full Support",
                    SupportLevel.PartialSupport => $"{FullVersion} ⚠ Partial Support",
                    SupportLevel.VanillaOnly => $"{FullVersion} Vanilla Only",
                    _ => FullVersion
                };
            }
        }

        public VersionInfo(string fullVersion, string majorVersion, string type, string loader, string releaseDate, string description)
        {
            FullVersion = fullVersion;
            MajorVersion = majorVersion;
            Type = type;
            Loader = loader;
            ReleaseDate = releaseDate;
            Description = description;
            IsLeafClientModSupported = false;
            IsFullySupported = false;
            SupportLevel = SupportLevel.VanillaOnly;
        }
    }


    public class MinecraftServerChecker
    {

        private static readonly HttpClient _httpClient = new HttpClient();
        private const string ApiBaseUrl = "https://api.mcstatus.io/v2/status/java/";

        public async Task<ServerStatusResult> GetServerStatusAsync(string address, int port = 25565)
        {
            string url = $"{ApiBaseUrl}{address}:{port}";
            Console.WriteLine($"[ServerCheck] Pinging {address}:{port} using mcstatus.io API...");

            try
            {
                var jsonResponse = await _httpClient.GetStringAsync(url);
                var response = JsonSerializer.Deserialize(jsonResponse, JsonContext.Default.ServerStatusResponse);

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
    public string? Motd { get; set; }
    public string? IconData { get; set; }
}



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
                uiUpdateAction(server, () => { }); 
            }
            catch (Exception ex)
            {
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
    public int FormatVersion { get; set; }
    public string? Game { get; set; }
    public string? VersionId { get; set; }
    public string? Name { get; set; }
    public string? Summary { get; set; }
    public List<ModrinthPackFile>? Files { get; set; }
    public Dictionary<string, string>? Dependencies { get; set; }
}

public class ModrinthPackFile
{
    public string? Path { get; set; }
    public Dictionary<string, string>? Hashes { get; set; }
    public List<string>? Downloads { get; set; }
    public long FileSize { get; set; }
    public string? FileType { get; set; }
}

public class NotificationWindow : Window
{
    private static NotificationWindow? _currentBanner;
    private readonly Border _card;
    private readonly TranslateTransform _transform;

    public NotificationWindow(string title, string message, string? buttonText, Action? buttonAction, bool isError)
    {
        SystemDecorations = SystemDecorations.None;
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        CanResize = false;
        SizeToContent = SizeToContent.WidthAndHeight;
        Title = "Notification";

        _transform = new TranslateTransform { Y = -150 }; 

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

        var iconBorder = new Border
        {
            Width = 40,
            Height = 40,
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(isError ? Color.Parse("#33FF0000") : Color.Parse("#33FFFFFF")),
            Margin = new Thickness(0, 0, 15, 0)
        };

        var iconText = new TextBlock
        {
            Text = isError ? "⚠️" : "ℹ️",
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            FontSize = 20
        };
        iconBorder.Child = iconText;

        var textStack = new StackPanel { VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Spacing = 2 };
        textStack.Children.Add(new TextBlock { Text = title, FontWeight = FontWeight.Bold, Foreground = Brushes.White, FontSize = 14 });
        textStack.Children.Add(new TextBlock { Text = message, Foreground = new SolidColorBrush(Color.Parse("#CCCCCC")), FontSize = 12, TextWrapping = TextWrapping.Wrap });

        Button? actionBtn = null;
        if (!string.IsNullOrEmpty(buttonText))
        {
            actionBtn = new Button
            {
                Content = buttonText,
                Background = new SolidColorBrush(Color.Parse(isError ? "#D32F2F" : "#2E7D32")), 
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 6),
                Margin = new Thickness(15, 0, 0, 0),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            actionBtn.Click += (_, __) => { buttonAction?.Invoke(); CloseBanner(); };
        }

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

        this.PointerPressed += (_, __) => CloseBanner();
    }

    public static void Show(string title, string message, string? buttonText = null, Action? buttonAction = null, bool isError = false)
    {
        if (_currentBanner != null)
        {
            try { _currentBanner.Close(); } catch { }
        }

        Dispatcher.UIThread.Post(async () =>
        {
            var window = new NotificationWindow(title, message, buttonText, buttonAction, isError);
            _currentBanner = window;
            Screen? primaryScreen = null;

            if (window.Screens != null)
                primaryScreen = window.Screens.Primary;

            if (primaryScreen == null && Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                primaryScreen = desktop.MainWindow?.Screens.Primary;
            }

            if (primaryScreen != null)
            {
                var workingArea = primaryScreen.WorkingArea;
                int x = workingArea.X + (workingArea.Width / 2) - 200; 
                int y = workingArea.Y + 20; 
                window.Position = new PixelPoint(x, y);
            }

            window.Show();
            await window.AnimateIn();
        });
    }

    private async Task AnimateIn()
    {
        for (int i = 0; i <= 30; i++)
        {
            double t = i / 30.0;
            double eased = 1 - Math.Pow(1 - t, 3); 
            _transform.Y = -150 + (150 * eased);
            await Task.Delay(10);
        }

        await Task.Delay(4000);

        CloseBanner();
    }

    private async void CloseBanner()
    {
        for (int i = 0; i <= 20; i++)
        {
            double t = i / 20.0;
            double eased = t * t; 
            _transform.Y = 0 - (150 * eased);
            await Task.Delay(10);
        }

        Close();
        if (_currentBanner == this) _currentBanner = null;
    }
}
