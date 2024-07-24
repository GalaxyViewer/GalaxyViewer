using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaxyViewer.Commands;
using GalaxyViewer.Models;
using GalaxyViewer.Services;

namespace GalaxyViewer.ViewModels
{
    public class PreferencesViewModel : ViewModelBase
    {
        private readonly PreferencesManager? _preferencesManager;
        private PreferencesModel _preferences;
        private bool _isLoadingPreferences;

        public PreferencesViewModel()
        {
            if (App.PreferencesManager != null) _preferencesManager = App.PreferencesManager;
            _preferences = App.PreferencesManager?.CurrentPreferences ?? new PreferencesModel();
            ThemeOptions = new ObservableCollection<string>(_preferences.ThemeOptions);
            LoginLocationOptions = new ObservableCollection<string>(_preferences.LoginLocationOptions);
            LanguageOptions = new ObservableCollection<string>(_preferences.LanguageOptions);
            FontOptions = new ObservableCollection<string>(_preferences.FontOptions);
            _selectedTheme = _preferences.Theme;
            _selectedLoginLocation = _preferences.LoginLocation;
            _selectedLanguage = _preferences.Language;
            _selectedFont = _preferences.Font;
            SaveCommand = new RelayCommand(async () => await SavePreferencesAsync());
        }

        private async void LoadPreferences()
        {
            _isLoadingPreferences = true;
            if (_preferencesManager != null)
                _preferences = await _preferencesManager.LoadPreferencesAsync();

            // Set selected options from preferences.xml
            if (_selectedTheme != _preferences.Theme)
                _selectedTheme = _preferences.Theme;
            if (_selectedLoginLocation != _preferences.LoginLocation)
                _selectedLoginLocation = _preferences.LoginLocation;
            if (_selectedLanguage != _preferences.Language)
                _selectedLanguage = _preferences.Language;
            if (_selectedFont != _preferences.Font)
                _selectedFont = _preferences.Font;

            OnPropertyChanged(nameof(SelectedTheme));
            OnPropertyChanged(nameof(SelectedLoginLocation));
            OnPropertyChanged(nameof(SelectedLanguage));
            OnPropertyChanged(nameof(SelectedFont));
            _isLoadingPreferences = false;
        }

        public ObservableCollection<string> ThemeOptions { get; private set; }
        public ObservableCollection<string> LoginLocationOptions { get; private set; }
        public ObservableCollection<string> LanguageOptions { get; private set; }
        public ObservableCollection<string> FontOptions { get; private set; }

        private string _selectedLoginLocation;
        private string _selectedLanguage;
        private string _selectedFont;
        private string _selectedTheme;

        public string SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (_selectedTheme != value && !_isLoadingPreferences)
                {
                    _selectedTheme = value;
                    OnPropertyChanged(nameof(SelectedTheme));
                }
            }
        }

        public string SelectedLoginLocation
        {
            get => _selectedLoginLocation;
            set
            {
                if (_selectedLoginLocation != value && !_isLoadingPreferences)
                {
                    _selectedLoginLocation = value;
                    OnPropertyChanged(nameof(SelectedLoginLocation));
                }
            }
        }

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage != value && !_isLoadingPreferences)
                {
                    _selectedLanguage = value;
                    OnPropertyChanged(nameof(SelectedLanguage));
                }
            }
        }

        public string SelectedFont
        {
            get => _selectedFont;
            set
            {
                if (_selectedFont != value && !_isLoadingPreferences)
                {
                    _selectedFont = value;
                    OnPropertyChanged(nameof(SelectedFont));
                }
            }
        }

        private async Task SavePreferencesAsync()
        {
            _preferences.Theme = SelectedTheme;
            _preferences.LoginLocation = SelectedLoginLocation;
            _preferences.Language = SelectedLanguage;
            _preferences.Font = SelectedFont;

            if (_preferencesManager != null)
                await _preferencesManager.SavePreferencesAsync(_preferences);
        }

        public ICommand SaveCommand { get; }
    }
}