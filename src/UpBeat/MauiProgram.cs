using Microsoft.Extensions.Logging;

namespace UpBeat;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

#if ANDROID
		// Use our custom Shell renderer to adjust the native tab bar
		builder.ConfigureMauiHandlers(handlers =>
			handlers.AddHandler<Shell, UpBeatShellRenderer>());

		// Remove the native underline drawn under the SearchBar text
		Microsoft.Maui.Handlers.SearchBarHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
		{
			var searchPlate = handler.PlatformView.FindViewById(Resource.Id.search_plate);
			searchPlate?.SetBackgroundColor(Android.Graphics.Color.Transparent);
		});
#endif

		return builder.Build();
	}
}
