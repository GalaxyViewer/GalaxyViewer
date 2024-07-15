using System.Globalization;
using System.Resources;

namespace GalaxyViewer.Assets.Localization
{
    public static class LocalizationManager
    {
        private static readonly ResourceManager ResourceManager = new ResourceManager(typeof(GalaxyViewer.Assets.Localization.Strings));

        public static string? GetString(string name)
        {
            return ResourceManager.GetString(name, CultureInfo.CurrentCulture);
        }

        public static void SetCulture(string cultureCode)
        {
            CultureInfo.CurrentCulture = new CultureInfo(cultureCode);
            CultureInfo.CurrentUICulture = new CultureInfo(cultureCode);
        }
    }
}