using System;
using System.Reflection;
using System.Collections.ObjectModel; // ADD THIS
using CommunityToolkit.Mvvm.ComponentModel;
using LeafClient.Models;

namespace LeafClient.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private LauncherSettings _settings = new LauncherSettings();

        // ADD: List of countries
        public ObservableCollection<string> Countries { get; } = new ObservableCollection<string>();

        public MainWindowViewModel()
        {
            // Initialize the countries list
            InitializeCountries();
        }

        private void InitializeCountries()
        {
            // Comprehensive list of countries (sorted alphabetically)
            var countryList = new[]
            {
                "Afghanistan", "Albania", "Algeria", "Andorra", "Angola", "Antigua and Barbuda",
                "Argentina", "Armenia", "Australia", "Austria", "Azerbaijan", "Bahamas", "Bahrain",
                "Bangladesh", "Barbados", "Belarus", "Belgium", "Belize", "Benin", "Bhutan",
                "Bolivia", "Bosnia and Herzegovina", "Botswana", "Brazil", "Brunei", "Bulgaria",
                "Burkina Faso", "Burundi", "Cabo Verde", "Cambodia", "Cameroon", "Canada",
                "Central African Republic", "Chad", "Chile", "China", "Colombia", "Comoros",
                "Congo (Congo-Brazzaville)", "Costa Rica", "Croatia", "Cuba", "Cyprus",
                "Czech Republic", "Democratic Republic of the Congo", "Denmark", "Djibouti",
                "Dominica", "Dominican Republic", "Ecuador", "Egypt", "El Salvador",
                "Equatorial Guinea", "Eritrea", "Estonia", "Eswatini", "Ethiopia", "Fiji",
                "Finland", "France", "Gabon", "Gambia", "Georgia", "Germany", "Ghana", "Greece",
                "Grenada", "Guatemala", "Guinea", "Guinea-Bissau", "Guyana", "Haiti", "Honduras",
                "Hungary", "Iceland", "India", "Indonesia", "Iran", "Iraq", "Ireland", "Israel",
                "Italy", "Jamaica", "Japan", "Jordan", "Kazakhstan", "Kenya", "Kiribati",
                "Korea, North", "Korea, South", "Kosovo", "Kuwait", "Kyrgyzstan", "Laos",
                "Latvia", "Lebanon", "Lesotho", "Liberia", "Libya", "Liechtenstein", "Lithuania",
                "Luxembourg", "Madagascar", "Malawi", "Malaysia", "Maldives", "Mali", "Malta",
                "Marshall Islands", "Mauritania", "Mauritius", "Mexico", "Micronesia", "Moldova",
                "Monaco", "Mongolia", "Montenegro", "Morocco", "Mozambique", "Myanmar", "Namibia",
                "Nauru", "Nepal", "Netherlands", "New Zealand", "Nicaragua", "Niger", "Nigeria",
                "North Macedonia", "Norway", "Oman", "Pakistan", "Palau", "Palestine", "Panama",
                "Papua New Guinea", "Paraguay", "Peru", "Philippines", "Poland", "Portugal",
                "Qatar", "Romania", "Russia", "Rwanda", "Saint Kitts and Nevis", "Saint Lucia",
                "Saint Vincent and the Grenadines", "Samoa", "San Marino", "Sao Tome and Principe",
                "Saudi Arabia", "Senegal", "Serbia", "Seychelles", "Sierra Leone", "Singapore",
                "Slovakia", "Slovenia", "Solomon Islands", "Somalia", "South Africa",
                "South Sudan", "Spain", "Sri Lanka", "Sudan", "Suriname", "Sweden", "Switzerland",
                "Syria", "Taiwan", "Tajikistan", "Tanzania", "Thailand", "Timor-Leste", "Togo",
                "Tonga", "Trinidad and Tobago", "Tunisia", "Turkey", "Turkmenistan", "Tuvalu",
                "Uganda", "Ukraine", "United Arab Emirates", "United Kingdom", "United States",
                "Uruguay", "Uzbekistan", "Vanuatu", "Vatican City", "Venezuela", "Vietnam",
                "Yemen", "Zambia", "Zimbabwe"
            };

            // Clear and add countries
            Countries.Clear();
            foreach (var country in countryList)
            {
                Countries.Add(country);
            }
        }

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

        // Change from string property to use the new Countries list
        public string PrayerTimeCountry
        {
            get => _settings.PrayerTimeCountry;
            set => SetProperty(_settings.PrayerTimeCountry, value, _settings, (s, v) => s.PrayerTimeCountry = v);
        }

        public string PrayerTimeCity
        {
            get => _settings.PrayerTimeCity;
            set => SetProperty(_settings.PrayerTimeCity, value, _settings, (s, v) => s.PrayerTimeCity = v);
        }

        public string Theme
        {
            get => _settings.Theme;
            set => SetProperty(_settings.Theme, value, _settings, (s, v) => s.Theme = v);
        }

        public void UpdateSettings(LauncherSettings newSettings)
        {
            _settings = newSettings;
            OnPropertyChanged(nameof(PrayerTimeCountry));
            OnPropertyChanged(nameof(PrayerTimeCity));
            OnPropertyChanged(nameof(Theme));
        }
    }
}