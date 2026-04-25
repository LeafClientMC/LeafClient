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
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeafClient.Views.Pages
{
    public partial class CosmeticsPageView : UserControl
    {
        private IMainWindowHost? _host;

        private WrapPanel? _cosmeticsGrid;
        private LeafClient.Controls.SkinRendererControl? _skinRenderer;
        private string _cosmeticsActiveTab = "all";
        private string _cosmeticsSearchQuery = "";
        private bool _cosmeticsInitialized = false;
        private bool _previewLightOn;
        private System.Collections.Generic.HashSet<string> _ownedIds = new();

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
            ("wings-angel", "Angel Wings", "wings", "Legendary", "Radiant feathered wings of pure light \u2014 graceful and divine", "\U0001f54a\ufe0f"),
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
                    Console.WriteLine("[Cosmetics] 3D skin renderer initialized.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Cosmetics] Skin renderer init failed: {ex.Message}");
                    _skinRenderer = null;
                }
            }
        }

        public async void LoadCosmeticsPage()
        {
            Console.WriteLine("[Cosmetics] LoadCosmeticsPage called");
            if (!_cosmeticsInitialized)
            {
                InitializeCosmeticsControls();
                _cosmeticsInitialized = true;
            }

            // Pull any cosmetic changes the mod wrote to equipped.json since
            // we were last shown (e.g. user equipped/unequipped in-game).
            await LoadEquippedJsonFromDiskAsync();

            // Refresh the loadout preset bar every time the page is shown
            RefreshLoadoutPresetBar();

            // Sync owned IDs from host — create a fresh set to avoid aliasing with _ownedCosmeticIds
            if (_host != null)
            {
                var fresh = new System.Collections.Generic.HashSet<string>();
                foreach (var item in StorePageView.StoreCatalog)
                    if (_host.IsOwned(item.Id))
                        fresh.Add(item.Id);
                _ownedIds = fresh;
            }

            // Update username display
            var usernameText = this.FindControl<TextBlock>("CosmeticsUsername");
            var accountTypeText = this.FindControl<TextBlock>("CosmeticsAccountType");
            if (usernameText != null)
                usernameText.Text = (_host?.CurrentSession?.Username ?? _host?.CurrentSettings?.SessionUsername ?? "Player").ToUpperInvariant();
            if (accountTypeText != null)
                accountTypeText.Text = _host?.CurrentSettings?.AccountType == "microsoft" ? "Microsoft" : "Offline";

            // Load skin texture for 3D renderer
            await LoadSkinFor3DRendererAsync();

            // Load equipped cosmetics into 3D renderer
            if (_skinRenderer != null)
            {
                var eq = _host?.CurrentSettings?.Equipped;
                if (eq != null)
                    CosmeticHelpers.ApplyEquippedToRenderer(_skinRenderer, eq);
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
                Console.WriteLine("[Cosmetics] Skin texture loaded into 3D renderer.");
                var fallback = this.FindControl<Image>("CosmeticsSkinFallback");
                if (fallback != null) fallback.IsVisible = false;
            }
            else
            {
                Console.WriteLine("[Cosmetics] All skin sources failed. No skin to display.");
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
                    if (eq != null) CosmeticHelpers.ApplyEquippedToRenderer(_skinRenderer, eq);
                }
                PopulateCosmeticsGrid();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cosmetics] OnAccountChanged failed: {ex.Message}");
            }
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

            // Build owned catalog items from StoreCatalog
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
                int n = new[] { eq.CapeId, eq.HatId, eq.WingsId, eq.BackItemId, eq.AuraId }
                    .Count(id => !string.IsNullOrEmpty(id));
                equippedLabel.Text = n == 1 ? "1 cosmetic equipped" : $"{n} cosmetics equipped";
            }

            foreach (var cos in filtered)
            {
                var card = CreateCosmeticCard(cos);
                _cosmeticsGrid.Children.Add(card);
            }
        }

        private Border CreateCosmeticCard((string Id, string Name, string Category, string Rarity, string Description, string Preview) cos)
        {
            bool isEquipped = _host?.IsCosmeticEquipped(cos.Id, cos.Category) ?? false;

            var (rarityMain, rarityGlow, rarityBg) = cos.Rarity switch
            {
                "Legendary" => ("#F59E0B", "#92400E", "#1A1408"),
                "Epic"      => ("#A855F7", "#6B21A8", "#110C1A"),
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
            previewBorder.Child = new TextBlock
            {
                Text = cos.Preview,
                FontSize = 42,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Center
            };
            outerStack.Children.Add(previewBorder);

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
            Console.WriteLine($"[Cosmetics] Equipping '{cosId}' (category: {category})");
            var settings = _host.CurrentSettings;
            settings.Equipped ??= new EquippedCosmetics();

            switch (category)
            {
                case "capes":
                    if (!string.IsNullOrEmpty(settings.Equipped.WingsId))
                    {
                        settings.Equipped.WingsId = null;
                        _skinRenderer?.ClearWings();
                    }
                    settings.Equipped.CapeId = cosId;
                    if (_skinRenderer != null)
                    {
                        var capeBytes = CosmeticHelpers.TryLoadCosmeticAsset(cosId);
                        if (capeBytes != null) _skinRenderer.UpdateCapeTexture(capeBytes);
                    }
                    break;
                case "hats":
                    settings.Equipped.HatId = cosId;
                    if (_skinRenderer != null)
                    {
                        var hatBytes = CosmeticHelpers.TryLoadCosmeticAsset(cosId);
                        if (hatBytes != null) _skinRenderer.UpdateHatTexture(hatBytes, cosId.Contains("horns"));
                    }
                    break;
                case "wings":
                    if (!string.IsNullOrEmpty(settings.Equipped.CapeId))
                    {
                        settings.Equipped.CapeId = null;
                        _skinRenderer?.ClearCape();
                    }
                    settings.Equipped.WingsId = cosId;
                    if (_skinRenderer != null)
                    {
                        var wingsBytes = CosmeticHelpers.TryLoadCosmeticAsset(cosId);
                        if (wingsBytes != null) _skinRenderer.UpdateWingsTexture(wingsBytes, cosId.Contains("angel"));
                    }
                    break;
                case "auras":
                    settings.Equipped.AuraId = cosId;
                    _skinRenderer?.SetAura(cosId.Replace("aura-", ""));
                    break;
            }

            await _host.SettingsService.SaveSettingsAsync(settings);
            SaveEquippedJson();
            PopulateCosmeticsGrid();
        }

        private async void UnequipCosmetic(string cosId, string category)
        {
            if (_host?.CurrentSettings?.Equipped == null) return;
            Console.WriteLine($"[Cosmetics] Unequipping '{cosId}' (category: {category})");
            var eq = _host.CurrentSettings.Equipped;

            switch (category)
            {
                case "capes":  eq.CapeId = null;  _skinRenderer?.ClearCape();  break;
                case "hats":   eq.HatId = null;   _skinRenderer?.ClearHat();   break;
                case "wings":  eq.WingsId = null;  _skinRenderer?.ClearWings(); break;
                case "auras":  eq.AuraId = null;   _skinRenderer?.ClearAura();  break;
            }

            await _host.SettingsService.SaveSettingsAsync(_host.CurrentSettings);
            SaveEquippedJson();
            PopulateCosmeticsGrid();
        }

        private void SaveEquippedJson()
        {
            try
            {
                var dir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LeafClient");
                System.IO.Directory.CreateDirectory(dir);
                var path = System.IO.Path.Combine(dir, "equipped.json");
                var json = JsonSerializer.Serialize(
                    _host?.CurrentSettings?.Equipped ?? new EquippedCosmetics(),
                    LeafClient.JsonContext.Default.EquippedCosmetics);
                File.WriteAllText(path, json);
                Console.WriteLine("[Cosmetics] equipped.json saved.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cosmetics] Failed to save equipped.json: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads equipped.json from disk and merges it back into the in-memory
        /// settings if it differs. This keeps the launcher in sync with cosmetic
        /// changes made inside the Fabric mod (which writes the same file).
        /// Persists the updated settings if anything changed.
        /// </summary>
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
                    Console.WriteLine($"[Cosmetics] equipped.json malformed, ignoring: {ex.Message}");
                    return;
                }
                if (fromDisk == null) return;

                var current = _host.CurrentSettings.Equipped ?? new EquippedCosmetics();
                if (EquippedEquals(current, fromDisk)) return;

                Console.WriteLine("[Cosmetics] equipped.json changed on disk — syncing into launcher state.");
                _host.CurrentSettings.Equipped = fromDisk;

                // Mirror to the active account so it survives full settings save/load round-trips.
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
                Console.WriteLine($"[Cosmetics] Failed to load equipped.json: {ex.Message}");
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

            var tabNames = new[] { "CosTab_All", "CosTab_Capes", "CosTab_Hats", "CosTab_Wings", "CosTab_Auras", "CosTab_Emotes" };
            foreach (var tabName in tabNames)
            {
                var tabBorder = this.FindControl<Border>(tabName);
                if (tabBorder == null) continue;
                bool isActive = tabBorder.Tag?.ToString() == tab;
                tabBorder.Background = isActive ? SolidColorBrush.Parse("#9333EA") : SolidColorBrush.Parse("#0F1A24");
                tabBorder.BorderThickness = isActive ? new Thickness(0) : new Thickness(1.5);
                tabBorder.BorderBrush = SolidColorBrush.Parse("#1C2A38");

                void UpdateTextColors(Avalonia.Visual parent)
                {
                    foreach (var child in parent.GetVisualChildren())
                    {
                        if (child is TextBlock tb)
                            tb.Foreground = isActive ? Brushes.White : SolidColorBrush.Parse("#9CA3AF");
                        if (child is Avalonia.Visual v)
                            UpdateTextColors(v);
                    }
                }
                UpdateTextColors(tabBorder);
            }

            PopulateCosmeticsGrid();
        }

        private void OnPreviewLightToggle(object? sender, TappedEventArgs e)
        {
            _previewLightOn = !_previewLightOn;
            _skinRenderer?.SetPreviewLight(_previewLightOn);

            var icon = this.FindControl<TextBlock>("PreviewLightIcon");
            var label = this.FindControl<TextBlock>("PreviewLightLabel");
            var btn = this.FindControl<Border>("PreviewLightBtn");
            if (icon != null) icon.Foreground = _previewLightOn ? SolidColorBrush.Parse("#FBBF24") : SolidColorBrush.Parse("#9CA3AF");
            if (label != null)
            {
                label.Text = _previewLightOn ? "Lit" : "Light";
                label.Foreground = _previewLightOn ? SolidColorBrush.Parse("#FBBF24") : SolidColorBrush.Parse("#9CA3AF");
            }
            if (btn != null) btn.Background = _previewLightOn ? SolidColorBrush.Parse("#40FBBF24") : SolidColorBrush.Parse("#50000000");
        }

        private void OnCosmeticsSearchChanged(object? sender, TextChangedEventArgs e)
        {
            var searchBox = this.FindControl<TextBox>("CosmeticsSearchBox");
            _cosmeticsSearchQuery = searchBox?.Text ?? "";
            PopulateCosmeticsGrid();
        }

        private void OnCosmeticsEmptyStoreLinkTapped(object? sender, TappedEventArgs e)
        {
            _host?.SwitchToPage(7); // store page index
        }

        // ═══════════════════════════════════════════════════════════
        //  Cosmetic Loadout Presets
        // ═══════════════════════════════════════════════════════════

        private void RefreshLoadoutPresetBar()
        {
            var bar = this.FindControl<StackPanel>("LoadoutPresetsBar");
            if (bar == null) return;
            bar.Children.Clear();

            var presets = _host?.CurrentSettings?.CosmeticPresets;
            if (presets == null || presets.Count == 0)
            {
                var empty = new TextBlock
                {
                    Text                = "No presets yet \u2014 save your current loadout to get started.",
                    Foreground          = SolidColorBrush.Parse("#4B5563"),
                    FontSize            = 11,
                    FontStyle           = FontStyle.Italic,
                    VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Center,
                };
                bar.Children.Add(empty);
                return;
            }

            foreach (var preset in presets)
                bar.Children.Add(BuildPresetChip(preset));
        }

        private Border BuildPresetChip(CosmeticPreset preset)
        {
            var outer = new Border
            {
                CornerRadius    = new CornerRadius(14),
                Background      = SolidColorBrush.Parse("#1A2638"),
                BorderBrush     = SolidColorBrush.Parse("#2A3F52"),
                BorderThickness = new Thickness(1),
                Padding         = new Thickness(4),
                Cursor          = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            };

            // Preset name — tap to load
            var nameLabel = new TextBlock
            {
                Text              = preset.Name,
                Foreground        = SolidColorBrush.Parse("#E5E7EB"),
                FontSize          = 11,
                FontWeight        = FontWeight.SemiBold,
                Padding           = new Thickness(10, 6, 8, 6),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            };
            Grid.SetColumn(nameLabel, 0);

            // Tap the label → load
            var loadZone = new Border
            {
                Background = Brushes.Transparent,
                Child      = nameLabel,
                Cursor     = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
            };
            loadZone.Tapped += (_, _) => LoadPreset(preset);
            Grid.SetColumn(loadZone, 0);

            // Delete (×) button
            var deleteBtn = new Border
            {
                Width           = 22,
                Height          = 22,
                CornerRadius    = new CornerRadius(11),
                Background      = SolidColorBrush.Parse("#33EF4444"),
                Margin          = new Thickness(0, 0, 4, 0),
                Cursor          = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text              = "\u00D7",
                    Foreground        = SolidColorBrush.Parse("#FCA5A5"),
                    FontSize          = 12,
                    FontWeight        = FontWeight.Bold,
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

            grid.Children.Add(loadZone);
            grid.Children.Add(deleteBtn);
            outer.Child = grid;
            return outer;
        }

        private async void LoadPreset(CosmeticPreset preset)
        {
            if (_host?.CurrentSettings == null || preset?.Equipped == null) return;

            // Copy preset values into the current equipped state
            var settings = _host.CurrentSettings;
            settings.Equipped ??= new EquippedCosmetics();
            settings.Equipped.CapeId     = preset.Equipped.CapeId;
            settings.Equipped.HatId      = preset.Equipped.HatId;
            settings.Equipped.WingsId    = preset.Equipped.WingsId;
            settings.Equipped.BackItemId = preset.Equipped.BackItemId;
            settings.Equipped.AuraId     = preset.Equipped.AuraId;

            // Apply to the 3D renderer
            if (_skinRenderer != null)
                CosmeticHelpers.ApplyEquippedToRenderer(_skinRenderer, settings.Equipped);

            await _host.SettingsService.SaveSettingsAsync(settings);
            SaveEquippedJson();
            PopulateCosmeticsGrid();
            Console.WriteLine($"[Cosmetics] Loaded preset: {preset.Name}");
        }

        private async void DeletePreset(CosmeticPreset preset)
        {
            if (_host?.CurrentSettings == null) return;
            var presets = _host.CurrentSettings.CosmeticPresets;
            if (presets == null) return;

            presets.RemoveAll(p => p.Id == preset.Id);
            await _host.SettingsService.SaveSettingsAsync(_host.CurrentSettings);
            RefreshLoadoutPresetBar();
            Console.WriteLine($"[Cosmetics] Deleted preset: {preset.Name}");
        }

        private async void OnSaveLoadoutTapped(object? sender, TappedEventArgs e)
        {
            if (_host?.CurrentSettings == null) return;

            // Prompt for a preset name via a simple inline dialog
            var name = await ShowPresetNameDialogAsync();
            if (string.IsNullOrWhiteSpace(name)) return;

            // Snapshot current equipped cosmetics
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
            Console.WriteLine($"[Cosmetics] Saved preset: {name}");
        }

        private Task<string?> ShowPresetNameDialogAsync()
        {
            var tcs = new TaskCompletionSource<string?>();

            // Host the dialog in the top-level window so it appears above the page
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
                Background   = SolidColorBrush.Parse("#9333EA"),
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

            // Overlay grid sits above every other child in the window's root panel.
            // ZIndex on the inner backdrop doesn't help — we need the *outer* grid
            // to win against the sidebar / top bar in root-panel sibling order.
            var overlayGrid = new Grid
            {
                ZIndex              = 999999,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment   = Avalonia.Layout.VerticalAlignment.Stretch,
            };
            overlayGrid.Children.Add(backdrop);
            overlayGrid.Children.Add(panel);

            // Attach as an adorner — add to the window's root Panel if possible
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
    }
}
