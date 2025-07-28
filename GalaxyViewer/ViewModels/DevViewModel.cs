using System;
using System.Reactive;
using ReactiveUI;
using Ursa.Controls;
using System.Windows.Input;

namespace GalaxyViewer.ViewModels;

public class DevViewModel : ReactiveObject
{
    public ICommand GoToDashboardCommand { get; }

    public DevViewModel(ICommand? goToDashboardCommand = null)
    {
        GoToDashboardCommand = goToDashboardCommand;
    }
}