using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using LeafClient.Models;
using LeafClient.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeafClient.Views.Pages
{
    public partial class CosmeticsPageView : UserControl
    {
        private IMainWindowHost? _host;

        private static readonly System.Collections.Generic.HashSet<string> _customizableIds =
            new(System.StringComparer.OrdinalIgnoreCase);

        private async System.Threading.Tasks.Task RefreshCustomizableSetAsync()
        {
            try
            {
                var catalog = await LeafClient.Services.LeafApiService.GetCosmeticsCatalogAsync();
                if (catalog == null) return;
                lock (_customizableIds)
                {
                    _customizableIds.Clear();
                    foreach (var c in catalog)
                    {
                        if (c.SupportsVariants || c.SupportsScale || c.SupportsOffset)
                            _customizableIds.Add(c.Id);
                    }
                }
            }
            catch { }
        }

        private static bool IsCustomizable(string id)
        {
            lock (_customizableIds) { return _customizableIds.Contains(id); }
        }

        private WrapPanel? _cosmeticsGrid;
        private LeafClient.Controls.SkinRendererControl? _skinRenderer;
        private string _cosmeticsActiveTab = "all";
        private string _cosmeticsSearchQuery = "";
        private bool _cosmeticsInitialized = false;
        private System.Collections.Generic.HashSet<string> _ownedIds = new();
        private static readonly Avalonia.Media.Imaging.Bitmap _nightBg = LoadBgAsset("avares://LeafClient/Assets/bg.jpg");
        private static readonly Avalonia.Media.Imaging.Bitmap _dayBg = LoadBgAsset("avares://LeafClient/Assets/skin-preview-day.jpg");

        private static Avalonia.Media.Imaging.Bitmap LoadBgAsset(string uri)
        {
            try { return new Avalonia.Media.Imaging.Bitmap(Avalonia.Platform.AssetLoader.Open(new Uri(uri))); }
            catch { return null!; }
        }

        public CosmeticsPageView()
        {
            InitializeComponent();
        }

        public void SetHost(IMainWindowHost host)
        {
            _host = host;
        }

        internal static readonly (string Id, string Name, string Category, string Rarity, string Description, string Preview)[] PlaceholderCosmetics =
        {
            ("cape-galaxy", "Leaf Cape", "capes", "Legendary", "Leaf Client's signature cape", "\U0001f33f"),
            ("wings-dragon", "Crimson Demon Wings", "wings", "Epic", "Demonic bat wings forged in hellfire \u2014 deep crimson membrane with glowing vein lines", "\U0001f987"),
            ("wings-black", "Shadow Demon Wings", "wings", "Epic", "Pitch-black demon wings that melt into the darkness", "\U0001f5a4"),
            ("wings-purple", "Void Demon Wings", "wings", "Epic", "Otherworldly purple demon wings pulsing with arcane energy", "\U0001f49c"),
            ("hat-crown", "Golden Crown", "hats", "Rare", "A golden crown of pure gold", "\U0001f451"),
            ("hat-horns", "Shadow Horns", "hats", "Epic", "Dark obsidian horns from the nether realm", "\U0001f608"),
            ("aura-darkness", "Darkness Aura", "auras", "Epic", "Dark shards whip menacingly around your character", "\U0001f311"),
            ("aura-hearts", "Broken Hearts", "auras", "Epic", "Crimson heart shards orbit in a haunting display", "\U0001f494"),
            ("aura-flames", "Inferno Aura", "auras", "Legendary", "Flickering flame wisps rise from the ground around you", "\U0001f525"),
        };

        private void InitializeCosmeticsControls()
        {
            _cosmeticsGrid = this.FindControl<WrapPanel>("CosmeticsGrid");

            var host = this.FindControl<Border>("SkinRendererHost");
            if (host != null)
            {
                try
                {
                    _skinRenderer = new LeafClient.Controls.SkinRendererControl
                    {
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
                    };
                    host.Child = _skinRenderer;
                    LeafLog.Info("Cosmetics", "3D skin renderer initialized.");
                }
                catch (Exception ex)
                {
                    LeafLog.Info("Cosmetics", $"Skin renderer init failed: {ex.Message}");
                    _skinRenderer = null;
                }
            }
        }

        public async void LoadCosmeticsPage()
        {
            LeafLog.Info("Cosmetics", "LoadCosmeticsPage called");
            if (!_cosmeticsInitialized)
            {
                InitializeCosmeticsControls();
                _cosmeticsInitialized = true;
            }
            _ = LeafClient.Services.BBModel.BBModelCatalog.RefreshAsync();
            ApplyPreviewBackgroundFromSettings();

            await LoadEquippedJsonFromDiskAsync();

            RefreshLoadoutPresetBar();

            _ = RefreshCustomizableSetAsync();

            if (_host != null)
            {
                var fresh = new System.Collections.Generic.HashSet<string>();
                foreach (var item in StorePageView.StoreCatalog)
                    if (_host.IsOwned(item.Id))
                        fresh.Add(item.Id);
                _ownedIds = fresh;
            }

            var usernameText = this.FindControl<TextBlock>("CosmeticsUsername");
            var accountTypeText = this.FindControl<TextBlock>("CosmeticsAccountType");
            if (usernameText != null)
                usernameText.Text = (_host?.CurrentSession?.Username ?? _host?.CurrentSettings?.SessionUsername ?? "Player").ToUpperInvariant();
            if (accountTypeText != null)
                accountTypeText.Text = _host?.CurrentSettings?.AccountType == "microsoft" ? "Microsoft" : "Offline";

            await LoadSkinFor3DRendererAsync();

            if (_skinRenderer != null)
            {
                var eq = _host?.CurrentSettings?.Equipped;
                if (eq != null)
                    _ = CosmeticHelpers.ApplyEquippedToRendererAsync(_skinRenderer, eq, _host?.CurrentSettings?.SessionAccessToken);
            }

            PopulateCosmeticsGrid();
        }

        private async Task LoadSkinFor3DRendererAsync()
        {
            if (_skinRenderer == null || _host == null) return;

            var skinData = await _host.FetchSkinBytesAsync();
            if (skinData != null)
            {
                _skinRenderer.UpdateSkinTexture(skinData);
                LeafLog.Info("Cosmetics", "Skin texture loaded into 3D renderer.");
                var fallback = this.FindControl<Image>("CosmeticsSkinFallback");
                if (fallback != null) fallback.IsVisible = false;
            }
            else
            {
                LeafLog.Info("Cosmetics", "All skin sources failed. No skin to display.");
            }
        }

        public void RefreshOwnedList(System.Collections.Generic.HashSet<string> ownedIds)
        {
            _ownedIds = ownedIds;
            PopulateCosmeticsGrid();
        }

        public async void OnAccountChanged()
        {
            try
            {
                LeafClient.Services.CosmeticHelpers.InvalidateCardPreviewCache();
                LeafClient.Services.MojangCapeService.InvalidateAllCaches();
                _mojangCapeProbed = false;
                _mojangCapeProbing = false;
                _mojangCapeBitmaps.Clear();
                _ownedMojangCapes = Array.Empty<LeafClient.Services.MojangCape>();

                var usernameText = this.FindControl<TextBlock>("CosmeticsUsername");
                var accountTypeText = this.FindControl<TextBlock>("CosmeticsAccountType");
                if (usernameText != null)
                    usernameText.Text = (_host?.CurrentSession?.Username ?? _host?.CurrentSettings?.SessionUsername ?? "Player").ToUpperInvariant();
                if (accountTypeText != null)
                    accountTypeText.Text = _host?.CurrentSettings?.AccountType == "microsoft" ? "Microsoft" : "Offline";

                await LoadSkinFor3DRendererAsync();
                if (_skinRenderer != null)
                {
                    var eq = _host?.CurrentSettings?.Equipped;
                    if (eq != null) _ = CosmeticHelpers.ApplyEquippedToRendererAsync(_skinRenderer, eq, _host?.CurrentSettings?.SessionAccessToken);
                }
                PopulateCosmeticsGrid();
                RefreshLoadoutPresetBar();
            }
            catch (Exception ex)
            {
                LeafLog.Info("Cosmetics", $"OnAccountChanged failed: {ex.Message}");
            }
        }

        public void PopulateCosmeticsGridPublic() => PopulateCosmeticsGrid();

        public async Task RefreshRendererPublicAsync()
        {
            try
            {
                var eq = _host?.CurrentSettings?.Equipped;
                if (_skinRenderer != null && eq != null)
                    await LeafClient.Services.CosmeticHelpers.ApplyEquippedToRendererAsync(_skinRenderer, eq, _host?.CurrentSettings?.SessionAccessToken);
            }
            catch (Exception ex) { LeafLog.Info("Cosmetics", $"RefreshRenderer failed: {ex.Message}"); }
        }

        private void PopulateCosmeticsGrid()
        {
            if (_cosmeticsGrid == null) return;
            _cosmeticsGrid.Children.Clear();

            var comingSoonPanel = this.FindControl<Border>("EmotesComingSoonPanel");
            var emptyState      = this.FindControl<Border>("CosmeticsEmptyState");
            var countText       = this.FindControl<TextBlock>("CosmeticsResultCount");

            bool isEmotesTab = _cosmeticsActiveTab == "emotes";

            if (comingSoonPanel != null)
                comingSoonPanel.IsVisible = isEmotesTab;

            if (countText != null)
                countText.IsVisible = !isEmotesTab;

            if (isEmotesTab)
            {
                if (emptyState != null) emptyState.IsVisible = false;
                return;
            }

            var ownedCatalog = StorePageView.StoreCatalog
                .Where(c => _ownedIds.Contains(c.Id))
                .Select(c => (c.Id, c.Name, c.Category, c.Rarity, c.Description, c.Preview))
                .ToArray();

            var filtered = ownedCatalog
                .Where(c => _cosmeticsActiveTab == "all" || c.Category == _cosmeticsActiveTab)
                .Where(c => string.IsNullOrWhiteSpace(_cosmeticsSearchQuery) ||
                            c.Name.Contains(_cosmeticsSearchQuery, StringComparison.OrdinalIgnoreCase) ||
                            c.Category.Contains(_cosmeticsSearchQuery, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            bool isEmpty = filtered.Length == 0;
            if (emptyState != null) emptyState.IsVisible = isEmpty;

            if (countText != null)
                countText.Text = $"Showing {filtered.Length} cosmetics";

            var equippedLabel = this.FindControl<TextBlock>("CosmeticsEquippedCount");
            if (equippedLabel != null && _host?.CurrentSettings?.Equipped != null)
            {
                var eq = _host.CurrentSettings.Equipped;
                int n = new[] { eq.CapeId, eq.HatId, eq.WingsId, eq.BackItemId, eq.AuraId, eq.FaceId }
                    .Count(id => !string.IsNullOrEmpty(id));
                equippedLabel.Text = n == 1 ? "1 cosmetic equipped" : $"{n} cosmetics equipped";
            }

            foreach (var cos in filtered)
            {
                var card = CreateCosmeticCard(cos);
                _cosmeticsGrid.Children.Add(card);
            }

            AddOwnedMojangCapeCards();
        }

        private bool _mojangCapeProbed;
        private bool _mojangCapeProbing;
        private readonly Dictionary<string, Avalonia.Media.Imaging.Bitmap> _mojangCapeBitmaps = new();
        private IReadOnlyList<LeafClient.Services.MojangCape> _ownedMojangCapes = Array.Empty<LeafClient.Services.MojangCape>();

        public static string MojangCapeIdPrefix => "mojang:";

        private void AddOwnedMojangCapeCards()
        {
            if (_cosmeticsActiveTab != "all" && _cosmeticsActiveTab != "capes") return;
            if (!_mojangCapeProbed)
            {
                if (!_mojangCapeProbing) _ = LoadOwnedMojangCapesAsync();
                return;
            }
            if (_ownedMojangCapes.Count == 0) return;

            foreach (var cape in _ownedMojangCapes)
            {
                if (!_mojangCapeBitmaps.TryGetValue(cape.Id, out var bitmap)) continue;

                string display = string.IsNullOrWhiteSpace(cape.Alias) ? "Mojang Cape" : cape.Alias;
                if (!string.IsNullOrWhiteSpace(_cosmeticsSearchQuery))
                {
                    bool matchesSearch = display.Contains(_cosmeticsSearchQuery, StringComparison.OrdinalIgnoreCase)
                                       || "capes".Contains(_cosmeticsSearchQuery, StringComparison.OrdinalIgnoreCase)
                                       || "mojang".Contains(_cosmeticsSearchQuery, StringComparison.OrdinalIgnoreCase);
                    if (!matchesSearch) continue;
                }

                var card = CreateMojangCapeCard(cape, bitmap, display);
                if (card != null && _cosmeticsGrid != null) _cosmeticsGrid.Children.Add(card);
            }
        }

        private async Task LoadOwnedMojangCapesAsync()
        {
            if (_mojangCapeProbing) return;
            _mojangCapeProbing = true;
            try
            {
                if (_host?.CurrentSettings?.AccountType != "microsoft")
                {
                    _mojangCapeProbed = true;
                    return;
                }

                string? mcToken = _host.CurrentSettings.SessionAccessToken;
                if (string.IsNullOrWhiteSpace(mcToken))
                {
                    _mojangCapeProbed = true;
                    return;
                }

                var profile = await LeafClient.Services.MojangCapeService.GetProfileAsync(mcToken);
                if (profile == null)
                {
                    _mojangCapeProbed = true;
                    return;
                }

                _ownedMojangCapes = profile.Capes;

                try
                {
                    if (_host?.CurrentSettings != null)
                    {
                        string? localCape = _host.CurrentSettings.Equipped?.CapeId;
                        bool localIsMojang = !string.IsNullOrEmpty(localCape) && localCape!.StartsWith(MojangCapeIdPrefix, StringComparison.Ordinal);
                        if (string.IsNullOrEmpty(localCape) || localIsMojang)
                        {
                            string? activeId = null;
                            foreach (var c in profile.Capes)
                            {
                                if (string.Equals(c.State, "ACTIVE", StringComparison.OrdinalIgnoreCase)) { activeId = c.Id; break; }
                            }
                            string desired = activeId != null ? MojangCapeIdPrefix + activeId : string.Empty;
                            string current = localCape ?? string.Empty;
                            if (current != desired)
                            {
                                _host.CurrentSettings.Equipped ??= new EquippedCosmetics();
                                _host.CurrentSettings.Equipped.CapeId = activeId != null ? desired : null;
                                _ = _host.SettingsService.SaveSettingsAsync(_host.CurrentSettings);
                                if (_skinRenderer != null)
                                    _ = LeafClient.Services.CosmeticHelpers.ApplyEquippedToRendererAsync(_skinRenderer, _host.CurrentSettings.Equipped, mcToken);
                            }
                        }
                    }
                }
                catch (Exception ex) { LeafLog.Info("Cosmetics", $"Mojang local-state sync failed: {ex.Message}"); }

                foreach (var cape in profile.Capes)
                {
                    if (_mojangCapeBitmaps.ContainsKey(cape.Id)) continue;
                    if (string.IsNullOrWhiteSpace(cape.Url)) continue;
                    try
                    {
                        var bytes = await LeafClient.Services.MojangCapeService.GetCapeTextureBytesAsync(cape.Url);
                        if (bytes == null) continue;
                        using var ms = new System.IO.MemoryStream(bytes);
                        _mojangCapeBitmaps[cape.Id] = new Avalonia.Media.Imaging.Bitmap(ms);
                    }
                    catch (Exception ex)
                    {
                        LeafLog.Error("Cosmetics", $"Failed to load Mojang cape texture for {cape.Id}: {ex.Message}");
                    }
                }

                _mojangCapeProbed = true;
                Avalonia.Threading.Dispatcher.UIThread.Post(PopulateCosmeticsGrid);
            }
            catch (Exception ex)
            {
                _mojangCapeProbed = true;
                LeafLog.Info("Cosmetics", $"LoadOwnedMojangCapesAsync failed: {ex.Message}");
            }
            finally
            {
                _mojangCapeProbing = false;
            }
        }

        public IReadOnlyDictionary<string, Avalonia.Media.Imaging.Bitmap> GetMojangCapeBitmaps() => _mojangCapeBitmaps;

        private Border? CreateMojangCapeCard(LeafClient.Services.MojangCape cape, Avalonia.Media.Imaging.Bitmap bitmap, string displayName)
        {
            string id = MojangCapeIdPrefix + cape.Id;
            string rarity = ClassifyMojangCapeRarity(cape.Alias);
            bool isEquipped = _host?.CurrentSettings?.Equipped?.CapeId == id;

            var (rarityMain, rarityGlow, rarityBg) = rarity switch
            {
                "Legendary" => ("#F59E0B", "#92400E", "#1A1408"),
                "Epic"      => ("#34D399", "#16A34A", "#110C1A"),
                "Rare"      => ("#3B82F6", "#1E3A8A", "#0C1320"),
                _           => ("#6B7280", "#374151", "#0F1318")
            };
            var mainColor = Color.Parse(rarityMain);
            var glowColor = Color.Parse(rarityGlow);

            var card = new Border
            {
                Width = 170, Height = 248,
                CornerRadius = new CornerRadius(16),
                BorderBrush = isEquipped
                    ? SolidColorBrush.Parse("#22C55E")
                    : new SolidColorBrush(new Color(0x50, glowColor.R, glowColor.G, glowColor.B)),
                BorderThickness = isEquipped ? new Thickness(2) : new Thickness(1),
                Margin = new Thickness(0, 0, 14, 14),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                Tag = id,
                ClipToBounds = true,
                Background = SolidColorBrush.Parse(rarityBg),
            };

            var outerStack = new StackPanel();
            var previewBorder = new Border
            {
                Height = 110,
                Margin = new Thickness(6, 6, 6, 0),
                CornerRadius = new CornerRadius(12),
                ClipToBounds = true,
            };
            var previewGradient = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
                EndPoint   = new RelativePoint(0.5, 1, RelativeUnit.Relative),
            };
            previewGradient.GradientStops.Add(new GradientStop(new Color(0x35, glowColor.R, glowColor.G, glowColor.B), 0));
            previewGradient.GradientStops.Add(new GradientStop(Color.Parse("#060A10"), 1));
            previewBorder.Background = previewGradient;

            var previewImg = new Avalonia.Controls.Image
            {
                Stretch = Avalonia.Media.Stretch.Uniform,
                Margin = new Thickness(4),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            };

            byte[]? cachedPreview = LeafClient.Services.CosmeticHelpers.GetCachedCardPreview(id + "_card");
            if (cachedPreview != null)
            {
                try
                {
                    using var ms = new System.IO.MemoryStream(cachedPreview);
                    previewImg.Source = new Avalonia.Media.Imaging.Bitmap(ms);
                    previewBorder.Child = previewImg;
                }
                catch
                {
                    previewBorder.Child = previewImg;
                }
            }
            else
            {
                var previewSpinner = new TextBlock
                {
                    Text = "...",
                    FontSize = 20,
                    Foreground = SolidColorBrush.Parse("#6B7280"),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                };
                previewBorder.Child = previewSpinner;
                _ = QueueMojangCapePreviewRenderAsync(id, cape, previewImg, previewBorder);
            }
            outerStack.Children.Add(previewBorder);

            var infoPanel = new StackPanel { Spacing = 4, Margin = new Thickness(12, 8, 12, 6) };
            infoPanel.Children.Add(new TextBlock
            {
                Text = displayName,
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                FontSize = 13,
                TextTrimming = TextTrimming.CharacterEllipsis,
            });
            var rarityBadge = new Border
            {
                Background = new SolidColorBrush(new Color(0x28, mainColor.R, mainColor.G, mainColor.B)),
                BorderBrush = new SolidColorBrush(new Color(0x55, mainColor.R, mainColor.G, mainColor.B)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(20),
                Padding = new Thickness(9, 3),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            };
            rarityBadge.Child = new TextBlock
            {
                Text = rarity.ToUpper(),
                Foreground = Brushes.White,
                FontSize = 9,
                FontWeight = FontWeight.ExtraBold,
                LetterSpacing = 0.8f,
            };
            infoPanel.Children.Add(rarityBadge);
            infoPanel.Children.Add(new TextBlock
            {
                Text = "Your official Minecraft cape",
                Foreground = SolidColorBrush.Parse("#6B7280"),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                MaxLines = 2,
            });
            outerStack.Children.Add(infoPanel);

            var equipBtn = new Border
            {
                Margin = new Thickness(8, 2, 8, 8),
                CornerRadius = new CornerRadius(8),
                Height = 30,
                Background = isEquipped
                    ? new SolidColorBrush(new Color(0x30, 0x22, 0xC5, 0x5E))
                    : new SolidColorBrush(new Color(0x28, mainColor.R, mainColor.G, mainColor.B)),
                BorderBrush = isEquipped
                    ? SolidColorBrush.Parse("#22C55E")
                    : new SolidColorBrush(new Color(0x60, mainColor.R, mainColor.G, mainColor.B)),
                BorderThickness = new Thickness(1),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
            };
            equipBtn.Child = new TextBlock
            {
                Text = isEquipped ? "\u2713  EQUIPPED" : "EQUIP",
                Foreground = isEquipped ? SolidColorBrush.Parse("#4ADE80") : Brushes.White,
                FontSize = 10,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                LetterSpacing = 0.6f,
            };
            equipBtn.Tapped += async (_, _) =>
            {
                await ToggleMojangCapeAsync(cape, id);
            };
            outerStack.Children.Add(equipBtn);
            card.Child = outerStack;
            return card;
        }

        private static string ClassifyMojangCapeRarity(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias)) return "Rare";
            string a = alias.Trim();
            if (a.Equals("Migrator", StringComparison.OrdinalIgnoreCase)) return "Legendary";
            if (a.Contains("15th", StringComparison.OrdinalIgnoreCase)) return "Legendary";
            if (a.Equals("Founder's", StringComparison.OrdinalIgnoreCase) || a.Contains("Founder", StringComparison.OrdinalIgnoreCase)) return "Legendary";
            if (a.Contains("Anniversary", StringComparison.OrdinalIgnoreCase)) return "Legendary";
            if (a.Contains("MineCon", StringComparison.OrdinalIgnoreCase)) return "Epic";
            if (a.Contains("Mojang", StringComparison.OrdinalIgnoreCase)) return "Epic";
            return "Rare";
        }

        private bool _mojangCapeMutating;

        private async Task ToggleMojangCapeAsync(LeafClient.Services.MojangCape cape, string localId)
        {
            if (_mojangCapeMutating) return;
            _mojangCapeMutating = true;
            try
            {
                if (_host?.CurrentSettings == null) return;
                string? mcToken = _host.CurrentSettings.SessionAccessToken;
                if (string.IsNullOrWhiteSpace(mcToken))
                {
                    LeafLog.Info("Cosmetics", "Mojang cape: no MC access token available.");
                    return;
                }

                bool currentlyEquipped = _host.CurrentSettings.Equipped?.CapeId == localId;

                if (currentlyEquipped) UnequipCosmetic(localId, "capes");
                else EquipCosmetic(localId, "capes");

                var refreshed = new List<LeafClient.Services.MojangCape>(_ownedMojangCapes.Count);
                foreach (var c in _ownedMojangCapes)
                {
                    string newState;
                    if (currentlyEquipped)
                        newState = c.Id == cape.Id ? "INACTIVE" : c.State;
                    else
                        newState = c.Id == cape.Id ? "ACTIVE" : "INACTIVE";
                    refreshed.Add(new LeafClient.Services.MojangCape(c.Id, newState, c.Url, c.Alias));
                }
                _ownedMojangCapes = refreshed;

                string expectedLocalAfter = currentlyEquipped ? string.Empty : localId;
                _ = Task.Run(async () =>
                {
                    LeafClient.Services.MojangCapeOperationOutcome outcome;
                    if (currentlyEquipped)
                    {
                        outcome = await LeafClient.Services.MojangCapeService.RemoveActiveCapeAsync(mcToken);
                    }
                    else
                    {
                        outcome = await LeafClient.Services.MojangCapeService.SetActiveCapeAsync(mcToken, cape.Id);
                    }

                    if (outcome != LeafClient.Services.MojangCapeOperationOutcome.Success)
                    {
                        LeafLog.Info("Cosmetics", $"Mojang cape API call outcome: {outcome}");
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            string actualLocal = _host?.CurrentSettings?.Equipped?.CapeId ?? string.Empty;
                            if (actualLocal != expectedLocalAfter) return;
                            if (currentlyEquipped) EquipCosmetic(localId, "capes");
                            else UnequipCosmetic(localId, "capes");
                        });
                    }
                });
            }
            finally
            {
                _mojangCapeMutating = false;
            }
        }

        private Border CreateCosmeticCard((string Id, string Name, string Category, string Rarity, string Description, string Preview) cos)
        {
            bool isEquipped = _host?.IsCosmeticEquipped(cos.Id, cos.Category) ?? false;

            var (rarityMain, rarityGlow, rarityBg) = cos.Rarity switch
            {
                "Legendary" => ("#F59E0B", "#92400E", "#1A1408"),
                "Epic"      => ("#34D399", "#16A34A", "#110C1A"),
                "Rare"      => ("#3B82F6", "#1E3A8A", "#0C1320"),
                _           => ("#6B7280", "#374151", "#0F1318")
            };

            var mainColor = Color.Parse(rarityMain);
            var glowColor = Color.Parse(rarityGlow);

            var card = new Border
            {
                Width = 170,
                Height = 248,
                CornerRadius = new CornerRadius(16),
                BorderBrush = isEquipped
                    ? SolidColorBrush.Parse("#22C55E")
                    : new SolidColorBrush(new Color(0x50, glowColor.R, glowColor.G, glowColor.B)),
                BorderThickness = isEquipped ? new Thickness(2) : new Thickness(1),
                Margin = new Thickness(0, 0, 14, 14),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                Tag = cos.Id,
                ClipToBounds = true,
                Background = SolidColorBrush.Parse(rarityBg)
            };

            var outerStack = new StackPanel();

            var previewBorder = new Border
            {
                Height = 110,
                Margin = new Thickness(6, 6, 6, 0),
                CornerRadius = new CornerRadius(12),
                ClipToBounds = true
            };
            var previewGradient = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
                EndPoint   = new RelativePoint(0.5, 1, RelativeUnit.Relative)
            };
            previewGradient.GradientStops.Add(new GradientStop(new Color(0x35, glowColor.R, glowColor.G, glowColor.B), 0));
            previewGradient.GradientStops.Add(new GradientStop(Color.Parse("#060A10"), 1));
            previewBorder.Background = previewGradient;
            var previewImg = new Avalonia.Controls.Image
            {
                Stretch = Avalonia.Media.Stretch.Uniform,
                Margin = new Thickness(4),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            };
            var previewFallback = new TextBlock
            {
                Text = cos.Preview,
                FontSize = 42,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Center
            };
            try
            {
                var cached = LeafClient.Services.CosmeticHelpers.GetCachedCardPreview(cos.Id);
                if (cached != null)
                {
                    using var msC = new System.IO.MemoryStream(cached);
                    previewImg.Source = new Avalonia.Media.Imaging.Bitmap(msC);
                    previewBorder.Child = previewImg;
                }
                else
                {
                    previewBorder.Child = previewFallback;
                    _ = QueuePreviewRenderAsync(cos.Id, cos.Category, previewImg).ContinueWith(_ =>
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            if (previewImg.Source != null) previewBorder.Child = previewImg;
                        });
                    });
                }
            }
            catch { previewBorder.Child = previewFallback; }

            var previewWrap = new Grid { Margin = new Thickness(0) };
            previewWrap.Children.Add(previewBorder);
            if (IsCustomizable(cos.Id))
            {
                var kebab = new Border
                {
                    Width = 28, Height = 28,
                    CornerRadius = new CornerRadius(14),
                    Background = new SolidColorBrush(Color.FromArgb(0xC8, 0x0F, 0x1A, 0x24)),
                    BorderBrush = new SolidColorBrush(Color.Parse("#1C2A38")),
                    BorderThickness = new Thickness(1),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                    Margin = new Thickness(0, 8, 8, 0),
                    Cursor = new Cursor(StandardCursorType.Hand),
                    Opacity = 0,
                    IsVisible = false,
                    Child = new TextBlock
                    {
                        Text = "⋯",
                        FontSize = 18,
                        Foreground = Brushes.White,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    },
                };
                var menu = new MenuFlyout();
                var customizeItem = new MenuItem { Header = "Customize…" };
                customizeItem.Click += (_, _) => _host?.ShowCustomizeCosmeticPopup(cos.Id);
                menu.Items.Add(customizeItem);
                kebab.Tapped += (_, e) =>
                {
                    e.Handled = true;
                    menu.ShowAt(kebab);
                };
                previewWrap.Children.Add(kebab);
                var capturedKebab = kebab;
                previewWrap.PointerEntered += (_, _) => { capturedKebab.IsVisible = true; capturedKebab.Opacity = 1; };
                previewWrap.PointerExited += (_, _) =>
                {
                    if (menu.IsOpen) return;
                    capturedKebab.Opacity = 0;
                    capturedKebab.IsVisible = false;
                };
            }
            outerStack.Children.Add(previewWrap);

            var infoPanel = new StackPanel { Spacing = 4, Margin = new Thickness(12, 8, 12, 6) };
            infoPanel.Children.Add(new TextBlock
            {
                Text = cos.Name,
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                FontSize = 13,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            var rarityBadge = new Border
            {
                Background = new SolidColorBrush(new Color(0x28, mainColor.R, mainColor.G, mainColor.B)),
                BorderBrush = new SolidColorBrush(new Color(0x55, mainColor.R, mainColor.G, mainColor.B)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(20),
                Padding = new Thickness(9, 3),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
            };
            rarityBadge.Child = new TextBlock
            {
                Text = cos.Rarity.ToUpper(),
                Foreground = Brushes.White,
                FontSize = 9,
                FontWeight = FontWeight.ExtraBold,
                LetterSpacing = 0.8f
            };
            infoPanel.Children.Add(rarityBadge);

            infoPanel.Children.Add(new TextBlock
            {
                Text = cos.Description,
                Foreground = SolidColorBrush.Parse("#6B7280"),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                MaxLines = 2
            });
            outerStack.Children.Add(infoPanel);

            var equipBtn = new Border
            {
                Margin = new Thickness(8, 2, 8, 8),
                CornerRadius = new CornerRadius(8),
                Height = 30,
                Background = isEquipped
                    ? new SolidColorBrush(new Color(0x30, 0x22, 0xC5, 0x5E))
                    : new SolidColorBrush(new Color(0x28, mainColor.R, mainColor.G, mainColor.B)),
                BorderBrush = isEquipped
                    ? SolidColorBrush.Parse("#22C55E")
                    : new SolidColorBrush(new Color(0x60, mainColor.R, mainColor.G, mainColor.B)),
                BorderThickness = new Thickness(1),
                Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
            };
            equipBtn.Child = new TextBlock
            {
                Text = isEquipped ? "\u2713  EQUIPPED" : "EQUIP",
                Foreground = isEquipped ? SolidColorBrush.Parse("#4ADE80") : Brushes.White,
                FontSize = 10,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                LetterSpacing = 0.6f,
            };

            var cosCapture = cos;
            equipBtn.Tapped += (_, _) =>
            {
                if (_host?.IsCosmeticEquipped(cosCapture.Id, cosCapture.Category) == true)
                    UnequipCosmetic(cosCapture.Id, cosCapture.Category);
                else
                    EquipCosmetic(cosCapture.Id, cosCapture.Category);
            };

            outerStack.Children.Add(equipBtn);
            card.Child = outerStack;
            return card;
        }

        private async void EquipCosmetic(string cosId, string category)
        {
            if (_host?.CurrentSettings == null) return;
            LeafLog.Info("Cosmetics", $"Equipping '{cosId}' (category: {category})");
            var settings = _host.CurrentSettings;
            settings.Equipped ??= new EquippedCosmetics();

            var preEq = settings.Equipped;
            if (preEq.HatId == cosId)   preEq.HatId = null;
            if (preEq.CapeId == cosId)  preEq.CapeId = null;
            if (preEq.WingsId == cosId) preEq.WingsId = null;
            if (preEq.AuraId == cosId)  preEq.AuraId = null;
            if (preEq.FaceId == cosId)  preEq.FaceId = null;

            switch (category)
            {
                case "capes":
                case "cape":
                    if (!string.IsNullOrEmpty(settings.Equipped.WingsId))
                        settings.Equipped.WingsId = null;
                    if (!string.IsNullOrEmpty(settings.Equipped.CapeId)
                        && settings.Equipped.CapeId!.StartsWith(MojangCapeIdPrefix, StringComparison.Ordinal)
                        && !cosId.StartsWith(MojangCapeIdPrefix, StringComparison.Ordinal))
                    {
                        var mcToken = settings.SessionAccessToken;
                        if (!string.IsNullOrWhiteSpace(mcToken))
                            _ = LeafClient.Services.MojangCapeService.RemoveActiveCapeAsync(mcToken);
                    }
                    settings.Equipped.CapeId = cosId;
                    break;
                case "hats":
                case "hat":
                    settings.Equipped.HatId = cosId;
                    break;
                case "wings":
                    if (!string.IsNullOrEmpty(settings.Equipped.CapeId))
                    {
                        if (settings.Equipped.CapeId!.StartsWith(MojangCapeIdPrefix, StringComparison.Ordinal))
                        {
                            var mcToken = settings.SessionAccessToken;
                            if (!string.IsNullOrWhiteSpace(mcToken))
                                _ = LeafClient.Services.MojangCapeService.RemoveActiveCapeAsync(mcToken);
                        }
                        settings.Equipped.CapeId = null;
                    }
                    settings.Equipped.WingsId = cosId;
                    break;
                case "auras":
                case "aura":
                    settings.Equipped.AuraId = cosId;
                    break;
                case "face":
                case "faces":
                    settings.Equipped.FaceId = cosId;
                    break;
            }

            await _host.SettingsService.SaveSettingsAsync(settings);
            SaveEquippedJson();
            PopulateCosmeticsGrid();

            if (_skinRenderer != null)
            {
                _ = CosmeticHelpers.ApplyEquippedToRendererAsync(_skinRenderer, settings.Equipped, settings.SessionAccessToken);
            }

            var jwt = _host?.CurrentSettings?.LeafApiJwt;
            if (!string.IsNullOrEmpty(jwt))
            {
                try { await LeafClient.Services.LeafApiService.EquipCosmeticAsync(jwt!, cosId); }
                catch (Exception ex) { LeafLog.Info("Cosmetics", $"Server-side equip error: {ex.Message}"); }
            }
        }

        private async void UnequipCosmetic(string cosId, string category)
        {
            if (_host?.CurrentSettings?.Equipped == null) return;
            LeafLog.Info("Cosmetics", $"Unequipping '{cosId}' (category: {category})");
            var eq = _host.CurrentSettings.Equipped;

            string? serverCategory = null;
            switch (category)
            {
                case "capes":  eq.CapeId = null;  serverCategory = "cape";  break;
                case "hats":   eq.HatId = null;   serverCategory = "hat";   break;
                case "wings":  eq.WingsId = null; serverCategory = "wings"; break;
                case "auras":  eq.AuraId = null;  serverCategory = "aura";  break;
                case "face":
                case "faces":  eq.FaceId = null;  serverCategory = "face";  break;
            }

            await _host.SettingsService.SaveSettingsAsync(_host.CurrentSettings);
            SaveEquippedJson();
            PopulateCosmeticsGrid();

            if (_skinRenderer != null)
            {
                _ = CosmeticHelpers.ApplyEquippedToRendererAsync(_skinRenderer, eq, _host?.CurrentSettings?.SessionAccessToken);
            }

            var jwt = _host?.CurrentSettings?.LeafApiJwt;
            if (serverCategory != null && !string.IsNullOrWhiteSpace(jwt))
            {
                try { await LeafClient.Services.LeafApiService.UnequipCosmeticAsync(jwt!, serverCategory); }
                catch (Exception ex) { LeafLog.Info("Cosmetics", $"Server-side unequip error: {ex.Message}"); }
            }
        }

        private void SaveEquippedJson()
        {
            try
            {
                var dir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LeafClient");
                System.IO.Directory.CreateDirectory(dir);
                var path = System.IO.Path.Combine(dir, "equipped.json");
                var src = _host?.CurrentSettings?.Equipped ?? new EquippedCosmetics();
                var sub = DecodeJwtSubLocal(_host?.CurrentSettings?.LeafApiJwt) ?? src.Sub;
                var snapshot = new EquippedCosmetics
                {
                    Sub = sub,
                    CapeId = src.CapeId,
                    HatId = src.HatId,
                    WingsId = src.WingsId,
                    BackItemId = src.BackItemId,
                    AuraId = src.AuraId,
                    FaceId = src.FaceId,
                    CapeVariant = src.CapeVariant,
                    HatVariant = src.HatVariant,
                    WingsVariant = src.WingsVariant,
                    BackItemVariant = src.BackItemVariant,
                    AuraVariant = src.AuraVariant,
                    FaceVariant = src.FaceVariant,
                    CapeScale = src.CapeScale, HatScale = src.HatScale, WingsScale = src.WingsScale, AuraScale = src.AuraScale, FaceScale = src.FaceScale,
                    CapeOffsetX = src.CapeOffsetX, CapeOffsetY = src.CapeOffsetY, CapeOffsetZ = src.CapeOffsetZ,
                    HatOffsetX = src.HatOffsetX, HatOffsetY = src.HatOffsetY, HatOffsetZ = src.HatOffsetZ,
                    WingsOffsetX = src.WingsOffsetX, WingsOffsetY = src.WingsOffsetY, WingsOffsetZ = src.WingsOffsetZ,
                    AuraOffsetX = src.AuraOffsetX, AuraOffsetY = src.AuraOffsetY, AuraOffsetZ = src.AuraOffsetZ,
                    FaceOffsetX = src.FaceOffsetX, FaceOffsetY = src.FaceOffsetY, FaceOffsetZ = src.FaceOffsetZ,
                };
                var json = JsonSerializer.Serialize(snapshot,
                    LeafClient.JsonContext.Default.EquippedCosmetics);
                File.WriteAllText(path, json);
                LeafLog.Info("Cosmetics", "equipped.json saved.");
            }
            catch (Exception ex)
            {
                LeafLog.Error("Cosmetics", $"Failed to save equipped.json: {ex.Message}");
            }
        }

        private static string? DecodeJwtSubLocal(string? jwt)
        {
            if (string.IsNullOrWhiteSpace(jwt)) return null;
            try
            {
                var parts = jwt.Split('.');
                if (parts.Length < 2) return null;
                var payload = parts[1];
                int pad = 4 - (payload.Length % 4);
                if (pad < 4) payload += new string('=', pad);
                payload = payload.Replace('-', '+').Replace('_', '/');
                var bytes = Convert.FromBase64String(payload);
                using var doc = System.Text.Json.JsonDocument.Parse(bytes);
                if (doc.RootElement.TryGetProperty("sub", out var subEl) && subEl.ValueKind == System.Text.Json.JsonValueKind.String)
                    return subEl.GetString();
            }
            catch { }
            return null;
        }

        private async Task LoadEquippedJsonFromDiskAsync()
        {
            if (_host?.CurrentSettings == null) return;
            try
            {
                var dir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LeafClient");
                var path = System.IO.Path.Combine(dir, "equipped.json");
                if (!File.Exists(path)) return;

                string json;
                try { json = File.ReadAllText(path); }
                catch (IOException) { return; }
                if (string.IsNullOrWhiteSpace(json)) return;

                EquippedCosmetics? fromDisk;
                try
                {
                    fromDisk = JsonSerializer.Deserialize(
                        json, LeafClient.JsonContext.Default.EquippedCosmetics);
                }
                catch (JsonException ex)
                {
                    LeafLog.Info("Cosmetics", $"equipped.json malformed, ignoring: {ex.Message}");
                    return;
                }
                if (fromDisk == null) return;

                var currentSub = DecodeJwtSubLocal(_host.CurrentSettings.LeafApiJwt);
                if (!string.IsNullOrEmpty(currentSub)
                    && !string.IsNullOrEmpty(fromDisk.Sub)
                    && !string.Equals(currentSub, fromDisk.Sub, StringComparison.Ordinal))
                {
                    LeafLog.Info("Cosmetics", $"equipped.json sub mismatch (file='{fromDisk.Sub}', current='{currentSub}') - ignoring.");
                    return;
                }

                var current = _host.CurrentSettings.Equipped ?? new EquippedCosmetics();
                if (EquippedEquals(current, fromDisk)) return;

                LeafLog.Info("Cosmetics", "equipped.json changed on disk - syncing into launcher state.");
                _host.CurrentSettings.Equipped = fromDisk;

                if (_host.CurrentSettings.SavedAccounts != null)
                {
                    foreach (var acct in _host.CurrentSettings.SavedAccounts)
                    {
                        if (acct == null) continue;
                        if (string.Equals(acct.Username, _host.CurrentSettings.SessionUsername,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            acct.Equipped = new EquippedCosmetics
                            {
                                CapeId = fromDisk.CapeId,
                                HatId = fromDisk.HatId,
                                WingsId = fromDisk.WingsId,
                                BackItemId = fromDisk.BackItemId,
                                AuraId = fromDisk.AuraId,
                            };
                            break;
                        }
                    }
                }

                if (_host.SettingsService != null)
                {
                    await _host.SettingsService.SaveSettingsAsync(_host.CurrentSettings);
                }
            }
            catch (Exception ex)
            {
                LeafLog.Error("Cosmetics", $"Failed to load equipped.json: {ex.Message}");
            }
        }

        private static bool EquippedEquals(EquippedCosmetics a, EquippedCosmetics b)
        {
            return string.Equals(a.CapeId,     b.CapeId,     StringComparison.Ordinal)
                && string.Equals(a.HatId,      b.HatId,      StringComparison.Ordinal)
                && string.Equals(a.WingsId,    b.WingsId,    StringComparison.Ordinal)
                && string.Equals(a.BackItemId, b.BackItemId, StringComparison.Ordinal)
                && string.Equals(a.AuraId,     b.AuraId,     StringComparison.Ordinal);
        }

        private void OnCosmeticTabTapped(object? sender, TappedEventArgs e)
        {
            if (sender is not Border b || b.Tag is not string tab) return;

            _cosmeticsActiveTab = tab;

            var tabNames = new[] { "CosTab_All", "CosTab_Capes", "CosTab_Hats", "CosTab_Wings", "CosTab_Auras", "CosTab_Emotes", "CosTab_Drops" };
            foreach (var tabName in tabNames)
            {
                var tabBorder = this.FindControl<Border>(tabName);
                if (tabBorder == null) continue;
                bool isActive = tabBorder.Tag?.ToString() == tab;
                bool isDrops = tabBorder.Tag?.ToString() == "drops";
                tabBorder.Background = isActive ? (isDrops ? SolidColorBrush.Parse("#16A34A") : SolidColorBrush.Parse("#22C55E")) : SolidColorBrush.Parse("#0F1A24");
                tabBorder.BorderThickness = isActive ? new Thickness(0) : new Thickness(1.5);
                tabBorder.BorderBrush = isDrops ? SolidColorBrush.Parse("#166534") : SolidColorBrush.Parse("#1C2A38");

                void UpdateTextColors(Avalonia.Visual parent)
                {
                    foreach (var child in parent.GetVisualChildren())
                    {
                        if (child is TextBlock tb)
                            tb.Foreground = isActive ? Brushes.White : (isDrops ? SolidColorBrush.Parse("#86EFAC") : SolidColorBrush.Parse("#9CA3AF"));
                        if (child is Avalonia.Visual v)
                            UpdateTextColors(v);
                    }
                }
                UpdateTextColors(tabBorder);
            }

            if (tab == "drops")
                _ = PopulateDropsHistoryAsync();
            else
                PopulateCosmeticsGrid();
        }

        private async void OnPreviewLightToggle(object? sender, TappedEventArgs e)
        {
            if (_host?.CurrentSettings == null) return;
            _host.CurrentSettings.PreviewDayBackground = !_host.CurrentSettings.PreviewDayBackground;
            ApplyPreviewBackgroundFromSettings();
            try { await _host.SettingsService.SaveSettingsAsync(_host.CurrentSettings); }
            catch (Exception ex) { LeafLog.Info("Cosmetics", $"PreviewDayBackground save failed: {ex.Message}"); }
        }

        private void ApplyPreviewBackgroundFromSettings()
        {
            bool day = _host?.CurrentSettings?.PreviewDayBackground == true;
            var img = this.FindControl<Image>("PreviewBackgroundImage");
            if (img != null)
            {
                img.Source = day ? _dayBg : _nightBg;
                img.Opacity = day ? 0.85 : 0.35;
            }
            var dim = this.FindControl<Border>("PreviewDimOverlay");
            if (dim != null) dim.Opacity = day ? 0.35 : 1.0;

            var icon = this.FindControl<TextBlock>("PreviewLightIcon");
            var label = this.FindControl<TextBlock>("PreviewLightLabel");
            if (icon != null)
            {
                icon.Text = day ? "\u263D" : "\u2600";
                icon.Foreground = day ? SolidColorBrush.Parse("#86EFAC") : SolidColorBrush.Parse("#FBBF24");
            }
            if (label != null)
            {
                label.Text = day ? "Night" : "Day";
                label.Foreground = day ? SolidColorBrush.Parse("#86EFAC") : SolidColorBrush.Parse("#FBBF24");
            }
        }

        private void OnCosmeticsSearchChanged(object? sender, TextChangedEventArgs e)
        {
            var searchBox = this.FindControl<TextBox>("CosmeticsSearchBox");
            _cosmeticsSearchQuery = searchBox?.Text ?? "";
            PopulateCosmeticsGrid();
        }

        private void OnCosmeticsEmptyStoreLinkTapped(object? sender, TappedEventArgs e)
        {
            _host?.SwitchToPage(7);
        }

        private const int MaxLoadouts = 3;

        private void RefreshLoadoutPresetBar()
        {
            var bar = this.FindControl<Grid>("LoadoutPresetsBar");
            if (bar == null) return;
            bar.Children.Clear();

            var presets = _host?.CurrentSettings?.CosmeticPresets ?? new System.Collections.Generic.List<CosmeticPreset>();
            int count = Math.Min(presets.Count, MaxLoadouts);

            var countLabel = this.FindControl<TextBlock>("LoadoutCountLabel");
            if (countLabel != null) countLabel.Text = $"{count} / {MaxLoadouts}";

            for (int slot = 0; slot < MaxLoadouts; slot++)
            {
                Control card = slot < count
                    ? BuildPresetCard(presets[slot])
                    : BuildAddLoadoutCard();
                Grid.SetColumn(card, slot);
                bar.Children.Add(card);
            }
        }

        private Control BuildPresetCard(CosmeticPreset preset)
        {
            var card = new Border
            {
                CornerRadius    = new CornerRadius(12),
                Background      = SolidColorBrush.Parse("#161E2D"),
                BorderBrush     = SolidColorBrush.Parse("#2A3F52"),
                BorderThickness = new Thickness(1),
                Padding         = new Thickness(0),
                Height          = 150,
                Cursor          = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
            };

            var rootGrid = new Grid
            {
                RowDefinitions = new RowDefinitions("*,Auto"),
            };

            var preview = BuildPresetPreview(preset);
            Grid.SetRow(preview, 0);
            rootGrid.Children.Add(preview);

            var footer = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                Background = SolidColorBrush.Parse("#0D1422"),
            };
            var nameLabel = new TextBlock
            {
                Text              = preset.Name,
                Foreground        = SolidColorBrush.Parse("#E5E7EB"),
                FontSize          = 12,
                FontWeight        = FontWeight.SemiBold,
                Padding           = new Thickness(10, 8, 6, 8),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                TextTrimming      = Avalonia.Media.TextTrimming.CharacterEllipsis,
            };
            Grid.SetColumn(nameLabel, 0);
            footer.Children.Add(nameLabel);

            var deleteBtn = new Border
            {
                Width           = 22,
                Height          = 22,
                CornerRadius    = new CornerRadius(11),
                Background      = SolidColorBrush.Parse("#33EF4444"),
                Margin          = new Thickness(0, 0, 6, 0),
                Cursor          = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text                = "\u00D7",
                    Foreground          = SolidColorBrush.Parse("#FCA5A5"),
                    FontSize            = 12,
                    FontWeight          = FontWeight.Bold,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Center,
                },
            };
            deleteBtn.Tapped += (_, ev) =>
            {
                ev.Handled = true;
                DeletePreset(preset);
            };
            Grid.SetColumn(deleteBtn, 1);
            footer.Children.Add(deleteBtn);

            Grid.SetRow(footer, 1);
            rootGrid.Children.Add(footer);

            card.Child = rootGrid;
            card.Tapped += (_, _) => LoadPreset(preset);
            return card;
        }

        private byte[]? _cachedSkinBytesForPresets;

        private bool _loadoutVisible = true;
        private double _loadoutFullHeight = double.NaN;
        private System.Threading.CancellationTokenSource? _loadoutAnimCts;
        private static readonly TimeSpan LoadoutAnimDuration = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan LoadoutFrameTime    = TimeSpan.FromMilliseconds(16);

        private void OnCosmeticsScrollChanged(object? sender, Avalonia.Controls.ScrollChangedEventArgs e)
        {
            var clip = this.FindControl<Border>("LoadoutSectionClip");
            var section = this.FindControl<Border>("LoadoutSection");
            var scroller = sender as Avalonia.Controls.ScrollViewer
                           ?? this.FindControl<Avalonia.Controls.ScrollViewer>("CosmeticsScrollViewer");
            if (clip == null || section == null || scroller == null) return;

            bool shouldShow = scroller.Offset.Y <= 0.5;
            if (shouldShow == _loadoutVisible) return;

            _loadoutVisible = shouldShow;
            _ = AnimateLoadoutAsync(shouldShow, clip, section);
        }

        private async Task AnimateLoadoutAsync(bool show, Border clip, Border section)
        {
            if (double.IsNaN(_loadoutFullHeight) || _loadoutFullHeight <= 0)
            {
                _loadoutFullHeight = clip.Bounds.Height;
                if (_loadoutFullHeight <= 0) _loadoutFullHeight = section.Bounds.Height;
                if (_loadoutFullHeight <= 0) _loadoutFullHeight = 174;
            }

            _loadoutAnimCts?.Cancel();
            _loadoutAnimCts = new System.Threading.CancellationTokenSource();
            var token = _loadoutAnimCts.Token;

            double startHeight  = !double.IsNaN(clip.Height) ? clip.Height : (show ? 0 : _loadoutFullHeight);
            double targetHeight = show ? _loadoutFullHeight : 0;
            double startOpacity = section.Opacity;
            double targetOpacity = show ? 1.0 : 0.0;

            var start = DateTime.UtcNow;
            try
            {
                while (true)
                {
                    if (token.IsCancellationRequested) return;

                    var elapsed = DateTime.UtcNow - start;
                    double t = Math.Clamp(elapsed.TotalMilliseconds / LoadoutAnimDuration.TotalMilliseconds, 0, 1);
                    double eased = show ? 1 - Math.Pow(1 - t, 3) : Math.Pow(t, 2);

                    double h2 = startHeight + (targetHeight - startHeight) * eased;
                    double op = startOpacity + (targetOpacity - startOpacity) * eased;

                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (token.IsCancellationRequested) return;
                        clip.Height = h2;
                        section.Opacity = op;
                    });

                    if (t >= 1) break;
                    await Task.Delay(LoadoutFrameTime, token);
                }

                if (!token.IsCancellationRequested)
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        clip.Height = targetHeight;
                        section.Opacity = targetOpacity;
                    });
                }
            }
            catch (OperationCanceledException) { }
        }

        private Control BuildPresetPreview(CosmeticPreset preset)
        {
            var host = new Border
            {
                Background = SolidColorBrush.Parse("#0A0F18"),
                ClipToBounds = true,
            };

            try
            {
                var renderer = new LeafClient.Controls.SkinRendererControl
                {
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                    VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Stretch,
                    IsHitTestVisible    = false,
                };
                host.Child = renderer;

                _ = LoadPresetRendererAsync(renderer, preset);
                return host;
            }
            catch (Exception ex)
            {
                LeafLog.Info("Cosmetics", $"BuildPresetPreview renderer init failed: {ex.Message}");
                host.Child = BuildPresetPreviewFallback(preset);
                return host;
            }
        }

        private async Task LoadPresetRendererAsync(LeafClient.Controls.SkinRendererControl renderer, CosmeticPreset preset)
        {
            try
            {
                if (_cachedSkinBytesForPresets == null && _host != null)
                    _cachedSkinBytesForPresets = await _host.FetchSkinBytesAsync();

                if (_cachedSkinBytesForPresets != null)
                    renderer.UpdateSkinTexture(_cachedSkinBytesForPresets);

                if (preset.Equipped != null)
                    CosmeticHelpers.ApplyEquippedToRenderer(renderer, preset.Equipped);
            }
            catch (Exception ex)
            {
                LeafLog.Info("Cosmetics", $"LoadPresetRendererAsync failed: {ex.Message}");
            }
        }

        private static Control BuildPresetPreviewFallback(CosmeticPreset preset)
        {
            var grid = new Grid
            {
                Background = SolidColorBrush.Parse("#0A0F18"),
                RowDefinitions    = new RowDefinitions("*,*"),
                ColumnDefinitions = new ColumnDefinitions("*,*"),
            };
            var eq = preset.Equipped;
            string?[] slotIds =
            {
                eq?.HatId,
                eq?.WingsId,
                eq?.CapeId,
                eq?.AuraId,
            };
            string[] slotLabels = { "HAT", "WINGS", "CAPE", "AURA" };
            string[] slotIcons  = { "\u26D1", "\uD83E\uDEC2", "\uD83E\uDDA9", "\u2728" };

            for (int i = 0; i < 4; i++)
            {
                var cell = new Border
                {
                    Margin          = new Thickness(2),
                    CornerRadius    = new CornerRadius(6),
                    Background      = SolidColorBrush.Parse(slotIds[i] != null ? "#1F2A3A" : "#0F1722"),
                    BorderBrush     = SolidColorBrush.Parse(slotIds[i] != null ? "#3B5070" : "#1C2A38"),
                    BorderThickness = new Thickness(1),
                };
                var stack = new StackPanel
                {
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Center,
                    Spacing = 1,
                };
                stack.Children.Add(new TextBlock
                {
                    Text                = slotIcons[i],
                    FontSize            = 16,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Foreground          = SolidColorBrush.Parse(slotIds[i] != null ? "#9CA3AF" : "#374151"),
                });
                stack.Children.Add(new TextBlock
                {
                    Text                = slotLabels[i],
                    FontSize            = 8,
                    LetterSpacing       = 0.5,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    Foreground          = SolidColorBrush.Parse(slotIds[i] != null ? "#6B7280" : "#1F2937"),
                });
                cell.Child = stack;
                Grid.SetRow(cell, i / 2);
                Grid.SetColumn(cell, i % 2);
                grid.Children.Add(cell);
            }
            return grid;
        }

        private Control BuildAddLoadoutCard()
        {
            var card = new Border
            {
                CornerRadius    = new CornerRadius(12),
                Background      = SolidColorBrush.Parse("#0F1722"),
                BorderBrush     = SolidColorBrush.Parse("#1F2937"),
                BorderThickness = new Thickness(1),
                Padding         = new Thickness(0),
                Height          = 150,
                Cursor          = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
            };
            var stack = new StackPanel
            {
                Spacing             = 6,
                VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            };
            stack.Children.Add(new TextBlock
            {
                Text                = "\uFF0B",
                FontSize            = 22,
                FontWeight          = FontWeight.Bold,
                Foreground          = SolidColorBrush.Parse("#22C55E"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            });
            stack.Children.Add(new TextBlock
            {
                Text                = "NEW LOADOUT",
                FontSize            = 10,
                FontWeight          = FontWeight.ExtraBold,
                LetterSpacing       = 1.0,
                Foreground          = SolidColorBrush.Parse("#22C55E"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            });
            card.Child = stack;
            card.Tapped += (s, e) => OnSaveLoadoutTapped(s, e);
            return card;
        }

        private async void LoadPreset(CosmeticPreset preset)
        {
            if (_host?.CurrentSettings == null || preset?.Equipped == null) return;

            var settings = _host.CurrentSettings;
            settings.Equipped ??= new EquippedCosmetics();
            settings.Equipped.CapeId     = preset.Equipped.CapeId;
            settings.Equipped.HatId      = preset.Equipped.HatId;
            settings.Equipped.WingsId    = preset.Equipped.WingsId;
            settings.Equipped.BackItemId = preset.Equipped.BackItemId;
            settings.Equipped.AuraId     = preset.Equipped.AuraId;

            if (_skinRenderer != null)
                _ = CosmeticHelpers.ApplyEquippedToRendererAsync(_skinRenderer, settings.Equipped, settings.SessionAccessToken);

            await _host.SettingsService.SaveSettingsAsync(settings);
            SaveEquippedJson();
            PopulateCosmeticsGrid();
            _ = SyncEquippedToServerAsync(settings.Equipped);
            LeafLog.Info("Cosmetics", $"Loaded preset: {preset.Name}");
        }

        private async Task SyncEquippedToServerAsync(EquippedCosmetics? eq)
        {
            if (eq == null) return;
            var jwt = _host?.CurrentSettings?.LeafApiJwt;
            if (string.IsNullOrEmpty(jwt)) return;
            string?[] ids = { eq.CapeId, eq.HatId, eq.WingsId, eq.AuraId };
            foreach (var id in ids)
            {
                if (string.IsNullOrEmpty(id)) continue;
                try
                {
                    var ok = await LeafClient.Services.LeafApiService.EquipCosmeticAsync(jwt!, id!);
                    if (!ok) LeafLog.Error("Cosmetics", $"Server-side equip failed for {id}");
                }
                catch (Exception ex)
                {
                    LeafLog.Error("Cosmetics", $"Server-side equip error for {id}: {ex.Message}");
                }
            }
        }

        private async void DeletePreset(CosmeticPreset preset)
        {
            if (_host?.CurrentSettings == null) return;
            var presets = _host.CurrentSettings.CosmeticPresets;
            if (presets == null) return;

            presets.RemoveAll(p => p.Id == preset.Id);
            await _host.SettingsService.SaveSettingsAsync(_host.CurrentSettings);
            RefreshLoadoutPresetBar();
            LeafLog.Info("Cosmetics", $"Deleted preset: {preset.Name}");
        }

        private async void OnSaveLoadoutTapped(object? sender, TappedEventArgs e)
        {
            if (_host?.CurrentSettings == null) return;

            var existing = _host.CurrentSettings.CosmeticPresets;
            if (existing != null && existing.Count >= MaxLoadouts)
            {
                LeafLog.Info("Cosmetics", $"Loadout cap reached ({MaxLoadouts}); delete one first.");
                return;
            }

            var name = await ShowPresetNameDialogAsync();
            if (string.IsNullOrWhiteSpace(name)) return;

            var eq = _host.CurrentSettings.Equipped ?? new EquippedCosmetics();
            var snapshot = new EquippedCosmetics
            {
                CapeId     = eq.CapeId,
                HatId      = eq.HatId,
                WingsId    = eq.WingsId,
                BackItemId = eq.BackItemId,
                AuraId     = eq.AuraId,
            };

            _host.CurrentSettings.CosmeticPresets ??= new System.Collections.Generic.List<CosmeticPreset>();
            _host.CurrentSettings.CosmeticPresets.Add(new CosmeticPreset
            {
                Name     = name.Trim(),
                Equipped = snapshot,
            });

            await _host.SettingsService.SaveSettingsAsync(_host.CurrentSettings);
            RefreshLoadoutPresetBar();
            LeafLog.Info("Cosmetics", $"Saved preset: {name}");
        }

        private Task<string?> ShowPresetNameDialogAsync()
        {
            var tcs = new TaskCompletionSource<string?>();

            var topLevel = TopLevel.GetTopLevel(this) as Window;
            if (topLevel == null) { tcs.TrySetResult(null); return tcs.Task; }

            var backdrop = new Border
            {
                Background          = SolidColorBrush.Parse("#CC000000"),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Stretch,
            };

            var panel = new Border
            {
                Width               = 420,
                Background          = SolidColorBrush.Parse("#0F1A24"),
                BorderBrush         = SolidColorBrush.Parse("#1C2A38"),
                BorderThickness     = new Thickness(1),
                CornerRadius        = new CornerRadius(16),
                Padding             = new Thickness(24),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Center,
            };

            var title = new TextBlock
            {
                Text       = "Save loadout",
                Foreground = SolidColorBrush.Parse("#E5E7EB"),
                FontSize   = 16,
                FontWeight = FontWeight.Bold,
                Margin     = new Thickness(0, 0, 0, 12),
            };

            var textBox = new TextBox
            {
                Watermark              = "Preset name",
                Background             = SolidColorBrush.Parse("#0B141C"),
                Foreground             = Brushes.White,
                BorderBrush            = SolidColorBrush.Parse("#2A3F52"),
                BorderThickness        = new Thickness(1),
                CornerRadius           = new CornerRadius(8),
                Padding                = new Thickness(12, 8, 12, 8),
                FontSize               = 13,
                Margin                 = new Thickness(0, 0, 0, 16),
            };

            var buttonRow = new StackPanel
            {
                Orientation         = Avalonia.Layout.Orientation.Horizontal,
                Spacing             = 8,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            };

            var cancelBtn = new Border
            {
                CornerRadius    = new CornerRadius(8),
                Background      = SolidColorBrush.Parse("#1C2A38"),
                BorderBrush     = SolidColorBrush.Parse("#2A3F52"),
                BorderThickness = new Thickness(1),
                Padding         = new Thickness(16, 8, 16, 8),
                Cursor          = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                Child           = new TextBlock
                {
                    Text       = "Cancel",
                    Foreground = SolidColorBrush.Parse("#9CA3AF"),
                    FontSize   = 12,
                    FontWeight = FontWeight.SemiBold,
                },
            };

            var saveBtn = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background   = SolidColorBrush.Parse("#22C55E"),
                Padding      = new Thickness(16, 8, 16, 8),
                Cursor       = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                Child        = new TextBlock
                {
                    Text       = "Save",
                    Foreground = Brushes.White,
                    FontSize   = 12,
                    FontWeight = FontWeight.Bold,
                },
            };

            buttonRow.Children.Add(cancelBtn);
            buttonRow.Children.Add(saveBtn);

            var stack = new StackPanel();
            stack.Children.Add(title);
            stack.Children.Add(textBox);
            stack.Children.Add(buttonRow);
            panel.Child = stack;

            var overlayGrid = new Grid
            {
                ZIndex              = 999999,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Stretch,
            };
            overlayGrid.Children.Add(backdrop);
            overlayGrid.Children.Add(panel);

            if (topLevel.Content is Panel rootPanel)
            {
                rootPanel.Children.Add(overlayGrid);
                textBox.Focus();

                void Close(string? result)
                {
                    rootPanel.Children.Remove(overlayGrid);
                    tcs.TrySetResult(result);
                }

                cancelBtn.Tapped   += (_, _) => Close(null);
                backdrop.Tapped    += (_, _) => Close(null);
                saveBtn.Tapped     += (_, _) => Close(textBox.Text);
                textBox.KeyDown    += (_, ke) =>
                {
                    if (ke.Key == Avalonia.Input.Key.Enter) Close(textBox.Text);
                    else if (ke.Key == Avalonia.Input.Key.Escape) Close(null);
                };
            }
            else
            {
                tcs.TrySetResult(null);
            }

            return tcs.Task;
        }

        private async Task PopulateDropsHistoryAsync()
        {
            if (_cosmeticsGrid == null) return;
            _cosmeticsGrid.Children.Clear();

            var comingSoonPanel = this.FindControl<Border>("EmotesComingSoonPanel");
            var emptyState      = this.FindControl<Border>("CosmeticsEmptyState");
            var countText       = this.FindControl<TextBlock>("CosmeticsResultCount");

            if (comingSoonPanel != null) comingSoonPanel.IsVisible = false;
            if (emptyState != null)      emptyState.IsVisible = false;
            if (countText != null)       countText.IsVisible = false;

            var jwt = _host?.CurrentSettings?.LeafApiJwt;
            if (string.IsNullOrEmpty(jwt))
            {
                _cosmeticsGrid.Children.Add(BuildDropsPlaceholder("Sign in to see your drop history."));
                return;
            }

            var loading = new TextBlock
            {
                Text = "Loading drop history…",
                Foreground = SolidColorBrush.Parse("#9CA3AF"),
                FontSize = 13,
                Margin = new Thickness(8, 16, 0, 0),
            };
            _cosmeticsGrid.Children.Add(loading);

            List<LeafApiDropClaimResult>? history = null;
            try { history = await LeafApiService.GetDropHistoryAsync(jwt); }
            catch (Exception ex) { LeafLog.Info("Cosmetics", $"GetDropHistoryAsync failed: {ex.Message}"); }

            _cosmeticsGrid.Children.Clear();

            if (history == null || history.Count == 0)
            {
                _cosmeticsGrid.Children.Add(BuildDropsPlaceholder("No drops claimed yet - your first Leaf+ drop will appear here."));
                return;
            }

            foreach (var claim in history.OrderByDescending(c => c.Month ?? ""))
            {
                var section = new StackPanel
                {
                    Spacing = 6,
                    Margin = new Thickness(0, 0, 0, 18),
                    Width = double.NaN,
                };
                var headerRow = new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Spacing = 8,
                };
                headerRow.Children.Add(new TextBlock
                {
                    Text = "🎁",
                    FontSize = 16,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                });
                headerRow.Children.Add(new TextBlock
                {
                    Text = FormatDropMonth(claim.Month) + (claim.IsFirstClaim ? "  •  Welcome drop" : ""),
                    Foreground = SolidColorBrush.Parse("#BBF7D0"),
                    FontSize = 13,
                    FontWeight = FontWeight.Bold,
                    LetterSpacing = 0.6,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                });
                if (claim.LpCompensation > 0)
                {
                    headerRow.Children.Add(new Border
                    {
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(8, 2),
                        Background = SolidColorBrush.Parse("#166534"),
                        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                        Child = new TextBlock
                        {
                            Text = $"+{claim.LpCompensation} LP",
                            Foreground = SolidColorBrush.Parse("#D1FAE5"),
                            FontSize = 10,
                            FontWeight = FontWeight.ExtraBold,
                            LetterSpacing = 0.6,
                        },
                    });
                }
                section.Children.Add(headerRow);

                var cards = new WrapPanel { Orientation = Avalonia.Layout.Orientation.Horizontal };
                if (claim.Granted != null)
                    foreach (var c in claim.Granted) cards.Children.Add(BuildHistoryDropCard(c, owned: false));
                if (claim.AlreadyOwned != null)
                    foreach (var c in claim.AlreadyOwned) cards.Children.Add(BuildHistoryDropCard(c, owned: true));
                section.Children.Add(cards);

                _cosmeticsGrid.Children.Add(section);
            }
        }

        private static string FormatDropMonth(string? month)
        {
            if (string.IsNullOrWhiteSpace(month)) return "Drop";
            if (DateTime.TryParseExact(month, "yyyy-MM", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt))
                return dt.ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture).ToUpperInvariant();
            return month.ToUpperInvariant();
        }

        private static Border BuildDropsPlaceholder(string text)
        {
            return new Border
            {
                Padding = new Thickness(20, 28),
                CornerRadius = new CornerRadius(12),
                Background = SolidColorBrush.Parse("#0F1722"),
                BorderBrush = SolidColorBrush.Parse("#1F2937"),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 8, 0, 0),
                Child = new TextBlock
                {
                    Text = text,
                    Foreground = SolidColorBrush.Parse("#9CA3AF"),
                    FontSize = 13,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                },
            };
        }

        private async Task QueuePreviewRenderAsync(string cosmeticId, string category, Avalonia.Controls.Image targetImage)
        {
            try
            {
                byte[]? skin = null;
                try { skin = _host != null ? await _host.FetchSkinBytesAsync() : null; } catch { }
                var bytes = await LeafClient.Services.CosmeticHelpers.RenderCardPreviewAsync(cosmeticId, category, skin);
                if (bytes == null) return;
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    try
                    {
                        using var ms = new System.IO.MemoryStream(bytes);
                        targetImage.Source = new Avalonia.Media.Imaging.Bitmap(ms);
                    }
                    catch { }
                });
            }
            catch (Exception ex) { LeafLog.Error("CardPreview", $"queue render failed for {cosmeticId}: {ex.Message}"); }
        }

        private async Task QueueMojangCapePreviewRenderAsync(string cosmeticId, LeafClient.Services.MojangCape cape, Avalonia.Controls.Image targetImage, Border previewBorder)
        {
            try
            {
                byte[]? skin = null;
                try { skin = _host != null ? await _host.FetchSkinBytesAsync() : null; } catch { }
                string? mcToken = _host?.CurrentSettings?.SessionAccessToken;
                var bytes = await LeafClient.Services.CosmeticHelpers.RenderMojangCapeCardPreviewAsync(cosmeticId, skin, mcToken);
                if (bytes == null) return;
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    try
                    {
                        using var ms = new System.IO.MemoryStream(bytes);
                        targetImage.Source = new Avalonia.Media.Imaging.Bitmap(ms);
                        previewBorder.Child = targetImage;
                    }
                    catch { }
                });
            }
            catch (Exception ex) { LeafLog.Error("CardPreview", $"Mojang cape render queue failed for {cosmeticId}: {ex.Message}"); }
        }

        private static string DropRarityHex(string? rarity)
        {
            return (rarity ?? "common").ToLowerInvariant() switch
            {
                "legendary" => "#F59E0B",
                "epic"      => "#34D399",
                "rare"      => "#3B82F6",
                _            => "#6B7280",
            };
        }

        private Border BuildHistoryDropCard(LeafApiDropCosmetic cos, bool owned)
        {
            var card = new Border
            {
                Width = 168, Height = 196,
                Margin = new Thickness(0, 0, 10, 10),
                CornerRadius = new CornerRadius(14),
                Background = SolidColorBrush.Parse(owned ? "#0F1722" : "#1A1042"),
                BorderBrush = SolidColorBrush.Parse(owned ? "#1F2937" : DropRarityHex(cos.Rarity)),
                BorderThickness = new Thickness(owned ? 1 : 1.5),
                ClipToBounds = true,
                Opacity = owned ? 0.65 : 1.0,
            };
            var grid = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };

            var preview = new Border
            {
                Background = SolidColorBrush.Parse(owned ? "#0A0F18" : "#0D0A28"),
            };
            var img = new Image
            {
                Width = 88, Height = 88,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Stretch = Avalonia.Media.Stretch.Uniform,
            };
            try
            {
                var cached = LeafClient.Services.CosmeticHelpers.GetCachedCardPreview(cos.Id);
                if (cached != null)
                {
                    using var ms0 = new System.IO.MemoryStream(cached);
                    img.Source = new Avalonia.Media.Imaging.Bitmap(ms0);
                }
                else
                {
                    var fallback = LeafClient.Services.CosmeticHelpers.TryLoadCosmeticAsset(cos.Id);
                    if (fallback != null)
                    {
                        using var msFb = new System.IO.MemoryStream(fallback);
                        img.Source = new Avalonia.Media.Imaging.Bitmap(msFb);
                    }
                    _ = QueuePreviewRenderAsync(cos.Id, cos.Category ?? "", img);
                }
            }
            catch { }
            preview.Child = img;
            Grid.SetRow(preview, 0);
            grid.Children.Add(preview);

            var rarityPill = new Border
            {
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(6, 2),
                Margin = new Thickness(8, 8, 0, 0),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                Background = SolidColorBrush.Parse(DropRarityHex(cos.Rarity)),
                Child = new TextBlock
                {
                    Text = (cos.Rarity ?? "common").ToUpperInvariant(),
                    Foreground = Brushes.White,
                    FontSize = 9,
                    FontWeight = FontWeight.ExtraBold,
                    LetterSpacing = 0.8,
                },
            };
            Grid.SetRow(rarityPill, 0);
            grid.Children.Add(rarityPill);

            if (owned)
            {
                var ownedPill = new Border
                {
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(6, 2),
                    Margin = new Thickness(0, 8, 8, 0),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                    Background = SolidColorBrush.Parse("#1F2937"),
                    Child = new TextBlock
                    {
                        Text = "OWNED",
                        Foreground = SolidColorBrush.Parse("#9CA3AF"),
                        FontSize = 9,
                        FontWeight = FontWeight.ExtraBold,
                        LetterSpacing = 0.8,
                    },
                };
                Grid.SetRow(ownedPill, 0);
                grid.Children.Add(ownedPill);
            }

            var footer = new StackPanel
            {
                Spacing = 2,
                Margin = new Thickness(12, 8, 12, 12),
                Background = SolidColorBrush.Parse(owned ? "#0D1422" : "#160A33"),
            };
            footer.Children.Add(new TextBlock
            {
                Text = cos.Name ?? cos.Id,
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                FontSize = 12,
                TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
            });
            footer.Children.Add(new TextBlock
            {
                Text = (cos.Category ?? "").ToUpperInvariant(),
                Foreground = SolidColorBrush.Parse(owned ? "#4B5563" : "#86EFAC"),
                FontSize = 9,
                FontWeight = FontWeight.SemiBold,
                LetterSpacing = 0.5,
            });
            Grid.SetRow(footer, 1);
            grid.Children.Add(footer);

            card.Child = grid;
            return card;
        }
    }
}
