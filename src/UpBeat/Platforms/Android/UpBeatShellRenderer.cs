using Android.Content.Res;
using Android.Graphics.Drawables;
using Android.Views;
using Google.Android.Material.BottomNavigation;
using Google.Android.Material.Navigation;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.Platform;

namespace UpBeat;

/// <summary>
/// Custom Android renderer for the Shell, used to tweak the native bottom tab bar.
/// </summary>
public class UpBeatShellRenderer : ShellRenderer
{
	protected override IShellBottomNavViewAppearanceTracker CreateBottomNavViewAppearanceTracker(ShellItem shellItem)
		=> new UpBeatBottomNavViewAppearanceTracker(this, shellItem);
}

/// <summary>
/// Hides tab labels so that icons are vertically centered in the tab bar,
/// and tints the touch ripple with the app's neon red.
/// </summary>
public class UpBeatBottomNavViewAppearanceTracker : ShellBottomNavViewAppearanceTracker
{
	public UpBeatBottomNavViewAppearanceTracker(IShellContext shellContext, ShellItem shellItem)
		: base(shellContext, shellItem)
	{
	}

	public override void SetAppearance(BottomNavigationView bottomView, IShellAppearanceElement appearance)
	{
		base.SetAppearance(bottomView, appearance);
		bottomView.LabelVisibilityMode = LabelVisibilityMode.LabelVisibilityUnlabeled;

		// Tint the touch ripple with our neon red instead of the default white
		if (Application.Current?.Resources.TryGetValue("NeonRed", out var value) == true && value is Color neonRed)
		{
			var rippleColor = ColorStateList.ValueOf(neonRed.WithAlpha(0.3f).ToPlatform());

			// MAUI gives each tab its own white ripple background, so we replace it on every tab
			if (bottomView.GetChildAt(0) is ViewGroup menuView)
			{
				for (var i = 0; i < menuView.ChildCount; i++)
				{
					var mask = new ColorDrawable(Android.Graphics.Color.White);
					menuView.GetChildAt(i)!.Background = new RippleDrawable(rippleColor, null, mask);
				}
			}
		}
	}
}
