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
			.UseMauiCommunityToolkit()
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
		builder.Services.AddSingleton<AudioPlayerService>();
		builder.Services.AddSingleton<IPlaylistRepository, PlaylistRepository>();
		builder.Services.AddSingleton<IDialogService, DialogService>();

		// ViewModels and pages
		builder.Services.AddSingleton<PlayerViewModel>();
		builder.Services.AddTransient<SearchViewModel>();
		builder.Services.AddTransient<PlaylistsViewModel>();
		builder.Services.AddTransient<PlaylistDetailViewModel>();
		builder.Services.AddTransient<HomePage>();
		builder.Services.AddTransient<SearchPage>();
		builder.Services.AddTransient<PlaylistsPage>();
		builder.Services.AddTransient<PlaylistDetailPage>();

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

		// MAUI bug: applying the text color also paints the search icon with the theme's default color (white).
		// The text color is applied again when the field loses focus, so the icon color is applied again after it.
		Microsoft.Maui.Handlers.SearchBarHandler.Mapper.AppendToMapping(nameof(SearchBar.TextColor), (handler, view) =>
			handler.UpdateValue(nameof(SearchBar.SearchIconColor)));
#endif

		return builder.Build();
	}
}
