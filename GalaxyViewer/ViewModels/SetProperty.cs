using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GalaxyViewer.ViewModels
{
    public abstract partial class ViewModelBase : INotifyPropertyChanged
    {
        public new event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null!)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}