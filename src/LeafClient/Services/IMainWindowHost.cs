using CmlLib.Core;
using CmlLib.Core.Auth;
using LeafClient.Models;
using System.Threading.Tasks;

namespace LeafClient.Services
{
    /// <summary>
    /// Abstraction over MainWindow that extracted page UserControls can use
    /// to access session state, settings, navigation and shared utilities.
    /// </summary>
    public interface IMainWindowHost
    {
        // Session
        MSession? CurrentSession { get; }
        MinecraftLauncher? Launcher { get; }
        string MinecraftFolder { get; }

        // Settings
        LauncherSettings CurrentSettings { get; }
        SettingsService SettingsService { get; }

        // Navigation
        void SwitchToPage(int index);

        // Skin helpers shared across pages
        Task<byte[]?> FetchSkinBytesAsync();
        bool IsCosmeticEquipped(string cosmeticId, string category);

        // Leaf API identity (JWT minecraft_username — UUID for Microsoft accounts, username for cracked)
        string? LeafIdentifier { get; }

        // Store checkout
        void OpenCheckout(string url);

        // Owned cosmetics
        bool IsOwned(string cosmeticId);
        void AddOwnedCosmetic(string cosmeticId);

        // Purchase celebration
        void ShowPurchaseCelebration(string id, string name, string preview, string rarity);

        // Monthly pass perks popup
        void ShowMonthlyPassPopup();

        // Refresh Leaf+ popup prices to the currently selected currency
        void RefreshLeafPlusPrices();

        // Update the coin balance widget
        void UpdateCoinBalance(int newBalance);
    }
}
