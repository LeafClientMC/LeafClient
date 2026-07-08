using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace LeafClient.Models
{
    public enum LauncherVisibility
    {
        KeepOpen,
        Hide
    }

    public enum UpdateDelivery
    {
        Normal,
        EarlyOptIn,
        LateOptOut
    }

    public enum NotificationPreference
    {
        Always,
        JustOnce,
        Never
    }

    public class InstalledMod
    {
        public string ModId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string MinecraftVersion { get; set; } = string.Empty;
        public string Loader { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public DateTime InstallDate { get; set; } = DateTime.Now;
        public string IconUrl { get; set; } = string.Empty;

        public bool IsAutoInstalled { get; set; } = false;

        public string? Sha256 { get; set; } = null;

        public string? ProfileId { get; set; } = null;
    }

    public class InstalledModpackEntry
    {
        public string ProjectId { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public string McVersion { get; set; } = string.Empty;
        public string ModpackVersion { get; set; } = string.Empty;
        public int FileCount { get; set; }
        public int OverrideCount { get; set; }
        public DateTime InstalledAt { get; set; } = DateTime.UtcNow;
        public List<string> ModFiles { get; set; } = new List<string>();
        public string? ProfileId { get; set; } = null;
    }

    public class InstalledContentEntry
    {
        public string ProjectId { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime InstalledAt { get; set; } = DateTime.UtcNow;
        public string? ProfileId { get; set; } = null;
    }

    public class AccountEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string AccountType { get; set; } = "offline";
        public string Username { get; set; } = "";
        public string? Uuid { get; set; }
        public string? AccessToken { get; set; }
        public string? Xuid { get; set; }
        public DateTime AddedDate { get; set; } = DateTime.Now;
        public string? LeafApiJwt { get; set; }
        public string? LeafApiRefreshToken { get; set; }
        public DateTime? FriendRequestsLastSeenUtc { get; set; }
        public EquippedCosmetics Equipped { get; set; } = new EquippedCosmetics();
        public List<string> OwnedCosmeticIds { get; set; } = new List<string>();
        public List<CosmeticPreset> CosmeticPresets { get; set; } = new List<CosmeticPreset>();
        public string? LastSeenCosmeticDropMonth { get; set; }
        public List<SkinInfo> CustomSkins { get; set; } = new List<SkinInfo>();
        public string? SelectedSkinId { get; set; }
    }

    public class EquippedCosmetics
    {
        public string? Sub { get; set; }
        public string? CapeId { get; set; }
        public string? HatId { get; set; }
        public string? WingsId { get; set; }
        public string? BackItemId { get; set; }
        public string? AuraId { get; set; }
        public string? FaceId { get; set; }

        public string? CapeVariant { get; set; }
        public string? HatVariant { get; set; }
        public string? WingsVariant { get; set; }
        public string? BackItemVariant { get; set; }
        public string? AuraVariant { get; set; }
        public string? FaceVariant { get; set; }

        public double? CapeScale { get; set; }
        public double? HatScale { get; set; }
        public double? WingsScale { get; set; }
        public double? BackItemScale { get; set; }
        public double? AuraScale { get; set; }
        public double? FaceScale { get; set; }

        public double? CapeOffsetX { get; set; }
        public double? CapeOffsetY { get; set; }
        public double? CapeOffsetZ { get; set; }
        public double? HatOffsetX { get; set; }
        public double? HatOffsetY { get; set; }
        public double? HatOffsetZ { get; set; }
        public double? WingsOffsetX { get; set; }
        public double? WingsOffsetY { get; set; }
        public double? WingsOffsetZ { get; set; }
        public double? BackItemOffsetX { get; set; }
        public double? BackItemOffsetY { get; set; }
        public double? BackItemOffsetZ { get; set; }
        public double? AuraOffsetX { get; set; }
        public double? AuraOffsetY { get; set; }
        public double? AuraOffsetZ { get; set; }
        public double? FaceOffsetX { get; set; }
        public double? FaceOffsetY { get; set; }
        public double? FaceOffsetZ { get; set; }
    }

    public class OwnedCosmeticsFile
    {
        public string? Sub { get; set; }
        public List<string> Ids { get; set; } = new List<string>();
    }

    public class CosmeticPreset
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Untitled";
        public EquippedCosmetics Equipped { get; set; } = new EquippedCosmetics();
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }

    public class LauncherProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "Default";
        public string MinecraftVersion { get; set; } = "";
        public string Loader { get; set; } = "";
        public string AccountSetting { get; set; } = "active";
        public string JavaSetting { get; set; } = "bundled";
        public string? CustomJavaPath { get; set; }
        public double AllocatedMemoryGb { get; set; } = 3.0;
        public int AllocatedMinMemoryMb { get; set; } = 1024;
        public string ModPreset { get; set; } = "none";
        public bool ShowUnsupportedVersionsInEditor { get; set; } = false;
        public string AccentColor { get; set; } = "#22C55E";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public Dictionary<string, bool> DisabledRequiredMods { get; set; } =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public string RenderEngine { get; set; } = "";

        public Dictionary<string, bool> CoreModOverrides { get; set; } =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public string? JvmArgumentsOverride { get; set; }

        public bool? UseCustomResolutionOverride { get; set; }
        public int? GameResolutionWidthOverride { get; set; }
        public int? GameResolutionHeightOverride { get; set; }

        public string? QuickJoinServerAddressOverride { get; set; }
        public string? QuickJoinServerPortOverride { get; set; }

        public string Description { get; set; } = "";

        public int LaunchCount { get; set; } = 0;

        public long PlaytimeSeconds { get; set; } = 0;

        public DateTime LastUsed { get; set; } = DateTime.MinValue;
    }

    public class LauncherSettings
    {

        public bool IsFirstLaunch { get; set; } = true;

        public bool IsLoggedIn { get; set; } = false;
        public string AccountType { get; set; } = "";
        public string? OfflineUsername { get; set; }
        public string? SessionUsername { get; set; }
        public string? SessionUuid { get; set; }
        public string? SessionAccessToken { get; set; }
        public string? SessionXuid { get; set; }
        public string? MicrosoftRefreshToken { get; set; }
        public string? LeafApiJwt { get; set; }
        public string? LeafApiRefreshToken { get; set; }
        public string? SuggestionUserId { get; set; }

        public string? LastCloudSyncAt { get; set; }

        public string? LastSeenCosmeticDropMonth { get; set; }

        public bool TrialPopupDismissed { get; set; } = false;
        public string? TrialEndedSeenForUserId { get; set; }
        public bool PerAccountSkinsResetDone { get; set; } = false;

        public List<AccountEntry> SavedAccounts { get; set; } = new List<AccountEntry>();
        public string? ActiveAccountId { get; set; }

        public bool LaunchOnStartup { get; set; } = false;
        public bool MinimizeToTray { get; set; } = false;
        public bool DiscordRichPresence { get; set; } = true;
        public string BackgroundTheme { get; set; } = "aurora";
        public string SelectedCurrency { get; set; } = "EUR";
        public string Language { get; set; } = "en";

        public bool ShowUsernameInDiscordRichPresence { get; set; } = true;
        public LauncherVisibility LauncherVisibilityOnGameLaunch { get; set; } = LauncherVisibility.KeepOpen;

        public string QuickJoinServerAddress { get; set; } = "";
        public string QuickJoinServerPort { get; set; } = "25565";
        public bool QuickLaunchEnabled { get; set; } = false;
        public string JvmArguments { get; set; } = "";
        public Dictionary<string, string> SelectedAddonByVersion { get; set; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);


        public bool UseCustomGameResolution { get; set; } = false;
        public int GameResolutionWidth { get; set; } = 1280;
        public int GameResolutionHeight { get; set; } = 720;
        public bool LockGameAspectRatio { get; set; } = true;

        public double MouseSensitivity { get; set; } = 0.5;
        public double ScrollSensitivity { get; set; } = 1.0;
        public bool AutoJump { get; set; } = false;
        public bool Touchscreen { get; set; } = false;
        public bool ToggleSprint { get; set; } = false;
        public bool ToggleCrouch { get; set; } = false;
        public bool Subtitles { get; set; } = true;

        public double RenderDistance { get; set; } = 32;
        public double SimulationDistance { get; set; } = 32;
        public double EntityDistance { get; set; } = 1;
        public double MaxFps { get; set; } = 0;
        public bool VSync { get; set; } = false;
        public bool Fullscreen { get; set; } = false;
        public bool EntityShadows { get; set; } = true;
        public bool HighContrast { get; set; } = false;
        public string RenderClouds { get; set; } = "Fast";

        public List<ServerInfo> CustomServers { get; set; } = new List<ServerInfo>();

        public bool PlayerHat { get; set; } = true;
        public bool PlayerCape { get; set; } = true;
        public bool PlayerJacket { get; set; } = true;
        public bool PlayerLeftSleeve { get; set; } = true;
        public bool PlayerRightSleeve { get; set; } = true;
        public bool PlayerLeftPant { get; set; } = true;
        public bool PlayerRightPant { get; set; } = true;
        public string PlayerMainHand { get; set; } = "Right";

        public UpdateDelivery GameUpdateDelivery { get; set; } = UpdateDelivery.Normal;
        public NotificationPreference ClosingNotificationsPreference { get; set; } = NotificationPreference.JustOnce;
        public bool EnableUpdateNotifications { get; set; } = true;
        public bool EnableFriendRequestNotifications { get; set; } = true;
        public bool EnableNewContentIndicators { get; set; } = true;
        public List<string> SeenNewsTitles { get; set; } = new List<string>();
        public List<string> SeenCosmeticIds { get; set; } = new List<string>();
        public List<string> SeenMcVersions { get; set; } = new List<string>();
        public List<string> SeenInventoryCosmeticIds { get; set; } = new List<string>();
        public List<string> SeenScreenshotPaths { get; set; } = new List<string>();
        public string? LastNotifiedUpdateVersion { get; set; }

        public bool EnableNatureTheme { get; set; } = false;

        public bool ShowGameOutputWindow { get; set; } = true;

        public bool HidePlaytimeDisplay { get; set; } = false;

        public List<LauncherProfile> Profiles { get; set; } = new List<LauncherProfile>();
        public string? ActiveProfileId { get; set; }

        public string SelectedMajorVersion { get; set; } = "1.21";
        public string SelectedSubVersion { get; set; } = "";

        public bool IsVulkanModEnabled { get; set; } = false;
        public string RenderBackendChoice { get; set; } = "";
        public bool PreviewDayBackground { get; set; } = false;
        public bool IsTestMode { get; set; } = false;
        public string TestModeModProjectPath { get; set; } = "";

        public List<InstalledMod> InstalledMods { get; set; } = new List<InstalledMod>();

        public bool EnablePreLaunchCheck { get; set; } = true;
        public bool EnableModMigrationCheck { get; set; } = true;
        public List<string> IgnoredModConflicts { get; set; } = new List<string>();
        public bool AutoFixOnLaunch { get; set; } = false;
        public bool AllowPrereleaseFabricLoaders { get; set; } = false;
        public bool ModFolderMigrationCompleted { get; set; } = false;

        public bool UseNewModSystem { get; set; } = true;

        public List<SkinInfo> CustomSkins { get; set; } = new List<SkinInfo>();
        public string? SelectedSkinId { get; set; }

        public EquippedCosmetics Equipped { get; set; } = new EquippedCosmetics();
        public List<CosmeticPreset> CosmeticPresets { get; set; } = new List<CosmeticPreset>();

        public long TotalPlaytimeSeconds { get; set; } = 0;
        public int TotalLaunchCount { get; set; } = 0;
        public long CurrentSessionSeconds { get; set; } = 0;
        public DateTime LastLaunchTime { get; set; } = DateTime.MinValue;
        public DateTime LastExitTime { get; set; } = DateTime.MinValue;
        public Dictionary<string, long> PlaytimeByVersion { get; set; } =
            new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> LaunchCountByVersion { get; set; } =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, long> PlaytimeByServer { get; set; } =
            new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        public LauncherSettings()
        {
        }

        public bool HasValidSession()
        {
            return IsLoggedIn &&
                   !string.IsNullOrEmpty(AccountType) &&
                   !string.IsNullOrEmpty(SessionUsername);
        }

        public void ClearAuthOnly()
        {
            IsLoggedIn = false;
            AccountType = "";
            OfflineUsername = null;

            SessionUsername = null;
            SessionUuid = null;
            SessionAccessToken = null;
            SessionXuid = null;
            MicrosoftRefreshToken = null;
            LeafApiJwt = null;
            LeafApiRefreshToken = null;
        }

        public int GetRenderCloudsMode()
        {
            return RenderClouds switch
            {
                "Off" => 0,
                "Fast" => 1,
                "Fancy" => 2,
                _ => 1
            };
        }
        public int GetPlayerMainHandMode()
        {
            return PlayerMainHand == "Left" ? 0 : 1;
        }

        public LauncherSettings Clone()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(this, JsonContext.Default.LauncherSettings);
            return System.Text.Json.JsonSerializer.Deserialize(json, JsonContext.Default.LauncherSettings) ?? new LauncherSettings();
        }

        public void ResetToDefaults()
        {
            var defaults = new LauncherSettings();

            this.IsFirstLaunch = defaults.IsFirstLaunch;
            this.LaunchOnStartup = defaults.LaunchOnStartup;
            this.MinimizeToTray = defaults.MinimizeToTray;
            this.DiscordRichPresence = defaults.DiscordRichPresence;
            this.ShowUsernameInDiscordRichPresence = defaults.ShowUsernameInDiscordRichPresence;
            this.LauncherVisibilityOnGameLaunch = defaults.LauncherVisibilityOnGameLaunch;
            this.CustomSkins = new List<SkinInfo>();
            this.SelectedSkinId = defaults.SelectedSkinId;
            this.QuickJoinServerAddress = defaults.QuickJoinServerAddress;
            this.QuickJoinServerPort = defaults.QuickJoinServerPort;
            this.QuickLaunchEnabled = defaults.QuickLaunchEnabled;
            this.JvmArguments = defaults.JvmArguments;
            this.UseCustomGameResolution = defaults.UseCustomGameResolution;
            this.GameResolutionWidth = defaults.GameResolutionWidth;
            this.GameResolutionHeight = defaults.GameResolutionHeight;
            this.LockGameAspectRatio = defaults.LockGameAspectRatio;
            this.SelectedAddonByVersion = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            this.MouseSensitivity = defaults.MouseSensitivity;
            this.ScrollSensitivity = defaults.ScrollSensitivity;
            this.AutoJump = defaults.AutoJump;
            this.Touchscreen = defaults.Touchscreen;
            this.ToggleSprint = defaults.ToggleSprint;
            this.ToggleCrouch = defaults.ToggleCrouch;
            this.Subtitles = defaults.Subtitles;
            this.RenderDistance = defaults.RenderDistance;
            this.SimulationDistance = defaults.SimulationDistance;
            this.EntityDistance = defaults.EntityDistance;
            this.MaxFps = defaults.MaxFps;
            this.VSync = defaults.VSync;
            this.Fullscreen = defaults.Fullscreen;
            this.EntityShadows = defaults.EntityShadows;
            this.HighContrast = defaults.HighContrast;
            this.RenderClouds = defaults.RenderClouds;
            this.CustomServers = new List<ServerInfo>();
            this.PlayerHat = defaults.PlayerHat;
            this.PlayerCape = defaults.PlayerCape;
            this.PlayerJacket = defaults.PlayerJacket;
            this.PlayerLeftSleeve = defaults.PlayerLeftSleeve;
            this.PlayerRightSleeve = defaults.PlayerRightSleeve;
            this.PlayerLeftPant = defaults.PlayerLeftPant;
            this.PlayerRightPant = defaults.PlayerRightPant;
            this.PlayerMainHand = defaults.PlayerMainHand;
            this.SelectedCurrency = defaults.SelectedCurrency;
            this.Language = defaults.Language;
            this.GameUpdateDelivery = defaults.GameUpdateDelivery;
            this.ClosingNotificationsPreference = defaults.ClosingNotificationsPreference;
            this.EnableUpdateNotifications = defaults.EnableUpdateNotifications;
            this.EnableFriendRequestNotifications = defaults.EnableFriendRequestNotifications;
            this.EnableNewContentIndicators = defaults.EnableNewContentIndicators;
            this.SelectedMajorVersion = defaults.SelectedMajorVersion;
            this.SelectedSubVersion = defaults.SelectedSubVersion;
            this.InstalledMods = new List<InstalledMod>();
            this.Equipped = new EquippedCosmetics();
        }
    }
}

public class ModCleanupEntry
{
    public string ModId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string MinecraftVersion { get; set; } = string.Empty;
    public DateTime AddedDate { get; set; } = DateTime.Now;
}
