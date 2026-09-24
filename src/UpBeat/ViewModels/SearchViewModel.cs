using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UpBeat.Models;
using UpBeat.Services;

namespace UpBeat.ViewModels;

public partial class SearchViewModel : ObservableObject
{
	private const string NewPlaylistOption = "+ New playlist";

	private readonly IMusicSource _musicSource;
	private readonly IPlaylistRepository _playlists;
	private readonly IDialogService _dialogs;

	public SearchViewModel(IMusicSource musicSource, IPlaylistRepository playlists, IDialogService dialogs, PlayerViewModel player)
	{
		_musicSource = musicSource;
		_playlists = playlists;
		_dialogs = dialogs;
		Player = player;
	}

	/// <summary>
	/// The app-wide player, used to play a search result.
	/// </summary>
	public PlayerViewModel Player { get; }

	[ObservableProperty]
	public partial string Query { get; set; } = string.Empty;

	[ObservableProperty]
	public partial IReadOnlyList<Track> Results { get; set; } = [];

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasMessage))]
	public partial string? Message { get; set; }

	public bool HasMessage => !string.IsNullOrEmpty(Message);

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

	/// <summary>
	/// Adds a search result to a playlist chosen by the user (on long press).
	/// </summary>
	[RelayCommand]
	private async Task AddToPlaylistAsync(Track track)
	{
		var playlists = await _playlists.GetPlaylistsAsync();
		var options = playlists.Select(p => p.Name).Append(NewPlaylistOption);

		var choice = await _dialogs.ChooseAsync("Add to playlist", options);
		if (choice is null)
		{
			return;
		}

		int playlistId;
		string playlistName;

		if (choice == NewPlaylistOption)
		{
			var name = await _dialogs.PromptAsync("New playlist", "Name of the playlist", "Create");
			if (name is null)
			{
				return;
			}

			var playlist = await _playlists.CreatePlaylistAsync(name);
			playlistId = playlist.Id;
			playlistName = playlist.Name;
		}
		else
		{
			var playlist = playlists.First(p => p.Name == choice);
			playlistId = playlist.Id;
			playlistName = playlist.Name;
		}

		await _playlists.AddTrackAsync(playlistId, track);
		await _dialogs.ShowToastAsync($"Added to {playlistName}");
	}
}
