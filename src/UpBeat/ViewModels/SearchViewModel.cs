using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UpBeat.Models;
using UpBeat.Services;

namespace UpBeat.ViewModels;

public partial class SearchViewModel : ObservableObject
{
	private readonly IMusicSource _musicSource;

	public SearchViewModel(IMusicSource musicSource)
	{
		_musicSource = musicSource;
	}

	[ObservableProperty]
	public partial string Query { get; set; } = string.Empty;

	[ObservableProperty]
	public partial IReadOnlyList<Track> Results { get; set; } = [];

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasMessage))]
	public partial string? Message { get; set; }

	public bool HasMessage => !string.IsNullOrEmpty(Message);

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasCurrentTrack))]
	public partial Track? CurrentTrack { get; set; }

	public bool HasCurrentTrack => CurrentTrack is not null;

	[ObservableProperty]
	public partial MediaSource? CurrentSource { get; set; }

	[RelayCommand]
	private async Task SearchAsync()
	{
		var query = Query.Trim();
		if (query.Length == 0)
		{
			return;
		}

		Message = null;
		Results = [];

		try
		{
			Results = await _musicSource.SearchAsync(query);

			if (Results.Count == 0)
			{
				Message = "No results found";
			}
		}
		catch (Exception)
		{
			Message = "Search failed. Check your internet connection.";
		}
	}

	[RelayCommand]
	private async Task PlayAsync(Track track)
	{
		CurrentTrack = track;

		try
		{
			var streamUrl = await _musicSource.GetAudioStreamUrlAsync(track.Id);
			CurrentSource = MediaSource.FromUri(streamUrl);
		}
		catch (Exception)
		{
			CurrentTrack = null;
			CurrentSource = null;
			Message = "Unable to play this track.";
		}
	}
}
