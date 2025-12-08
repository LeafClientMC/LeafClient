using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace LeafClient.Models
{
    // New Enums for settings
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

    // This is the correct, single definition for InstalledMod
    public class InstalledMod
    {
        public string ModId { get; set; } = string.Empty;          // Unique identifier (e.g., Modrinth project ID, or unique part of filename)
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;        // Mod's own version, e.g., "1.0.0"
        public string MinecraftVersion { get; set; } = string.Empty; // Minecraft version this mod is for, e.g., "1.20.1"
        public string FileName { get; set; } = string.Empty;       // The actual .jar filename (e.g., "sodium-fabric-1.20.1.jar")
        public string DownloadUrl { get; set; } = string.Empty;    // URL to re-download if needed ("internal" for LeafClient's own mod)
        public bool Enabled { get; set; } = true;
        public DateTime InstallDate { get; set; } = DateTime.Now;
        public string IconUrl { get; set; } = string.Empty;
    }

    public enum PrayerCalculationMethod
    {
        Jafari = 0,       // Ithna Ashari
        Karachi = 1,      // University of Islamic Sciences, Karachi
        ISNA = 2,         // Islamic Society of North America
        MWL = 3,          // Muslim World League
        Makkah = 4,       // Umm Al-Qura University, Makkah
        Egypt = 5,        // Egyptian General Authority of Survey
        Tehran = 7,       // Institute of Geophysics, University of Tehran
        Dubai = 9,        // Dubai (Official)
        Kuwait = 10,      // Kuwait (Official)
        Qatar = 11,       // Qatar
        Singapore = 12,   // Majlis Ugama Islam Singapura
        Turkey = 13,      // Diyanet İşleri Başkanlığı
        Russia = 14       // Spiritual Administration of Muslims of Russia
    }

    public class LauncherSettings
    {

        public bool IsFirstLaunch { get; set; } = true;

        //////////////////////////////////////////////////////////////////////
        // AUTHENTICATION SETTINGS
        //////////////////////////////////////////////////////////////////////
        public bool IsLoggedIn { get; set; } = false;
        public string AccountType { get; set; } = ""; // "microsoft" or "offline"
        public string? OfflineUsername { get; set; }
        public string? SessionUsername { get; set; }
        public string? SessionUuid { get; set; }
        public string? SessionAccessToken { get; set; }
        public string? SessionXuid { get; set; }
        public string? SuggestionUserId { get; set; }

        //////////////////////////////////////////////////////////////////////
        // GENERAL SETTINGS
        //////////////////////////////////////////////////////////////////////
        public bool LaunchOnStartup { get; set; } = false;
        public bool MinimizeToTray { get; set; } = false;
        public bool DiscordRichPresence { get; set; } = true;
        public bool AnimationsEnabled { get; set; } = true;
        public string Theme { get; set; } = "Dark"; // "Dark", "Light", "Auto"

        // NEW: Discord Rich Presence Username Visibility
        public bool ShowUsernameInDiscordRichPresence { get; set; } = true;
        // NEW: Launcher Visibility on Game Launch
        public LauncherVisibility LauncherVisibilityOnGameLaunch { get; set; } = LauncherVisibility.KeepOpen;

        //////////////////////////////////////////////////////////////////////
        // ISLAMIC FEATURES SETTINGS
        //////////////////////////////////////////////////////////////////////
        public bool EnablePrayerTimeReminder { get; set; } = false;
        public string PrayerTimeCountry { get; set; } = "United Arab Emirates"; // Default country
        public string PrayerTimeCity { get; set; } = "Dubai"; // Default city
        public PrayerCalculationMethod PrayerTimeCalculationMethod { get; set; } = PrayerCalculationMethod.Dubai; // Default calculation method
        public int PrayerReminderMinutesBefore { get; set; } = 10; // Default 10 minutes, minimum 10

        //////////////////////////////////////////////////////////////////////
        // LAUNCH OPTIONS
        //////////////////////////////////////////////////////////////////////
        public int MinRamAllocationMb { get; set; } = 1000;
        public string MaxRamAllocationGb { get; set; } = "8 GB";
        public string QuickJoinServerAddress { get; set; } = "";
        public string QuickJoinServerPort { get; set; } = "25565";
        public bool QuickLaunchEnabled { get; set; } = false;
        public string JvmArguments { get; set; } = "";
        public Dictionary<string, string> SelectedAddonByVersion { get; set; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public string? SelectedFabricProfileName { get; set; }

        // NEW: Game Resolution
        public bool UseCustomGameResolution { get; set; } = false;
        public int GameResolutionWidth { get; set; } = 1280; // Default width
        public int GameResolutionHeight { get; set; } = 720; // Default height
        public bool LockGameAspectRatio { get; set; } = true; // Default to locked aspect ratio


        //////////////////////////////////////////////////////////////////////
        // GAME OPTIONS - ACCESSIBILITY AND CONTROLS
        //////////////////////////////////////////////////////////////////////
        public double MouseSensitivity { get; set; } = 0.5;
        public double ScrollSensitivity { get; set; } = 1.0;
        public bool AutoJump { get; set; } = false;
        public bool Touchscreen { get; set; } = false;
        public bool ToggleSprint { get; set; } = false;
        public bool ToggleCrouch { get; set; } = false;
        public bool Subtitles { get; set; } = true;

        //////////////////////////////////////////////////////////////////////
        // GAME OPTIONS - RENDERING
        //////////////////////////////////////////////////////////////////////
        public double RenderDistance { get; set; } = 32;
        public double SimulationDistance { get; set; } = 32;
        public double EntityDistance { get; set; } = 1;
        public double MaxFps { get; set; } = 0;
        public bool VSync { get; set; } = false;
        public bool Fullscreen { get; set; } = false;
        public bool EntityShadows { get; set; } = true;
        public bool HighContrast { get; set; } = false;
        public string RenderClouds { get; set; } = "Fast";

        //////////////////////////////////////////////////////////////////////
        // GAME OPTIONS - SERVER SETTINGS
        //////////////////////////////////////////////////////////////////////

        public List<ServerInfo> CustomServers { get; set; } = new List<ServerInfo>();

        //////////////////////////////////////////////////////////////////////
        // GAME OPTIONS - PLAYER SETTINGS
        //////////////////////////////////////////////////////////////////////
        public bool PlayerHat { get; set; } = true;
        public bool PlayerCape { get; set; } = true;
        public bool PlayerJacket { get; set; } = true;
        public bool PlayerLeftSleeve { get; set; } = true;
        public bool PlayerRightSleeve { get; set; } = true;
        public bool PlayerLeftPant { get; set; } = true;
        public bool PlayerRightPant { get; set; } = true;
        public string PlayerMainHand { get; set; } = "Right";

        //////////////////////////////////////////////////////////////////////
        // APPEARANCE SETTINGS
        //////////////////////////////////////////////////////////////////////
        // Theme and Animations are already here (moved from General)
        // public string Theme { get; set; } = "Dark";
        // public bool AnimationsEnabled { get; set; } = true;

        // NEW: Update Delivery
        public UpdateDelivery GameUpdateDelivery { get; set; } = UpdateDelivery.Normal;
        // NEW: Closing Notifications, Update Notifications, New Content Indicators
        public NotificationPreference ClosingNotificationsPreference { get; set; } = NotificationPreference.JustOnce;
        public bool EnableUpdateNotifications { get; set; } = true;
        public bool EnableNewContentIndicators { get; set; } = true;


        //////////////////////////////////////////////////////////////////////
        // VERSION SETTINGS
        //////////////////////////////////////////////////////////////////////
        public string SelectedMajorVersion { get; set; } = "1.21";
        public string SelectedSubVersion { get; set; } = "1.21.4";

        //////////////////////////////////////////////////////////////////////
        // MOD SETTINGS
        //////////////////////////////////////////////////////////////////////

        public Dictionary<string, bool> OptiFineEnabledByVersion { get; set; } =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        public bool IsLithiumEnabled { get; set; } = false;
        public bool IsOptiFineEnabled { get; set; } = false;
        public bool IsSodiumEnabled { get; set; } = true;

        // This list now correctly refers to the top-level InstalledMod class
        public List<InstalledMod> InstalledMods { get; set; } = new List<InstalledMod>();

        //////////////////////////////////////////////////////////////////////
        // SKINS SETTINGS
        //////////////////////////////////////////////////////////////////////
        public List<SkinInfo> CustomSkins { get; set; } = new List<SkinInfo>();
        public string? SelectedSkinId { get; set; }


        //////////////////////////////////////////////////////////////////////
        // CONSTRUCTOR WITH DEFAULT VALUES
        //////////////////////////////////////////////////////////////////////
        public LauncherSettings()
        {
        }

        //////////////////////////////////////////////////////////////////////
        // HELPER METHODS
        //////////////////////////////////////////////////////////////////////

        public int GetMaxRamInMb()
        {
            var parts = MaxRamAllocationGb.Split(' ');
            if (parts.Length > 0 && int.TryParse(parts[0], out int gb))
            {
                return gb * 1024;
            }
            return 8192;
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
            return new LauncherSettings
            {
                IsFirstLaunch = this.IsFirstLaunch,
                IsLoggedIn = this.IsLoggedIn,
                AccountType = this.AccountType,
                OfflineUsername = this.OfflineUsername,
                SessionUsername = this.SessionUsername,
                SessionUuid = this.SessionUuid,
                SessionAccessToken = this.SessionAccessToken,
                SessionXuid = this.SessionXuid,
                LaunchOnStartup = this.LaunchOnStartup,
                MinimizeToTray = this.MinimizeToTray,
                DiscordRichPresence = this.DiscordRichPresence,
                EnablePrayerTimeReminder = this.EnablePrayerTimeReminder,                 // NEW
                PrayerTimeCountry = this.PrayerTimeCountry,                               // NEW
                PrayerTimeCity = this.PrayerTimeCity,                                     // NEW
                PrayerTimeCalculationMethod = this.PrayerTimeCalculationMethod,           // NEW
                PrayerReminderMinutesBefore = this.PrayerReminderMinutesBefore,
                ShowUsernameInDiscordRichPresence = this.ShowUsernameInDiscordRichPresence, // NEW
                LauncherVisibilityOnGameLaunch = this.LauncherVisibilityOnGameLaunch,     // NEW
                MinRamAllocationMb = this.MinRamAllocationMb,
                MaxRamAllocationGb = this.MaxRamAllocationGb,
                QuickJoinServerAddress = this.QuickJoinServerAddress,
                QuickJoinServerPort = this.QuickJoinServerPort,
                QuickLaunchEnabled = this.QuickLaunchEnabled,
                JvmArguments = this.JvmArguments,
                UseCustomGameResolution = this.UseCustomGameResolution,                   // NEW
                GameResolutionWidth = this.GameResolutionWidth,                           // NEW
                GameResolutionHeight = this.GameResolutionHeight,                         // NEW
                LockGameAspectRatio = this.LockGameAspectRatio,                           // NEW
                SelectedAddonByVersion = new Dictionary<string, string>(this.SelectedAddonByVersion, StringComparer.OrdinalIgnoreCase),
                SelectedFabricProfileName = this.SelectedFabricProfileName,
                MouseSensitivity = this.MouseSensitivity,
                ScrollSensitivity = this.ScrollSensitivity,
                AutoJump = this.AutoJump,
                Touchscreen = this.Touchscreen,
                ToggleSprint = this.ToggleSprint,
                ToggleCrouch = this.ToggleCrouch,
                Subtitles = this.Subtitles,
                RenderDistance = this.RenderDistance,
                SimulationDistance = this.SimulationDistance,
                EntityDistance = this.EntityDistance,
                MaxFps = this.MaxFps,
                VSync = this.VSync,
                Fullscreen = this.Fullscreen,
                EntityShadows = this.EntityShadows,
                HighContrast = this.HighContrast,
                RenderClouds = this.RenderClouds,
                PlayerHat = this.PlayerHat,
                PlayerCape = this.PlayerCape,
                PlayerJacket = this.PlayerJacket,
                PlayerLeftSleeve = this.PlayerLeftSleeve,
                PlayerRightSleeve = this.PlayerRightSleeve,
                PlayerLeftPant = this.PlayerLeftPant,
                PlayerRightPant = this.PlayerRightPant,
                PlayerMainHand = this.PlayerMainHand,
                Theme = this.Theme,
                AnimationsEnabled = this.AnimationsEnabled,
                GameUpdateDelivery = this.GameUpdateDelivery,                             // NEW
                ClosingNotificationsPreference = this.ClosingNotificationsPreference,     // NEW
                EnableUpdateNotifications = this.EnableUpdateNotifications,               // NEW
                EnableNewContentIndicators = this.EnableNewContentIndicators,             // NEW
                SelectedMajorVersion = this.SelectedMajorVersion,
                SelectedSubVersion = this.SelectedSubVersion,
                SelectedSkinId = this.SelectedSkinId,
                OptiFineEnabledByVersion = new Dictionary<string, bool>(this.OptiFineEnabledByVersion, StringComparer.OrdinalIgnoreCase),
                CustomSkins = new List<SkinInfo>(this.CustomSkins.Select(s => new SkinInfo
                {
                    Id = s.Id,
                    Name = s.Name,
                    FilePath = s.FilePath,
                    CreatedDate = s.CreatedDate,
                    ModifiedDate = s.ModifiedDate
                })),
                InstalledMods = new List<InstalledMod>(this.InstalledMods.Select(m => new InstalledMod // Clone InstalledMods
                {
                    ModId = m.ModId,
                    Name = m.Name,
                    Description = m.Description,
                    Version = m.Version,
                    MinecraftVersion = m.MinecraftVersion,
                    FileName = m.FileName,
                    DownloadUrl = m.DownloadUrl,
                    Enabled = m.Enabled,
                    InstallDate = m.InstallDate,
                    IconUrl = m.IconUrl
                }))
            };
        }

        public bool Validate()
        {
            if (MinRamAllocationMb < 512 || MinRamAllocationMb > 32768)
                return false;
            if (RenderDistance < 2 || RenderDistance > 64)
                return false;
            if (SimulationDistance < 5 || SimulationDistance > 64)
                return false;
            // MaxFps can be 0 for unlimited, so check for valid range when not 0
            if (MaxFps != 0 && (MaxFps < 30 || MaxFps > 300))
                return false;
            if (Theme != "Dark" && Theme != "Light" && Theme != "Auto")
                return false;
            if (!string.IsNullOrEmpty(AccountType) &&
                AccountType != "microsoft" &&
                AccountType != "offline")
                return false;

            if (EnablePrayerTimeReminder)
            {
                if (string.IsNullOrWhiteSpace(PrayerTimeCountry)) return false;
                if (string.IsNullOrWhiteSpace(PrayerTimeCity)) return false;
                if (PrayerReminderMinutesBefore < 10) return false;
            }

            // NEW: Game Resolution validation
            if (UseCustomGameResolution)
            {
                if (GameResolutionWidth <= 0 || GameResolutionWidth > 7680) return false; // Max 8K width
                if (GameResolutionHeight <= 0 || GameResolutionHeight > 4320) return false; // Max 8K height
            }

            // NEW: Launcher Visibility validation
            if (LauncherVisibilityOnGameLaunch != LauncherVisibility.KeepOpen && LauncherVisibilityOnGameLaunch != LauncherVisibility.Hide)
                return false;

            // NEW: Update Delivery validation
            if (GameUpdateDelivery != UpdateDelivery.Normal && GameUpdateDelivery != UpdateDelivery.EarlyOptIn && GameUpdateDelivery != UpdateDelivery.LateOptOut)
                return false;

            // NEW: Notification Preference validation
            if (ClosingNotificationsPreference != NotificationPreference.Always && ClosingNotificationsPreference != NotificationPreference.JustOnce && ClosingNotificationsPreference != NotificationPreference.Never)
                return false;

            return true;
        }

        // AOT-FIX: Replaced reflection-based property copying with manual assignment.
        // The original method used typeof().GetProperties(), which is not compatible with AOT compilation
        // because it relies on runtime reflection that can be trimmed away.
        public void ResetToDefaults()
        {
            var defaults = new LauncherSettings();

            // Preserve authentication data
            var tempIsLoggedIn = this.IsLoggedIn;
            var tempAccountType = this.AccountType;
            var tempOfflineUsername = this.OfflineUsername;
            var tempSessionUsername = this.SessionUsername;
            var tempSessionUuid = this.SessionUuid;
            var tempSessionAccessToken = this.SessionAccessToken;
            var tempSessionXuid = this.SessionXuid;
            var tempSelectedFabricProfileName = this.SelectedFabricProfileName;

            // Manually reset all other properties to their defaults
            this.IsFirstLaunch = defaults.IsFirstLaunch;
            this.LaunchOnStartup = defaults.LaunchOnStartup;
            this.MinimizeToTray = defaults.MinimizeToTray;
            this.DiscordRichPresence = defaults.DiscordRichPresence;
            this.ShowUsernameInDiscordRichPresence = defaults.ShowUsernameInDiscordRichPresence; // NEW
            this.LauncherVisibilityOnGameLaunch = defaults.LauncherVisibilityOnGameLaunch;     // NEW
            this.CustomSkins = new List<SkinInfo>(); // Reset custom skins list
            this.SelectedSkinId = defaults.SelectedSkinId;
            this.MinRamAllocationMb = defaults.MinRamAllocationMb;
            this.MaxRamAllocationGb = defaults.MaxRamAllocationGb;
            this.QuickJoinServerAddress = defaults.QuickJoinServerAddress;
            this.QuickJoinServerPort = defaults.QuickJoinServerPort;
            this.QuickLaunchEnabled = defaults.QuickLaunchEnabled;
            this.JvmArguments = defaults.JvmArguments;
            this.UseCustomGameResolution = defaults.UseCustomGameResolution;                   // NEW
            this.GameResolutionWidth = defaults.GameResolutionWidth;                           // NEW
            this.GameResolutionHeight = defaults.GameResolutionHeight;                         // NEW
            this.LockGameAspectRatio = defaults.LockGameAspectRatio;                           // NEW
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
            this.CustomServers = new List<ServerInfo>(); // Reset custom servers list
            this.PlayerHat = defaults.PlayerHat;
            this.PlayerCape = defaults.PlayerCape;
            this.PlayerJacket = defaults.PlayerJacket;
            this.PlayerLeftSleeve = defaults.PlayerLeftSleeve;
            this.PlayerRightSleeve = defaults.PlayerRightSleeve;
            this.PlayerLeftPant = defaults.PlayerLeftPant;
            this.PlayerRightPant = defaults.PlayerRightPant;
            this.PlayerMainHand = defaults.PlayerMainHand;
            this.Theme = defaults.Theme;
            this.AnimationsEnabled = defaults.AnimationsEnabled;
            this.GameUpdateDelivery = defaults.GameUpdateDelivery;                             // NEW
            this.ClosingNotificationsPreference = defaults.ClosingNotificationsPreference;     // NEW
            this.EnableUpdateNotifications = defaults.EnableUpdateNotifications;               // NEW
            this.EnableNewContentIndicators = defaults.EnableNewContentIndicators;             // NEW
            this.EnablePrayerTimeReminder = defaults.EnablePrayerTimeReminder;                 // NEW
            this.PrayerTimeCountry = defaults.PrayerTimeCountry;                               // NEW
            this.PrayerTimeCity = defaults.PrayerTimeCity;                                     // NEW
            this.PrayerTimeCalculationMethod = defaults.PrayerTimeCalculationMethod;           // NEW
            this.PrayerReminderMinutesBefore = defaults.PrayerReminderMinutesBefore;           // NEW
            this.SelectedMajorVersion = defaults.SelectedMajorVersion;
            this.SelectedSubVersion = defaults.SelectedSubVersion;
            this.OptiFineEnabledByVersion = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            this.IsLithiumEnabled = defaults.IsLithiumEnabled;
            this.IsOptiFineEnabled = defaults.IsOptiFineEnabled;
            this.IsSodiumEnabled = defaults.IsSodiumEnabled;
            this.InstalledMods = new List<InstalledMod>(); // Reset installed mods list

            // Restore authentication data
            this.IsLoggedIn = tempIsLoggedIn;
            this.AccountType = tempAccountType;
            this.OfflineUsername = tempOfflineUsername;
            this.SessionUsername = tempSessionUsername;
            this.SessionUuid = tempSessionUuid;
            this.SessionAccessToken = tempSessionAccessToken;
            this.SessionXuid = tempSessionXuid;
            this.SelectedFabricProfileName = tempSelectedFabricProfileName;
        }
    }
}

public class ModCleanupEntry
{
    public string ModId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string MinecraftVersion { get; set; } = string.Empty;
    public DateTime AddedDate { get; set; } = DateTime.Now; // To track when it was added
}
