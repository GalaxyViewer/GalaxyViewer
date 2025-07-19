using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace GalaxyViewer.Assets.Localization;

public class LocalizationManager : INotifyPropertyChanged
{
    private readonly ResourceManager _resourceManager = new(typeof(Strings));
    private CultureInfo _currentCulture = CultureInfo.CurrentCulture;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string GetString(string name)
    {
        return _resourceManager.GetString(name, _currentCulture) ?? $"[{name}]";
    }

    public void SetCulture(string cultureCode)
    {
        var newCulture = new CultureInfo(cultureCode);
        if (_currentCulture.Name != newCulture.Name)
        {
            _currentCulture = newCulture;
            CultureInfo.CurrentCulture = newCulture;
            CultureInfo.CurrentUICulture = newCulture;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }
    }
}