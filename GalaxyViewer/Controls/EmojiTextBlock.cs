using System.Linq;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace GalaxyViewer.Controls
{
    public abstract partial class EmojiTextBlock : TextBlock
    {
        private static readonly Regex EmojiRegex = MyRegex();

        public static readonly StyledProperty<FontFamily> EmojiFontFamilyProperty =
            AvaloniaProperty.Register<EmojiTextBlock, FontFamily>(nameof(EmojiFontFamily));

        public FontFamily EmojiFontFamily
        {
            get => GetValue(EmojiFontFamilyProperty);
            set => SetValue(EmojiFontFamilyProperty, value);
        }

        public static readonly StyledProperty<FontFamily> DefaultFontFamilyProperty =
            AvaloniaProperty.Register<EmojiTextBlock, FontFamily>(nameof(DefaultFontFamily));

        public FontFamily DefaultFontFamily
        {
            get => GetValue(DefaultFontFamilyProperty);
            set => SetValue(DefaultFontFamilyProperty, value);
        }

        protected EmojiTextBlock()
        {
            this.PropertyChanged += (_, e) =>
            {
                if (e.Property == TextProperty)
                {
                    ApplyFonts();
                }
            };
        }

        private void ApplyFonts()
        {
            if (string.IsNullOrEmpty(Text))
                return;

            var inlines = Text.Select(ch => new Run(ch.ToString()) { FontFamily = EmojiRegex.IsMatch(ch.ToString()) ? EmojiFontFamily : DefaultFontFamily }).Cast<Inline>().ToList();

            Inlines?.Clear();
            Inlines?.AddRange(inlines);
        }

        [GeneratedRegex(@"[\u203C-\u3299\u1F000-\u1F644\u1F680-\u1F6FF\u1F700-\u1F77F\u1F780-\u1F7FF\u1F800-\u1F8FF\u1F900-\u1F9FF\u1FA00-\u1FA6F\u1FA70-\u1FAFF\u1FB00-\u1FBFF]", RegexOptions.Compiled)]
        private static partial Regex MyRegex();
    }
}