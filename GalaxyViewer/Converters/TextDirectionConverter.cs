using System;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace GalaxyViewer.Converters;

public class TextDirectionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string text || string.IsNullOrEmpty(text))
            return FlowDirection.LeftToRight;

        // Check if the first character in the text is RTL
        var firstChar = text.FirstOrDefault(char.IsLetter);
        if (firstChar == default)
            return FlowDirection.LeftToRight;

        var unicodeCategory = char.GetUnicodeCategory(firstChar);

        // Check if it's an RTL character
        if (IsRtlUnicodeCategory(unicodeCategory))
            return FlowDirection.RightToLeft;

        // Additional check for RTL scripts by Unicode ranges
        if (IsRtlCharacter(firstChar))
            return FlowDirection.RightToLeft;

        return FlowDirection.LeftToRight;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private static bool IsRtlUnicodeCategory(UnicodeCategory category)
    {
        return category == UnicodeCategory.OtherLetter &&
               char.GetUnicodeCategory('\u0627') == category; // Arabic letter Alef
    }

    private static bool IsRtlCharacter(char character)
    {
        var code = (int)character;

        // Hebrew: U+0590 to U+05FF
        if (code >= 0x0590 && code <= 0x05FF) return true;

        // Arabic: U+0600 to U+06FF
        if (code >= 0x0600 && code <= 0x06FF) return true;

        // Arabic Supplement: U+0750 to U+077F
        if (code >= 0x0750 && code <= 0x077F) return true;

        // Arabic Extended-A: U+08A0 to U+08FF
        if (code >= 0x08A0 && code <= 0x08FF) return true;

        // Persian/Farsi and Urdu use Arabic script
        // Thaana (Maldivian): U+0780 to U+07BF
        if (code >= 0x0780 && code <= 0x07BF) return true;

        // Syriac: U+0700 to U+074F
        if (code >= 0x0700 && code <= 0x074F) return true;

        return false;
    }
}