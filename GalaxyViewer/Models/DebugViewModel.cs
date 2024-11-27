using ReactiveUI;

namespace GalaxyViewer.Models;

public class DebugViewModel(IScreen screen) : ReactiveObject, IRoutableViewModel
{
    public string UrlPathSegment => "debug";
    public IScreen HostScreen { get; } = screen;
}