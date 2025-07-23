using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GalaxyViewer.Views
{
    public partial class MfaPromptDialog : UserControl
    {
        public MfaPromptDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}