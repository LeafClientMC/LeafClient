using System;
using System.Diagnostics;
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
    // ── Constants ────────────────────────────────────────────────────────────
    private const string DownloadUrl =
        "https://github.com/LeafClientMC/LeafClient/raw/refs/heads/main/latestexe/LeafClient.zip";

    private const double ProgressBarMaxWidth = 400.0;

    private static readonly string DefaultInstallPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LeafClient");

    // ── UI references ────────────────────────────────────────────────────────
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

    // ── State ────────────────────────────────────────────────────────────────
    private int _currentStep;
    private bool _isInstalling;
    private double _currentProgress;
    private double _targetProgress;
    private DispatcherTimer? _progressTimer;

    // ─────────────────────────────────────────────────────────────────────────

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

        // Title bar drag
        var titleBar = this.FindControl<Border>("TitleBar");
        if (titleBar != null)
        {
            titleBar.PointerPressed += (_, e) =>
            {
                if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                    BeginMoveDrag(e);
            };
        }

        // Background parallax mouse-follow
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

        // Smooth progress bar timer
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

    // ─────────────────────────────────────────────────────────────────────────
    // PROGRESS ANIMATION
    // ─────────────────────────────────────────────────────────────────────────

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

    // ─────────────────────────────────────────────────────────────────────────
    // STEP TRANSITIONS
    // ─────────────────────────────────────────────────────────────────────────

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
            case 0: // Welcome
                _nextBtn.IsVisible = true;
                _nextBtn.Content = "Next";
                _backBtn.IsVisible = false;
                _cancelBtn.IsVisible = false;
                break;
            case 1: // Config
                _nextBtn.IsVisible = true;
                _nextBtn.Content = "Install";
                _backBtn.IsVisible = true;
                _cancelBtn.IsVisible = false;
                break;
            case 2: // Installing
                _nextBtn.IsVisible = false;
                _backBtn.IsVisible = false;
                _cancelBtn.IsVisible = true;
                break;
            case 3: // Complete
                _nextBtn.IsVisible = false;
                _backBtn.IsVisible = false;
                _cancelBtn.IsVisible = false;
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BUTTON HANDLERS
    // ─────────────────────────────────────────────────────────────────────────

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

    // ─────────────────────────────────────────────────────────────────────────
    // INSTALL LOGIC
    // ─────────────────────────────────────────────────────────────────────────

    private async Task RunInstallAsync(string installPath)
    {
        UpdateInstallStatus("Installing Leaf Client", "Preparing...", 5);
        Directory.CreateDirectory(installPath);

        UpdateInstallStatus("Installing Leaf Client", "Downloading...", 10);
        var zipPath = Path.Combine(Path.GetTempPath(), "LeafClient_download.zip");

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

        await Task.Run(() => ZipFile.ExtractToDirectory(zipPath, installPath, overwriteFiles: true));
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

    // ─────────────────────────────────────────────────────────────────────────
    // WINDOW CHROME
    // ─────────────────────────────────────────────────────────────────────────

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

    // ─────────────────────────────────────────────────────────────────────────
    // BROWSE
    // ─────────────────────────────────────────────────────────────────────────

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

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────

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
        catch { /* best effort */ }
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
            // Shortcut creation is best-effort
        }
    }
}
