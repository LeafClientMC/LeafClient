using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using LeafClient.Services;

namespace LeafClient.Views
{
    public partial class MainWindow
    {
        public enum ModMigrationOutcome
        {
            Cancel,
            Continue,
        }

        private async Task<ModMigrationOutcome> RunModMigrationCheckBeforeLaunchAsync(string mcVersion)
        {
            if (_currentSettings == null || string.IsNullOrWhiteSpace(mcVersion))
                return ModMigrationOutcome.Continue;

            var nonAutoInstalled = _currentSettings.InstalledMods
                .Where(m => !m.IsAutoInstalled && !string.IsNullOrWhiteSpace(m.ModId))
                .ToList();
            if (nonAutoInstalled.Count == 0) return ModMigrationOutcome.Continue;

            bool hasMatching = nonAutoInstalled.Any(m =>
                string.Equals(m.MinecraftVersion, mcVersion, StringComparison.OrdinalIgnoreCase));
            var candidateFromVersions = nonAutoInstalled
                .Where(m => !string.Equals(m.MinecraftVersion, mcVersion, StringComparison.OrdinalIgnoreCase))
                .Select(m => m.MinecraftVersion)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .GroupBy(v => v, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .ToList();
            if (candidateFromVersions.Count == 0) return ModMigrationOutcome.Continue;

            string fromVersion = candidateFromVersions[0];

            var service = new ModMigrationService(_modrinthClient, _minecraftFolder, _currentSettings, _settingsService);

            ModMigrationService.MigrationPlan plan;
            try
            {
                _gameOutputWindow?.AppendLog($"[ModMigration] Scanning Modrinth for {fromVersion} → {mcVersion} migrations...", "INFO");
                plan = await service.BuildPlanAsync(fromVersion, mcVersion, "fabric");
            }
            catch (Exception ex)
            {
                LeafLog.Error("ModMigration", $"Plan build failed: {ex.Message}");
                return ModMigrationOutcome.Continue;
            }

            if (!plan.NeedsAttention)
            {
                _gameOutputWindow?.AppendLog("[ModMigration] No migrations needed.", "INFO");
                return ModMigrationOutcome.Continue;
            }

            int upgradable = plan.Upgradable.Count();
            int incompat = plan.Incompatible.Count();
            _gameOutputWindow?.AppendLog($"[ModMigration] {upgradable} upgradable, {incompat} incompatible.", "INFO");

            ModMigrationWindow.Decision decision = ModMigrationWindow.Decision.Skip;
            ModMigrationService.MigrationResult? applied = null;

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var dialog = new ModMigrationWindow(service, plan);
                if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop &&
                    desktop.MainWindow != null)
                {
                    await dialog.ShowDialog(desktop.MainWindow);
                }
                else
                {
                    await dialog.ShowDialog(this);
                }
                decision = dialog.Result;
                applied = dialog.AppliedResult;
            });

            if (decision == ModMigrationWindow.Decision.Cancel) return ModMigrationOutcome.Cancel;
            if (applied != null)
            {
                _gameOutputWindow?.AppendLog(
                    $"[ModMigration] Updated={applied.Updated} Disabled={applied.Disabled} Failed={applied.Failed} Backup={applied.BackupFolder}",
                    applied.Failed > 0 ? "WARN" : "INFO");
                foreach (var msg in applied.FailureMessages)
                    _gameOutputWindow?.AppendLog($"[ModMigration]  ! {msg}", "WARN");
                try { RefreshLastMigrationPanel(); } catch { }
            }
            if (decision == ModMigrationWindow.Decision.ApplyOnly) return ModMigrationOutcome.Cancel;
            return ModMigrationOutcome.Continue;
        }
    }
}
