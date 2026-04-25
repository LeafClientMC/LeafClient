using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeafClient.Services
{
    /// <summary>Immutable descriptor for a supported display currency.</summary>
    public sealed record CurrencyInfo(
        string Code,
        string Symbol,
        string Name,
        string FlagCode,        // ISO 3166-1 alpha-2, lowercase (e.g. "ae") — used for flagcdn.com
        decimal FallbackRate);

    /// <summary>
    /// Static service that converts EUR-denominated prices to the user's selected currency.
    /// Live rates are fetched from Frankfurter.app (ECB data, free, no key needed).
    /// Falls back to built-in rates when offline or when the API is unavailable.
    /// </summary>
    public static class CurrencyService
    {
        public static readonly IReadOnlyList<CurrencyInfo> SupportedCurrencies = new CurrencyInfo[]
        {
            new("EUR", "€",    "Euro",              "eu",  1.00m),
            new("USD", "$",    "US Dollar",         "us",  1.09m),
            new("GBP", "£",    "British Pound",     "gb",  0.86m),
            new("AED", "د.إ", "UAE Dirham",        "ae",  3.99m),
            new("SAR", "﷼",   "Saudi Riyal",       "sa",  4.09m),
            new("RUB", "₽",   "Russian Ruble",     "ru",  99.50m),
            new("INR", "₹",   "Indian Rupee",      "in",  90.80m),
            new("TRY", "₺",   "Turkish Lira",      "tr",  35.20m),
            new("JPY", "¥",   "Japanese Yen",      "jp",  163.00m),
            new("CAD", "C$",  "Canadian Dollar",   "ca",  1.49m),
            new("AUD", "A$",  "Australian Dollar", "au",  1.65m),
            new("CNY", "¥",   "Chinese Yuan",      "cn",  7.87m),
        };

        private static readonly Dictionary<string, decimal> _liveRates = new();
        private static string _selectedCode = "EUR";

        public static string SelectedCode
        {
            get => _selectedCode;
            set
            {
                if (GetCurrency(value) != null)
                    _selectedCode = value;
            }
        }

        public static CurrencyInfo? GetCurrency(string code)
        {
            foreach (var c in SupportedCurrencies)
                if (c.Code == code) return c;
            return null;
        }

        /// <summary>Converts a EUR base amount to the currently selected currency.</summary>
        public static decimal Convert(decimal eurAmount)
        {
            if (_selectedCode == "EUR") return eurAmount;
            return eurAmount * GetRate(_selectedCode);
        }

        /// <summary>
        /// Formats a EUR base price as a display string in the selected currency.
        /// Returns the string as-is if <paramref name="rawPrice"/> is "FREE".
        /// </summary>
        public static string FormatPrice(decimal eurBasePrice)
        {
            var info = GetCurrency(_selectedCode)!;
            var converted = Convert(eurBasePrice);

            // Zero-decimal currencies
            if (_selectedCode is "JPY")
                return $"{info.Symbol}{(int)Math.Round(converted, MidpointRounding.AwayFromZero)}";

            return $"{info.Symbol}{converted:F2}";
        }

        private static decimal GetRate(string code)
        {
            if (_liveRates.TryGetValue(code, out var live)) return live;
            return GetCurrency(code)?.FallbackRate ?? 1m;
        }

        /// <summary>
        /// Fetches live EUR-based exchange rates from Frankfurter.app.
        /// Silently falls back to hardcoded rates on any network or parse failure.
        /// RUB is not provided by the ECB feed; it will always use the fallback rate.
        /// </summary>
        public static async Task TryFetchLiveRatesAsync()
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(6) };
                var json = await http.GetStringAsync("https://api.frankfurter.app/latest?base=EUR");
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("rates", out var ratesEl))
                {
                    _liveRates.Clear();
                    foreach (var entry in ratesEl.EnumerateObject())
                    {
                        if (entry.Value.TryGetDecimal(out var val))
                            _liveRates[entry.Name] = val;
                    }
                }
            }
            catch
            {
                // Network unavailable or API changed — fall back to built-in rates silently.
            }
        }
    }
}
