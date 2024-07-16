using Avalonia.Controls;

namespace GalaxyViewer.Views
{
    public class BaseWindow : Window
    {
        protected BaseWindow()
        {
            Icon = new WindowIcon("Assets/galaxy.ico");
            CanResize = true;
        }
    }
}