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
	private readonly IMusicSource _musicSource;
	private readonly AudioPlayerService _audioPlayer;

	// While the user drags the progress bar, position updates from the player are ignored
	private bool _isSeeking;

	public PlayerViewModel(IMusicSource musicSource, AudioPlayerService audioPlayer)
	{
		_musicSource = musicSource;
		_audioPlayer = audioPlayer;

		_audioPlayer.PlayingChanged += (_, isPlaying) => IsPlaying = isPlaying;
		_audioPlayer.DurationChanged += (_, duration) => SetDuration(duration);
		_audioPlayer.PositionChanged += (_, position) =>
		{
			if (!_isSeeking)
			{
				PositionSeconds = position.TotalSeconds;
			}
		};
	}

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasCurrentTrack))]
	public partial Track? CurrentTrack { get; set; }

	public bool HasCurrentTrack => CurrentTrack is not null;

	[ObservableProperty]
	public partial bool IsPlaying { get; set; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Position))]
	public partial double PositionSeconds { get; set; }

	public TimeSpan Position => TimeSpan.FromSeconds(PositionSeconds);

	// At least 1 second, because the progress bar maximum must be greater than its minimum (0)
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Duration))]
	public partial double DurationSeconds { get; set; } = 1;

	public TimeSpan Duration => TimeSpan.FromSeconds(DurationSeconds);

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasError))]
	public partial string? ErrorMessage { get; set; }

	public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

	[RelayCommand]
	private async Task PlayTrackAsync(Track track)
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

			_audioPlayer.Play(streamUrl, track.Title, track.Author);
		}
		catch (Exception) when (CurrentTrack == track)
		{
			ErrorMessage = "Unable to play this track.";
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
