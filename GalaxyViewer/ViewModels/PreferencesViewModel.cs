using System;
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
            _preferencesManager = App.PreferencesManager ??
                                  throw new InvalidOperationException(
                                      "PreferencesManager is not initialized.");
            LoadPreferences();
            ThemeOptions = new ObservableCollection<string>(PreferencesOptions.ThemeOptions);
            LoginLocationOptions =
                new ObservableCollection<string>(PreferencesOptions.LoginLocationOptions);
            LanguageOptions = new ObservableCollection<string>(PreferencesOptions.LanguageOptions);
            FontOptions = new ObservableCollection<string>(PreferencesOptions.FontOptions);
            SaveCommand = new RelayCommand(async () => await SavePreferencesAsync());
        }

        private async void LoadPreferences()
        {
            _isLoadingPreferences = true;
            if (_preferencesManager != null)
                _preferences = await _preferencesManager.LoadPreferencesAsync();

            // Set selected options from preferences
            _selectedTheme = _preferences.Theme;
            _selectedLoginLocation = _preferences.LoginLocation;
            _selectedLanguage = _preferences.Language;
            _selectedFont = _preferences.Font;

            OnPropertyChanged(nameof(SelectedTheme));
            OnPropertyChanged(nameof(SelectedLoginLocation));
            OnPropertyChanged(nameof(SelectedLanguage));
            OnPropertyChanged(nameof(SelectedFont));
            _isLoadingPreferences = false;
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
                if (_selectedTheme == value || _isLoadingPreferences) return;
                _selectedTheme = value;
                OnPropertyChanged(nameof(SelectedTheme));
            }
        }

        public string SelectedLoginLocation
        {
            get => _selectedLoginLocation;
            set
            {
                if (_selectedLoginLocation == value || _isLoadingPreferences) return;
                _selectedLoginLocation = value;
                OnPropertyChanged(nameof(SelectedLoginLocation));
            }
        }

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage == value || _isLoadingPreferences) return;
                _selectedLanguage = value;
                OnPropertyChanged(nameof(SelectedLanguage));
            }
        }

        public string SelectedFont
        {
            get => _selectedFont;
            set
            {
                if (_selectedFont == value || _isLoadingPreferences) return;
                _selectedFont = value;
                OnPropertyChanged(nameof(SelectedFont));
            }
        }

        public ICommand SaveCommand { get; }
    }
}