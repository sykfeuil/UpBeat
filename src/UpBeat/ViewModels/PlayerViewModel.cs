using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UpBeat.Models;
using UpBeat.Services;

namespace UpBeat.ViewModels;

/// <summary>
/// App-wide state of the player, shared by every mini player.
/// </summary>
public partial class PlayerViewModel : ObservableObject
{
	// "Previous" restarts the current track if it has been playing for longer than this
	private const double RestartThresholdSeconds = 3;

	private readonly IMusicSource _musicSource;
	private readonly AudioPlayerService _audioPlayer;
	private readonly IHistoryRepository _history;
	private readonly IPlaylistRepository _playlists;

	// While the user drags the progress bar, position updates from the player are ignored
	private bool _isSeeking;

	// Queue: the tracks of a playlist, played one after the other. Empty when a single track is played.
	private int? _queuePlaylistId;
	private IReadOnlyList<PlaylistTrack> _queue = [];
	private int _queueIndex;

	// Tracks that failed one after the other, to stop instead of looping forever when none can be played
	private int _failuresInARow;

	public PlayerViewModel(IMusicSource musicSource, AudioPlayerService audioPlayer, IHistoryRepository history, IPlaylistRepository playlists)
	{
		_musicSource = musicSource;
		_audioPlayer = audioPlayer;
		_history = history;
		_playlists = playlists;

		_audioPlayer.PlayingChanged += (_, isPlaying) =>
		{
			IsPlaying = isPlaying;

			if (isPlaying)
			{
				_failuresInARow = 0;
			}
		};
		_audioPlayer.DurationChanged += (_, duration) => SetDuration(duration);
		_audioPlayer.PositionChanged += (_, position) =>
		{
			if (!_isSeeking)
			{
				PositionSeconds = position.TotalSeconds;
			}
		};
		_audioPlayer.MediaEnded += (_, _) => PlayNext();
		_audioPlayer.MediaFailed += (_, _) => OnTrackFailed();
		_audioPlayer.NextRequested += (_, _) => PlayNext();
		_audioPlayer.PreviousRequested += (_, _) => PlayPreviousCommand.Execute(null);

		_playlists.PlaylistChanged += OnPlaylistChanged;
	}

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasCurrentTrack))]
	public partial Track? CurrentTrack { get; set; }

	public bool HasCurrentTrack => CurrentTrack is not null;

	/// <summary>
	/// True while a playlist is played, to show the previous/next buttons.
	/// </summary>
	[ObservableProperty]
	public partial bool HasQueue { get; set; }

	[ObservableProperty]
	public partial bool IsPlaying { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Position), nameof(Progress))]
	public partial double PositionSeconds { get; set; }

	public TimeSpan Position => TimeSpan.FromSeconds(PositionSeconds);

	// At least 1 second, because the progress bar maximum must be greater than its minimum (0)
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Duration), nameof(Progress))]
	public partial double DurationSeconds { get; set; } = 1;

	public TimeSpan Duration => TimeSpan.FromSeconds(DurationSeconds);

	/// <summary>
	/// Elapsed part of the track, from 0 to 1, for the progress bar.
	/// </summary>
	public double Progress => Math.Clamp(PositionSeconds / DurationSeconds, 0, 1);

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasError))]
	public partial string? ErrorMessage { get; set; }

	public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

	/// <summary>
	/// Plays a single track (from the search or the history).
	/// </summary>
	[RelayCommand]
	private Task PlayTrackAsync(Track track)
	{
		SetQueue(null, []);
		_failuresInARow = 0;
		return LoadAndPlayAsync(track);
	}

	/// <summary>
	/// Plays the tracks of a playlist one after the other, starting at the given track.
	/// After the last track, playback goes back to the first one.
	/// </summary>
	public async Task PlayPlaylistAsync(int playlistId, int startTrackId)
	{
		var tracks = await _playlists.GetTracksAsync(playlistId);
		var startIndex = IndexOf(tracks, startTrackId);
		if (startIndex < 0)
		{
			return;
		}

		SetQueue(playlistId, tracks);
		_queueIndex = startIndex;
		_failuresInARow = 0;
		await LoadAndPlayAsync(_queue[_queueIndex].ToTrack());
	}

	/// <summary>
	/// Next track of the queue, or back to the first one after the last.
	/// </summary>
	[RelayCommand]
	private void PlayNext()
	{
		if (_queue.Count == 0)
		{
			return;
		}

		_queueIndex = (_queueIndex + 1) % _queue.Count;
		_ = LoadAndPlayAsync(_queue[_queueIndex].ToTrack());
	}

	/// <summary>
	/// Like most players: restarts the current track if it has been playing for a few seconds,
	/// otherwise goes to the previous track of the queue (the last one before the first).
	/// </summary>
	[RelayCommand]
	private async Task PlayPreviousAsync()
	{
		if (_queue.Count == 0 || PositionSeconds > RestartThresholdSeconds)
		{
			await _audioPlayer.SeekToAsync(TimeSpan.Zero);
			return;
		}

		_queueIndex = (_queueIndex - 1 + _queue.Count) % _queue.Count;
		await LoadAndPlayAsync(_queue[_queueIndex].ToTrack());
	}

	private async Task LoadAndPlayAsync(Track track)
	{
		CurrentTrack = track;
		ErrorMessage = null;
		SetDuration(track.Duration ?? TimeSpan.Zero);
		PositionSeconds = 0;

		try
		{
			var streamUrl = await _musicSource.GetAudioStreamUrlAsync(track.Id);

			// Another track may have been chosen while this one was loading: keep only the latest
			if (CurrentTrack != track)
			{
				return;
			}

			_audioPlayer.Play(streamUrl, track.Title, track.Author, track.ThumbnailUrl);
			await _history.AddAsync(track);
		}
		catch (Exception) when (CurrentTrack == track)
		{
			OnTrackFailed();
		}
	}

	/// <summary>
	/// A track could not be played: in a queue, try the next one, unless every track has failed.
	/// </summary>
	private void OnTrackFailed()
	{
		_failuresInARow++;

		if (_queue.Count > 0 && _failuresInARow < _queue.Count)
		{
			PlayNext();
		}
		else
		{
			ErrorMessage = "Unable to play this track.";
		}
	}

	/// <summary>
	/// Keeps the queue in sync with its playlist when tracks are added, removed or reordered.
	/// </summary>
	private async void OnPlaylistChanged(object? sender, int playlistId)
	{
		if (playlistId != _queuePlaylistId)
		{
			return;
		}

		var currentTrackId = _queue.Count > 0 ? _queue[_queueIndex].Id : -1;
		var tracks = await _playlists.GetTracksAsync(playlistId);

		var newIndex = IndexOf(tracks, currentTrackId);
		if (newIndex < 0)
		{
			// The current track was removed: the track that followed it will be played next
			newIndex = Math.Min(_queueIndex, tracks.Count) - 1;
		}

		SetQueue(tracks.Count > 0 ? playlistId : null, tracks);
		_queueIndex = newIndex;
	}

	private void SetQueue(int? playlistId, IReadOnlyList<PlaylistTrack> tracks)
	{
		_queuePlaylistId = playlistId;
		_queue = tracks;
		HasQueue = tracks.Count > 0;
	}

	private static int IndexOf(IReadOnlyList<PlaylistTrack> tracks, int trackId)
	{
		for (var i = 0; i < tracks.Count; i++)
		{
			if (tracks[i].Id == trackId)
			{
				return i;
			}
		}

		return -1;
	}

	[RelayCommand]
	private void TogglePlayPause() => _audioPlayer.TogglePlayPause();

	[RelayCommand]
	private void BeginSeek() => _isSeeking = true;

	[RelayCommand]
	private async Task EndSeekAsync()
	{
		await _audioPlayer.SeekToAsync(Position);
		_isSeeking = false;
	}

	private void SetDuration(TimeSpan duration)
	{
		if (duration > TimeSpan.Zero || DurationSeconds <= 1)
		{
			DurationSeconds = Math.Max(1, duration.TotalSeconds);
		}
	}
}
