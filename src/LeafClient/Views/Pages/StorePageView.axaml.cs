using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using LeafClient.Models;
using LeafClient.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace LeafClient.Views.Pages
{
    /// <summary>Wraps a <see cref="CurrencyInfo"/> with a lazily-loaded flag bitmap for the ComboBox.</summary>
    public sealed class CurrencyListItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public CurrencyInfo Info { get; }

        private Bitmap? _flagBitmap;
        public Bitmap? FlagBitmap
        {
            get => _flagBitmap;
            set { _flagBitmap = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FlagBitmap))); }
        }

        public CurrencyListItem(CurrencyInfo info) { Info = info; }
    }

    public partial class StorePageView : UserControl
    {
        private IMainWindowHost? _host;

        private WrapPanel? _storeGrid;
        private LeafClient.Controls.SkinRendererControl? _storeRenderer;
        private string _storeActiveTab = "all";
        private string _storeActiveRarity = "all"; // all | Legendary | Epic | Rare | Common
        private string _storeSearchQuery  = "";
        private bool _storeInitialized = false;
        private Border? _storePreviewCard;
        private TextBlock? _storePreviewName;
        private TextBlock? _storePreviewDesc;
        private Border? _storePreviewRarityBadge;
        private TextBlock? _storePreviewRarityText;
        private TextBlock? _storePreviewPrice;
        private Border? _storePreviewActionBtn;
        private TextBlock? _storePreviewActionText;
        private Border? _storePreviewCoinBtn;
        private TextBlock? _storePreviewCoinText;
        private byte[]? _storeSkinCache;
        private bool _storePreviewLightOn;
        private string? _previewedItemId;

        // Currency selector controls
        private ComboBox? _currencySelector;
        private TextBlock? _storePillPriceText;
        private TextBlock? _storeSubPriceText;
        private readonly List<CurrencyListItem> _currencyItems =
            CurrencyService.SupportedCurrencies.Select(c => new CurrencyListItem(c)).ToList();

        private static readonly Dictionary<string, string> CheckoutUrls = new()
        {
            ["wings-angel"]       = "https://leafclient.lemonsqueezy.com/checkout/buy/22224900-456c-4302-949b-a1aa05121602",
            ["aura-broken-hearts"]= "https://leafclient.lemonsqueezy.com/checkout/buy/f525eccc-6ee3-4ff0-b97e-eea836847208",
            ["wings-dragon"]      = "https://leafclient.lemonsqueezy.com/checkout/buy/ecd21dbd-9aa3-45b9-9afa-ea931e0e8e14",
            ["hat-crown"]         = "https://leafclient.lemonsqueezy.com/checkout/buy/c8cc19c0-a9d8-4b79-b64e-88ec15ea7c2f",
            ["aura-inferno"]      = "https://leafclient.lemonsqueezy.com/checkout/buy/031569d7-6297-4376-8f03-0b133272e558",
            ["wings-black"]       = "https://leafclient.lemonsqueezy.com/checkout/buy/01f8bc6c-abe9-4ffb-a30a-c0f6deffb157",
            ["hat-shadow-horns"]  = "https://leafclient.lemonsqueezy.com/checkout/buy/1052435e-151a-4518-90cd-ac81eb08f1fe",
            ["wings-purple"]      = "https://leafclient.lemonsqueezy.com/checkout/buy/d5e35f6f-3a41-406d-b69a-973588451bc5",
        };

        /// <summary>
        /// Base EUR prices for each paid cosmetic. Free items are not present here.
        /// These are used to convert to the user's selected currency.
        /// </summary>
        private static readonly Dictionary<string, int> CoinPrices = new()
        {
            ["wings-angel"]        = 600,
            ["wings-dragon"]       = 800,
            ["wings-purple"]       = 900,
            ["wings-black"]        = 500,
            ["hat-crown"]          = 600,
            ["hat-shadow-horns"]   = 400,
            ["aura-inferno"]       = 200,
            ["aura-broken-hearts"] = 200,
        };

        private static readonly Dictionary<string, decimal> BasePricesEur = new()
        {
            ["wings-angel"]        = 5.99m,
            ["wings-dragon"]       = 7.99m,
            ["wings-purple"]       = 8.99m,
            ["wings-black"]        = 4.99m,
            ["hat-crown"]          = 5.99m,
            ["hat-shadow-horns"]   = 3.99m,
            ["aura-inferno"]       = 1.99m,
            ["aura-broken-hearts"] = 1.99m,
        };

        public StorePageView()
        {
            InitializeComponent();
        }

        public void SetHost(IMainWindowHost host)
        {
            _host = host;
        }

        // ── Store Catalog data ──────────────────────────────────────────────────
        // Only items that are actually published on LemonSqueezy are listed here.
        // BUY NOW is disabled for items whose CheckoutUrl is empty (URL not yet created).
        internal static readonly (string Id, string Name, string Category, string Rarity, string Description, string Preview, string Price, bool Available)[] StoreCatalog =
        {
            ("wings-angel",        "Angel Wings",          "wings", "Legendary", "Radiant feathered wings of pure light \u2014 graceful and divine.",          "\U0001f54a\ufe0f", "\u20ac5.99", true),
            ("wings-dragon",       "Crimson Dragon Wings", "wings", "Epic",      "Demonic bat wings with deep crimson membrane and glowing vein lines.",        "\U0001f987",       "\u20ac7.99", true),
            ("wings-purple",       "Void Demon Wings",     "wings", "Epic",      "Otherworldly purple demon wings pulsing with arcane energy.",                 "\U0001f49c",       "\u20ac8.99", true),
            ("wings-black",        "Shadow Demon Wings",   "wings", "Epic",      "Pitch-black demon wings that melt into the darkness.",                        "\U0001f5a4",       "\u20ac4.99", true),
            ("hat-crown",          "Golden Crown",         "hats",  "Rare",      "A hand-crafted crown of solid gold \u2014 dripping in royalty.",              "\U0001f451",       "\u20ac5.99", true),
            ("hat-shadow-horns",   "Shadow Horns",         "hats",  "Rare",      "Dark curved horns that emerge from the shadows.",                             "\U0001f608",       "\u20ac3.99", true),
            ("aura-inferno",       "Inferno Aura",         "auras", "Common",    "Scorching flames that dance around you with every step.",                     "\U0001f525",       "\u20ac1.99", true),
            ("aura-broken-hearts", "Broken Hearts Aura",   "auras", "Common",    "Shattered heart particles that swirl around you endlessly.",                  "\U0001f494",       "\u20ac1.99", true),
            ("aura-darkness",      "Darkness Aura",        "auras", "Rare",      "A shadowy aura of pure darkness that follows your every move.",               "\U0001f311",       "FREE",       true),
            ("cape-leaf",          "Leaf Cape",            "capes", "Common",    "The signature Leaf Client cape \u2014 free for all players.",                 "\U0001f343",       "FREE",       true),
        };

        // ── Helpers ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the display price string for an item, converted to the user's
        /// selected currency. Free items always return "FREE".
        /// </summary>
        private static string GetDisplayPrice(string itemId, string rawPrice)
        {
            if (rawPrice == "FREE") return "FREE";
            if (BasePricesEur.TryGetValue(itemId, out var eurBase))
                return CurrencyService.FormatPrice(eurBase);
            return rawPrice; // fallback: show original string unchanged
        }

        private void InitializeStoreControls()
        {
            _storeGrid              = this.FindControl<WrapPanel>("StoreGrid");
            _storePreviewCard       = this.FindControl<Border>("StorePreviewCard");
            _storePreviewName       = this.FindControl<TextBlock>("StorePreviewName");
            _storePreviewDesc       = this.FindControl<TextBlock>("StorePreviewDesc");
            _storePreviewRarityBadge = this.FindControl<Border>("StorePreviewRarityBadge");
            _storePreviewRarityText  = this.FindControl<TextBlock>("StorePreviewRarityText");
            _storePreviewPrice      = this.FindControl<TextBlock>("StorePreviewPrice");
            _storePreviewActionBtn  = this.FindControl<Border>("StorePreviewActionBtn");
            _storePreviewActionText = this.FindControl<TextBlock>("StorePreviewActionText");
            _storePreviewCoinBtn    = this.FindControl<Border>("StorePreviewCoinBtn");
            _storePreviewCoinText   = this.FindControl<TextBlock>("StorePreviewCoinText");
            _storePillPriceText     = this.FindControl<TextBlock>("StorePillPriceText");
            _storeSubPriceText      = this.FindControl<TextBlock>("StoreSubPriceText");

            if (_storePreviewActionBtn != null)
                _storePreviewActionBtn.Tapped += OnStorePreviewActionTapped;

            // Currency selector
            _currencySelector = this.FindControl<ComboBox>("CurrencySelector");
            if (_currencySelector != null)
            {
                _currencySelector.ItemsSource = _currencyItems;

                // Restore the saved currency from settings (defaults to "EUR")
                var savedCode = _host?.CurrentSettings?.SelectedCurrency ?? "EUR";
                CurrencyService.SelectedCode = savedCode;

                var selected = _currencyItems.FirstOrDefault(c => c.Info.Code == savedCode)
                    ?? _currencyItems[0];
                _currencySelector.SelectedItem = selected;

                // Handler is wired via AXAML (SelectionChanged="OnCurrencyChanged")
            }

            var host = this.FindControl<Border>("StoreRendererHost");
            if (host != null)
            {
                try
                {
                    _storeRenderer = new LeafClient.Controls.SkinRendererControl
                    {
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                        VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Stretch
                    };
                    host.Child = _storeRenderer;
                    Console.WriteLine("[Store] 3D store preview renderer initialized.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Store] Renderer init failed: {ex.Message}");
                    _storeRenderer = null;
                }
            }
        }

        public async void LoadStorePage()
        {
            if (!_storeInitialized)
            {
                InitializeStoreControls();
                _storeInitialized = true;

                // Fetch live rates in the background; UI refresh happens after completion.
                _ = Task.Run(async () =>
                {
                    await CurrencyService.TryFetchLiveRatesAsync();
                    // Refresh prices on the UI thread once live rates arrive.
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(RefreshCurrencyDisplay);
                });

                // Load flag images asynchronously — items update via INotifyPropertyChanged.
                _ = LoadFlagsAsync();
            }

            if (_storeRenderer != null && _storeSkinCache == null && _host != null)
            {
                _storeSkinCache = await _host.FetchSkinBytesAsync();
                if (_storeSkinCache != null)
                    _storeRenderer.UpdateSkinTexture(_storeSkinCache);
            }
            else if (_storeRenderer != null && _storeSkinCache != null)
            {
                _storeRenderer.UpdateSkinTexture(_storeSkinCache);
            }

            if (_storeRenderer != null && _host?.CurrentSettings?.Equipped != null)
            {
                CosmeticHelpers.ApplyEquippedToRenderer(_storeRenderer, _host.CurrentSettings.Equipped);
            }

            UpdateStorePreviewPanel(StoreCatalog[0]);
            HighlightStoreTab(_storeActiveTab);
            PopulateStoreGrid();
            RefreshSubscriptionPrices();
        }

        // ── Currency ─────────────────────────────────────────────────────────────

        private async Task LoadFlagsAsync()
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            foreach (var item in _currencyItems)
            {
                try
                {
                    var url = $"https://flagcdn.com/w40/{item.Info.FlagCode}.png";
                    var bytes = await http.GetByteArrayAsync(url);
                    using var ms = new MemoryStream(bytes);
                    var bmp = new Bitmap(ms);
                    // Set on UI thread so INotifyPropertyChanged triggers properly
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => item.FlagBitmap = bmp);
                }
                catch
                {
                    // Flag CDN unavailable — leave FlagBitmap null, shows empty circle
                }
            }
        }

        private void OnCurrencyChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_currencySelector?.SelectedItem is not CurrencyListItem listItem) return;
            var info = listItem.Info;

            CurrencyService.SelectedCode = info.Code;

            // Persist to settings
            if (_host?.CurrentSettings != null)
            {
                _host.CurrentSettings.SelectedCurrency = info.Code;
                _ = _host.SettingsService.SaveSettingsAsync(_host.CurrentSettings);
            }

            RefreshCurrencyDisplay();
            _host?.RefreshLeafPlusPrices();
        }

        /// <summary>
        /// Re-renders all price text to reflect the currently selected currency.
        /// Efficient: rebuilds only the store grid (cards contain price TextBlocks)
        /// and updates the preview panel and subscription price indicators.
        /// </summary>
        private void RefreshCurrencyDisplay()
        {
            PopulateStoreGrid();
            RefreshSubscriptionPrices();

            // Refresh the preview panel price for whichever item is currently shown
            if (_previewedItemId != null)
            {
                var item = Array.Find(StoreCatalog, c => c.Id == _previewedItemId);
                if (item.Id != null)
                    UpdateStorePreviewPanel(item);
            }
        }

        private void RefreshSubscriptionPrices()
        {
            var subMonthly = CurrencyService.FormatPrice(4.99m);
            if (_storePillPriceText != null)
                _storePillPriceText.Text = $"from {subMonthly}/mo";
            if (_storeSubPriceText != null)
                _storeSubPriceText.Text = subMonthly;
        }

        // ── Grid ─────────────────────────────────────────────────────────────────

        private void PopulateStoreGrid()
        {
            if (_storeGrid == null) return;
            _storeGrid.Children.Clear();

            string q = (_storeSearchQuery ?? "").Trim();
            var filtered = StoreCatalog
                .Where(c => _storeActiveTab == "all" || c.Category == _storeActiveTab)
                .Where(c => _storeActiveRarity == "all" || c.Rarity == _storeActiveRarity)
                .Where(c => q.Length == 0
                            || c.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                            || c.Description.Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            foreach (var item in filtered)
            {
                var card = CreateStoreCard(item);
                _storeGrid.Children.Add(card);
            }
        }

        private void OnStoreSearchChanged(object? sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                _storeSearchQuery = tb.Text ?? "";
                PopulateStoreGrid();
            }
        }

        private void OnMonthlyPassTapped(object? sender, TappedEventArgs e)
        {
            _host?.ShowMonthlyPassPopup();
        }

        private void OnStoreRarityTapped(object? sender, TappedEventArgs e)
        {
            if (sender is Border b && b.Tag is string tag)
            {
                _storeActiveRarity = tag;
                HighlightStoreRarity(tag);
                PopulateStoreGrid();
            }
        }

        private void HighlightStoreRarity(string activeRarity)
        {
            foreach (var (name, tag) in new[]
            {
                ("StoreRarity_All",       "all"),
                ("StoreRarity_Legendary", "Legendary"),
                ("StoreRarity_Epic",      "Epic"),
                ("StoreRarity_Rare",      "Rare"),
                ("StoreRarity_Common",    "Common"),
            })
            {
                var chip = this.FindControl<Border>(name);
                if (chip == null) continue;

                bool isActive = tag == activeRarity;
                chip.Background  = isActive
                    ? SolidColorBrush.Parse("#9333EA")
                    : SolidColorBrush.Parse("#0F1A24");
                chip.BorderBrush = isActive
                    ? SolidColorBrush.Parse("#9333EA")
                    : SolidColorBrush.Parse("#1C2A38");
                chip.BorderThickness = new Thickness(1);

                if (chip.Child is TextBlock tb)
                {
                    if (isActive)
                        tb.Foreground = Brushes.White;
                    else
                    {
                        tb.Foreground = tag switch
                        {
                            "Legendary" => SolidColorBrush.Parse("#F59E0B"),
                            "Epic"      => SolidColorBrush.Parse("#A855F7"),
                            "Rare"      => SolidColorBrush.Parse("#3B82F6"),
                            "Common"    => SolidColorBrush.Parse("#6B7280"),
                            _           => SolidColorBrush.Parse("#9CA3AF"),
                        };
                    }
                }
            }
        }

        private Border CreateStoreCard(
            (string Id, string Name, string Category, string Rarity, string Description, string Preview, string Price, bool Available) item)
        {
            var (rarityMain, rarityGlow, rarityBg) = item.Rarity switch
            {
                "Legendary" => ("#F59E0B", "#92400E", "#1A1408"),
                "Epic"      => ("#A855F7", "#6B21A8", "#110C1A"),
                "Rare"      => ("#3B82F6", "#1E3A8A", "#0C1320"),
                _           => ("#6B7280", "#374151", "#0F1318")
            };

            var mainColor = Color.Parse(rarityMain);
            var glowColor = Color.Parse(rarityGlow);
            bool owned    = _host?.IsOwned(item.Id) ?? false;
            bool isFree   = item.Price == "FREE";
            bool hasUrl   = CheckoutUrls.TryGetValue(item.Id, out var itemCheckoutUrl)
                            && !string.IsNullOrWhiteSpace(itemCheckoutUrl);
            bool isComingSoon = !item.Available || (!hasUrl && !isFree);

            var card = new Border
            {
                Width           = 178,
                Height          = 310,
                CornerRadius    = new CornerRadius(18),
                BorderBrush     = item.Available
                    ? new SolidColorBrush(new Color(0x55, glowColor.R, glowColor.G, glowColor.B))
                    : SolidColorBrush.Parse("#25FFFFFF"),
                BorderThickness = new Thickness(1),
                Margin          = new Thickness(6, 6, 14, 14),
                Cursor          = item.Available
                    ? new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
                    : new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow),
                Tag             = item.Id,
                ClipToBounds    = false,
                Background      = SolidColorBrush.Parse(rarityBg),
                Opacity         = item.Available ? 1.0 : 0.6
            };

            var scaleTransform = new ScaleTransform(1.0, 1.0);
            card.RenderTransform = scaleTransform;
            card.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

            var outerStack = new StackPanel();

            var previewBorder = new Border
            {
                Height       = 130,
                Margin       = new Thickness(7, 7, 7, 0),
                CornerRadius = new CornerRadius(13),
                ClipToBounds = true
            };

            var previewGrad = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
                EndPoint   = new RelativePoint(0.5, 1, RelativeUnit.Relative)
            };
            previewGrad.GradientStops.Add(new GradientStop(new Color(0x40, glowColor.R, glowColor.G, glowColor.B), 0));
            previewGrad.GradientStops.Add(new GradientStop(Color.Parse("#060A10"), 1));
            previewBorder.Background = previewGrad;

            var effectGrid = new Grid();

            var glowCircle = new Ellipse
            {
                Width   = 52,
                Height  = 52,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Center,
                Opacity = 0.18
            };
            glowCircle.Fill = new SolidColorBrush(mainColor);
            effectGrid.Children.Add(glowCircle);

            effectGrid.Children.Add(new Viewbox
            {
                Width               = 36,
                Height              = 36,
                Stretch             = Stretch.Uniform,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text                = item.Available ? item.Preview : "\U0001f512",
                    FontSize            = 44,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Center,
                    Opacity             = item.Available ? 1.0 : 0.45,
                }
            });

            if (!item.Available)
            {
                effectGrid.Children.Add(new Border
                {
                    Background          = SolidColorBrush.Parse("#CC0D1117"),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                    VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Bottom,
                    Height              = 22,
                    Child = new TextBlock
                    {
                        Text                = "COMING SOON",
                        Foreground          = SolidColorBrush.Parse("#90FFFFFF"),
                        FontSize            = 8.5,
                        FontWeight          = FontWeight.ExtraBold,
                        LetterSpacing       = 1.0,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Center
                    }
                });
            }

            previewBorder.Child = effectGrid;
            outerStack.Children.Add(previewBorder);

            var infoPanel = new StackPanel { Spacing = 4, Margin = new Thickness(11, 8, 11, 4) };

            infoPanel.Children.Add(new TextBlock
            {
                Text         = item.Name,
                Foreground   = Brushes.White,
                FontWeight   = FontWeight.Bold,
                FontSize     = 14,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            var badge = new Border
            {
                Background      = new SolidColorBrush(new Color(0x28, mainColor.R, mainColor.G, mainColor.B)),
                BorderBrush     = new SolidColorBrush(new Color(0x55, mainColor.R, mainColor.G, mainColor.B)),
                BorderThickness = new Thickness(1),
                CornerRadius    = new CornerRadius(20),
                Padding         = new Thickness(8, 2.5),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
            };
            badge.Child = new TextBlock
            {
                Text          = item.Rarity.ToUpper(),
                Foreground    = new SolidColorBrush(mainColor),
                FontSize      = 8.5,
                FontWeight    = FontWeight.ExtraBold,
                LetterSpacing = 0.8
            };
            infoPanel.Children.Add(badge);

            infoPanel.Children.Add(new TextBlock
            {
                Text         = item.Description,
                Foreground   = SolidColorBrush.Parse("#80FFFFFF"),
                FontSize     = 10.5,
                TextWrapping = TextWrapping.Wrap,
                MaxLines     = 2
            });

            outerStack.Children.Add(infoPanel);

            var bottomStack = new StackPanel { Margin = new Thickness(10, 4, 10, 9), Spacing = 5 };

            // Display price converted to the user's selected currency
            string displayPrice = item.Available ? GetDisplayPrice(item.Id, item.Price) : "\u2014";

            var priceText = new TextBlock
            {
                Text                = displayPrice,
                Foreground          = !item.Available
                    ? SolidColorBrush.Parse("#55FFFFFF")
                    : isFree
                        ? SolidColorBrush.Parse("#4ADE80")
                        : new SolidColorBrush(mainColor),
                FontWeight          = FontWeight.Bold,
                FontSize            = 14.5,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
            };
            bottomStack.Children.Add(priceText);

            if (!isFree && item.Available && CoinPrices.TryGetValue(item.Id, out var coinAmt))
            {
                bottomStack.Children.Add(new TextBlock
                {
                    Text                = $"\U0001f343 {coinAmt:N0}",
                    Foreground          = SolidColorBrush.Parse("#4ADE80"),
                    FontSize            = 11,
                    FontWeight          = FontWeight.SemiBold,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
                });
            }

            IBrush buyBg;
            IBrush buyBorder;
            IBrush buyFg;
            string buyText;
            bool buyClickable;

            if (owned)
            {
                buyBg      = SolidColorBrush.Parse("#2022C55E");
                buyBorder  = SolidColorBrush.Parse("#22C55E");
                buyFg      = SolidColorBrush.Parse("#4ADE80");
                buyText    = "OWNED \u2713";
                buyClickable = false;
            }
            else if (isComingSoon)
            {
                buyBg      = SolidColorBrush.Parse("#15FFFFFF");
                buyBorder  = SolidColorBrush.Parse("#22FFFFFF");
                buyFg      = SolidColorBrush.Parse("#44FFFFFF");
                buyText    = "COMING SOON";
                buyClickable = false;
            }
            else if (isFree)
            {
                buyBg      = SolidColorBrush.Parse("#20228B45");
                buyBorder  = SolidColorBrush.Parse("#4ADE80");
                buyFg      = SolidColorBrush.Parse("#4ADE80");
                buyText    = "GET FREE";
                buyClickable = true;
            }
            else
            {
                buyBg      = new SolidColorBrush(new Color(0x30, mainColor.R, mainColor.G, mainColor.B));
                buyBorder  = new SolidColorBrush(new Color(0x65, mainColor.R, mainColor.G, mainColor.B));
                buyFg      = Brushes.White;
                buyText    = "BUY NOW";
                buyClickable = true;
            }

            var buyBtn = new Border
            {
                Background      = buyBg,
                BorderBrush     = buyBorder,
                BorderThickness = new Thickness(1),
                CornerRadius    = new CornerRadius(10),
                Height          = 36,
                Cursor          = buyClickable
                    ? new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
                    : new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Arrow),
            };
            buyBtn.Child = new TextBlock
            {
                Text                = buyText,
                Foreground          = buyFg,
                FontSize            = 10,
                FontWeight          = FontWeight.ExtraBold,
                LetterSpacing       = 0.5,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Center
            };

            if (buyClickable && isFree && !owned)
            {
                var itemCaptureFree = item;
                buyBtn.Tapped += (_, _) => OnCardFreeClaimTapped(itemCaptureFree);
            }
            else if (buyClickable && !isFree)
            {
                var itemCapturePaid = item;
                buyBtn.Tapped += (_, _) => OpenPurchasePopupForItem(itemCapturePaid);
            }

            bottomStack.Children.Add(buyBtn);
            outerStack.Children.Add(bottomStack);
            card.Child = outerStack;

            if (item.Available)
            {
                var itemCapture = item;
                card.PointerEntered += async (_, _) =>
                {
                    scaleTransform.ScaleX = 1.03;
                    scaleTransform.ScaleY = 1.03;

                    card.BorderBrush = new SolidColorBrush(new Color(0xCC, glowColor.R, glowColor.G, glowColor.B));
                    card.BorderThickness = new Thickness(1.5);

                    UpdateStorePreviewPanel(itemCapture);

                    if (_storeRenderer != null)
                        await CosmeticHelpers.ApplyCosmeticPreviewToRendererAsync(
                            _storeRenderer, itemCapture.Id, itemCapture.Category,
                            _host?.CurrentSettings);
                };

                card.PointerExited += async (_, _) =>
                {
                    scaleTransform.ScaleX = 1.0;
                    scaleTransform.ScaleY = 1.0;

                    card.BorderBrush     = new SolidColorBrush(new Color(0x55, glowColor.R, glowColor.G, glowColor.B));
                    card.BorderThickness = new Thickness(1);

                    if (_storeRenderer != null && _host?.CurrentSettings?.Equipped != null)
                        await CosmeticHelpers.ApplyEquippedToRendererAsync(_storeRenderer, _host.CurrentSettings.Equipped);
                };
            }

            return card;
        }

        private void UpdateStorePreviewPanel(
            (string Id, string Name, string Category, string Rarity, string Description, string Preview, string Price, bool Available) item)
        {
            _previewedItemId = item.Id;

            var (rarityMain, _, _) = item.Rarity switch
            {
                "Legendary" => ("#F59E0B", "#92400E", "#1A1408"),
                "Epic"      => ("#A855F7", "#6B21A8", "#110C1A"),
                "Rare"      => ("#3B82F6", "#1E3A8A", "#0C1320"),
                _           => ("#6B7280", "#374151", "#0F1318")
            };
            var mainColor = Color.Parse(rarityMain);

            if (_storePreviewName  != null) _storePreviewName.Text  = item.Name;
            if (_storePreviewDesc  != null) _storePreviewDesc.Text  = item.Description;
            if (_storePreviewPrice != null)
            {
                _storePreviewPrice.Text = !item.Available
                    ? "Coming Soon"
                    : GetDisplayPrice(item.Id, item.Price);
            }

            if (_storePreviewRarityText != null)
                _storePreviewRarityText.Text = item.Rarity.ToUpper();

            if (_storePreviewRarityBadge != null)
            {
                _storePreviewRarityBadge.Background = new SolidColorBrush(
                    new Color(0x28, mainColor.R, mainColor.G, mainColor.B));
                _storePreviewRarityBadge.BorderBrush = new SolidColorBrush(
                    new Color(0x55, mainColor.R, mainColor.G, mainColor.B));
            }
            if (_storePreviewRarityText != null)
                _storePreviewRarityText.Foreground = new SolidColorBrush(mainColor);

            if (_storePreviewActionBtn != null && _storePreviewActionText != null)
            {
                bool isFreeItem = item.Price == "FREE";
                bool isOwned    = _host?.IsOwned(item.Id) ?? false;
                bool hasCheckout = item.Available
                    && CheckoutUrls.TryGetValue(item.Id, out var checkoutUrl)
                    && !string.IsNullOrWhiteSpace(checkoutUrl);
                bool isComingSoon = !item.Available || (!hasCheckout && !isFreeItem);

                _storePreviewActionBtn.IsVisible = true;

                if (isOwned)
                {
                    _storePreviewActionBtn.Background  = SolidColorBrush.Parse("#2022C55E");
                    _storePreviewActionBtn.BorderBrush = SolidColorBrush.Parse("#22C55E");
                    _storePreviewActionBtn.Cursor      = new Cursor(StandardCursorType.Arrow);
                    _storePreviewActionText.Text       = "OWNED \u2713";
                    _storePreviewActionText.Foreground = SolidColorBrush.Parse("#4ADE80");
                }
                else if (isComingSoon)
                {
                    _storePreviewActionBtn.Background  = SolidColorBrush.Parse("#15FFFFFF");
                    _storePreviewActionBtn.BorderBrush = SolidColorBrush.Parse("#22FFFFFF");
                    _storePreviewActionBtn.Cursor      = new Cursor(StandardCursorType.Arrow);
                    _storePreviewActionText.Text       = "COMING SOON";
                    _storePreviewActionText.Foreground = SolidColorBrush.Parse("#44FFFFFF");
                }
                else if (isFreeItem)
                {
                    _storePreviewActionBtn.Background  = SolidColorBrush.Parse("#20228B45");
                    _storePreviewActionBtn.BorderBrush = SolidColorBrush.Parse("#4ADE80");
                    _storePreviewActionBtn.Cursor      = new Cursor(StandardCursorType.Hand);
                    _storePreviewActionText.Text       = "GET FREE";
                    _storePreviewActionText.Foreground = SolidColorBrush.Parse("#4ADE80");
                }
                else
                {
                    _storePreviewActionBtn.Background  = new SolidColorBrush(new Color(0x30, mainColor.R, mainColor.G, mainColor.B));
                    _storePreviewActionBtn.BorderBrush = new SolidColorBrush(new Color(0x65, mainColor.R, mainColor.G, mainColor.B));
                    _storePreviewActionBtn.Cursor      = new Cursor(StandardCursorType.Hand);
                    _storePreviewActionText.Text       = "BUY NOW";
                    _storePreviewActionText.Foreground = Brushes.White;
                }

                if (_storePreviewCoinBtn != null)
                    _storePreviewCoinBtn.IsVisible = false;
            }
        }

        private void HighlightStoreTab(string activeTab)
        {
            var mainGreen = Color.Parse("#4CAF50");

            foreach (var (name, tag) in new[]
            {
                ("StoreTab_All",   "all"),
                ("StoreTab_Capes", "capes"),
                ("StoreTab_Hats",  "hats"),
                ("StoreTab_Wings", "wings"),
                ("StoreTab_Auras", "auras"),
            })
            {
                var tab = this.FindControl<Border>(name);
                if (tab == null) continue;

                bool isActive = tag == activeTab;
                tab.Background = isActive
                    ? new SolidColorBrush(new Color(0x30, mainGreen.R, mainGreen.G, mainGreen.B))
                    : SolidColorBrush.Parse("Transparent");
                tab.BorderBrush = isActive
                    ? new SolidColorBrush(new Color(0x60, mainGreen.R, mainGreen.G, mainGreen.B))
                    : SolidColorBrush.Parse("#20FFFFFF");

                var tb = tab.Child as TextBlock
                      ?? (tab.Child as Border)?.Child as TextBlock;
                if (tb != null)
                    tb.Foreground = isActive
                        ? new SolidColorBrush(mainGreen)
                        : SolidColorBrush.Parse("#80FFFFFF");
            }
        }

        private void OnStoreTabTapped(object? sender, TappedEventArgs e)
        {
            if (sender is Border b && b.Tag is string tag)
            {
                _storeActiveTab = tag;
                HighlightStoreTab(tag);
                PopulateStoreGrid();
            }
        }

        private void OnStorePreviewLightToggle(object? sender, TappedEventArgs e)
        {
            _storePreviewLightOn = !_storePreviewLightOn;
            _storeRenderer?.SetPreviewLight(_storePreviewLightOn);

            var icon = this.FindControl<TextBlock>("StorePreviewLightIcon");
            var label = this.FindControl<TextBlock>("StorePreviewLightLabel");
            var btn = this.FindControl<Border>("StorePreviewLightBtn");
            if (icon != null) icon.Foreground = _storePreviewLightOn ? SolidColorBrush.Parse("#FBBF24") : SolidColorBrush.Parse("#9CA3AF");
            if (label != null)
            {
                label.Text = _storePreviewLightOn ? "Lit" : "Light";
                label.Foreground = _storePreviewLightOn ? SolidColorBrush.Parse("#FBBF24") : SolidColorBrush.Parse("#9CA3AF");
            }
            if (btn != null) btn.Background = _storePreviewLightOn ? SolidColorBrush.Parse("#40FBBF24") : SolidColorBrush.Parse("#50000000");
        }

        private void OnStorePreviewActionTapped(object? sender, TappedEventArgs e)
        {
            if (_previewedItemId == null || _host == null) return;
            if (_host.IsOwned(_previewedItemId)) return;

            var catalogItem = System.Array.Find(StoreCatalog, c => c.Id == _previewedItemId);
            if (catalogItem.Id == null) return;

            if (catalogItem.Price == "FREE")
            {
                OnCardFreeClaimTapped(catalogItem);
                return;
            }

            OpenPurchasePopupForItem(catalogItem);
        }

        private void OnCardFreeClaimTapped(
            (string Id, string Name, string Category, string Rarity, string Description, string Preview, string Price, bool Available) item)
        {
            if (_host == null) return;
            _host.AddOwnedCosmetic(item.Id);
            _host.ShowPurchaseCelebration(item.Id, item.Name, item.Preview, item.Rarity);
            PopulateStoreGrid();
            UpdateStorePreviewPanel(item);
        }

        private void OpenPurchasePopupForItem(
            (string Id, string Name, string Category, string Rarity, string Description, string Preview, string Price, bool Available) item)
        {
            if (_host == null) return;
            if (!CheckoutUrls.TryGetValue(item.Id, out var checkoutUrl) || string.IsNullOrWhiteSpace(checkoutUrl)) return;

            CoinPrices.TryGetValue(item.Id, out var coinPrice);
            var displayPrice = GetDisplayPrice(item.Id, item.Price);

            _host.ShowPurchaseChoice(item.Id, item.Name, item.Preview, item.Rarity, displayPrice, coinPrice, checkoutUrl);
        }

        public void RefreshAfterPurchase(string itemId)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                PopulateStoreGrid();
                var catalogItem = System.Array.Find(StoreCatalog, c => c.Id == itemId);
                if (catalogItem.Id != null)
                    UpdateStorePreviewPanel(catalogItem);
            });
        }
    }
}
