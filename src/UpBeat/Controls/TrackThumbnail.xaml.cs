namespace UpBeat.Controls;

/// <summary>
/// Small rounded thumbnail of a track or playlist.
/// </summary>
public partial class TrackThumbnail : ContentView
{
	public static readonly BindableProperty UrlProperty = BindableProperty.Create(
		nameof(Url),
		typeof(string),
		typeof(TrackThumbnail),
		propertyChanged: (bindable, _, newValue) =>
			((TrackThumbnail)bindable).ThumbnailImage.Source = newValue is string url ? ImageSource.FromUri(new Uri(url)) : null);

	public TrackThumbnail()
	{
		InitializeComponent();
	}

	/// <summary>
	/// URL of the image to show, or <c>null</c> for an empty thumbnail.
	/// </summary>
	public string? Url
	{
		get => (string?)GetValue(UrlProperty);
		set => SetValue(UrlProperty, value);
	}
}
