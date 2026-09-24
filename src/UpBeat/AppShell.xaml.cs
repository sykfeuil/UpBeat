using UpBeat.Views;

namespace UpBeat;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Pages reached by navigation (not by a tab)
		Routing.RegisterRoute(nameof(PlaylistDetailPage), typeof(PlaylistDetailPage));
	}
}
