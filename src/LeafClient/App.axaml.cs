using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using LeafClient.Services;
using LeafClient.ViewModels;
using LeafClient.Views;
using LeafClient.Models;
using System;
using System.Threading.Tasks;
using Avalonia.Diagnostics;
using Avalonia.Data.Core;
using System.Linq;

namespace LeafClient
{
    public partial class App : Application
    {
        private static bool _themeInitialized = false;

        public bool IsSwapToLogin { get; set; } = false;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public class NoOpTraceListener : System.Diagnostics.TraceListener
        {
            public override void Write(string? message) { }
            public override void WriteLine(string? message) { }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;

        public static void ForceForeground(Window window)
        {
            if (window == null) return;
            try
            {
                window.Show();
                if (window.WindowState == WindowState.Minimized)
                    window.WindowState = WindowState.Normal;
                window.Activate();
                bool prevTopmost = window.Topmost;
                window.Topmost = true;
                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        window.Topmost = prevTopmost;
                        window.Activate();
                        if (OperatingSystem.IsWindows())
                        {
                            try
                            {
                                var handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                                if (handle != IntPtr.Zero)
                                {
                                    ShowWindow(handle, SW_RESTORE);
                                    BringWindowToTop(handle);
                                    SetForegroundWindow(handle);
                                }
                            }
                            catch (Exception ex) { LeafLog.Info("App", $"ForceForeground Win32 failed: {ex.Message}"); }
                        }
                    }
                    catch { }
                }, DispatcherPriority.Render);
            }
            catch (Exception ex)
            {
                LeafLog.Error("App", $"ForceForeground failed: {ex.Message}");
            }
        }

        public void SafeShutdown()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                LeafLog.Info("App", "Performing safe shutdown");
                desktop.Shutdown();
            }
        }

        public override void OnFrameworkInitializationCompleted()
        {
            System.Diagnostics.Trace.Listeners.Clear();
            System.Diagnostics.Trace.Listeners.Add(new NoOpTraceListener());

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
#if DEBUG
            this.AttachDevTools();
#endif
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                Dispatcher.UIThread.UnhandledException += (s, e) =>
                {
                    LeafClient.Services.LeafLog.Error("App", $"UI THREAD EXCEPTION: {e.Exception}");

                    bool isSuppressible =
                        e.Exception is System.Net.Http.HttpRequestException ||
                        e.Exception is System.IO.IOException ||
                        e.Exception is System.Threading.Tasks.TaskCanceledException ||
                        e.Exception is System.Net.Sockets.SocketException ||
                        e.Exception is System.Net.WebException ||
                        e.Exception is System.Net.Http.HttpIOException ||
                        e.Exception is System.Net.NetworkInformation.NetworkInformationException ||
                        e.Exception.Source?.Contains("System.Net") == true ||
                        e.Exception.Source?.Contains("HttpClient") == true ||
                        e.Exception.Message.Contains("network", StringComparison.OrdinalIgnoreCase) ||
                        e.Exception.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                        e.Exception.Message.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
                        e.Exception.Message.Contains("DNS", StringComparison.OrdinalIgnoreCase) ||
                        e.Exception.Message.Contains("No such host", StringComparison.OrdinalIgnoreCase) ||
                        e.Exception.InnerException?.Message.Contains("network", StringComparison.OrdinalIgnoreCase) == true;

                    if (isSuppressible)
                    {
                        LeafLog.Info("SUPPRESSED", $"Prevented system alert for: {e.Exception.GetType().Name}");
                        e.Handled = true;
                    }
                    else
                    {
                        byte[]? screenshot = ScreenshotCaptureService.CaptureScreenAsPng();

                        e.Handled = true;

                        LeafLog.Info("CRASH", $"Caught unhandled exception: {e.Exception.GetType().Name} - showing crash overlay");

                        var caughtEx = e.Exception;
                        Task.Run(async () =>
                        {
                            await Task.Delay(1000);
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime dl
                                    && dl.MainWindow is MainWindow mw)
                                {
                                    mw.ShowCrashReportOverlay(caughtEx, screenshot);
                                }
                                else
                                {
                                    Task.Run(async () =>
                                    {
                                        try
                                        {
                                            var ss = new SettingsService();
                                            var settings = await ss.LoadSettingsAsync();
                                            await CrashReportService.SendAsync(caughtEx, screenshot, settings);
                                        }
                                        catch { }
                                    });
                                }
                            });
                        });
                    }
                };

                var startupWindowShown = new System.Threading.ManualResetEventSlim(false);

                Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(12));
                    if (!startupWindowShown.IsSet)
                    {
                        LeafLog.Error("App", "Startup watchdog fired - settings/session load exceeded 12s. Forcing LoginWindow.");
                        try
                        {
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                if (!startupWindowShown.IsSet)
                                {
                                    startupWindowShown.Set();
                                    ShowLoginWindow();
                                }
                            });
                        }
                        catch (Exception wdEx)
                        {
                            LeafLog.Error("App", $"Watchdog failed: {wdEx.Message}");
                        }
                    }
                });

                Task.Run(async () =>
                {
                    try
                    {
                        var settingsService = new SettingsService();
                        var settings = await settingsService.LoadSettingsAsync();

                        if (!_themeInitialized)
                        {
                            string theme = string.IsNullOrWhiteSpace(settings.Theme) ? "Dark" : settings.Theme;
                            await Dispatcher.UIThread.InvokeAsync(() => ThemeService.SetTheme(theme));
                            _themeInitialized = true;
                        }

                        bool shouldShowMain = false;

                        if (settings.IsLoggedIn)
                        {
                            var sessionService = new SessionService();
                            var sessionTask = sessionService.GetCurrentSessionAsync();
                            var winner = await Task.WhenAny(sessionTask, Task.Delay(TimeSpan.FromSeconds(6)));
                            CmlLib.Core.Auth.MSession? session = null;
                            if (winner == sessionTask)
                            {
                                try { session = await sessionTask; } catch { session = null; }
                            }
                            else
                            {
                                LeafLog.Info("App", "Session validation exceeded 6s - falling back to login.");
                            }

                            if (session is not null && session.CheckIsValid())
                            {
                                shouldShowMain = true;
                            }
                            else
                            {
                                try { await sessionService.LogoutAsync(); } catch { }
                            }
                        }

                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (startupWindowShown.IsSet) return;
                            startupWindowShown.Set();
                            if (shouldShowMain)
                                ShowMainWindow();
                            else
                                ShowLoginWindow();
                        });
                    }
                    catch (Exception ex)
                    {
                        LeafClient.Services.LeafLog.Error("App", $"Startup error: {ex.Message}");
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            if (startupWindowShown.IsSet) return;
                            startupWindowShown.Set();
                            ShowLoginWindow();
                        });
                    }
                });
            }

            base.OnFrameworkInitializationCompleted();
        }

        public void ShowLoginWindow()
        {
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return;

            LeafLog.Info("App", "ShowLoginWindow called");

            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var loginWindow = new LoginWindow();

            loginWindow.LoginCompleted += (success) =>
            {
                LeafLog.Info("App", $"LoginCompleted - success: {success}");
                if (success)
                {
                    loginWindow.Close();

                    try
                    {
                        var mainWindow = new MainWindow();
                        desktop.MainWindow = mainWindow;
                        ForceForeground(mainWindow);
                    }
                    catch (Exception ex)
                    {
                        LeafLog.Error("FATAL", $"Failed to create MainWindow: {ex}");
                    }
                }
            };

            loginWindow.Closed += (_, __) =>
            {
                Dispatcher.UIThread.Post(async () =>
                {
                    await Task.Delay(200);

                    bool hasVisibleWindow = desktop.Windows.Any(w => w.IsVisible);
                    if (!loginWindow.LoginSuccessful && !hasVisibleWindow)
                    {
                        LeafLog.Info("App", "Login cancelled or failed. No visible windows. Shutting down.");
                        desktop.Shutdown();
                    }
                    else
                    {
                        LeafLog.Info("App", "LoginWindow closed; keeping app alive.");
                    }
                }, DispatcherPriority.Background);
            };

            desktop.MainWindow = loginWindow;
            ForceForeground(loginWindow);
        }
        public void SwitchToLoginWindow()
        {
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return;

            LeafLog.Info("App", "Switching to login window");

            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var loginWindow = new LoginWindow();

            loginWindow.LoginCompleted += (success) =>
            {
                LeafLog.Info("App", $"Login completed: {success}");
                if (success)
                {
                    loginWindow.Close();

                    try
                    {
                        var mainWindow = new MainWindow();
                        desktop.MainWindow = mainWindow;
                        ForceForeground(mainWindow);
                    }
                    catch (Exception ex)
                    {
                        LeafLog.Error("FATAL", $"Failed to create MainWindow: {ex}");
                    }
                }
            };

            loginWindow.Closed += (_, __) =>
            {
                Dispatcher.UIThread.Post(async () =>
                {
                    await Task.Delay(200);

                    bool hasVisibleWindow = desktop.Windows.Any(w => w.IsVisible);
                    if (!loginWindow.LoginSuccessful && !hasVisibleWindow)
                    {
                        LeafLog.Info("App", "No successful login and no windows - shutting down");
                        desktop.Shutdown();
                    }
                    else
                    {
                        LeafLog.Info("App", "LoginWindow closed; keeping app alive.");
                    }
                }, DispatcherPriority.Background);
            };

            desktop.MainWindow = loginWindow;
            ForceForeground(loginWindow);
        }
        private void EnsureSingleWindow()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                while (desktop.Windows.Count > 1)
                {
                    var window = desktop.Windows[0];
                    if (window != desktop.MainWindow)
                    {
                        window.Close();
                    }
                }
            }
        }

        public void ShowMainWindow()
        {
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return;

            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            IsSwapToLogin = false;

            try
            {
                var mainWindow = new MainWindow();
                desktop.MainWindow = mainWindow;
                mainWindow.Show();
                mainWindow.Activate();
            }
            catch (Exception ex)
            {
                LeafLog.Error("FATAL", $"Failed to create MainWindow: {ex}");
            }
        }

    }
}
