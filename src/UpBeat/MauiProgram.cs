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
				fonts.AddFont("JetBrainsMono-Regular.ttf", "JetBrainsMonoRegular");
				fonts.AddFont("JetBrainsMono-Bold.ttf", "JetBrainsMonoBold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Services
		builder.Services.AddSingleton<IMusicSource, YoutubeMusicSource>();
		builder.Services.AddSingleton<AudioPlayerService>();
		builder.Services.AddSingleton<AppDatabase>();
		builder.Services.AddSingleton<IPlaylistRepository, PlaylistRepository>();
		builder.Services.AddSingleton<IHistoryRepository, HistoryRepository>();
		builder.Services.AddSingleton<IDialogService, DialogService>();

		// ViewModels and pages
		builder.Services.AddSingleton<PlayerViewModel>();
		builder.Services.AddTransient<SearchViewModel>();
		builder.Services.AddTransient<PlaylistsViewModel>();
		builder.Services.AddTransient<PlaylistDetailViewModel>();
		builder.Services.AddTransient<HistoryViewModel>();
		builder.Services.AddTransient<HomePage>();
		builder.Services.AddTransient<SearchPage>();
		builder.Services.AddTransient<PlaylistsPage>();
		builder.Services.AddTransient<PlaylistDetailPage>();
		builder.Services.AddTransient<HistoryPage>();

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

		// Replace the native magnifier and clear icons of the SearchBar with the app's pixel-art icons
		Microsoft.Maui.Handlers.SearchBarHandler.Mapper.AppendToMapping("PixelIcons", (handler, view) =>
		{
			handler.PlatformView.FindViewById<Android.Widget.ImageView>(Resource.Id.search_mag_icon)?.SetImageResource(Resource.Drawable.icon_search);
			handler.PlatformView.FindViewById<Android.Widget.ImageView>(Resource.Id.search_close_btn)?.SetImageResource(Resource.Drawable.icon_clear);
		});

		// MAUI bug: applying the text color also paints the search icon with the theme's default color (white).
		// The text color is applied again when the field loses focus, so the icon color is applied again after it.
		Microsoft.Maui.Handlers.SearchBarHandler.Mapper.AppendToMapping(nameof(SearchBar.TextColor), (handler, view) =>
			handler.UpdateValue(nameof(SearchBar.SearchIconColor)));
#endif

		return builder.Build();
	}
}
