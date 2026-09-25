using CommunityToolkit.Maui.Core;
using UpBeat.Models;
using UpBeat.ViewModels;

namespace UpBeat.Views;

public partial class PlaylistsPage : ContentPage
{
	private readonly PlaylistsViewModel _viewModel;

	// A long press is followed by a "tap" event when the finger is lifted: this flag ignores it
	private bool _isLongPressing;

	public PlaylistsPage(PlaylistsViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		// Reload each time the page is shown, since playlists may have changed elsewhere
		_viewModel.LoadCommand.Execute(null);
	}

	private async void OnSearchButtonPressed(object? sender, EventArgs e)
	{
		// The search runs while typing: the search key only hides the keyboard
		await SearchInput.HideSoftInputAsync(CancellationToken.None);
	}

	// Each playlist row carries its PlaylistSummary as BindingContext

	private void OnPlaylistTapped(object? sender, TouchGestureCompletedEventArgs e)
	{
		if (_isLongPressing)
		{
			_isLongPressing = false;
			return;
		}

		if (sender is BindableObject { BindingContext: PlaylistSummary playlist })
		{
			_viewModel.OpenPlaylistCommand.Execute(playlist);
		}
	}

	private void OnPlaylistLongPressed(object? sender, LongPressCompletedEventArgs e)
	{
		_isLongPressing = true;

		if (sender is BindableObject { BindingContext: PlaylistSummary playlist })
		{
			_viewModel.ShowPlaylistOptionsCommand.Execute(playlist);
		}
	}
}
