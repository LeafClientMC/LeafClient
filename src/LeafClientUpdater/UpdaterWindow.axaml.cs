using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace LeafClientUpdater.Views
{
    // RENAMED: from MainWindow to UpdaterWindow
    public partial class UpdaterWindow : Window
    {
        private TextBlock? _statusText;
        private ProgressBar? _updateProgressBar;
        private Button? _closeButton;

        public UpdaterWindow()
        {
            InitializeComponent();

            _statusText = this.FindControl<TextBlock>("StatusText");
            _updateProgressBar = this.FindControl<ProgressBar>("UpdateProgressBar");
            _closeButton = this.FindControl<Button>("CloseButton");

            if (_closeButton != null)
            {
                _closeButton.Click += (_, __) => Close();
            }

            this.Opened += async (s, e) => await StartUpdateProcess();
        }

        private async Task StartUpdateProcess()
        {
            string[] args = Environment.GetCommandLineArgs();

            if (args.Length < 3)
            {
                UpdateStatus("Error: Missing update arguments.", true);
                Console.WriteLine("Usage: LeafClientUpdater.exe <pathToOldExe> <newExeDownloadUrl>");
                return;
            }

            string oldExePath = args[1];
            string newExeDownloadUrl = args[2];
            string appDirectory = System.IO.Path.GetDirectoryName(oldExePath)!;
            string newExeTempPath = System.IO.Path.Combine(appDirectory, "LeafClient_new.exe");
            string oldExeBackupPath = System.IO.Path.Combine(appDirectory, "LeafClient.old");
            string mainExeName = System.IO.Path.GetFileName(oldExePath);

            UpdateStatus($"Updating {mainExeName}...", false);
            Console.WriteLine($"[Updater] Starting update process for {mainExeName}");
            Console.WriteLine($"[Updater] Old EXE Path: {oldExePath}");
            Console.WriteLine($"[Updater] New EXE URL: {newExeDownloadUrl}");

            try
            {
                UpdateStatus("Waiting for main application to exit...", false);
                Console.WriteLine("[Updater] Waiting for main application to exit...");
                await Task.Delay(2000);

                Process[] processes = Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(mainExeName));
                foreach (var p in processes)
                {
                    try
                    {
                        p.Kill();
                        p.WaitForExit(1000);
                        Console.WriteLine($"[Updater] Force-killed lingering process {p.Id}");
                    }
                    catch { /* ignore */ }
                }

                await Task.Delay(1000);

                UpdateStatus($"Downloading new {mainExeName}...", false);
                Console.WriteLine($"[Updater] Downloading new {mainExeName} from {newExeDownloadUrl}...");
                using (var client = new HttpClient())
                {
                    byte[] newExeBytes = await client.GetByteArrayAsync(newExeDownloadUrl);
                    await System.IO.File.WriteAllBytesAsync(newExeTempPath, newExeBytes);
                }
                Console.WriteLine($"[Updater] New {mainExeName} downloaded to {newExeTempPath}");

                UpdateStatus("Installing update...", false);
                Console.WriteLine($"[Updater] Renaming old {mainExeName} to {System.IO.Path.GetFileName(oldExeBackupPath)}");
                if (System.IO.File.Exists(oldExeBackupPath))
                {
                    System.IO.File.Delete(oldExeBackupPath);
                }
                System.IO.File.Move(oldExePath, oldExeBackupPath);

                Console.WriteLine($"[Updater] Moving new {mainExeName} from temp to {oldExePath}");
                System.IO.File.Move(newExeTempPath, oldExePath);

                UpdateStatus("Update successful! Launching Leaf Client...", false);
                Console.WriteLine($"[Updater] Update successful! Launching new {mainExeName}...");

                Process.Start(new ProcessStartInfo
                {
                    FileName = oldExePath,
                    UseShellExecute = true
                });

                try
                {
                    if (System.IO.File.Exists(oldExeBackupPath))
                    {
                        System.IO.File.Delete(oldExeBackupPath);
                        Console.WriteLine("[Updater] Cleaned up old backup file.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Updater] Warning: Could not delete old backup file: {ex.Message}");
                }

                await Task.Delay(1000);
                Close();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Update failed: {ex.Message}", true);
                Console.Error.WriteLine($"[Updater ERROR] Update failed: {ex.Message}");
            }
        }

        private void UpdateStatus(string message, bool isError)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_statusText != null)
                {
                    _statusText.Text = message;
                    _statusText.Foreground = isError ? Avalonia.Media.Brushes.Red : Avalonia.Media.Brushes.White;
                }
                if (_updateProgressBar != null)
                {
                    _updateProgressBar.IsIndeterminate = !isError;
                }
                if (_closeButton != null)
                {
                    _closeButton.IsVisible = isError;
                }
            });
        }
    }
}
