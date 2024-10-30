using System;

namespace GalaxyViewer.Models;

public class Grid(
    string gridNick,
    string gridName,
    string platform,
    string loginUri,
    string loginPage,
    string helperUri,
    string website,
    string support,
    string register,
    string password,
    string version)
{
    public string GridNick { get; set; } = !string.IsNullOrEmpty(gridNick) ? gridNick : throw new ArgumentNullException(nameof(gridNick));
    public string GridName { get; set; } = !string.IsNullOrEmpty(gridName) ? gridName : throw new ArgumentNullException(nameof(gridName));
    public string Platform { get; set; } = !string.IsNullOrEmpty(platform) ? platform : throw new ArgumentNullException(nameof(platform));
    public string LoginUri { get; set; } = !string.IsNullOrEmpty(loginUri) ? loginUri : throw new ArgumentNullException(nameof(loginUri));
    public string LoginPage { get; set; } = !string.IsNullOrEmpty(loginPage) ? loginPage : throw new ArgumentNullException(nameof(loginPage));
    public string HelperUri { get; set; } = !string.IsNullOrEmpty(helperUri) ? helperUri : throw new ArgumentNullException(nameof(helperUri));
    public string Website { get; set; } = !string.IsNullOrEmpty(website) ? website : throw new ArgumentNullException(nameof(website));
    public string Support { get; set; } = !string.IsNullOrEmpty(support) ? support : throw new ArgumentNullException(nameof(support));
    public string Register { get; set; } = !string.IsNullOrEmpty(register) ? register : throw new ArgumentNullException(nameof(register));
    public string Password { get; set; } = !string.IsNullOrEmpty(password) ? password : throw new ArgumentNullException(nameof(password));
    public string Version { get; set; } = !string.IsNullOrEmpty(version) ? version : throw new ArgumentNullException(nameof(version));
}