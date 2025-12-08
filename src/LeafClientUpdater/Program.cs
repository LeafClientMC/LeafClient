using Avalonia;
using System;

namespace LeafClientUpdater
{
    class Program
    {
        // Initialization code. Don't use any Avalonia/UI APIs here.
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
        // REMOVED: .UseReactiveUI() call
    }
}
