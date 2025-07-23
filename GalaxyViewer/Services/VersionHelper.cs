using System;
using System.Reflection;

namespace GalaxyViewer.Services;

public static class VersionHelper
{
    private static string GetApplicationVersion()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version?.ToString() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    public static string GetInformationalVersion()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var versionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            return versionAttribute?.InformationalVersion ?? GetApplicationVersion();
        }
        catch
        {
            return GetApplicationVersion();
        }
    }
}