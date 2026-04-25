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
    // In App.xaml.cs
    public partial class App : Application
    {
        private static bool _themeInitialized = false;

        // NEW: tracks when we are swapping to login from an in-app logout
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

        public void SafeShutdown()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                Console.WriteLine("[App] Performing safe shutdown");
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
                // Keep process alive when windows are swapped (logout -> login)
                desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                Dispatcher.UIThread.UnhandledException += (s, e) =>
                {
                    Console.WriteLine("UI THREAD EXCEPTION: " + e.Exception);

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
                        Console.WriteLine($"[SUPPRESSED] Prevented system alert for: {e.Exception.GetType().Name}");
                        e.Handled = true;
                    }
                    else
                    {
                        // Capture screenshot immediately before the overlay appears
                        byte[]? screenshot = ScreenshotCaptureService.CaptureScreenAsPng();

                        // Mark handled so the app stays alive
                        e.Handled = true;

                        Console.WriteLine($"[CRASH] Caught unhandled exception: {e.Exception.GetType().Name} — showing crash overlay");

                        var caughtEx = e.Exception;
                        // Delay 1 second, then show the in-window overlay
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
                                    // No MainWindow available — submit silently
                                    Task.Run(async () =>
                                    {
                                        try
                                        {
                                            var ss = new SettingsService();
                                            var settings = await ss.LoadSettingsAsync();
                                            await CrashReportService.SendAsync(caughtEx, screenshot, settings);
                                        }
                                        catch { /* silent */ }
                                    });
                                }
                            });
                        });
                    }
                };

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
                            var session = await sessionService.GetCurrentSessionAsync();

                            if (session is not null && session.CheckIsValid())
                            {
                                shouldShowMain = true;
                            }
                            else
                            {
                                await sessionService.LogoutAsync();
                            }
                        }

                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            // Stay on explicit shutdown; never flip to OnLastWindowClose
                            if (shouldShowMain)
                                ShowMainWindow();
                            else
                                ShowLoginWindow();
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Startup error: {ex.Message}");
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
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

            Console.WriteLine("[App] ShowLoginWindow called");

            // Ensure we never auto-shutdown when swapping windows
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var loginWindow = new LoginWindow();

            loginWindow.LoginCompleted += (success) =>
            {
                Console.WriteLine($"[App] LoginCompleted - success: {success}");
                if (success)
                {
                    // Close login window immediately — LoginSuccessful is already true so the
                    // Closed handler won't trigger a shutdown, even before MainWindow appears.
                    loginWindow.Close();

                    try
                    {
                        var mainWindow = new MainWindow();
                        desktop.MainWindow = mainWindow;
                        mainWindow.Show();
                        mainWindow.Activate();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[FATAL] Failed to create MainWindow: {ex}");
                    }
                }
            };

            loginWindow.Closed += (_, __) =>
            {
                // Delay and verify visible windows to avoid shutting down during the swap
                Dispatcher.UIThread.Post(async () =>
                {
                    await Task.Delay(200); // small stabilization delay

                    bool hasVisibleWindow = desktop.Windows.Any(w => w.IsVisible);
                    if (!loginWindow.LoginSuccessful && !hasVisibleWindow)
                    {
                        Console.WriteLine("[App] Login cancelled or failed. No visible windows. Shutting down.");
                        desktop.Shutdown();
                    }
                    else
                    {
                        Console.WriteLine("[App] LoginWindow closed; keeping app alive.");
                    }
                }, DispatcherPriority.Background);
            };

            desktop.MainWindow = loginWindow;
            loginWindow.Show();
            loginWindow.Activate();
        }
        public void SwitchToLoginWindow()
        {
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return;

            Console.WriteLine("[App] Switching to login window");

            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var loginWindow = new LoginWindow();

            loginWindow.LoginCompleted += (success) =>
            {
                Console.WriteLine($"[App] Login completed: {success}");
                if (success)
                {
                    loginWindow.Close();

                    try
                    {
                        var mainWindow = new MainWindow();
                        desktop.MainWindow = mainWindow;
                        mainWindow.Show();
                        mainWindow.Activate();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[FATAL] Failed to create MainWindow: {ex}");
                    }
                }
            };

            loginWindow.Closed += (_, __) =>
            {
                Dispatcher.UIThread.Post(async () =>
                {
                    await Task.Delay(200); // small stabilization delay

                    bool hasVisibleWindow = desktop.Windows.Any(w => w.IsVisible);
                    if (!loginWindow.LoginSuccessful && !hasVisibleWindow)
                    {
                        Console.WriteLine("[App] No successful login and no windows - shutting down");
                        desktop.Shutdown();
                    }
                    else
                    {
                        Console.WriteLine("[App] LoginWindow closed; keeping app alive.");
                    }
                }, DispatcherPriority.Background);
            };

            desktop.MainWindow = loginWindow;
            loginWindow.Show();
            loginWindow.Activate();
        }
        private void EnsureSingleWindow()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // If we have multiple windows, close extras
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

            // Keep explicit shutdown across the app lifetime
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Clear swap flag when main is active
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
                Console.WriteLine($"[FATAL] Failed to create MainWindow: {ex}");
            }
        }

    }
}
