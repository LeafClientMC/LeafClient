using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using LeafClient.Services;

namespace LeafClient.Markup
{
    public sealed class TrExtension : MarkupExtension
    {
        public string? Key { get; set; }

        public TrExtension() { }
        public TrExtension(string key) { Key = key; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new Binding
            {
                Source = LocalizationService.Instance,
                Path = nameof(LocalizationService.ActiveLocale),
                Mode = BindingMode.OneWay,
                Converter = LocalizationKeyConverter.Instance,
                ConverterParameter = Key ?? string.Empty,
            };
        }
    }

    internal sealed class LocalizationKeyConverter : IValueConverter
    {
        public static readonly LocalizationKeyConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var key = parameter as string ?? string.Empty;
            return LocalizationService.Instance[key];
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
