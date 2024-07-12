using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GalaxyViewer.ViewModels;

namespace GalaxyViewer.Views
{
    public partial class PreferencesView : UserControl
    {
        public PreferencesView()
        {
            InitializeComponent();
            LoadViewModelAsync();
        }

        private async void LoadViewModelAsync()
        {
            var viewModel = await CreateAsync();
            DataContext = viewModel;
        }

        private static async Task<PreferencesViewModel> CreateAsync()
        {
            var viewModel = new PreferencesViewModel();
            await viewModel.LoadPreferencesAsync();
            return viewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}