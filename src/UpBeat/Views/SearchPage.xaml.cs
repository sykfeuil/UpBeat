using CommunityToolkit.Maui.Core;
using UpBeat.Models;
using UpBeat.ViewModels;

namespace UpBeat.Views;

public partial class SearchPage : ContentPage
{
	private readonly SearchViewModel _viewModel;

	// A long press is followed by a "tap" event when the finger is lifted: this flag ignores it
	private bool _isLongPressing;

	public SearchPage(SearchViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
	}

	private async void OnSearchButtonPressed(object? sender, EventArgs e)
	{
		// Hide the keyboard so that the results are fully visible
		await SearchInput.HideSoftInputAsync(CancellationToken.None);
	}

	// Each result row carries its Track as BindingContext

	private void OnResultTapped(object? sender, TouchGestureCompletedEventArgs e)
	{
		if (_isLongPressing)
		{
			_isLongPressing = false;
			return;
		}

		if (sender is BindableObject { BindingContext: Track track })
		{
			_viewModel.Player.PlayTrackCommand.Execute(track);
		}
	}

	private void OnResultLongPressed(object? sender, LongPressCompletedEventArgs e)
	{
		_isLongPressing = true;

		if (sender is BindableObject { BindingContext: Track track })
		{
			_viewModel.AddToPlaylistCommand.Execute(track);
		}
	}
}
