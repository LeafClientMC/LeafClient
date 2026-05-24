#pragma warning disable CS8600, CS8601, CS8602, CS8603, CS8604, CS8618, CS8625
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;
using Avalonia.Threading;
using LeafClient.Services.ModFolderManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LeafClient.Services;

namespace LeafClient.Views
{
    public partial class MainWindow
    {
        private TaskCompletionSource<PreLaunchCheckDecision>? _preLaunchTcs;
        private List<ConflictReport> _currentConflicts = new();
        private bool _preLaunchCompatOnlyMode;

        private async void BringLauncherToFrontForPrompt()
        {
            try
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
                    Activate();
                });
                bool prev = Topmost;
                await Dispatcher.UIThread.InvokeAsync(() => Topmost = true);
                await Task.Delay(120);
                await Dispatcher.UIThread.InvokeAsync(() => Topmost = prev);
            }
            catch (Exception ex)
            {
                LeafClient.Services.LeafLog.Info("PreLaunchCheck", $"BringLauncherToFrontForPrompt failed: {ex.Message}");
            }
        }

        public enum PreLaunchCheckDecision
        {
            Cancel,
            LaunchAnyway,
            AutoFix
        }

        public Task<PreLaunchCheckDecision> ShowPreLaunchCheckOverlayAsync(
            List<ConflictReport> conflicts,
            string mcVersion,
            bool compatOnlyMode = false)
        {
            _currentConflicts = conflicts ?? new List<ConflictReport>();
            _preLaunchCompatOnlyMode = compatOnlyMode;
            _preLaunchTcs = new TaskCompletionSource<PreLaunchCheckDecision>();

            try
            {
                int blockers = _currentConflicts.Count(c => c.Severity == ConflictSeverity.Blocker);
                int warnings = _currentConflicts.Count(c => c.Severity == ConflictSeverity.Warning);
                int notices = _currentConflicts.Count(c => c.Severity == ConflictSeverity.Notice);

                if (PreLaunchCheckSubtitle != null)
                {
                    var parts = new List<string>();
                    if (blockers > 0) parts.Add($"{blockers} blocker{(blockers == 1 ? "" : "s")}");
                    if (warnings > 0) parts.Add($"{warnings} warning{(warnings == 1 ? "" : "s")}");
                    if (notices > 0) parts.Add($"{notices} notice{(notices == 1 ? "" : "s")}");
                    string severitySummary = parts.Count == 0 ? "Issues detected." : string.Join(" · ", parts);
                    PreLaunchCheckSubtitle.Text = $"{severitySummary} (Minecraft {mcVersion})";
                }

                BuildConflictList();
                ResetActionButtons();

                if (PreLaunchCheckOverlay != null)
                {
                    PreLaunchCheckOverlay.IsVisible = true;
                    PreLaunchCheckOverlay.Opacity = 0;
                    if (PreLaunchCheckPanel != null)
                    {
                        PreLaunchCheckPanel.RenderTransform = new TranslateTransform(0, -40);
                        PreLaunchCheckPanel.Opacity = 0;
                    }
                    AnimatePreLaunchOverlayIn();

                    BringLauncherToFrontForPrompt();
                    _gameOutputWindow?.AppendLog(
                        "==============================================",
                        "WARN");
                    _gameOutputWindow?.AppendLog(
                        "[PreLaunchCheck] ACTION REQUIRED - switch to the Leaf Client window to resolve mod conflicts.",
                        "WARN");
                    _gameOutputWindow?.AppendLog(
                        "==============================================",
                        "WARN");
                }
            }
            catch
            {
            }

            return _preLaunchTcs.Task;
        }

        private void ResetActionButtons()
        {
            if (_preLaunchCompatOnlyMode)
            {
                if (PreLaunchLaunchAnywayButton != null) PreLaunchLaunchAnywayButton.IsVisible = false;
                if (PreLaunchCancelButtonText != null) PreLaunchCancelButtonText.Text = "CLOSE";
            }
            else
            {
                if (PreLaunchLaunchAnywayButton != null) PreLaunchLaunchAnywayButton.IsVisible = true;
                if (PreLaunchCancelButtonText != null) PreLaunchCancelButtonText.Text = "CANCEL LAUNCH";
            }
        }

        private HashSet<string> BuildPermissiveManagedIdSet(string mcVersion)
        {
            var permissive = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var profile = _currentSettings?.Profiles?.FirstOrDefault(p => p.Id == _currentSettings.ActiveProfileId);
                string preset = (profile?.ModPreset ?? "none").ToLowerInvariant();
                var disabled = profile?.DisabledRequiredMods ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

                bool IsManagedSlugEnabled(string slug)
                {
                    if (disabled.TryGetValue(slug, out bool isDisabled) && isDisabled) return false;
                    return true;
                }

                foreach (var m in _preInstalledCoreMods)
                {
                    if (IsManagedSlugEnabled(m.slug)) permissive.Add(m.slug);
                }
                if (_preInstalledPresetExtras.TryGetValue(preset, out var extras))
                {
                    foreach (var m in extras)
                    {
                        if (IsManagedSlugEnabled(m.slug)) permissive.Add(m.slug);
                    }
                }
            }
            catch { }

            try
            {
                var activeProfileForGate = _currentSettings?.Profiles?.FirstOrDefault(p => p.Id == _currentSettings.ActiveProfileId);
                var disabledMap = activeProfileForGate?.DisabledRequiredMods
                                  ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

                if (_currentSettings?.InstalledMods != null)
                {
                    foreach (var im in _currentSettings.InstalledMods)
                    {
                        if (im == null || string.IsNullOrEmpty(im.ModId)) continue;
                        if (!im.Enabled) continue;
                        if (!string.IsNullOrEmpty(im.MinecraftVersion)
                            && !string.Equals(im.MinecraftVersion, mcVersion, StringComparison.OrdinalIgnoreCase))
                            continue;
                        if (disabledMap.TryGetValue(im.ModId, out bool isDisabled) && isDisabled) continue;
                        permissive.Add(im.ModId);
                    }
                }
            }
            catch { }

            permissive.Add("leafclient");
            permissive.Add("minecraft");
            permissive.Add("java");
            permissive.Add("fabricloader");
            permissive.Add("fabric-loader");
            permissive.Add("fabric");
            permissive.Add("fabric-api");

            if (permissive.Contains("fabric-api"))
            {
                foreach (var sub in BundledModsAllowlist.FabricApiSubModules)
                    permissive.Add(sub);
            }

            return permissive;
        }

        private void AnimatePreLaunchOverlayIn()
        {
            try
            {
                if (PreLaunchCheckOverlay == null) return;
                var fade = new Animation
                {
                    Duration = TimeSpan.FromMilliseconds(180),
                    Easing = new CubicEaseOut(),
                    FillMode = FillMode.Forward,
                    Children =
                    {
                        new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(Visual.OpacityProperty, 0.0) } },
                        new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(Visual.OpacityProperty, 1.0) } }
                    }
                };
                _ = fade.RunAsync(PreLaunchCheckOverlay);

                if (PreLaunchCheckPanel != null)
                {
                    var slide = new Animation
                    {
                        Duration = TimeSpan.FromMilliseconds(260),
                        Easing = new CubicEaseOut(),
                        FillMode = FillMode.Forward,
                        Children =
                        {
                            new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(Visual.OpacityProperty, 0.0) } },
                            new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(Visual.OpacityProperty, 1.0) } }
                        }
                    };
                    _ = slide.RunAsync(PreLaunchCheckPanel);
                    Dispatcher.UIThread.Post(() =>
                    {
                        try { PreLaunchCheckPanel.RenderTransform = new TranslateTransform(0, 0); } catch { }
                    }, DispatcherPriority.Background);
                }
            }
            catch { }
        }

        private void BuildConflictList()
        {
            if (PreLaunchConflictsList == null) return;
            var items = new List<Control>();
            foreach (var conflict in _currentConflicts.OrderBy(c => (int)c.Severity))
            {
                items.Add(BuildConflictRow(conflict));
            }
            try
            {
                var disableAllBtn = this.FindControl<Button>("PreLaunchDisableAllButton");
                if (disableAllBtn != null)
                {
                    int blockerCount = _currentConflicts.Count(c =>
                        c.Severity == ConflictSeverity.Blocker
                        && !string.IsNullOrEmpty(c.Source.FilePath)
                        && File.Exists(c.Source.FilePath));
                    disableAllBtn.IsVisible = blockerCount > 1;
                }
            }
            catch { }
            if (items.Count == 0)
            {
                items.Add(new TextBlock
                {
                    Text = "No conflicts detected.",
                    Foreground = new SolidColorBrush(Color.Parse("#9CA3AF")),
                    FontSize = 12,
                    Margin = new Thickness(0, 12, 0, 12),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
                });
            }
            PreLaunchConflictsList.ItemsSource = items;
        }

        private Control BuildConflictRow(ConflictReport conflict)
        {
            var (accent, badgeText) = conflict.Severity switch
            {
                ConflictSeverity.Blocker => ("#EF4444", "BLOCKER"),
                ConflictSeverity.Warning => ("#F59E0B", "WARNING"),
                _ => ("#3B82F6", "NOTICE")
            };

            var border = new Border
            {
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14),
                Background = new SolidColorBrush(Color.Parse("#11091F")),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.Parse("#231A3A"))
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto")
            };

            var badge = new Border
            {
                Background = new SolidColorBrush(Color.Parse(accent)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 10, 0),
                Child = new TextBlock
                {
                    Text = badgeText,
                    FontSize = 9,
                    FontWeight = FontWeight.Black,
                    Foreground = new SolidColorBrush(Colors.White),
                    LetterSpacing = 1
                }
            };
            Grid.SetColumn(badge, 0);
            grid.Children.Add(badge);

            var stack = new StackPanel { Spacing = 3 };
            stack.Children.Add(new TextBlock
            {
                Text = conflict.Reason,
                Foreground = new SolidColorBrush(Color.Parse("#E5E7EB")),
                FontSize = 12,
                FontWeight = FontWeight.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });
            if (!string.IsNullOrEmpty(conflict.InstalledVersion))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = $"Installed: {conflict.InstalledVersion}  ·  Required: {conflict.RequiredRange}",
                    Foreground = new SolidColorBrush(Color.Parse("#6B7280")),
                    FontSize = 10,
                    TextWrapping = TextWrapping.Wrap
                });
            }
            else if (!string.IsNullOrEmpty(conflict.RequiredRange))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = $"Required: {conflict.RequiredRange}",
                    Foreground = new SolidColorBrush(Color.Parse("#6B7280")),
                    FontSize = 10,
                    TextWrapping = TextWrapping.Wrap
                });
            }
            Grid.SetColumn(stack, 1);
            grid.Children.Add(stack);

            var actionsStack = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Vertical,
                Spacing = 4,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                Margin = new Thickness(8, 0, 0, 0)
            };

            string key = DependencyResolver.MakeKey(conflict);

            if (!string.IsNullOrEmpty(conflict.Source.FilePath) && File.Exists(conflict.Source.FilePath))
            {
                var disableBtn = MakeRowActionButton("DISABLE", "#FCA5A5", "#7F1D1D");
                disableBtn.Click += (_, _) => OnDisableConflictModClick(conflict, border);
                actionsStack.Children.Add(disableBtn);
            }

            if (!string.IsNullOrEmpty(conflict.Source.ModId))
            {
                var helpBtn = MakeRowActionButton("HELP", "#9CA3AF", "#374151");
                helpBtn.Click += (_, _) => OnConflictGetHelpClick(conflict);
                actionsStack.Children.Add(helpBtn);
            }

            var ignoreBtn = MakeRowActionButton("IGNORE", "#9CA3AF", "#374151");
            ignoreBtn.Click += (_, _) => OnIgnoreConflictClick(key, border);
            actionsStack.Children.Add(ignoreBtn);

            Grid.SetColumn(actionsStack, 2);
            grid.Children.Add(actionsStack);

            border.Child = grid;
            return border;
        }

        private static Button MakeRowActionButton(string text, string fg, string borderColor) => new()
        {
            Content = new TextBlock
            {
                Text = text,
                FontSize = 9,
                FontWeight = FontWeight.Bold,
                LetterSpacing = 1,
                Foreground = new SolidColorBrush(Color.Parse(fg)),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
            },
            Background = Brushes.Transparent,
            BorderBrush = new SolidColorBrush(Color.Parse(borderColor)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 4),
            Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            MinWidth = 78
        };

        private void OnDisableConflictModClick(ConflictReport conflict, Border row)
        {
            try
            {
                var src = conflict.Source;
                if (string.IsNullOrEmpty(src.FilePath) || !File.Exists(src.FilePath))
                {
                    ShowToast($"Couldn't locate jar for {src.DisplayName}", LeafClient.Services.ToastType.Error);
                    return;
                }
                var disabled = src.FilePath + ".disabled";
                try { if (File.Exists(disabled)) File.Delete(disabled); } catch { }
                File.Move(src.FilePath, disabled);

                if (BundledModsAllowlist.IsLauncherManaged(src.ModId))
                {
                    var profile = _currentSettings?.Profiles?.FirstOrDefault(p => p.Id == _currentSettings.ActiveProfileId);
                    if (profile != null)
                    {
                        profile.DisabledRequiredMods ??= new System.Collections.Generic.Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                        profile.DisabledRequiredMods[src.ModId] = true;
                        _ = _settingsService.SaveSettingsAsync(_currentSettings);
                    }
                }

                row.Opacity = 0.4;
                row.IsHitTestVisible = false;
                ShowToast($"Disabled {src.DisplayName}", LeafClient.Services.ToastType.Success);
            }
            catch (Exception ex)
            {
                ShowToast($"Disable failed: {ex.Message}", LeafClient.Services.ToastType.Error);
            }
        }

        private void OnConflictGetHelpClick(ConflictReport conflict)
        {
            try
            {
                string slug = !string.IsNullOrEmpty(conflict.Source.ModId)
                    ? conflict.Source.ModId
                    : conflict.TargetId;
                if (string.IsNullOrEmpty(slug)) return;
                string url = $"https://modrinth.com/mod/{Uri.EscapeDataString(slug)}";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                LeafClient.Services.LeafLog.Error("PreLaunchCheck", $"GetHelp failed: {ex.Message}");
            }
        }

        private void OnIgnoreConflictClick(string key, Border row)
        {
            try
            {
                if (_currentSettings == null) return;
                if (_currentSettings.IgnoredModConflicts == null)
                    _currentSettings.IgnoredModConflicts = new List<string>();
                if (!_currentSettings.IgnoredModConflicts.Contains(key))
                    _currentSettings.IgnoredModConflicts.Add(key);
                _ = _settingsService.SaveSettingsAsync(_currentSettings);
                row.Opacity = 0.4;
                row.IsHitTestVisible = false;
            }
            catch { }
        }

        private void OnPreLaunchLaunchAnyway(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            HidePreLaunchOverlay();
            _preLaunchTcs?.TrySetResult(PreLaunchCheckDecision.LaunchAnyway);
        }

        private void OnPreLaunchDisableAllAndRetry(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                int disabled = 0;
                var profile = _currentSettings?.Profiles?.FirstOrDefault(p => p.Id == _currentSettings.ActiveProfileId);
                foreach (var conflict in _currentConflicts.Where(c => c.Severity == ConflictSeverity.Blocker))
                {
                    var src = conflict.Source;
                    if (string.IsNullOrEmpty(src.FilePath)) continue;
                    if (!File.Exists(src.FilePath)) continue;
                    try
                    {
                        var dest = src.FilePath + ".disabled";
                        try { if (File.Exists(dest)) File.Delete(dest); } catch { }
                        File.Move(src.FilePath, dest);
                        disabled++;

                        if (BundledModsAllowlist.IsLauncherManaged(src.ModId) && profile != null)
                        {
                            profile.DisabledRequiredMods ??= new System.Collections.Generic.Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                            profile.DisabledRequiredMods[src.ModId] = true;
                        }
                    }
                    catch (Exception inner)
                    {
                        LeafClient.Services.LeafLog.Error("PreLaunchCheck", $"Failed to disable {src.FileName}: {inner.Message}");
                    }
                }

                if (profile != null) _ = _settingsService.SaveSettingsAsync(_currentSettings);

                ShowToast($"Disabled {disabled} mod{(disabled == 1 ? "" : "s")} - launching", LeafClient.Services.ToastType.Success);
                HidePreLaunchOverlay();
                _preLaunchTcs?.TrySetResult(PreLaunchCheckDecision.LaunchAnyway);
            }
            catch (Exception ex)
            {
                ShowToast($"Disable-all failed: {ex.Message}", LeafClient.Services.ToastType.Error);
                HidePreLaunchOverlay();
                _preLaunchTcs?.TrySetResult(PreLaunchCheckDecision.Cancel);
            }
        }

        private void OnPreLaunchCancel(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            HidePreLaunchOverlay();
            _preLaunchTcs?.TrySetResult(PreLaunchCheckDecision.Cancel);
        }

        private void OnPreLaunchCloseTapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            HidePreLaunchOverlay();
            _preLaunchTcs?.TrySetResult(PreLaunchCheckDecision.Cancel);
        }

        private void HidePreLaunchOverlay()
        {
            try
            {
                if (PreLaunchCheckOverlay != null)
                    PreLaunchCheckOverlay.IsVisible = false;
            }
            catch { }
        }

        private async Task<PreLaunchCheckDecision> RunPreLaunchCompatibilityCheckAsync(string mcVersion)
        {
            string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
            if (!System.IO.Directory.Exists(modsFolder))
                return PreLaunchCheckDecision.LaunchAnyway;

            int javaMajor = DetectJavaMajorForMcVersion(mcVersion);
            string fabricLoaderVersion = "0.16.10";

            var ignored = new HashSet<string>(_currentSettings?.IgnoredModConflicts ?? new List<string>(),
                StringComparer.OrdinalIgnoreCase);
            var permissive = BuildPermissiveManagedIdSet(mcVersion);

            var willDisable = await LeafClient.Services.ModCompatFilter.PreviewDisablesAsync(
                modsFolder, mcVersion, javaMajor);

            var (mods, conflicts) = await Task.Run(async () =>
            {
                var scanned = await ModScanner.ScanFolderAsync(modsFolder, fastMode: true).ConfigureAwait(false);
                if (willDisable.Count > 0)
                {
                    scanned = scanned
                        .Where(m => !willDisable.Contains(m.FilePath ?? ""))
                        .ToList();
                }
                var bundled = await LibrariesJijScanner.ScanAsync(_minecraftFolder).ConfigureAwait(false);
                var combined = new List<ModMetadata>(scanned.Count + bundled.Count);
                combined.AddRange(scanned);
                combined.AddRange(bundled);
                var found = DependencyResolver.FindConflicts(
                    combined, mcVersion, javaMajor, fabricLoaderVersion, ignored, permissive);
                return (scanned, found);
            });

            if (mods == null || mods.Count == 0)
                return PreLaunchCheckDecision.LaunchAnyway;

            var actionable = conflicts
                .Where(c => c.Severity == ConflictSeverity.Blocker || c.Severity == ConflictSeverity.Warning)
                .ToList();

            if (actionable.Count == 0)
            {
                _gameOutputWindow?.AppendLog(
                    $"[PreLaunchCheck] {mods.Count} mod(s) scanned, no actionable conflicts.", "INFO");
                return PreLaunchCheckDecision.LaunchAnyway;
            }

            _gameOutputWindow?.AppendLog(
                $"[PreLaunchCheck] {mods.Count} mod(s) scanned, {actionable.Count} actionable conflict(s) - prompting user.",
                "WARN");

            return await ShowPreLaunchCheckOverlayAsync(actionable, mcVersion);
        }

        private async void OnCheckCompatibilityClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                string selectedVersion = ResolveCurrentMcVersion();
                if (string.IsNullOrEmpty(selectedVersion))
                {
                    ShowToast("Pick a Minecraft version on the Home page first.", LeafClient.Services.ToastType.Info);
                    return;
                }

                string modsFolder = System.IO.Path.Combine(_minecraftFolder, "mods");
                if (!System.IO.Directory.Exists(modsFolder))
                {
                    ShowToast("No mods folder found yet. Launch the game once to create it.", LeafClient.Services.ToastType.Info);
                    return;
                }

                int javaMajor = DetectJavaMajorForMcVersion(selectedVersion);
                var ignoredSnap = new HashSet<string>(_currentSettings?.IgnoredModConflicts ?? new List<string>(),
                    StringComparer.OrdinalIgnoreCase);
                var permissive = BuildPermissiveManagedIdSet(selectedVersion);

                var willDisable = await LeafClient.Services.ModCompatFilter.PreviewDisablesAsync(
                    modsFolder, selectedVersion, javaMajor);

                var (mods, conflicts) = await Task.Run(async () =>
                {
                    var scanned = await ModScanner.ScanFolderAsync(modsFolder, fastMode: true).ConfigureAwait(false);
                    if (willDisable.Count > 0)
                    {
                        scanned = scanned
                            .Where(m => !willDisable.Contains(m.FilePath ?? ""))
                            .ToList();
                    }
                    var bundled = await LibrariesJijScanner.ScanAsync(_minecraftFolder).ConfigureAwait(false);
                    var combined = new List<ModMetadata>(scanned.Count + bundled.Count);
                    combined.AddRange(scanned);
                    combined.AddRange(bundled);
                    var found = DependencyResolver.FindConflicts(
                        combined, selectedVersion, javaMajor, "0.16.10", ignoredSnap, permissive);
                    return (scanned, found);
                });

                var actionable = conflicts
                    .Where(c => c.Severity == ConflictSeverity.Blocker || c.Severity == ConflictSeverity.Warning)
                    .ToList();

                LeafClient.Services.LeafLog.Info("PreLaunchCheck", $"Scanned {mods.Count} mod(s) for MC {selectedVersion}.");
                foreach (var m in mods)
                {
                    LeafClient.Services.LeafLog.Info("PreLaunchCheck", $"- {m.FileName} | id={m.ModId} v={m.Version} parsed={m.IsParsed} parseErr={m.ParseError ?? "-"} depends={m.Depends.Count} breaks={m.Breaks.Count}");
                    foreach (var b in m.Breaks)
                        LeafClient.Services.LeafLog.Info("PreLaunchCheck", $"breaks: {b.TargetId} {b.VersionRange}");
                }
                LeafClient.Services.LeafLog.Info("PreLaunchCheck", $"Permissive set: {string.Join(", ", permissive)}");
                LeafClient.Services.LeafLog.Info("PreLaunchCheck", $"Total conflicts: {conflicts.Count} | Actionable: {actionable.Count}");

                if (actionable.Count == 0)
                {
                    ShowToast($"No conflicts detected across {mods.Count} mod(s). See log for details.", LeafClient.Services.ToastType.Success);
                    return;
                }

                await ShowPreLaunchCheckOverlayAsync(actionable, selectedVersion, compatOnlyMode: true);
            }
            catch (Exception ex)
            {
                ShowToast($"Compatibility check failed: {ex.Message}", LeafClient.Services.ToastType.Error);
            }
        }

        private string ResolveCurrentMcVersion()
        {
            try
            {
                var activeProfile = _currentSettings?.Profiles?.FirstOrDefault(p => p.Id == _currentSettings.ActiveProfileId);
                if (activeProfile != null && !string.IsNullOrWhiteSpace(activeProfile.MinecraftVersion))
                    return activeProfile.MinecraftVersion;

                if (!string.IsNullOrWhiteSpace(_currentSettings?.SelectedSubVersion))
                    return _currentSettings.SelectedSubVersion;

                if (_versionDropdown?.SelectedItem is VersionInfo vi && !string.IsNullOrWhiteSpace(vi.FullVersion))
                    return vi.FullVersion;

                if (_versionDropdown?.ItemCount > 0 && _versionDropdown.Items[0] is VersionInfo first
                    && !string.IsNullOrWhiteSpace(first.FullVersion))
                    return first.FullVersion;
            }
            catch { }
            return "";
        }

        private static int DetectJavaMajorForMcVersion(string mcVersion)
        {
            if (string.IsNullOrEmpty(mcVersion)) return 21;
            var parts = mcVersion.Split('.');
            if (parts.Length < 2) return 21;
            if (!int.TryParse(parts[1], out int minor)) return 21;
            if (minor >= 21) return 21;
            if (minor >= 18) return 17;
            if (minor >= 17) return 16;
            return 8;
        }
    }
}
