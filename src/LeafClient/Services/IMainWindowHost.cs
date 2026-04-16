using CmlLib.Core;
using CmlLib.Core.Auth;
using LeafClient.Models;
using System.Threading.Tasks;

namespace LeafClient.Services
{
    public interface IMainWindowHost
    {
        MSession? CurrentSession { get; }
        MinecraftLauncher? Launcher { get; }
        string MinecraftFolder { get; }

        LauncherSettings CurrentSettings { get; }
        SettingsService SettingsService { get; }

        void SwitchToPage(int index);

        Task<byte[]?> FetchSkinBytesAsync();
        bool IsCosmeticEquipped(string cosmeticId, string category);

        string? LeafIdentifier { get; }

        int CoinBalance { get; }

        void OpenCheckout(string url);

        void ShowPurchaseChoice(string itemId, string itemName, string preview, string rarity, string priceText, int coinPrice, string checkoutUrl);

        bool IsOwned(string cosmeticId);
        void AddOwnedCosmetic(string cosmeticId);

        void ShowPurchaseCelebration(string id, string name, string preview, string rarity);

        void ShowMonthlyPassPopup();

        void RefreshLeafPlusPrices();

        void UpdateCoinBalance(int newBalance);
    }
}
