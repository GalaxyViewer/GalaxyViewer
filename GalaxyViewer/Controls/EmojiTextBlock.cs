using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Serilog;

namespace GalaxyViewer.Controls;

public partial class EmojiTextBlock : TextBlock
{
    public static readonly StyledProperty<FontFamily> EmojiFontFamilyProperty =
        AvaloniaProperty.Register<EmojiTextBlock, FontFamily>(nameof(EmojiFontFamily));

    public static readonly StyledProperty<FontFamily> DefaultFontFamilyProperty =
        AvaloniaProperty.Register<EmojiTextBlock, FontFamily>(nameof(DefaultFontFamily));

    public FontFamily EmojiFontFamily
    {
        get => GetValue(EmojiFontFamilyProperty);
        set => SetValue(EmojiFontFamilyProperty, value);
    }

    public FontFamily DefaultFontFamily
    {
        get => GetValue(DefaultFontFamilyProperty);
        set => SetValue(DefaultFontFamilyProperty, value);
    }

    protected EmojiTextBlock()
    {
        PropertyChanged += (_, e) =>
        {
            if (e.Property == TextProperty ||
                e.Property == EmojiFontFamilyProperty ||
                e.Property == DefaultFontFamilyProperty)
            {
                ApplyFonts();
            }
        };
    }

    private void ApplyFonts()
    {
        if (string.IsNullOrEmpty(Text))
            return;

        var inlines = new List<Inline>();
        for (int i = 0; i < Text.Length; i++)
        {
            string charOrEmoji;
            bool isEmoji;

            if (char.IsSurrogatePair(Text, i))
            {
                charOrEmoji = Text.Substring(i, 2);
                isEmoji = true;
                i++;
            }
            else
            {
                charOrEmoji = Text[i].ToString();
                isEmoji = char.IsSurrogate(Text[i]) ||
                          (Text[i] >= '\u2600' && Text[i] <= '\u27BF') ||
                          (Text[i] >= '\u2B50' && Text[i] <= '\u2B55');
            }

            var fontFamily = isEmoji ? EmojiFontFamily : DefaultFontFamily;

            var run = new Run(charOrEmoji)
            {
                FontFamily = fontFamily
            };
            inlines.Add(run);
        }

        if (Inlines == null)
        {
            Inlines = new InlineCollection();
        }

        Inlines.Clear();
        Inlines.AddRange(inlines);

        Log.Information("Font application complete. Total inlines: {Count}", inlines.Count);
    }
}