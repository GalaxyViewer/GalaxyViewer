using System;

namespace GalaxyViewer.Models
{
    public class Grid
    {
        public string GridNick { get; set; }
        public string GridName { get; set; }
        public string Platform { get; set; }
        public string LoginUri { get; set; }
        public string LoginPage { get; set; }
        public string HelperUri { get; set; }
        public string Website { get; set; }
        public string Support { get; set; }
        public string Register { get; set; }
        public string Password { get; set; }
        public string Version { get; set; }

        public Grid(string gridNick, string gridName, string platform, string loginUri, string loginPage, string helperUri, string website, string support, string register, string password, string version)
        {
            GridNick = !string.IsNullOrEmpty(gridNick) ? gridNick : throw new ArgumentNullException(nameof(gridNick));
            GridName = !string.IsNullOrEmpty(gridName) ? gridName : throw new ArgumentNullException(nameof(gridName));
            Platform = !string.IsNullOrEmpty(platform) ? platform : throw new ArgumentNullException(nameof(platform));
            LoginUri = !string.IsNullOrEmpty(loginUri) ? loginUri : throw new ArgumentNullException(nameof(loginUri));
            LoginPage = !string.IsNullOrEmpty(loginPage) ? loginPage : throw new ArgumentNullException(nameof(loginPage));
            HelperUri = !string.IsNullOrEmpty(helperUri) ? helperUri : throw new ArgumentNullException(nameof(helperUri));
            Website = !string.IsNullOrEmpty(website) ? website : throw new ArgumentNullException(nameof(website));
            Support = !string.IsNullOrEmpty(support) ? support : throw new ArgumentNullException(nameof(support));
            Register = !string.IsNullOrEmpty(register) ? register : throw new ArgumentNullException(nameof(register));
            Password = !string.IsNullOrEmpty(password) ? password : throw new ArgumentNullException(nameof(password));
            Version = !string.IsNullOrEmpty(version) ? version : throw new ArgumentNullException(nameof(version));
        }
    }
}