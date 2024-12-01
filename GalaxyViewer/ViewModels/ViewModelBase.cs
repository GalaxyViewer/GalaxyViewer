using System.Runtime.CompilerServices;
using ReactiveUI;

namespace GalaxyViewer.ViewModels;

public partial class ViewModelBase : ReactiveObject
{
    protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
    {
        this.RaisePropertyChanged(propertyName);
    }
}