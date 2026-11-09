using UpBeat.ViewModels;

namespace UpBeat.Views;

public partial class HomePage : ContentPage
{
	private Window? _window;
	private bool _isShown;

	public HomePage(HomeViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		_isShown = true;
		Visualizer.Start();

		// Also stop animating while the app is in the background (e.g. screen off), to save the battery
		if (_window is null && Window is not null)
		{
			_window = Window;
			_window.Stopped += (_, _) => Visualizer.Stop();
			_window.Resumed += (_, _) =>
			{
				if (_isShown)
				{
					Visualizer.Start();
				}
			};
		}
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();

		_isShown = false;
		Visualizer.Stop();
	}
}
