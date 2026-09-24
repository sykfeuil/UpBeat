using UpBeat.Services;

namespace UpBeat.Views;

public partial class HomePage : ContentPage
{
	public HomePage(AudioPlayerService audioPlayer)
	{
		InitializeComponent();

		// The audio player must be in the visual tree to work. The home tab hosts it,
		// because it is the first page created and it is never destroyed.
		PlayerHost.Content = audioPlayer.Player;
	}
}
