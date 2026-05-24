using Avalonia;
using Avalonia.Logging;
using System;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using LeafClient.Services;

namespace LeafClient
{
    internal sealed class Program
    {
        public const string SingleInstancePipeName = "LeafClient_Wake_v1";
        private const string SingleInstanceMutexName = "LeafClient_SingleInstance_v1";

        private static Mutex? _singleInstanceMutex;
        public static bool IsPrimaryInstance { get; private set; }

        [STAThread]
        public static void Main(string[] args)
        {

            InstallGlobalCrashHandlers();
            LogStartupEnvironment();

            LeafClient.Services.LeafLog.Info("Program", "Application starting...");
            Console.WriteLine("[Program] Base directory: " + AppContext.BaseDirectory);

            bool isPostUpdateRelaunch = string.Equals(
                Environment.GetEnvironmentVariable("LEAFCLIENT_POST_UPDATE_RELAUNCH"),
                "1", StringComparison.Ordinal);
            if (isPostUpdateRelaunch)
            {
                LeafClient.Services.LeafLog.Info("Program", "Post-update relaunch detected - waiting briefly for mutex/handle cleanup.");
            }

            try
            {
                bool createdNew = false;
                int mutexAttempts = isPostUpdateRelaunch ? 10 : 1;
                for (int i = 0; i < mutexAttempts; i++)
                {
                    try
                    {
                        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out createdNew);
                        if (createdNew) break;
                    }
                    catch { }

                    if (!createdNew && i < mutexAttempts - 1)
                    {
                        try { _singleInstanceMutex?.Dispose(); _singleInstanceMutex = null; } catch { }
                        System.Threading.Thread.Sleep(500);
                    }
                }
                IsPrimaryInstance = createdNew;
                if (!createdNew)
                {
                    LeafClient.Services.LeafLog.Info("Program", "Another instance is running - signalling it via named pipe and exiting.");
                    try { SignalRunningInstanceToWake(); }
                    catch (Exception ex) { LeafClient.Services.LeafLog.Error("Program", $"Failed to signal running instance: {ex.Message}"); }
                    return;
                }
            }
            catch (Exception ex)
            {
                LeafClient.Services.LeafLog.Error("Program", $"Single-instance guard setup failed (non-fatal): {ex.Message}");
                IsPrimaryInstance = true;
            }

            try
            {
                if (Services.UpdateService.ApplyPendingUpdate())
                    LeafClient.Services.LeafLog.Info("Program", "Staged update applied successfully");
            }
            catch (Exception ex)
            {
                LeafClient.Services.LeafLog.Error("Program", $"Update apply failed (non-fatal): {ex.Message}");
            }

            try
            {
                var asm = Assembly.Load("XboxAuthNet.Game");
                Console.WriteLine("[Program] Assembly.Load(\"XboxAuthNet.Game\") succeeded: " + asm.FullName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Program] Assembly.Load(\"XboxAuthNet.Game\") FAILED: " + ex);
            }

            try
            {
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args);
                LeafClient.Services.LeafLog.Info("Program", "Application exited normally");
            }
            catch (Exception ex)
            {
                LeafClient.Services.LeafLog.Info("Program", $"FATAL ERROR: {ex.Message}");
                LeafClient.Services.LeafLog.Info("Program", $"Type: {ex.GetType().Name}");
                LeafClient.Services.LeafLog.Info("Program", $"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    LeafClient.Services.LeafLog.Info("Program", $"Inner exception: {ex.InnerException.Message}");
                    LeafClient.Services.LeafLog.Info("Program", $"Inner stack trace: {ex.InnerException.StackTrace}");
                }
                throw;
            }
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            var builder = AppBuilder
                .Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .With(new SkiaOptions { MaxGpuResourceSizeBytes = 192L * 1024 * 1024 })
                .With(new Avalonia.Win32PlatformOptions
                {
                    RenderingMode = new[]
                    {
                        Avalonia.Win32RenderingMode.AngleEgl,
                        Avalonia.Win32RenderingMode.Software,
                    },
                })
                .With(new Avalonia.X11PlatformOptions
                {
                    RenderingMode = new[]
                    {
                        Avalonia.X11RenderingMode.Glx,
                        Avalonia.X11RenderingMode.Software,
                    },
                });

            Logger.Sink = new SilentAvaloniaSink();

            return builder;
        }

        private static string GetEarlyCrashLogPath()
        {
            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var dir = System.IO.Path.Combine(appData, "LeafClient", "Logs");
                System.IO.Directory.CreateDirectory(dir);
                return System.IO.Path.Combine(dir, $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            }
            catch { return System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"leafclient_crash_{DateTime.Now:yyyyMMdd_HHmmss}.txt"); }
        }

        private static void WriteCrashLog(string source, Exception ex)
        {
            try
            {
                var path = GetEarlyCrashLogPath();
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"=== LeafClient unhandled exception ({source}) ===");
                sb.AppendLine($"Timestamp     : {DateTimeOffset.Now:O}");
                sb.AppendLine($"OS            : {Environment.OSVersion} / {RuntimeInformation.OSDescription}");
                sb.AppendLine($"Arch          : {RuntimeInformation.OSArchitecture} (proc: {RuntimeInformation.ProcessArchitecture})");
                sb.AppendLine($"Framework     : {RuntimeInformation.FrameworkDescription}");
                sb.AppendLine($"WorkingSet    : {Environment.WorkingSet / (1024 * 1024)} MB");
                sb.AppendLine($"ProcessorCount: {Environment.ProcessorCount}");
                sb.AppendLine($"BaseDir       : {AppContext.BaseDirectory}");
                sb.AppendLine($"CmdLine       : {Environment.CommandLine}");
                sb.AppendLine();
                AppendException(sb, ex, depth: 0);
                System.IO.File.WriteAllText(path, sb.ToString());
                LeafClient.Services.LeafLog.Error("CrashLog", $"Wrote {path}");
            }
            catch (Exception logEx)
            {
                LeafClient.Services.LeafLog.Error("CrashLog", $"Failed to write crash log: {logEx.Message}");
            }
        }

        private static void AppendException(System.Text.StringBuilder sb, Exception? ex, int depth)
        {
            if (ex == null) return;
            string indent = new string(' ', depth * 2);
            sb.AppendLine($"{indent}Type        : {ex.GetType().FullName}");
            sb.AppendLine($"{indent}Message     : {ex.Message}");
            sb.AppendLine($"{indent}Source      : {ex.Source}");
            sb.AppendLine($"{indent}TargetSite  : {ex.TargetSite}");
            sb.AppendLine($"{indent}StackTrace  :");
            sb.AppendLine(ex.StackTrace ?? $"{indent}  <none>");
            if (ex is AggregateException agg)
            {
                int i = 0;
                foreach (var inner in agg.InnerExceptions)
                {
                    sb.AppendLine($"{indent}--- AggregateException inner[{i++}] ---");
                    AppendException(sb, inner, depth + 1);
                }
            }
            else if (ex.InnerException != null)
            {
                sb.AppendLine($"{indent}--- InnerException ---");
                AppendException(sb, ex.InnerException, depth + 1);
            }
        }

        private static void InstallGlobalCrashHandlers()
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    var ex = e.ExceptionObject as Exception ?? new Exception("Non-Exception thrown: " + e.ExceptionObject);
                    LeafClient.Services.LeafLog.Error("FATAL", $"AppDomain unhandled: {ex.GetType().Name}: {ex.Message}");
                    Console.Error.WriteLine(ex.StackTrace);
                    WriteCrashLog("AppDomain.UnhandledException (terminating=" + e.IsTerminating + ")", ex);
                };
                System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
                {
                    LeafClient.Services.LeafLog.Error("WARN", $"UnobservedTaskException: {e.Exception.GetType().Name}: {e.Exception.Message}");
                    Console.Error.WriteLine(e.Exception.StackTrace);
                    WriteCrashLog("TaskScheduler.UnobservedTaskException", e.Exception);
                    e.SetObserved();
                };
                LeafClient.Services.LeafLog.Info("Program", "Global crash handlers installed.");
            }
            catch (Exception ex)
            {
                LeafClient.Services.LeafLog.Error("Program", $"Failed to install crash handlers: {ex.Message}");
            }
        }

        private static void LogStartupEnvironment()
        {
            try
            {
                Console.WriteLine("======================== LeafClient startup ========================");
                Console.WriteLine($"Time          : {DateTimeOffset.Now:O}");
                Console.WriteLine($"OS            : {Environment.OSVersion} ({RuntimeInformation.OSDescription})");
                Console.WriteLine($"Architecture  : {RuntimeInformation.OSArchitecture} (proc: {RuntimeInformation.ProcessArchitecture})");
                Console.WriteLine($"Framework     : {RuntimeInformation.FrameworkDescription}");
                Console.WriteLine($"ProcessorCount: {Environment.ProcessorCount}");
                Console.WriteLine($"WorkingDir    : {Environment.CurrentDirectory}");
                Console.WriteLine($"BaseDir       : {AppContext.BaseDirectory}");
                Console.WriteLine($"CmdLine       : {Environment.CommandLine}");
                try { Console.WriteLine($"Username      : {Environment.UserName} / Machine: {Environment.MachineName}"); } catch { }
                try { Console.WriteLine($"AppDataRoaming: {Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}"); } catch { }
                Console.WriteLine("====================================================================");
            }
            catch (Exception ex) { LeafClient.Services.LeafLog.Error("Program", $"LogStartupEnvironment failed: {ex.Message}"); }
        }

        private const uint ASFW_ANY = unchecked((uint)-1);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllowSetForegroundWindow(uint dwProcessId);

        private static void SignalRunningInstanceToWake()
        {
            if (OperatingSystem.IsWindows())
            {
                try { AllowSetForegroundWindow(ASFW_ANY); } catch { }
            }
            using var client = new NamedPipeClientStream(".", SingleInstancePipeName, PipeDirection.Out);
            client.Connect(2000);
            using var w = new StreamWriter(client) { AutoFlush = true };
            w.WriteLine("WAKE");
        }

        public class SilentAvaloniaSink : ILogSink
        {
            public bool IsEnabled(LogEventLevel level, string area) => false;
            public void Log(LogEventLevel level, string area, object? source, string messageTemplate) { }
            public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues) { }
        }
    }
}
