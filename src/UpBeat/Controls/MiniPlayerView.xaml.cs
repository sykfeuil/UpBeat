using UpBeat.ViewModels;

namespace UpBeat.Controls;

/// <summary>
/// Mini player shown at the bottom of every page.
/// </summary>
public partial class MiniPlayerView : ContentView
{
	public MiniPlayerView()
	{
		InitializeComponent();

		// Every mini player shares the same app-wide player state
		BindingContext = IPlatformApplication.Current?.Services.GetRequiredService<PlayerViewModel>();
	}
}
