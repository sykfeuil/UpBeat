using UpBeat.ViewModels;

namespace UpBeat.Views;

public partial class PlaylistDetailPage : ContentPage
{
	private readonly PlaylistDetailViewModel _viewModel;

	public PlaylistDetailPage(PlaylistDetailViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = _viewModel = viewModel;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		// Reload each time the page is shown again, since tracks may have been added elsewhere (e.g. from the search)
		if (_viewModel.PlaylistId > 0)
		{
			_viewModel.LoadCommand.Execute(null);
		}
	}

	private void OnReorderCompleted(object? sender, EventArgs e)
	{
		// The list has already moved the item in the collection: save the new order
		_viewModel.SaveOrderCommand.Execute(null);
	}
}
