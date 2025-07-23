using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace GalaxyViewer.Controls;

public class EmojiAwareTextBlock : TextBlock
{
    public static readonly StyledProperty<FontFamily> EmojiFontFamilyProperty =
        AvaloniaProperty.Register<EmojiAwareTextBlock, FontFamily>(
            nameof(EmojiFontFamily));

    public FontFamily EmojiFontFamily
    {
        get => GetValue(EmojiFontFamilyProperty);
        set => SetValue(EmojiFontFamilyProperty, value);
    }

    public EmojiAwareTextBlock()
    {
        PropertyChanged += (_, e) =>
        {
            if (e.Property == TextProperty || e.Property == EmojiFontFamilyProperty)
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
        for (var i = 0; i < Text.Length; i++)
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
                // This really could use some improvement, but it's as good as I can get for now
                isEmoji = char.IsSurrogate(Text[i]) ||
                          (Text[i] >= '\u2600' && Text[i] <= '\u27BF') ||
                          (Text[i] >= '\u2B50' && Text[i] <= '\u2B55');
            }

            var run = new Run(charOrEmoji)
            {
                FontFamily = isEmoji ? EmojiFontFamily : FontFamily
            };
            inlines.Add(run);
        }

        if (Inlines == null)
        {
            Inlines = new InlineCollection();
        }

        Inlines.Clear();
        Inlines.AddRange(inlines);
    }
}