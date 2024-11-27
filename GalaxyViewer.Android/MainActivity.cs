using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Avalonia;
using Avalonia.Android;
using Avalonia.ReactiveUI;
using GalaxyViewer.Services;

namespace GalaxyViewer.Android
{
    [Activity(
        Label = "GalaxyViewer.Android",
        Theme = "@style/MyTheme.NoActionBar",
        Icon = "@drawable/icon",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
    public class MainActivity : AvaloniaMainActivity<App>
    {
        const int RequestStorageId = 0;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) != Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.WriteExternalStorage }, RequestStorageId);
            }
            else
            {
                LoadResources();
                // TODO: Fix this so that it doesn't instantly crash and asks for permission before loading resources
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (requestCode == RequestStorageId)
            {
                if (grantResults.Length > 0 && grantResults[0] == Permission.Granted)
                {
                    // Permission granted, proceed with loading resources
                    LoadResources();
                }
                else
                {
                    // Permission denied, show a message to the user
                    var message = "Permission to access storage denied. The application will not be able to load resources.";
                    var toast = Toast.MakeText(this, message, ToastLength.Long);
                    toast.Show();
                }
            }
        }

        private void LoadResources()
        {
            // Load your resources here
            InitializeLiteDbService();
        }

        private void InitializeLiteDbService()
        {
            // Initialize LiteDbService here
            var liteDbService = new LiteDbService();
            // Use liteDbService as needed
        }

        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            return base.CustomizeAppBuilder(builder)
                .WithInterFont()
                .UseReactiveUI();
        }
    }
}