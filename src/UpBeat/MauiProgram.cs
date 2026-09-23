using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using UpBeat.Services;
using UpBeat.ViewModels;
using UpBeat.Views;

namespace UpBeat;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkitMediaElement(isAndroidForegroundServiceEnabled: true)
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Services
		builder.Services.AddSingleton<IMusicSource, YoutubeMusicSource>();

		// ViewModels and pages
		builder.Services.AddTransient<SearchViewModel>();
		builder.Services.AddTransient<SearchPage>();

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
