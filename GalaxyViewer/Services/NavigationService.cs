using System;
using System.Collections.Generic;
using Avalonia.Controls;

namespace GalaxyViewer.Services;

public class NavigationService
{
    private readonly Dictionary<string, Type> _routes = new();
    private readonly ContentControl _contentControl;

    public NavigationService(ContentControl contentControl)
    {
        _contentControl = contentControl;
    }

    public void RegisterRoute(string uri, Type viewType)
    {
        _routes[uri] = viewType;
    }

    public void NavigateTo(string uri)
    {
        if (_routes.TryGetValue(uri, out var viewType))
        {
            var view = (Control)Activator.CreateInstance(viewType)!;
            _contentControl.Content = view;
        }
        else
        {
            throw new InvalidOperationException($"No view registered for URI: {uri}");
        }
    }
}