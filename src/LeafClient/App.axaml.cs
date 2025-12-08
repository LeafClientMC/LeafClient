using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using LeafClient.Services;
using LeafClient.ViewModels;
using LeafClient.Views;
using System;
using System.Threading.Tasks;
using Avalonia.Diagnostics;
using Avalonia.Data.Core;

namespace LeafClient
{
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
            public override void Write(string message) { /* nothing */ }
            public override void WriteLine(string message) { /* nothing */ }
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

            // CRITICAL: Never shutdown automatically
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var loginWindow = new LoginWindow();

            loginWindow.LoginCompleted += (success) =>
            {
                Console.WriteLine($"[App] LoginCompleted - success: {success}");
                if (success)
                {
                    // Login successful - show main window
                    Dispatcher.UIThread.Post(() =>
                    {
                        var mainWindow = new MainWindow();
                        mainWindow.Show();
                        desktop.MainWindow = mainWindow;
                        loginWindow.Close();
                    });
                }
            };

            loginWindow.Closed += (_, __) =>
            {
                Console.WriteLine($"[App] LoginWindow Closed - LoginSuccessful: {loginWindow.LoginSuccessful}, WindowCount: {desktop.Windows.Count}");

                // If login failed, check if there are ANY windows left
                // We need to post this check to ensure window count is updated after close
                Dispatcher.UIThread.Post(() =>
                {
                    if (!loginWindow.LoginSuccessful && desktop.Windows.Count == 0)
                    {
                        Console.WriteLine("[App] No successful login and no windows - shutting down");
                        desktop.Shutdown();
                    }
                    else
                    {
                        Console.WriteLine("[App] Not shutting down - either login succeeded or other windows exist");
                    }
                });
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

            // Ensure we don't auto-shutdown
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var loginWindow = new LoginWindow();

            loginWindow.LoginCompleted += (success) =>
            {
                Console.WriteLine($"[App] Login completed: {success}");
                if (success)
                {
                    // Create new main window
                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                    desktop.MainWindow = mainWindow;
                    loginWindow.Close();
                }
            };

            loginWindow.Closed += (_, __) =>
            {
                Console.WriteLine($"[App] LoginWindow closed - successful: {loginWindow.LoginSuccessful}");

                // Only shutdown if login failed AND no other windows exist
                if (!loginWindow.LoginSuccessful && desktop.Windows.Count == 0)
                {
                    Console.WriteLine("[App] No successful login and no windows - shutting down");
                    desktop.Shutdown();
                }
            };

            desktop.MainWindow = loginWindow;
            loginWindow.Show();
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

            var mainWindow = new MainWindow();
            desktop.MainWindow = mainWindow;
            mainWindow.Show();
            mainWindow.Activate();
        }

    }
}
