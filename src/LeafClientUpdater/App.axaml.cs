using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LeafClientUpdater.Views;

namespace LeafClientUpdater
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // FIX: Point to the updater's own window
                desktop.MainWindow = new UpdaterWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
