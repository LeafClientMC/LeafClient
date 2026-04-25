using System;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using LeafClient.Models;

namespace LeafClient.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private LauncherSettings _settings = new LauncherSettings();

        public string LastUpdatedText => $"Last updated: November 21st 2025";

        public string AppVersionText
        {
            get
            {
                Version? version = Assembly.GetExecutingAssembly().GetName().Version;
                if (version != null)
                {
                    string versionString = version.ToString();
                    if (versionString.EndsWith(".0.0"))
                    {
                        versionString = versionString.Substring(0, versionString.Length - 4);
                    }
                    else if (versionString.EndsWith(".0"))
                    {
                        versionString = versionString.Substring(0, versionString.Length - 2);
                    }
                    return $"v{versionString}";
                }
                return "vUnknown";
            }
        }

        public string Theme
        {
            get => _settings.Theme;
            set => SetProperty(_settings.Theme, value, _settings, (s, v) => s.Theme = v);
        }

        public void UpdateSettings(LauncherSettings newSettings)
        {
            _settings = newSettings;
            OnPropertyChanged(nameof(Theme));
        }
    }
}