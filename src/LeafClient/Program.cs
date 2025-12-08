using Avalonia;
using Avalonia.Logging;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace LeafClient
{
    internal sealed class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AllocConsole();

        [STAThread]
        public static void Main(string[] args)
        {
            //AllocConsole();

            Console.WriteLine("[Program] Application starting...");
            Console.WriteLine("[Program] Base directory: " + AppContext.BaseDirectory);
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
                Console.WriteLine("[Program] Application exited normally");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Program] FATAL ERROR: {ex.Message}");
                Console.WriteLine($"[Program] Type: {ex.GetType().Name}");
                Console.WriteLine($"[Program] Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[Program] Inner exception: {ex.InnerException.Message}");
                    Console.WriteLine($"[Program] Inner stack trace: {ex.InnerException.StackTrace}");
                }
                //Console.WriteLine("\nPress any key to exit...");
                //Console.ReadKey();
                throw;
            }
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            var builder = AppBuilder
                .Configure<App>()
                .UsePlatformDetect()
                .WithInterFont();

            // Override Avalonia logger sink
            Logger.Sink = new SilentAvaloniaSink();

            return builder;
        }

        public class SilentAvaloniaSink : ILogSink
        {
            public bool IsEnabled(LogEventLevel level, string area) => false;
            public void Log(LogEventLevel level, string area, object? source, string messageTemplate) { /* no-op */ }
            public void Log(LogEventLevel level, string area, object? source, string messageTemplate, object[] propertyValues) { /* no-op */ }
        }
    }
}
