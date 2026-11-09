using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UpBeat.Models;
using UpBeat.Services;

namespace UpBeat.ViewModels;

/// <summary>
/// App-wide state of the player, shared by every mini player.
/// Everything is played from a single queue: a playlist, or a single track followed by suggestions.
/// When the queue runs out, suggestions (YouTube mixes) are added, so playback never stops.
/// </summary>
public partial class PlayerViewModel : ObservableObject
{
	// "Previous" restarts the current track if it has been playing for longer than this
	private const double RestartThresholdSeconds = 3;

	// Suggestions are loaded when no more than this many tracks are left after the playing one
	private const int MinUpcomingTracks = 1;

	// Mixes tried in a row when they only contain tracks already in the queue
	private const int MaxMixAttempts = 3;

	// Unplayable tracks in a row before stopping, instead of going through an endless queue
	private const int MaxFailuresInARow = 5;

	// Stream URLs of the recent tracks: a known URL is only used for this long, since YouTube URLs expire after a few hours
	private const int UrlCacheCapacity = 20;
	private static readonly TimeSpan UrlLifetime = TimeSpan.FromHours(1);

	private readonly IMusicSource _musicSource;
	private readonly AudioPlayerService _audioPlayer;
	private readonly IHistoryRepository _history;
	private readonly IPlaylistRepository _playlists;
	private readonly PlaybackQueue _queue = new();

	// URLs of the played tracks and of the next one, so that they start without waiting
	private readonly StreamUrlCache _streamUrls;

	// While the user drags the progress bar, position updates from the player are ignored
	private bool _isSeeking;

	// Tracks that failed one after the other
	private int _failuresInARow;

	// Suggestions being loaded, for the queue version they were asked for
	private (Task Task, int QueueVersion)? _loadingSuggestions;

	public PlayerViewModel(IMusicSource musicSource, AudioPlayerService audioPlayer, IHistoryRepository history, IPlaylistRepository playlists)
	{
		_musicSource = musicSource;
		_audioPlayer = audioPlayer;
		_history = history;
		_playlists = playlists;
		_streamUrls = new StreamUrlCache(trackId => _musicSource.GetAudioStreamUrlAsync(trackId), TimeProvider.System, UrlCacheCapacity, UrlLifetime);

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
		_audioPlayer.MediaEnded += (_, _) => PlayNextCommand.Execute(null);
		_audioPlayer.MediaFailed += (_, _) => _ = OnTrackFailedAsync();
		_audioPlayer.NextRequested += (_, _) => PlayNextCommand.Execute(null);
		_audioPlayer.PreviousRequested += (_, _) => PlayPreviousCommand.Execute(null);
	}

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasCurrentTrack))]
	public partial Track? CurrentTrack { get; set; }

	public bool HasCurrentTrack => CurrentTrack is not null;

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
	/// Plays a single track (from the search or the history), followed by suggestions.
	/// </summary>
	[RelayCommand]
	private Task PlayTrackAsync(Track track)
	{
		_queue.Replace([track], 0);
		_failuresInARow = 0;
		return PlayCurrentAsync();
	}

	/// <summary>
	/// Plays a copy of the tracks of a playlist, starting at the given track, followed by suggestions.
	/// </summary>
	public async Task PlayPlaylistAsync(int playlistId, int startTrackId)
	{
		var tracks = await _playlists.GetTracksAsync(playlistId);
		var startIndex = tracks.ToList().FindIndex(track => track.Id == startTrackId);
		if (startIndex < 0)
		{
			return;
		}

		_queue.Replace(tracks.Select(track => track.ToTrack()), startIndex);
		_failuresInARow = 0;
		await PlayCurrentAsync();
	}

	/// <summary>
	/// Plays the track right after the current one ("Play next"). Plays it now if nothing is playing.
	/// </summary>
	public Task InsertNextAsync(Track track)
	{
		if (_queue.Current is null)
		{
			return PlayTrackAsync(track);
		}

		_queue.InsertNext(track);
		PrefetchNextUrl();
		return Task.CompletedTask;
	}

	/// <summary>
	/// Plays the track after the tracks already chosen, before the suggestions ("Add to queue").
	/// Plays it now if nothing is playing.
	/// </summary>
	public Task AppendAsync(Track track)
	{
		if (_queue.Current is null)
		{
			return PlayTrackAsync(track);
		}

		_queue.Append(track);
		PrefetchNextUrl();
		return Task.CompletedTask;
	}

	/// <summary>
	/// Next track of the queue. At the end of the queue, waits for suggestions.
	/// </summary>
	[RelayCommand(AllowConcurrentExecutions = true)]
	private async Task PlayNextAsync()
	{
		await TryPlayNextAsync();
	}

	/// <summary>
	/// Like most players: restarts the current track if it has been playing for a few seconds,
	/// or if it is the first one; otherwise goes back to the previous track.
	/// </summary>
	[RelayCommand(AllowConcurrentExecutions = true)]
	private async Task PlayPreviousAsync()
	{
		if (_queue.Current is null)
		{
			return;
		}

		if (PositionSeconds > RestartThresholdSeconds || !_queue.MovePrevious())
		{
			await _audioPlayer.SeekToAsync(TimeSpan.Zero);
			return;
		}

		await PlayCurrentAsync();
	}

	/// <summary>
	/// Moves to the next track and plays it. Returns <c>false</c> if there is none, even after loading suggestions.
	/// </summary>
	private async Task<bool> TryPlayNextAsync()
	{
		if (_queue.Current is null)
		{
			return false;
		}

		if (!_queue.MoveNext())
		{
			// End of the queue: suggestions are usually loaded already, unless the tracks were skipped quickly
			var queueVersion = _queue.Version;
			await LoadSuggestionsAsync();

			// Another track or playlist may have been played in the meantime
			if (_queue.Version != queueVersion || !_queue.MoveNext())
			{
				return false;
			}
		}

		await PlayCurrentAsync();
		return true;
	}

	private async Task PlayCurrentAsync()
	{
		if (_queue.Current is not { } item)
		{
			return;
		}

		var track = item.Track;
		CurrentTrack = track;
		ErrorMessage = null;
		SetDuration(track.Duration ?? TimeSpan.Zero);
		PositionSeconds = 0;

		try
		{
			var streamUrl = await _streamUrls.GetAsync(track.Id);

			// Another track may have been chosen while this one was loading: keep only the latest
			if (_queue.Current != item)
			{
				return;
			}

			_audioPlayer.Play(streamUrl, track.Title, track.Author, track.ThumbnailUrl);
		}
		catch (Exception)
		{
			// Only the latest track matters: the failure of an older one is ignored
			if (_queue.Current == item)
			{
				await OnTrackFailedAsync();
			}

			return;
		}

		// Get ready for what comes next while this track plays
		PrefetchNextUrl();
		if (_queue.RemainingCount <= MinUpcomingTracks)
		{
			_ = LoadSuggestionsAsync();
		}

		// The track is playing: failing to save the history must not be handled as a playback failure
		try
		{
			await _history.AddAsync(track);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Unable to add the track to the history: {ex}");
		}
	}

	/// <summary>
	/// A track could not be played: try the next one, unless too many tracks have failed in a row.
	/// </summary>
	private async Task OnTrackFailedAsync()
	{
		_failuresInARow++;

		// The URL may have been the problem: it will be resolved again if the track is played again
		if (_queue.Current is { } item)
		{
			_streamUrls.Invalidate(item.Track.Id);
		}

		if (_failuresInARow >= MaxFailuresInARow || !await TryPlayNextAsync())
		{
			ErrorMessage = "Unable to play this track.";
		}
	}

	/// <summary>
	/// Resolves the stream URL of the next track in the background, unless it is already known.
	/// </summary>
	private void PrefetchNextUrl()
	{
		if (_queue.Next is { } next)
		{
			_streamUrls.Prefetch(next.Track.Id);
		}
	}

	/// <summary>
	/// Adds suggestions at the end of the queue. Only one load runs at a time for a given queue.
	/// </summary>
	private Task LoadSuggestionsAsync()
	{
		if (_loadingSuggestions is not { } loading || loading.Task.IsCompleted || loading.QueueVersion != _queue.Version)
		{
			loading = (AddSuggestionsAsync(_queue.Version), _queue.Version);
			_loadingSuggestions = loading;
		}

		return loading.Task;
	}

	/// <summary>
	/// Loads the mix of the last track of the queue. If it brings nothing new, tries the mix of an earlier track.
	/// </summary>
	private async Task AddSuggestionsAsync(int queueVersion)
	{
		try
		{
			for (var attempt = 0; attempt < MaxMixAttempts && _queue.TakeMixSeed() is { } seed; attempt++)
			{
				var suggestions = await _musicSource.GetSuggestedTracksAsync(seed.Id);

				// Another track or playlist has been played in the meantime: these suggestions are no longer wanted
				if (_queue.Version != queueVersion)
				{
					return;
				}

				if (_queue.AppendSuggestions(suggestions) > 0)
				{
					PrefetchNextUrl();
					return;
				}
			}
		}
		catch (Exception ex)
		{
			// Playback goes on with the tracks already queued
			Console.WriteLine($"Unable to load suggestions: {ex}");
		}
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
