using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaxyViewer.Commands;
using GalaxyViewer.Models;
using GalaxyViewer.Services;

namespace GalaxyViewer.ViewModels;

public class PreferencesViewModel : ViewModelBase
{
    private readonly PreferencesManager? _preferencesManager;
    private PreferencesModel _preferences;
    private bool _isLoadingPreferences;

    public PreferencesViewModel()
    {
        if (App.PreferencesManager != null) _preferencesManager = App.PreferencesManager;
        _preferences = App.PreferencesManager?.CurrentPreferences ?? new PreferencesModel();
        var preferencesOptions = _preferencesManager?.GetCurrentPreferencesOptions();
        ThemeOptions = new ObservableCollection<string>(preferencesOptions?["ThemeOptions"] ??
                                                        []);
        LoginLocationOptions = new ObservableCollection<string>(
            preferencesOptions?["LoginLocationOptions"] ??
            []);
        LanguageOptions = new ObservableCollection<string>(
            preferencesOptions?["LanguageOptions"] ??
            []);
        FontOptions =
            new ObservableCollection<string>(preferencesOptions?["FontOptions"] ?? []);
        _selectedTheme = _preferences.Theme;
        _selectedLoginLocation = _preferences.LoginLocation;
        _selectedLanguage = _preferences.Language;
        _selectedFont = _preferences.Font;
        _selectedGridNick = _preferences.SelectedGridNick;
        SaveCommand = new RelayCommand(async () => await SavePreferencesAsync());
        LoadPreferences();
    }

    private async void LoadPreferences()
    {
        _isLoadingPreferences = true;
        if (_preferencesManager != null)
            _preferences = await _preferencesManager.LoadPreferencesAsync();

        SelectedTheme = _preferences.Theme;
        SelectedLoginLocation = _preferences.LoginLocation;
        SelectedLanguage = _preferences.Language;
        SelectedFont = _preferences.Font;
        SelectedGridNick = _preferences.SelectedGridNick;
        var gridOptions = _preferencesManager?.GetGridOptions();
        GridOptions = new ObservableCollection<string>(gridOptions ?? []);

        _isLoadingPreferences = false;
    }

    public ObservableCollection<string> ThemeOptions { get; private set; }
    public ObservableCollection<string> LoginLocationOptions { get; private set; }
    public ObservableCollection<string> LanguageOptions { get; private set; }
    public ObservableCollection<string> FontOptions { get; private set; }
    public ObservableCollection<string> GridOptions { get; private set; }

    private string _selectedLoginLocation;
    private string _selectedLanguage;
    private string _selectedFont;
    private string _selectedTheme;
    private string _selectedGridNick;

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

    public string SelectedGridNick
    {
        get => _selectedGridNick;
        set
        {
            if (_selectedGridNick == value || _isLoadingPreferences) return;
            _selectedGridNick = value;
            OnPropertyChanged(nameof(SelectedGridNick));
        }
    }

    private async Task SavePreferencesAsync()
    {
        _preferences.Theme = SelectedTheme;
        _preferences.LoginLocation = SelectedLoginLocation;
        _preferences.Language = SelectedLanguage;
        _preferences.Font = SelectedFont;
        _preferences.SelectedGridNick = SelectedGridNick;

        if (_preferencesManager != null)
            await _preferencesManager.SavePreferencesAsync(_preferences);
    }

    public ICommand SaveCommand { get; }
}