using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GalaxyViewer.ViewModels;

namespace GalaxyViewer.Views
{
    public partial class LoggedInView : UserControl
    {
        public LoggedInView(LoggedInViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}