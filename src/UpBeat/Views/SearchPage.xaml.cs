using CommunityToolkit.Maui.Core;
using UpBeat.ViewModels;

namespace UpBeat.Views;

public partial class SearchPage : ContentPage
{
	public SearchPage(SearchViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	private async void OnSearchButtonPressed(object? sender, EventArgs e)
	{
		// Hide the keyboard so that the results are fully visible
		await SearchInput.HideSoftInputAsync(CancellationToken.None);
	}

	private void OnPlayPauseClicked(object? sender, EventArgs e)
	{
		if (Player.CurrentState == MediaElementState.Playing)
		{
			Player.Pause();
		}
		else
		{
			Player.Play();
		}
	}

	private void OnPlayerStateChanged(object? sender, MediaStateChangedEventArgs e)
	{
		// Show "pause" while playing, "play" otherwise
		Dispatcher.Dispatch(() =>
			PlayPauseButton.Source = e.NewState == MediaElementState.Playing ? "icon_pause.png" : "icon_play.png");
	}
}
