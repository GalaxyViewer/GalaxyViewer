using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ReactiveUI;
using OpenMetaverse;
using GalaxyViewer.Services;
using GalaxyViewer.Models;
using Serilog;
using Avalonia.Media;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace GalaxyViewer.ViewModels
{
    public partial class AddressBarViewModel : ViewModelBase, IDisposable
    {
        private readonly LiteDbService _liteDbService;
        private readonly GridClient _client;
        private SessionModel _session;
        private readonly ICommand? _openPreferencesCommand;

        private DateTime _lastLocationUpdate = DateTime.MinValue;
        private const int LocationUpdateThrottleMs = 100;

        private readonly List<string> _locationHistory = [];
        private int _currentHistoryIndex = -1;

        private Vector3 _lastKnownPosition = Vector3.Zero;
        private string _lastKnownRegion = string.Empty;

        // Regex for parsing SLURL coordinates like "Region Name/128/128/23"
        [GeneratedRegex(@"^(.+?)\/(\d+)\/(\d+)\/(\d+)$")]
        private static partial Regex SlurlRegex();

        public AddressBarViewModel(LiteDbService liteDbService,
            GridClient client,
            ICommand? openPreferencesCommand = null)
        {
            _liteDbService = liteDbService;
            _client = client;
            _session = _liteDbService.GetSession();
            _openPreferencesCommand = openPreferencesCommand;

            HomeCommand = ReactiveCommand.Create(GoHome);
            BackCommand = ReactiveCommand.Create(GoBack, this.WhenAnyValue(x => x.CanGoBack));
            ForwardCommand =
                ReactiveCommand.Create(GoForward, this.WhenAnyValue(x => x.CanGoForward));
            SettingsCommand = ReactiveCommand.Create(OpenSettings);

            StartEditCommand = ReactiveCommand.Create(StartEdit);
            CommitEditCommand = ReactiveCommand.Create(CommitEdit);
            CancelEditCommand = ReactiveCommand.Create(CancelEdit);

            _liteDbService.PropertyChanged += OnLiteDbServicePropertyChanged;
            _client.Self.TeleportProgress += OnTeleportProgress;
            _client.Objects.AvatarUpdate += OnAvatarUpdate;
            _client.Network.SimConnected += OnSimConnected;
            _client.Network.SimDisconnected += OnSimDisconnected;

            if (_client.Network.Connected)
            {
                UpdateLocationDisplay();
            }
        }

        public ReactiveCommand<Unit, Unit> HomeCommand { get; }
        public ReactiveCommand<Unit, Unit> BackCommand { get; }
        public ReactiveCommand<Unit, Unit> ForwardCommand { get; }
        public ReactiveCommand<Unit, Unit> SettingsCommand { get; }
        public ReactiveCommand<Unit, Unit> StartEditCommand { get; }
        public ReactiveCommand<Unit, Unit> CommitEditCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelEditCommand { get; }

        private string _currentLocationDisplay = string.Empty;

        public string CurrentLocationDisplay
        {
            get => _currentLocationDisplay;
            set
            {
                if (_currentLocationDisplay == value) return;
                _currentLocationDisplay = value;
                OnPropertyChanged(nameof(CurrentLocationDisplay));
            }
        }

        private string _currentCoordinatesDisplay = string.Empty;

        public string CurrentCoordinatesDisplay
        {
            get => _currentCoordinatesDisplay;
            set
            {
                if (_currentCoordinatesDisplay == value) return;
                _currentCoordinatesDisplay = value;
                OnPropertyChanged(nameof(CurrentCoordinatesDisplay));
            }
        }

        private bool _isEditing;

        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (_isEditing == value) return;
                _isEditing = value;
                OnPropertyChanged(nameof(IsEditing));
                OnPropertyChanged(nameof(IsDisplaying));
            }
        }

        public bool IsDisplaying => !IsEditing;

        private string _editableLocation = string.Empty;

        public string EditableLocation
        {
            get => _editableLocation;
            set
            {
                if (_editableLocation == value) return;
                _editableLocation = value;
                OnPropertyChanged(nameof(EditableLocation));
            }
        }

        private string _maturityRating = "G";

        public string MaturityRating
        {
            get => _maturityRating;
            set
            {
                if (_maturityRating == value) return;
                _maturityRating = value;
                OnPropertyChanged(nameof(MaturityRating));
                OnPropertyChanged(nameof(MaturityRatingTooltip)); // Don't forget this!
            }
        }

        public string MaturityRatingTooltip =>
            MaturityRating switch
            {
                "G" => Application.Current?.FindResource("AddressBar_Maturity_G") as string ??
                       "General: Suitable for all ages - family-friendly content",
                "M" => Application.Current?.FindResource("AddressBar_Maturity_M") as string ??
                       "Moderate: Suitable for ages 17 and above, may include moderate use of violence and 'sexy' content",
                "A" => Application.Current?.FindResource("AddressBar_Maturity_A") as string ??
                       "Adult: Suitable for ages 18 and above, may include heavy use of violence, drugs, or sexual content",
                _ => Application.Current?.FindResource("AddressBar_Maturity_Default") as string ??
                     "Content rating information"
            };

        public string MaturityRatingColorHex =>
            MaturityRating switch
            {
                "G" => "#4CAF50", // Green
                "M" => "#FF9800", // Orange
                "A" => "#F44336", // Red
                _ => "#4CAF50"
            };

        public SolidColorBrush MaturityRatingBrush =>
            new(Color.Parse(MaturityRatingColorHex));

        public bool CanGoBack => _currentHistoryIndex > 0;
        public bool CanGoForward => _currentHistoryIndex < _locationHistory.Count - 1;

        private void GoHome()
        {
            try
            {
                if (!_client.Network.Connected) return;
                _client.Self.GoHome();
                Log.Information("Teleporting home");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to teleport home");
            }
        }

        private void GoBack()
        {
            if (!CanGoBack) return;

            _currentHistoryIndex--;
            var location = _locationHistory[_currentHistoryIndex];
            TeleportToLocation(location);

            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));
        }

        private void GoForward()
        {
            if (!CanGoForward) return;

            _currentHistoryIndex++;
            var location = _locationHistory[_currentHistoryIndex];
            TeleportToLocation(location);

            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));
        }

        private string GetCurrentLocationForEditing()
        {
            if (_client.Network?.Connected != true || _client.Network.CurrentSim == null)
                return _session.CurrentLocation;
            // Return in the format expected by the dialog: "Region Name/X/Y/Z"
            var regionName = _client.Network.CurrentSim.Name;
            var x = (int)_client.Self.SimPosition.X;
            var y = (int)_client.Self.SimPosition.Y;
            var z = (int)_client.Self.SimPosition.Z;
            return $"{regionName}/{x}/{y}/{z}";
        }

        public bool ShowMaturityRating => !string.IsNullOrEmpty(MaturityRating);

        public string MaturityRatingColor => MaturityRatingColorHex;

        private void StartEdit()
        {
            Log.Information("Starting address edit mode");
            var currentLocation = GetCurrentLocationForEditing();
            EditableLocation = currentLocation;
            IsEditing = true;
        }

        private void CommitEdit()
        {
            Log.Information($"CommitEdit calling SearchCommand with: '{EditableLocation}'");
            if (!string.IsNullOrWhiteSpace(EditableLocation))
            {
                Search();
            }
            else
            {
                Log.Warning("EditableLocation is empty or null - no location to navigate to");
            }

            IsEditing = false;
        }

        private void CancelEdit()
        {
            Log.Information("Cancelling address edit");
            EditableLocation = GetCurrentLocationForEditing();
            IsEditing = false;
        }

        private void Search()
        {
            var location = EditableLocation.Trim();
            Log.Information("Search/Go requested with location: '{Location}'", location);

            if (string.IsNullOrEmpty(location))
            {
                Log.Warning("EditableLocation is empty or null - no location to navigate to");
                return;
            }

            TeleportToLocation(location);
            IsEditing = false;
        }

        private void TeleportToLocation(string location)
        {
            try
            {
                if (!_client.Network.Connected)
                {
                    Log.Warning("Not connected to grid - cannot teleport");
                    return;
                }

                if (ParseLocationString(location, out var regionName, out var x, out var y,
                        out var z))
                {
                    Log.Information("Teleporting to {RegionName} ({F},{F1},{F2})", regionName, x, y,
                        z);

                    IsTeleporting = true;
                    TeleportStatusMessage = $"Teleporting to {regionName}...";

                    Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        try
                        {
                            _client.Self.Teleport(regionName, new Vector3(x, y, z));
                            AddToHistory(location);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Failed to initiate teleport to {Location}", location);
                            IsTeleporting = false;
                            TeleportStatusMessage = "Teleport failed";

                            await Task.Delay(3000);
                            if (TeleportStatusMessage == "Teleport failed")
                            {
                                TeleportStatusMessage = "";
                            }
                        }
                    }, DispatcherPriority.Background);
                }
                else
                {
                    Log.Error("Failed to parse location: {Location}", location);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to teleport to location: {location}");
                IsTeleporting = false;
                TeleportStatusMessage = "";
            }
        }

        private bool ParseLocationString(string location, out string regionName, out float x,
            out float y, out float z)
        {
            regionName = "";
            x = y = z = 0;

            if (string.IsNullOrWhiteSpace(location))
                return false;

            // Handle secondlife:// URLs
            if (location.StartsWith("secondlife://"))
            {
                return ParseSecondLifeUrl(location, out regionName, out x, out y, out z);
            }

            // Handle maps.secondlife.com URLs
            if (location.Contains("maps.secondlife.com"))
            {
                return ParseMapsUrl(location, out regionName, out x, out y, out z);
            }

            // Handle "Region Name/X/Y/Z" format
            var match = SlurlRegex().Match(location);
            if (match.Success)
            {
                regionName = Uri.UnescapeDataString(match.Groups[1].Value);
                if (float.TryParse(match.Groups[2].Value, out x) &&
                    float.TryParse(match.Groups[3].Value, out y) &&
                    float.TryParse(match.Groups[4].Value, out z))
                {
                    return true;
                }
            }

            // Handle just region name
            regionName = location;
            x = y = 128; // Default to center
            z = 0;
            return true;
        }

        private bool ParseSecondLifeUrl(string url, out string regionName, out float x, out float y,
            out float z)
        {
            regionName = "";
            x = y = z = 0;

            try
            {
                var path = url.Substring("secondlife://".Length);
                var parts = path.Split('/');

                if (parts.Length >= 1)
                {
                    regionName = Uri.UnescapeDataString(parts[0]);

                    if (parts.Length >= 3)
                    {
                        float.TryParse(parts[1], out x);
                        float.TryParse(parts[2], out y);
                        if (parts.Length >= 4)
                            float.TryParse(parts[3], out z);
                    }
                    else
                    {
                        x = y = 128; // Default center
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to parse secondlife URL: {Url}", url);
            }

            return false;
        }

        private static bool ParseMapsUrl(string url, out string regionName, out float x,
            out float y, out float z)
        {
            regionName = "";
            x = y = z = 0;

            try
            {
                var secondlifeIndex = url.IndexOf("secondlife/", StringComparison.Ordinal);
                if (secondlifeIndex >= 0)
                {
                    var path = url.Substring(secondlifeIndex + "secondlife/".Length);
                    var parts = path.Split('/');

                    if (parts.Length >= 1)
                    {
                        regionName = Uri.UnescapeDataString(parts[0]);

                        if (parts.Length >= 3)
                        {
                            float.TryParse(parts[1], out x);
                            float.TryParse(parts[2], out y);
                            if (parts.Length >= 4)
                                float.TryParse(parts[3], out z);
                        }
                        else
                        {
                            x = y = 128; // Default center
                        }

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to parse maps URL: {Url}", url);
            }

            return false;
        }

        private void AddToHistory(string location)
        {
            // TODO: Implement proper history management
            if (_currentHistoryIndex < _locationHistory.Count - 1)
            {
                _locationHistory.RemoveRange(_currentHistoryIndex + 1,
                    _locationHistory.Count - _currentHistoryIndex - 1);
            }

            _locationHistory.Add(location);
            _currentHistoryIndex = _locationHistory.Count - 1;

            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));
        }

        private void OpenSettings()
        {
            Log.Information("Opening preferences window");
            if (_openPreferencesCommand?.CanExecute(null) == true)
            {
                _openPreferencesCommand.Execute(null);
            }
            else
            {
                Log.Warning("No preferences navigation command available");
            }
        }

        private void UpdateLocationDisplay()
        {
            if (_client.Network?.Connected != true || _client.Network.CurrentSim == null)
            {
                CurrentLocationDisplay = _session.CurrentLocation;
                CurrentCoordinatesDisplay = "";
                MaturityRating = "G";
                return;
            }

            var regionName = _client.Network.CurrentSim.Name;
            var x = (int)_client.Self.SimPosition.X;
            var y = (int)_client.Self.SimPosition.Y;
            var z = (int)_client.Self.SimPosition.Z;

            CurrentLocationDisplay = $"{regionName} ({x},{y},{z})";
            CurrentCoordinatesDisplay = $"{x}/{y}/{z}";

            // Update maturity rating based on region access level
            MaturityRating = _client.Network.CurrentSim.Access switch
            {
                SimAccess.Mature => "M",
                SimAccess.Adult => "A",
                _ => "G"
            };

            OnPropertyChanged(nameof(MaturityRatingColorHex));
            OnPropertyChanged(nameof(MaturityRatingColor));
            OnPropertyChanged(nameof(MaturityRatingBrush));
            OnPropertyChanged(nameof(ShowMaturityRating));
        }

        private void OnLiteDbServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(LiteDbService.Session):
                    _session = _liteDbService.GetSession();
                    UpdateLocationDisplay();
                    break;
            }
        }

        private void OnAvatarUpdate(object? sender, AvatarUpdateEventArgs e)
        {
            if (e.Avatar.ID != _client.Self.AgentID)
                return;

            // Check if we need to throttle updates to prevent performance issues
            var now = DateTime.UtcNow;
            if (now - _lastLocationUpdate < TimeSpan.FromMilliseconds(LocationUpdateThrottleMs))
                return;

            // Check if position or region has actually changed to avoid unnecessary updates
            var currentPosition = _client.Self.SimPosition;
            var currentRegion = _client.Network.CurrentSim?.Name ?? "";

            if (!HasLocationChanged(currentPosition, currentRegion)) return;
            _lastLocationUpdate = now;
            _lastKnownPosition = currentPosition;
            _lastKnownRegion = currentRegion;

            Dispatcher.UIThread.InvokeAsync(UpdateLocationDisplay, DispatcherPriority.Background);
        }

        private void OnSimConnected(object? sender, EventArgs e)
        {
            Log.Information("Connected to simulator: {Simulator}",
                _client.Network.CurrentSim?.Name);
            UpdateLocationDisplay();
        }

        private void OnSimDisconnected(object? sender, EventArgs e)
        {
            Log.Information("Disconnected from simulator");
            CurrentLocationDisplay = "Disconnected";
            CurrentCoordinatesDisplay = "";
        }

        private void OnTeleportProgress(object? sender, TeleportEventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                switch (e.Status)
                {
                    case TeleportStatus.Start:
                        IsTeleporting = true;
                        TeleportStatusMessage = "Initializing teleport...";
                        break;

                    case TeleportStatus.Progress:
                        TeleportStatusMessage = e.Message ?? "Teleporting...";
                        break;

                    case TeleportStatus.Failed:
                        IsTeleporting = false;
                        TeleportStatusMessage = $"Teleport failed: {e.Message}";
                        Log.Warning("Teleport failed: {Message}", e.Message);

                        Task.Delay(5000).ContinueWith(_ =>
                        {
                            if (TeleportStatusMessage.StartsWith("Teleport failed"))
                            {
                                TeleportStatusMessage = "";
                            }
                        }, TaskScheduler.FromCurrentSynchronizationContext());
                        break;

                    case TeleportStatus.Finished:
                        IsTeleporting = false;
                        TeleportStatusMessage = "Teleport complete";
                        Log.Information("Teleport completed successfully");

                        UpdateLocationDisplay();

                        Task.Delay(2000).ContinueWith(_ =>
                        {
                            if (TeleportStatusMessage == "Teleport complete")
                            {
                                TeleportStatusMessage = "";
                            }
                        }, TaskScheduler.FromCurrentSynchronizationContext());
                        break;

                    case TeleportStatus.Cancelled:
                        IsTeleporting = false;
                        TeleportStatusMessage = "Teleport cancelled";

                        Task.Delay(3000).ContinueWith(_ =>
                        {
                            if (TeleportStatusMessage == "Teleport cancelled")
                            {
                                TeleportStatusMessage = "";
                            }
                        }, TaskScheduler.FromCurrentSynchronizationContext());
                        break;
                }

                OnPropertyChanged(nameof(ShowTeleportStatus));
            }, DispatcherPriority.Normal);
        }

        private bool _isTeleporting;

        public bool IsTeleporting
        {
            get => _isTeleporting;
            set
            {
                if (_isTeleporting == value) return;
                _isTeleporting = value;
                OnPropertyChanged(nameof(IsTeleporting));
                OnPropertyChanged(nameof(ShowTeleportStatus));
            }
        }

        private string _teleportStatusMessage = "";

        public string TeleportStatusMessage
        {
            get => _teleportStatusMessage;
            set
            {
                if (_teleportStatusMessage == value) return;
                _teleportStatusMessage = value;
                OnPropertyChanged(nameof(TeleportStatusMessage));
                OnPropertyChanged(nameof(ShowTeleportStatus));
            }
        }

        private bool HasLocationChanged(Vector3 currentPosition, string currentRegion)
        {
            if (currentRegion != _lastKnownRegion)
                return true;

            const float
                minMovementThreshold =
                    0.5f; // Minimum distance to consider as movement is 0.5 meters
            var distance = Vector3.Distance(currentPosition, _lastKnownPosition);
            return distance >= minMovementThreshold;
        }

        public bool ShowTeleportStatus =>
            IsTeleporting && !string.IsNullOrEmpty(TeleportStatusMessage);

        public void Dispose()
        {
            _liteDbService.PropertyChanged -= OnLiteDbServicePropertyChanged;
            _client.Self.TeleportProgress -= OnTeleportProgress;
            _client.Objects.AvatarUpdate -= OnAvatarUpdate;
            _client.Network.SimConnected -= OnSimConnected;
            _client.Network.SimDisconnected -= OnSimDisconnected;
        }
    }
}