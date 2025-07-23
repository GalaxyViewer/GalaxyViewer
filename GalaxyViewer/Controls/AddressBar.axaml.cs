using Avalonia.Controls;
using Avalonia.Input;
using System.Linq;
using Avalonia.VisualTree;
using GalaxyViewer.ViewModels;
using System.Reactive;
using Avalonia.Markup.Xaml;

namespace GalaxyViewer.Controls
{
    public partial class AddressBar : UserControl
    {
        public AddressBar()
        {
            InitializeComponent();
            AddHandler(PointerPressedEvent, OnRootPointerPressed, handledEventsToo: true);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnRootPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is not AddressBarViewModel vm)
                return;
            if (!vm.IsEditing)
                return;

            var textBox = this.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(tb => tb.IsVisible);
            if (textBox == null)
                return;

            var pointerPos = e.GetPosition(textBox);
            var bounds = new Avalonia.Rect(0, 0, textBox.Bounds.Width, textBox.Bounds.Height);
            if (!bounds.Contains(pointerPos))
            {
                vm.CancelEditCommand.Execute(Unit.Default);
            }
        }
    }
}