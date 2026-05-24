using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using LeafClient.Services;

namespace LeafClient.Views
{
    public partial class ModMigrationWindow : Window
    {
        public enum Decision
        {
            Cancel,
            Skip,
            Apply,
            ApplyOnly,
        }

        public Decision Result { get; private set; } = Decision.Cancel;
        public ModMigrationService.MigrationResult? AppliedResult { get; private set; }

        private readonly ModMigrationService _service;
        private readonly ModMigrationService.MigrationPlan _plan;
        private readonly bool _launchMode;
        private bool _busy;
        private bool _applyOnlyMode;
        private CancellationTokenSource? _cts;

        public ModMigrationWindow() : this(BuildEmptyService(), BuildEmptyPlan()) { }

        public ModMigrationWindow(ModMigrationService service, ModMigrationService.MigrationPlan plan)
            : this(service, plan, launchMode: true) { }

        public ModMigrationWindow(ModMigrationService service, ModMigrationService.MigrationPlan plan, bool launchMode)
        {
            _service = service;
            _plan = plan;
            _launchMode = launchMode;
            InitializeComponent();
            ApplyLaunchModeChrome();
            Render();
            Opened += (_, _) =>
            {
                try
                {
                    var bgImage = this.FindControl<Image>("BgImage");
                    if (bgImage != null)
                    {
                        var bmp = BackgroundThemeService.Instance.GetCurrentBitmap();
                        if (bmp != null) bgImage.Source = bmp;
                    }
                }
                catch { }
            };
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private static ModMigrationService BuildEmptyService() => null!;
        private static ModMigrationService.MigrationPlan BuildEmptyPlan() =>
            new() { FromMcVersion = "", ToMcVersion = "", Loader = "fabric", Items = new() };

        private void Render()
        {
            var headerTitle = this.FindControl<TextBlock>("HeaderTitle");
            var headerSubtitle = this.FindControl<TextBlock>("HeaderSubtitle");
            var upgradeSection = this.FindControl<Border>("UpgradeSection");
            var upgradeTitle = this.FindControl<TextBlock>("UpgradeSectionTitle");
            var upgradeList = this.FindControl<ItemsControl>("UpgradeList");
            var incompatSection = this.FindControl<Border>("IncompatibleSection");
            var incompatTitle = this.FindControl<TextBlock>("IncompatibleSectionTitle");
            var incompatList = this.FindControl<ItemsControl>("IncompatibleList");
            var emptySection = this.FindControl<Border>("EmptySection");
            var footerHint = this.FindControl<TextBlock>("FooterHint");
            var applyButton = this.FindControl<Button>("ApplyButton");

            var upgrades = _plan.Upgradable.ToList();
            var incompats = _plan.Incompatible.ToList();
            var unknowns = _plan.NotOnModrinth.ToList();
            int upgradeCount = upgrades.Count;
            int incompatCount = incompats.Count;
            int unknownCount = unknowns.Count;

            if (headerTitle != null)
                headerTitle.Text = $"Update mods for Minecraft {_plan.ToMcVersion}";
            if (headerSubtitle != null)
            {
                var parts = new List<string>();
                if (upgradeCount > 0) parts.Add($"{upgradeCount} can be updated");
                if (incompatCount > 0) parts.Add($"{incompatCount} have no release for the target");
                if (unknownCount > 0) parts.Add($"{unknownCount} couldn't be identified on Modrinth");
                headerSubtitle.Text = parts.Count == 0
                    ? "No installed mods need attention for the target version."
                    : string.Join(", ", parts) + ".";
            }

            if (upgradeSection != null) upgradeSection.IsVisible = upgradeCount > 0;
            if (incompatSection != null) incompatSection.IsVisible = incompatCount > 0;
            if (emptySection != null) emptySection.IsVisible = upgradeCount == 0 && incompatCount == 0 && unknownCount == 0;
            if (upgradeTitle != null) upgradeTitle.Text = $"Updates available ({upgradeCount})";
            if (incompatTitle != null) incompatTitle.Text = $"No compatible version ({incompatCount})";

            var unknownSection = this.FindControl<Border>("UnknownSection");
            var unknownTitle = this.FindControl<TextBlock>("UnknownSectionTitle");
            var unknownList = this.FindControl<ItemsControl>("UnknownList");
            if (unknownSection != null) unknownSection.IsVisible = unknownCount > 0;
            if (unknownTitle != null) unknownTitle.Text = $"Not on Modrinth ({unknownCount})";
            if (unknownList != null) unknownList.ItemsSource = BuildUnknownRows(unknowns);

            if (upgradeList != null) upgradeList.ItemsSource = BuildUpgradeRows(upgrades);
            if (incompatList != null) incompatList.ItemsSource = BuildIncompatRows(incompats);

            if (footerHint != null)
            {
                if (upgradeCount == 0 && incompatCount == 0)
                    footerHint.Text = unknownCount > 0
                        ? "Apply has no Modrinth-managed changes to make. Unknown jars are left untouched."
                        : (_launchMode ? "Nothing to apply. Skip or close to launch." : "Nothing to apply. Close to dismiss.");
                else
                    footerHint.Text = "Apply will move replaced jars to mods/.leaf-backup/ so you can restore them.";
            }
            if (applyButton != null) applyButton.IsEnabled = (upgradeCount + incompatCount) > 0;
        }

        private IEnumerable<Border> BuildUnknownRows(IEnumerable<ModMigrationService.PlanItem> items)
        {
            foreach (var item in items)
            {
                var name = new TextBlock { Text = item.DisplayName, Foreground = new SolidColorBrush(Color.Parse("#F1F5F9")), FontWeight = FontWeight.SemiBold, FontSize = 12 };
                var sub = new TextBlock
                {
                    Text = $"{item.Source.FileName}  ·  ModId '{item.Source.ModId}'",
                    Foreground = new SolidColorBrush(Color.Parse("#94A3B8")),
                    FontSize = 11,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                };
                var stack = new StackPanel { Spacing = 2, Children = { name, sub } };
                yield return new Border
                {
                    Padding = new Avalonia.Thickness(10, 8),
                    Margin = new Avalonia.Thickness(0, 0, 0, 6),
                    CornerRadius = new Avalonia.CornerRadius(8),
                    Background = new SolidColorBrush(Color.Parse("#15140A")),
                    Child = stack,
                };
            }
        }

        private IEnumerable<Border> BuildUpgradeRows(IEnumerable<ModMigrationService.PlanItem> items)
        {
            foreach (var item in items)
            {
                var name = new TextBlock { Text = item.DisplayName, Foreground = new SolidColorBrush(Color.Parse("#F1F5F9")), FontWeight = FontWeight.SemiBold, FontSize = 12 };
                var sub = new TextBlock
                {
                    Text = $"{item.Source.Version}  →  {item.NewVersionNumber ?? "(unknown)"}    {item.NewFileName}",
                    Foreground = new SolidColorBrush(Color.Parse("#94A3B8")),
                    FontSize = 11,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                };
                var stack = new StackPanel { Spacing = 2, Children = { name, sub } };
                var check = new CheckBox
                {
                    IsChecked = item.Selected,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Margin = new Avalonia.Thickness(0, 0, 10, 0),
                };
                check.IsCheckedChanged += (_, _) => item.Selected = check.IsChecked == true;
                var grid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                };
                Grid.SetColumn(check, 0);
                Grid.SetColumn(stack, 1);
                grid.Children.Add(check);
                grid.Children.Add(stack);
                yield return new Border
                {
                    Padding = new Avalonia.Thickness(10, 8),
                    Margin = new Avalonia.Thickness(0, 0, 0, 6),
                    CornerRadius = new Avalonia.CornerRadius(8),
                    Background = new SolidColorBrush(Color.Parse("#0F1B27")),
                    Child = grid,
                };
            }
        }

        private IEnumerable<Border> BuildIncompatRows(IEnumerable<ModMigrationService.PlanItem> items)
        {
            foreach (var item in items)
            {
                var name = new TextBlock { Text = item.DisplayName, Foreground = new SolidColorBrush(Color.Parse("#F1F5F9")), FontWeight = FontWeight.SemiBold, FontSize = 12 };
                var sub = new TextBlock
                {
                    Text = $"Currently for {item.Source.MinecraftVersion}  ·  {item.Source.FileName}",
                    Foreground = new SolidColorBrush(Color.Parse("#94A3B8")),
                    FontSize = 11,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                };
                var stack = new StackPanel { Spacing = 2, Children = { name, sub } };
                var check = new CheckBox
                {
                    IsChecked = item.Selected,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Margin = new Avalonia.Thickness(0, 0, 10, 0),
                };
                check.IsCheckedChanged += (_, _) => item.Selected = check.IsChecked == true;
                var grid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                };
                Grid.SetColumn(check, 0);
                Grid.SetColumn(stack, 1);
                grid.Children.Add(check);
                grid.Children.Add(stack);
                yield return new Border
                {
                    Padding = new Avalonia.Thickness(10, 8),
                    Margin = new Avalonia.Thickness(0, 0, 0, 6),
                    CornerRadius = new Avalonia.CornerRadius(8),
                    Background = new SolidColorBrush(Color.Parse("#1A1014")),
                    Child = grid,
                };
            }
        }

        private void OnDragZonePointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                BeginMoveDrag(e);
        }

        private void OnMinimizeClick(object? sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnMaximizeClick(object? sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void ApplyLaunchModeChrome()
        {
            var cancel = this.FindControl<Button>("CancelButton");
            var skip = this.FindControl<Button>("SkipButton");
            var applyOnly = this.FindControl<Button>("ApplyOnlyButton");
            var apply = this.FindControl<Button>("ApplyButton");

            if (_launchMode)
            {
                if (cancel != null) { cancel.Content = "Cancel launch"; cancel.IsVisible = true; }
                if (skip != null) { skip.Content = "Skip and launch"; skip.IsVisible = true; }
                if (applyOnly != null) { applyOnly.Content = "Apply only"; applyOnly.IsVisible = true; }
                if (apply != null) { apply.Content = "Apply and launch"; apply.IsVisible = true; }
            }
            else
            {
                if (cancel != null) { cancel.Content = "Close"; cancel.IsVisible = true; }
                if (skip != null) skip.IsVisible = false;
                if (applyOnly != null) applyOnly.IsVisible = false;
                if (apply != null) { apply.Content = "Apply"; apply.IsVisible = true; }
            }
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            if (_busy)
            {
                try { _cts?.Cancel(); } catch { }
                return;
            }
            Result = Decision.Cancel;
            Close();
        }

        private void OnSkipClick(object? sender, RoutedEventArgs e)
        {
            if (_busy) return;
            Result = Decision.Skip;
            Close();
        }

        private async void OnApplyOnlyClick(object? sender, RoutedEventArgs e)
        {
            _applyOnlyMode = true;
            await RunApplyAsync();
        }

        private async void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            _applyOnlyMode = !_launchMode;
            await RunApplyAsync();
        }

        private async Task RunApplyAsync()
        {
            if (_busy || _service == null) return;
            var anySelected = _plan.Items.Any(i =>
                (i.Status == ModMigrationService.ItemStatus.CanUpgrade ||
                 i.Status == ModMigrationService.ItemStatus.NoCompatibleVersion) && i.Selected);
            if (!anySelected)
            {
                Result = _applyOnlyMode ? Decision.ApplyOnly : Decision.Skip;
                Close();
                return;
            }

            _busy = true;
            _cts = new CancellationTokenSource();
            var box = this.FindControl<Border>("ProgressBox");
            var detail = this.FindControl<TextBlock>("ProgressDetail");
            var apply = this.FindControl<Button>("ApplyButton");
            var applyOnly = this.FindControl<Button>("ApplyOnlyButton");
            var skip = this.FindControl<Button>("SkipButton");
            if (box != null) box.IsVisible = true;
            if (apply != null) { apply.IsEnabled = false; apply.Content = "Working..."; }
            if (applyOnly != null) applyOnly.IsEnabled = false;
            if (skip != null) skip.IsEnabled = false;

            try
            {
                var progress = new Progress<string>(s =>
                {
                    if (detail != null) Dispatcher.UIThread.Post(() => detail.Text = s);
                });
                AppliedResult = await _service.ApplyAsync(_plan, progress, _cts.Token);
                Result = _applyOnlyMode ? Decision.ApplyOnly : Decision.Apply;
                _busy = false;
                ShowResults(AppliedResult);
            }
            catch (OperationCanceledException)
            {
                Result = Decision.Cancel;
                _busy = false;
                Close();
            }
            catch (Exception ex)
            {
                LeafLog.Error("ModMigration", $"Apply failed: {ex.Message}");
                Result = Decision.Skip;
                _busy = false;
                Close();
            }
        }

        private void ShowResults(ModMigrationService.MigrationResult? r)
        {
            if (r == null) { Close(); return; }
            HideIf("UpgradeSection");
            HideIf("IncompatibleSection");
            HideIf("UnknownSection");
            HideIf("EmptySection");
            HideIf("BackupInfoSection");
            HideIf("ProgressBox");
            HideIf("PlanButtonRow");

            var header = this.FindControl<TextBlock>("HeaderTitle");
            var sub = this.FindControl<TextBlock>("HeaderSubtitle");
            bool hadFailures = r.Failed > 0;
            if (header != null) header.Text = hadFailures
                ? $"Migration finished with {r.Failed} failure{(r.Failed == 1 ? "" : "s")}"
                : "Migration complete";
            if (sub != null) sub.Text = $"Minecraft {r.FromMcVersion} → {r.ToMcVersion}";

            ShowIf("ResultsSummarySection");
            var resHeader = this.FindControl<TextBlock>("ResultsHeader");
            var resSummary = this.FindControl<TextBlock>("ResultsSummary");
            var resBackup = this.FindControl<TextBlock>("ResultsBackupPath");
            if (resHeader != null) resHeader.Text = $"Updated {r.Updated}  ·  Disabled {r.Disabled}  ·  Failed {r.Failed}";
            if (resSummary != null) resSummary.Text = hadFailures
                ? "Some mods could not be updated. Their originals are preserved in the backup folder below. The rest succeeded."
                : "Replaced jars were moved to the backup folder below. Restore by copying them back into mods/.";
            if (resBackup != null) resBackup.Text = string.IsNullOrEmpty(r.BackupFolder) ? "(no backup folder created)" : r.BackupFolder;

            if (r.UpdatedItems.Count > 0)
            {
                ShowIf("ResultsUpdatedSection");
                var t = this.FindControl<TextBlock>("ResultsUpdatedTitle");
                if (t != null) t.Text = $"Updated ({r.UpdatedItems.Count})";
                var list = this.FindControl<ItemsControl>("ResultsUpdatedList");
                if (list != null) list.ItemsSource = r.UpdatedItems.Select(s => BuildRow(s, "#86EFAC")).ToList();
            }
            if (r.DisabledItems.Count > 0)
            {
                ShowIf("ResultsDisabledSection");
                var t = this.FindControl<TextBlock>("ResultsDisabledTitle");
                if (t != null) t.Text = $"Disabled ({r.DisabledItems.Count})";
                var list = this.FindControl<ItemsControl>("ResultsDisabledList");
                if (list != null) list.ItemsSource = r.DisabledItems.Select(s => BuildRow(s, "#FCD34D")).ToList();
            }
            if (r.FailureMessages.Count > 0)
            {
                ShowIf("ResultsFailedSection");
                var t = this.FindControl<TextBlock>("ResultsFailedTitle");
                if (t != null) t.Text = $"Failed ({r.FailureMessages.Count})";
                var list = this.FindControl<ItemsControl>("ResultsFailedList");
                if (list != null) list.ItemsSource = r.FailureMessages.Select(s => BuildRow(s, "#FCA5A5")).ToList();
            }
            ShowIf("ResultsButtonRow");

            var hint = this.FindControl<TextBlock>("ResultsFooterHint");
            string nextStep = _applyOnlyMode
                ? "Close to return to the launcher."
                : (_launchMode ? "Close to continue launching." : "Close to dismiss.");
            if (hint != null) hint.Text = hadFailures
                ? "You can retry later. Originals are safe in the backup folder."
                : "All set. " + nextStep;
            var resultsClose = this.FindControl<Button>("ResultsCloseButton");
            if (resultsClose != null)
                resultsClose.Content = (_launchMode && !_applyOnlyMode) ? "Close and launch" : "Close";
        }

        private void HideIf(string name)
        {
            var c = this.FindControl<Control>(name);
            if (c != null) c.IsVisible = false;
        }
        private void ShowIf(string name)
        {
            var c = this.FindControl<Control>(name);
            if (c != null) c.IsVisible = true;
        }

        private Border BuildRow(string text, string colorHex)
        {
            return new Border
            {
                Padding = new Avalonia.Thickness(10, 6),
                Margin = new Avalonia.Thickness(0, 0, 0, 4),
                CornerRadius = new Avalonia.CornerRadius(6),
                Background = new SolidColorBrush(Color.Parse("#0F1B27")),
                Child = new TextBlock
                {
                    Text = text,
                    Foreground = new SolidColorBrush(Color.Parse(colorHex)),
                    FontSize = 11,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                },
            };
        }

        private void OnOpenBackupClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                string? path = AppliedResult?.BackupFolder;
                if (string.IsNullOrWhiteSpace(path) || !System.IO.Directory.Exists(path))
                {
                    LeafLog.Info("ModMigration", "No backup folder to open.");
                    return;
                }
                LeafClient.Utils.SystemUtil.OpenFolder(path);
            }
            catch (Exception ex) { LeafLog.Error("ModMigration", $"Open backup failed: {ex.Message}"); }
        }

        private async void OnCopyLogClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                var r = AppliedResult;
                if (r == null) return;
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"LeafClient mod migration {r.FromMcVersion} -> {r.ToMcVersion}");
                sb.AppendLine($"Completed: {r.CompletedAtUtc:u}");
                sb.AppendLine($"Updated: {r.Updated}  Disabled: {r.Disabled}  Failed: {r.Failed}");
                sb.AppendLine($"Backup folder: {r.BackupFolder}");
                if (r.UpdatedItems.Count > 0) { sb.AppendLine(""); sb.AppendLine("Updated:"); foreach (var s in r.UpdatedItems) sb.AppendLine($"  - {s}"); }
                if (r.DisabledItems.Count > 0) { sb.AppendLine(""); sb.AppendLine("Disabled:"); foreach (var s in r.DisabledItems) sb.AppendLine($"  - {s}"); }
                if (r.FailureMessages.Count > 0) { sb.AppendLine(""); sb.AppendLine("Failed:"); foreach (var s in r.FailureMessages) sb.AppendLine($"  - {s}"); }
                var clip = this.Clipboard;
                if (clip != null) await clip.SetTextAsync(sb.ToString());
            }
            catch (Exception ex) { LeafLog.Error("ModMigration", $"Copy log failed: {ex.Message}"); }
        }

        private void OnResultsCloseClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
