using Android.App;
using Android.Content.PM;
using Android.OS;
using UpBeat.Services;

namespace UpBeat;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
	protected override void OnDestroy()
	{
		// IsFinishing is true when the app is really closed (e.g. swiped away from recent apps),
		// and false when Android only recreates the screen
		if (IsFinishing)
		{
			// Release the player, so that the music stops and its notification disappears
			IPlatformApplication.Current?.Services.GetService<AudioPlayerService>()?.Release();
		}

		base.OnDestroy();
	}
}
