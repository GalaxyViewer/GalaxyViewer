using System;
using System.Globalization;
using Avalonia.Data.Converters;
using GalaxyViewer.Assets.Localization;

namespace GalaxyViewer.Converters;

public class LocalizedStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is string key) return LocalizationManager.GetString(key) ?? "Key not found";
        return "Key not found";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Change to en-US if the language is not found
        return value;
    }
}