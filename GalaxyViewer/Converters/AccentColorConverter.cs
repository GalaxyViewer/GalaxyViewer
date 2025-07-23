using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;
using GalaxyViewer.Models;

namespace GalaxyViewer.Converters;

public class AccentColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string accentColorPreference || accentColorPreference == "System Default")
            return GetSystemAccentBrush();

        var isDarkTheme = IsCurrentThemeDark();

        var colorValue = PreferencesOptions.GetAccentColorForTheme(accentColorPreference, isDarkTheme);

        try
        {
            return new SolidColorBrush(Color.Parse(colorValue));
        }
        catch
        {
            return GetSystemAccentBrush();
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static bool IsCurrentThemeDark()
    {
        try
        {
            var app = Avalonia.Application.Current;
            if (app?.ActualThemeVariant != null)
            {
                return app.ActualThemeVariant == ThemeVariant.Dark;
            }
        }
        catch
        {
            // Continue to fallback
        }

        // Fallback: assume light theme
        return false;
    }

    private static IBrush GetSystemAccentBrush()
    {
        try
        {
            if (Avalonia.Application.Current?.TryGetResource("SystemAccentColorBrush",
                Avalonia.Application.Current.ActualThemeVariant, out var resource) == true)
            {
                if (resource is IBrush brush)
                    return brush;
            }

            if (Avalonia.Application.Current?.TryGetResource("SystemAccentColor",
                Avalonia.Application.Current.ActualThemeVariant, out var colorResource) == true)
            {
                if (colorResource is Color color)
                    return new SolidColorBrush(color);
            }
        }
        catch
        {
            // Continue to fallback
        }

        // Final fallback to a reasonable blue
        return new SolidColorBrush(Color.Parse("#0078D4"));
    }
}