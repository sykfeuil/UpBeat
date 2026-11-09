using Android.App;
using Android.Content;
using AndroidX.Media3.Common;
using AndroidX.Media3.ExoPlayer;
using AndroidX.Media3.ExoPlayer.Audio;
using AndroidX.Media3.Session;
using UpBeat.Audio;
using Google.Common.Util.Concurrent;
using AudioAttributes = AndroidX.Media3.Common.AudioAttributes;
using MediaMetadata = AndroidX.Media3.Common.MediaMetadata;
using Uri = Android.Net.Uri;

namespace UpBeat.Services;

/// <summary>
/// App-wide audio player, built on Media3 (ExoPlayer).
/// ExoPlayer plays one track at a time: the queue is managed by the app (see PlayerViewModel).
/// The media session, used by <see cref="PlaybackService"/>, shows the notification and the lock screen controls.
/// Use it from the UI thread only: ExoPlayer is bound to the thread that created it.
/// </summary>
public sealed class AudioPlayerService
{
	// Player.STATE_* values, not exposed as named constants by the .NET binding
	private const int StateReady = 3;
	private const int StateEnded = 4;

	// How often the position is sent while playing (ExoPlayer does not report it by itself)
	private static readonly TimeSpan PositionInterval = TimeSpan.FromMilliseconds(500);

	private IExoPlayer? _player;
	private MediaSession? _session;
	private IDispatcherTimer? _positionTimer;

	// Connection to PlaybackService: while it is open, the service runs and shows the notification
	private IListenableFuture? _serviceConnection;

	/// <summary>
	/// The media session, or <c>null</c> until a track has been played.
	/// </summary>
	public MediaSession? Session => _session;

	/// <summary>
	/// Levels of the played audio per frequency band, for the visualizer.
	/// </summary>
	public SpectrumAnalyzer Spectrum { get; } = new();

	/// <summary>
	/// Bass, middle and treble gains applied to the played audio.
	/// </summary>
	public ThreeBandEqualizer Equalizer { get; } = new();

	/// <summary>
	/// Position in the playing track, as heard.
	/// </summary>
	public TimeSpan CurrentPosition => TimeSpan.FromMilliseconds(Math.Max(0, _player?.CurrentPosition ?? 0));

	/// <summary>
	/// True while audio is actually playing (not paused, not loading).
	/// </summary>
	public bool IsPlaying => _player?.IsPlaying == true;

	/// <summary>Raised when playback starts (<c>true</c>) or stops (<c>false</c>).</summary>
	public event EventHandler<bool>? PlayingChanged;

	/// <summary>Raised regularly while playing, with the current position.</summary>
	public event EventHandler<TimeSpan>? PositionChanged;

	/// <summary>Raised when the duration of the current media is known.</summary>
	public event EventHandler<TimeSpan>? DurationChanged;

	/// <summary>Raised when the current media has been played to the end.</summary>
	public event EventHandler? MediaEnded;

	/// <summary>Raised when the current media cannot be played (e.g. the stream fails).</summary>
	public event EventHandler? MediaFailed;

	/// <summary>Raised when the "previous" button of the media notification is pressed.</summary>
	public event EventHandler? PreviousRequested;

	/// <summary>Raised when the "next" button of the media notification is pressed.</summary>
	public event EventHandler? NextRequested;

	public void Play(string streamUrl, string title, string artist, string? artworkUrl)
	{
		var player = EnsurePlayer();

		// Shown in the media notification and on the lock screen
		var metadata = new MediaMetadata.Builder()
			.SetTitle(title)!
			.SetArtist(artist)!
			.SetArtworkUri(artworkUrl is null ? null : Uri.Parse(artworkUrl))!
			.Build();

		var mediaItem = new MediaItem.Builder()
			.SetUri(streamUrl)!
			.SetMediaMetadata(metadata)!
			.Build();

		Spectrum.SetNextStartPosition(TimeSpan.Zero);
		player.SetMediaItem(mediaItem);
		player.Prepare();
		player.Play();
	}

	public void TogglePlayPause()
	{
		if (_player is null)
		{
			return;
		}

		if (_player.PlayWhenReady)
		{
			_player.Pause();
		}
		else
		{
			_player.Play();
		}
	}

	public Task SeekToAsync(TimeSpan position)
	{
		Spectrum.SetNextStartPosition(position);
		_player?.SeekTo((long)position.TotalMilliseconds);
		PositionChanged?.Invoke(this, position);
		return Task.CompletedTask;
	}

	/// <summary>
	/// Stops playback and releases the player, which also stops the service and removes the media notification.
	/// </summary>
	public void Release()
	{
		_positionTimer?.Stop();

		if (_serviceConnection is not null)
		{
			MediaController.ReleaseFuture(_serviceConnection);
			_serviceConnection = null;
		}

		_session?.Release();
		_session = null;

		_player?.Release();
		_player = null;
	}

	/// <summary>
	/// Creates the player, its media session and the background service on first use.
	/// </summary>
	private IExoPlayer EnsurePlayer()
	{
		if (_player is not null)
		{
			return _player;
		}

		var context = Platform.AppContext;

		var audioAttributes = new AudioAttributes.Builder()
			.SetUsage(C.UsageMedia)!
			.SetContentType(C.AudioContentTypeMusic)!
			.Build();

		// The decoded audio goes through the equalizer, then a copy of it goes to the spectrum analyzer
		var renderersFactory = new EffectsRenderersFactory(context, [
			new EqualizerAudioProcessor(Equalizer),
			new TeeAudioProcessor(new SpectrumAudioBufferSink(Spectrum)),
		]);

		_player = new ExoPlayerBuilder(context, renderersFactory)
			// Pause when another app plays sound (e.g. a call), and when headphones are unplugged
			.SetAudioAttributes(audioAttributes, handleAudioFocus: true)!
			.SetHandleAudioBecomingNoisy(true)!
			// Keep the CPU and Wi-Fi awake while streaming with the screen off
			.SetWakeMode(C.WakeModeNetwork)!
			.Build()!;
		_player.AddListener(new PlayerListener(this));

		_session = new MediaSession.Builder(context, new QueueControlsPlayer(
				_player,
				onPrevious: () => PreviousRequested?.Invoke(this, EventArgs.Empty),
				onNext: () => NextRequested?.Invoke(this, EventArgs.Empty)))
			// Tapping the notification opens the app
			.SetSessionActivity(CreateOpenAppIntent(context))!
			.Build();

		// Connecting to the service starts it: Media3 then runs it in the foreground while music plays
		var serviceToken = new SessionToken(context, new ComponentName(context, Java.Lang.Class.FromType(typeof(PlaybackService))));
		_serviceConnection = new MediaController.Builder(context, serviceToken).BuildAsync();

		_positionTimer = Microsoft.Maui.Controls.Application.Current!.Dispatcher.CreateTimer();
		_positionTimer.Interval = PositionInterval;
		_positionTimer.Tick += (_, _) =>
		{
			if (_player is not null)
			{
				PositionChanged?.Invoke(this, TimeSpan.FromMilliseconds(Math.Max(0, _player.CurrentPosition)));
			}
		};

		return _player;
	}

	private static PendingIntent? CreateOpenAppIntent(Context context)
	{
		var intent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName!);
		return intent is null
			? null
			: PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
	}

	/// <summary>
	/// Receives the player events (on the UI thread) and turns them into the service events.
	/// </summary>
	private sealed class PlayerListener(AudioPlayerService service) : Java.Lang.Object, IPlayerListener
	{
		public void OnIsPlayingChanged(bool isPlaying)
		{
			if (isPlaying)
			{
				service._positionTimer?.Start();
			}
			else
			{
				service._positionTimer?.Stop();
			}

			service.PlayingChanged?.Invoke(service, isPlaying);
		}

		public void OnPlaybackStateChanged(int playbackState)
		{
			switch (playbackState)
			{
				case StateReady when service._player is { Duration: > 0 } player:
					service.DurationChanged?.Invoke(service, TimeSpan.FromMilliseconds(player.Duration));
					break;

				case StateEnded:
					service.MediaEnded?.Invoke(service, EventArgs.Empty);
					break;
			}
		}

		public void OnPlayerError(PlaybackException? error)
		{
			Console.WriteLine($"Playback error: {error?.ErrorCodeName}: {error?.Message}");
			service.MediaFailed?.Invoke(service, EventArgs.Empty);
		}
	}
}
