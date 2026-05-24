using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace LeafClient.Services
{
    public sealed record CurrencyInfo(
        string Code,
        string Symbol,
        string Name,
        string FlagCode,
        decimal FallbackRate);

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

        public static decimal Convert(decimal eurAmount)
        {
            if (_selectedCode == "EUR") return eurAmount;
            return eurAmount * GetRate(_selectedCode);
        }

        public static string FormatPrice(decimal eurBasePrice)
        {
            var info = GetCurrency(_selectedCode)!;
            var converted = Convert(eurBasePrice);

            if (_selectedCode is "JPY")
                return $"{info.Symbol}{(int)Math.Round(converted, MidpointRounding.AwayFromZero)}";

            return $"{info.Symbol}{converted:F2}";
        }

        private static decimal GetRate(string code)
        {
            if (_liveRates.TryGetValue(code, out var live)) return live;
            return GetCurrency(code)?.FallbackRate ?? 1m;
        }

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
            }
        }
    }
}
