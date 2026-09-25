using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace UpBeat.Services;

/// <summary>
/// App-wide audio player. It owns the single <see cref="MediaElement"/> of the app,
/// which must be placed once in the visual tree (see HomePage).
/// </summary>
public class AudioPlayerService
{
	public AudioPlayerService()
	{
		// Player events may be raised from a background thread: forward them on the UI thread
		Player.StateChanged += (_, e) => MainThread.BeginInvokeOnMainThread(() =>
			PlayingChanged?.Invoke(this, e.NewState == MediaElementState.Playing));
		Player.PositionChanged += (_, e) => MainThread.BeginInvokeOnMainThread(() =>
			PositionChanged?.Invoke(this, e.Position));
		// When a media is opened, Duration may still hold the previous media's duration:
		// watch the property itself, which is updated once the real duration is known
		Player.PropertyChanged += (_, e) =>
		{
			if (e.PropertyName == nameof(MediaElement.Duration))
			{
				MainThread.BeginInvokeOnMainThread(() => DurationChanged?.Invoke(this, Player.Duration));
			}
		};
	}

	/// <summary>
	/// The audio player view (audio only, nothing is displayed).
	/// </summary>
	public MediaElement Player { get; } = new()
	{
		ShouldAutoPlay = true,
		ShouldShowPlaybackControls = false,
	};

	/// <summary>Raised when playback starts (<c>true</c>) or stops (<c>false</c>).</summary>
	public event EventHandler<bool>? PlayingChanged;

	/// <summary>Raised regularly while playing, with the current position.</summary>
	public event EventHandler<TimeSpan>? PositionChanged;

	/// <summary>Raised when the duration of the current media is known.</summary>
	public event EventHandler<TimeSpan>? DurationChanged;

	public void Play(string streamUrl, string title, string artist, string? artworkUrl)
	{
		// Shown in the media notification and on the lock screen
		Player.MetadataTitle = title;
		Player.MetadataArtist = artist;
		Player.MetadataArtworkUrl = artworkUrl ?? string.Empty;

		Player.Source = MediaSource.FromUri(streamUrl);
	}

	public void TogglePlayPause()
	{
		if (Player.CurrentState == MediaElementState.Playing)
		{
			Player.Pause();
		}
		else
		{
			Player.Play();
		}
	}

	public Task SeekToAsync(TimeSpan position) => Player.SeekTo(position);

	/// <summary>
	/// Stops playback and releases the native player, which also removes the media notification.
	/// </summary>
	public void Release()
	{
		Player.Stop();
		Player.Handler?.DisconnectHandler();
	}
}
