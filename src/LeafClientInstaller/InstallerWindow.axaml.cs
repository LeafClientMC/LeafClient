using System;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace LeafClientInstaller;

public partial class InstallerWindow : Window
{
    private const string CdnBase = "https://cdn.leafclient.com/updates";

    private static string DownloadUrl
    {
        get
        {
            if (OperatingSystem.IsLinux())
                return $"{CdnBase}/LeafClient-linux-x64.tar.gz";
            return $"{CdnBase}/LeafClient-win-x64.zip";
        }
    }

    private static string DownloadFileName =>
        OperatingSystem.IsLinux() ? "LeafClient_download.tar.gz" : "LeafClient_download.zip";

    private const double ProgressBarMaxWidth = 400.0;

    private static readonly string DefaultInstallPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LeafClient");

    private StackPanel? _stepWelcome;
    private StackPanel? _stepConfig;
    private StackPanel? _stepInstalling;
    private StackPanel? _stepComplete;
    private Border? _progressFill;
    private TextBlock? _installingTitle;
    private TextBlock? _installingStatus;
    private Border? _errorBanner;
    private TextBlock? _errorBannerText;
    private TextBox? _pathBox;
    private CheckBox? _desktopShortcutCheck;
    private CheckBox? _startMenuShortcutCheck;
    private Button? _nextBtn;
    private Button? _backBtn;
    private Button? _cancelBtn;

    private int _currentStep;
    private bool _isInstalling;
    private double _currentProgress;
    private double _targetProgress;
    private DispatcherTimer? _progressTimer;

    public InstallerWindow()
    {
        InitializeComponent();

        _stepWelcome = this.FindControl<StackPanel>("StepWelcome");
        _stepConfig = this.FindControl<StackPanel>("StepConfig");
        _stepInstalling = this.FindControl<StackPanel>("StepInstalling");
        _stepComplete = this.FindControl<StackPanel>("StepComplete");
        _progressFill = this.FindControl<Border>("ProgressFill");
        _installingTitle = this.FindControl<TextBlock>("InstallingTitle");
        _installingStatus = this.FindControl<TextBlock>("InstallingStatus");
        _errorBanner = this.FindControl<Border>("ErrorBanner");
        _errorBannerText = this.FindControl<TextBlock>("ErrorBannerText");
        _pathBox = this.FindControl<TextBox>("PathBox");
        _desktopShortcutCheck = this.FindControl<CheckBox>("DesktopShortcutCheck");
        _startMenuShortcutCheck = this.FindControl<CheckBox>("StartMenuShortcutCheck");
        _nextBtn = this.FindControl<Button>("NextBtn");
        _backBtn = this.FindControl<Button>("BackBtn");
        _cancelBtn = this.FindControl<Button>("CancelBtn");

        if (_pathBox != null)
            _pathBox.Text = DefaultInstallPath;

        var titleBar = this.FindControl<Border>("TitleBar");
        if (titleBar != null)
        {
            titleBar.PointerPressed += (_, e) =>
            {
                if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                    BeginMoveDrag(e);
            };
        }

        var bgImage = this.FindControl<Image>("BgImage");
        if (bgImage != null)
        {
            double _pTargetX = 0, _pTargetY = 0, _pCurX = 0, _pCurY = 0;
            this.PointerMoved += (_, e) =>
            {
                var pos = e.GetPosition(this);
                _pTargetX = (pos.X / Width - 0.5) * -15;
                _pTargetY = (pos.Y / Height - 0.5) * -10;
            };

            var parallaxTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            parallaxTimer.Tick += (_, _) =>
            {
                _pCurX += (_pTargetX - _pCurX) * 0.06;
                _pCurY += (_pTargetY - _pCurY) * 0.06;
                if (bgImage.RenderTransform is Avalonia.Media.TransformGroup tg)
                {
                    var translate = tg.Children[1] as Avalonia.Media.TranslateTransform;
                    if (translate != null)
                    {
                        translate.X = _pCurX;
                        translate.Y = _pCurY;
                    }
                }
            };
            parallaxTimer.Start();
        }

        _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _progressTimer.Tick += OnProgressTick;

        _currentStep = 0;

        this.Loaded += async (_, _) =>
        {
            await Task.Delay(80);
            if (_stepWelcome != null)
                _stepWelcome.Opacity = 1.0;
        };
    }

    private void OnProgressTick(object? sender, EventArgs e)
    {
        if (Math.Abs(_currentProgress - _targetProgress) > 0.1)
        {
            _currentProgress += (_targetProgress - _currentProgress) * 0.08;
            UpdateProgressBarWidth(_currentProgress);
        }
    }

    private void UpdateProgressBarWidth(double progress)
    {
        if (_progressFill != null)
        {
            _progressFill.Width = Math.Max(0, Math.Min(ProgressBarMaxWidth, ProgressBarMaxWidth * (progress / 100.0)));
        }
    }

    private async Task TransitionToStep(StackPanel? fromStep, StackPanel? toStep)
    {
        if (fromStep != null)
        {
            fromStep.Opacity = 0;
            await Task.Delay(300);
            fromStep.IsVisible = false;
        }

        if (toStep != null)
        {
            toStep.IsVisible = true;
            await Task.Delay(30);
            toStep.Opacity = 1;
        }
    }

    private void UpdateNavigationButtons()
    {
        if (_nextBtn == null || _backBtn == null || _cancelBtn == null) return;

        switch (_currentStep)
        {
            case 0:
                _nextBtn.IsVisible = true;
                _nextBtn.Content = "Next";
                _backBtn.IsVisible = false;
                _cancelBtn.IsVisible = false;
                break;
            case 1:
                _nextBtn.IsVisible = true;
                _nextBtn.Content = "Install";
                _backBtn.IsVisible = true;
                _cancelBtn.IsVisible = false;
                break;
            case 2:
                _nextBtn.IsVisible = false;
                _backBtn.IsVisible = false;
                _cancelBtn.IsVisible = true;
                break;
            case 3:
                _nextBtn.IsVisible = false;
                _backBtn.IsVisible = false;
                _cancelBtn.IsVisible = false;
                break;
        }
    }

    private async void Next_Click(object? sender, RoutedEventArgs e)
    {
        switch (_currentStep)
        {
            case 0:
                _currentStep = 1;
                UpdateNavigationButtons();
                await TransitionToStep(_stepWelcome, _stepConfig);
                break;
            case 1:
                await StartInstall();
                break;
        }
    }

    private async void Back_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentStep == 1)
        {
            _currentStep = 0;
            UpdateNavigationButtons();
            await TransitionToStep(_stepConfig, _stepWelcome);
        }
    }

    private async Task StartInstall()
    {
        if (_isInstalling) return;
        _isInstalling = true;

        HideError();

        var installPath = _pathBox?.Text?.Trim() ?? DefaultInstallPath;

        _currentProgress = 0;
        _targetProgress = 0;
        UpdateProgressBarWidth(0);

        _currentStep = 2;
        UpdateNavigationButtons();
        await TransitionToStep(_stepConfig, _stepInstalling);

        _progressTimer?.Start();

        try
        {
            await RunInstallAsync(installPath);

            _progressTimer?.Stop();

            _currentStep = 3;
            UpdateNavigationButtons();
            await TransitionToStep(_stepInstalling, _stepComplete);
        }
        catch (Exception ex)
        {
            _progressTimer?.Stop();
            ShowError($"Installation failed: {ex.Message}");

            _currentStep = 1;
            UpdateNavigationButtons();
            await TransitionToStep(_stepInstalling, _stepConfig);
        }
        finally
        {
            _isInstalling = false;
        }
    }

    private void Launch_Click(object? sender, RoutedEventArgs e)
    {
        var installPath = _pathBox?.Text?.Trim() ?? DefaultInstallPath;
        var exePath = FindExecutable(installPath);

        if (exePath is not null && File.Exists(exePath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true
            });
        }

        CloseWindow();
    }

    private async Task RunInstallAsync(string installPath)
    {
        UpdateInstallStatus("Installing Leaf Client", "Preparing...", 5);
        Directory.CreateDirectory(installPath);

        UpdateInstallStatus("Installing Leaf Client", "Downloading...", 10);
        var zipPath = Path.Combine(Path.GetTempPath(), DownloadFileName);

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("LeafClientInstaller/1.0");

            using var response = await client.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            long downloadedBytes = 0;

            await using var contentStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

            var buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                downloadedBytes += bytesRead;

                if (totalBytes > 0)
                {
                    var downloadProgress = (double)downloadedBytes / totalBytes;
                    var progressValue = 10 + (int)(downloadProgress * 60);
                    var sizeText = $"Downloading... {downloadedBytes / 1024:N0} KB";
                    if (totalBytes > 0)
                        sizeText += $" / {totalBytes / 1024:N0} KB";

                    UpdateInstallStatus("Installing Leaf Client", sizeText, progressValue);
                }
            }
        }

        UpdateInstallStatus("Installing Leaf Client", "Extracting files...", 75);
        if (Directory.Exists(installPath))
        {
            if (OperatingSystem.IsWindows())
            {
                foreach (var file in Directory.GetFiles(installPath, "*.exe"))
                {
                    TryDeleteFile(file);
                }
                foreach (var file in Directory.GetFiles(installPath, "*.dll"))
                {
                    TryDeleteFile(file);
                }
            }
            else
            {
                var leafBin = Path.Combine(installPath, "LeafClient");
                if (File.Exists(leafBin)) TryDeleteFile(leafBin);
            }
        }

        if (OperatingSystem.IsLinux())
        {
            await ExtractTarGzAsync(zipPath, installPath);
            TryChmodExecutable(Path.Combine(installPath, "LeafClient"));
        }
        else
        {
            await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, installPath, overwriteFiles: true));
        }
        TryDeleteFile(zipPath);

        UpdateInstallStatus("Installing Leaf Client", "Finalizing...", 85);

        var exePath = FindExecutable(installPath);

        if (_desktopShortcutCheck?.IsChecked == true && exePath is not null)
        {
            UpdateInstallStatus("Installing Leaf Client", "Creating desktop shortcut...", 90);
            CreateShortcut(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Leaf Client.lnk"),
                exePath,
                installPath);
        }

        if (_startMenuShortcutCheck?.IsChecked == true && exePath is not null)
        {
            UpdateInstallStatus("Installing Leaf Client", "Creating Start Menu shortcut...", 95);
            var startMenuDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                "Programs",
                "Leaf Client");
            Directory.CreateDirectory(startMenuDir);
            CreateShortcut(
                Path.Combine(startMenuDir, "Leaf Client.lnk"),
                exePath,
                installPath);
        }

        UpdateInstallStatus("Installing Leaf Client", "Complete!", 100);
        await Task.Delay(400);
    }

    private void MinimizeWindow_Click(object? sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void CloseWindow_Click(object? sender, RoutedEventArgs e)
        => CloseWindow();

    private void CloseWindow()
    {
        _progressTimer?.Stop();
        this.Close();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        _progressTimer?.Stop();
        base.OnClosing(e);
    }

    private async void Browse_Click(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Install Location",
            AllowMultiple = false
        });

        if (folders.Count > 0 && _pathBox != null)
        {
            _pathBox.Text = folders[0].Path.LocalPath;
        }
    }

    private void UpdateInstallStatus(string title, string status, int progress)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_installingTitle != null) _installingTitle.Text = title;
            if (_installingStatus != null) _installingStatus.Text = status;
            _targetProgress = progress;
        });
    }

    private async void ShowError(string message)
    {
        if (_errorBanner != null && _errorBannerText != null)
        {
            _errorBannerText.Text = message;
            _errorBanner.IsVisible = true;
            await Task.Delay(5000);
            _errorBanner.IsVisible = false;
        }
    }

    private void HideError()
    {
        if (_errorBanner != null)
            _errorBanner.IsVisible = false;
    }

    private static string? FindExecutable(string directory)
    {
        if (!Directory.Exists(directory)) return null;

        var isWindows = OperatingSystem.IsWindows();
        var binaryName = isWindows ? "LeafClient.exe" : "LeafClient";
        var searchPattern = isWindows ? "*.exe" : "LeafClient";

        var leafBin = Path.Combine(directory, binaryName);
        if (File.Exists(leafBin)) return leafBin;

        foreach (var bin in Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories))
        {
            return bin;
        }

        return null;
    }

    private static void TryDeleteFile(string path)
    {
        try { File.Delete(path); }
        catch { }
    }

    private static async Task ExtractTarGzAsync(string archivePath, string installPath)
    {
        var stagingDir = Path.Combine(Path.GetTempPath(), "LeafClient_extract_" + Path.GetRandomFileName());
        Directory.CreateDirectory(stagingDir);

        try
        {
            await using (var fs = File.OpenRead(archivePath))
            await using (var gz = new GZipStream(fs, CompressionMode.Decompress))
            {
                await TarFile.ExtractToDirectoryAsync(gz, stagingDir, overwriteFiles: true);
            }

            var entries = Directory.GetFileSystemEntries(stagingDir);
            string sourceRoot = stagingDir;
            if (entries.Length == 1 && Directory.Exists(entries[0]))
                sourceRoot = entries[0];

            Directory.CreateDirectory(installPath);
            CopyDirectoryContents(sourceRoot, installPath);
        }
        finally
        {
            try { Directory.Delete(stagingDir, recursive: true); }
            catch { }
        }
    }

    private static void CopyDirectoryContents(string source, string dest)
    {
        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(source, dir);
            Directory.CreateDirectory(Path.Combine(dest, rel));
        }
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(source, file);
            var target = Path.Combine(dest, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    private static void TryChmodExecutable(string path)
    {
        if (!File.Exists(path)) return;
        if (OperatingSystem.IsWindows()) return;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "/bin/chmod",
                Arguments = $"+x \"{path}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var p = Process.Start(psi);
            p?.WaitForExit(3000);
        }
        catch { }
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        try
        {
            var script = $@"
$ws = New-Object -ComObject WScript.Shell
$sc = $ws.CreateShortcut('{shortcutPath.Replace("'", "''")}')
$sc.TargetPath = '{targetPath.Replace("'", "''")}'
$sc.WorkingDirectory = '{workingDirectory.Replace("'", "''")}'
$sc.Description = 'Leaf Client'
$sc.Save()
";
            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-NoProfile -NonInteractive -Command \"{script.Replace("\"", "\\\"")}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using var proc = Process.Start(psi);
            proc?.WaitForExit(5000);
        }
        catch
        {
        }
    }
}
