using System.ComponentModel;

namespace GalaxyViewer.ViewModels;

public abstract partial class ViewModelBase : INotifyPropertyChanged
{
    public new event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}