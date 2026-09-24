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

	private void OnReorderCompleted(object? sender, EventArgs e)
	{
		// The list has already moved the item in the collection: save the new order
		_viewModel.SaveOrderCommand.Execute(null);
	}
}
