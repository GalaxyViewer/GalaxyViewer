using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaxyViewer.Commands;
using GalaxyViewer.Models;
using GalaxyViewer.Services;
using Serilog;

namespace GalaxyViewer.ViewModels;

public class PreferencesViewModel : ViewModelBase
{
    private readonly PreferencesManager? _preferencesManager;
    private PreferencesModel _preferences;
    private bool _isLoadingPreferences;

    public ICommand? BackCommand { get; }

    public PreferencesViewModel(ICommand? backCommand = null)
    {
        BackCommand = backCommand;

        if (App.PreferencesManager != null) _preferencesManager = App.PreferencesManager;

        _preferences = new PreferencesModel();
        var preferencesOptions = _preferencesManager?.GetCurrentPreferencesOptions();

        ThemeOptions = new ObservableCollection<string>(preferencesOptions?["ThemeOptions"] ??
                                                        PreferencesOptions.ThemeOptions);
        LoginLocationOptions = new ObservableCollection<string>(
            preferencesOptions?["LoginLocationOptions"] ??
            PreferencesOptions.LoginLocationOptions);
        LanguageOptions = new ObservableCollection<string>(
            preferencesOptions?["LanguageOptions"] ??
            PreferencesOptions.LanguageOptions);
        FontOptions = new ObservableCollection<string>(preferencesOptions?["FontOptions"] ??
                                                       PreferencesOptions.FontOptions);
        AccentColorOptions = new ObservableCollection<string>(preferencesOptions?["AccentColorOptions"] ??
                                                              PreferencesOptions.AccentColorOptions);

        // Initialize with default values - these will be overridden by LoadPreferences()
        _selectedTheme = ThemeOptions.First();
        _selectedLoginLocation = LoginLocationOptions.First();
        _selectedLanguage = LanguageOptions.First();
        _selectedFont = FontOptions.First();
        _selectedGridNick = "";
        _selectedAccentColor = AccentColorOptions.First();

        SaveCommand = new RelayCommand(async () => await SavePreferencesAsync());

        _ = LoadPreferencesAsync();
    }

    private async Task LoadPreferencesAsync()
    {
        try
        {
            _isLoadingPreferences = true;

            await Task.Run(async () =>
            {
                if (_preferencesManager != null)
                {
                    var preferences = await _preferencesManager.LoadPreferencesAsync();

                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _preferences = preferences;
                        UpdateUIFromPreferences();
                    });
                }
            });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load preferences in PreferencesViewModel");
        }
        finally
        {
            _isLoadingPreferences = false;
        }
    }

    private void UpdateUIFromPreferences()
    {
        var loadedTheme = _preferences.Theme;

        if (loadedTheme == "Default")
        {
            loadedTheme = "System";
        }

        _selectedTheme = !string.IsNullOrEmpty(loadedTheme) &&
                        ThemeOptions.Contains(loadedTheme)
                        ? loadedTheme
                        : "System";

        _selectedLoginLocation = !string.IsNullOrEmpty(_preferences.LoginLocation) &&
                                 LoginLocationOptions.Contains(_preferences.LoginLocation)
                                 ? _preferences.LoginLocation
                                 : LoginLocationOptions.First();

        _selectedLanguage = !string.IsNullOrEmpty(_preferences.Language) &&
                           LanguageOptions.Contains(_preferences.Language)
                           ? _preferences.Language
                           : LanguageOptions.First();

        _selectedFont = !string.IsNullOrEmpty(_preferences.Font) &&
                       FontOptions.Contains(_preferences.Font)
                       ? _preferences.Font
                       : FontOptions.First();

        _selectedGridNick = _preferences.SelectedGridNick ?? "";

        _selectedAccentColor = !string.IsNullOrEmpty(_preferences.AccentColor) &&
                               AccentColorOptions.Contains(_preferences.AccentColor)
                               ? _preferences.AccentColor
                               : AccentColorOptions.First(); // "System Default" - not really working yet

        var gridOptions = _preferencesManager?.GetGridOptions();
        GridOptions = new ObservableCollection<string>(gridOptions ?? []);

        _isLoadingPreferences = false;

        OnPropertyChanged(nameof(SelectedTheme));
        OnPropertyChanged(nameof(SelectedLoginLocation));
        OnPropertyChanged(nameof(SelectedLanguage));
        OnPropertyChanged(nameof(SelectedFont));
        OnPropertyChanged(nameof(SelectedGridNick));
        OnPropertyChanged(nameof(SelectedAccentColor));
    }

    public ObservableCollection<string> ThemeOptions { get; private set; }
    public ObservableCollection<string> LoginLocationOptions { get; private set; }
    public ObservableCollection<string> LanguageOptions { get; private set; }
    public ObservableCollection<string> FontOptions { get; private set; }
    public ObservableCollection<string> GridOptions { get; private set; }
    public ObservableCollection<string> AccentColorOptions { get; private set; }

    private string _selectedLoginLocation;
    private string _selectedLanguage;
    private string _selectedFont;
    private string _selectedTheme;
    private string _selectedGridNick;
    private string _selectedAccentColor;

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

    public string SelectedAccentColor
    {
        get => _selectedAccentColor;
        set
        {
            if (_selectedAccentColor == value || _isLoadingPreferences) return;
            _selectedAccentColor = value;
            OnPropertyChanged(nameof(SelectedAccentColor));
        }
    }

    private async Task SavePreferencesAsync()
    {
        _preferences.Theme = SelectedTheme;
        _preferences.LoginLocation = SelectedLoginLocation;
        _preferences.Language = SelectedLanguage;
        _preferences.Font = SelectedFont;
        _preferences.SelectedGridNick = SelectedGridNick;
        _preferences.AccentColor = SelectedAccentColor;

        if (_preferencesManager != null)
        {
            await _preferencesManager.SavePreferencesAsync(_preferences);
            Log.Information("Preferences saved successfully");
        }
        else
        {
            Log.Warning("PreferencesManager is null - preferences not saved");
        }
    }

    public ICommand SaveCommand { get; }
}