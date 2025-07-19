using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace GalaxyViewer.Converters;

public class BoolToDoubleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue || parameter is not string doubleParams)
            return 1.0;
        var values = doubleParams.Split(' ', ',');
        if (values.Length < 2) return 1.0;
        if (double.TryParse(values[0].Trim(), out var trueValue) &&
            double.TryParse(values[1].Trim(), out var falseValue))
        {
            return boolValue ? trueValue : falseValue;
        }

        return 1.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}