using Microsoft.Extensions.DependencyInjection;

namespace UpBeat;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		// UpBeat always uses its dark synthwave theme, whatever the system setting
		UserAppTheme = AppTheme.Dark;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}