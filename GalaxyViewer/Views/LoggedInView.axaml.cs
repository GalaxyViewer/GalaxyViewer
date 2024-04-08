using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace GalaxyViewer.Views
{
    public partial class LoggedInView : UserControl
    {
        public LoggedInView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}