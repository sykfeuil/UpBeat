using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UpBeat.Helpers;
using UpBeat.Models;
using UpBeat.Services;

namespace UpBeat.ViewModels;

public partial class SearchViewModel : ObservableObject
{
	private const string NewPlaylistOption = "+ New playlist";

	// Search while typing: wait for a short pause, and only from a few characters
	private const int MinQueryLength = 2;
	private static readonly TimeSpan TypingDelay = TimeSpan.FromMilliseconds(400);

	private readonly IMusicSource _musicSource;
	private readonly IPlaylistRepository _playlists;
	private readonly IDialogService _dialogs;
	private readonly Debouncer _searchDebouncer = new();

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

	[ObservableProperty]
	public partial bool IsSearching { get; set; }

	/// <summary>
	/// Called by the generated Query property each time the text changes.
	/// </summary>
	partial void OnQueryChanged(string value) => _ = RunSearchAsync(value, TypingDelay);

	/// <summary>
	/// Search key of the keyboard: search right away.
	/// </summary>
	[RelayCommand]
	private Task SearchAsync() => RunSearchAsync(Query, TimeSpan.Zero);

	private Task RunSearchAsync(string text, TimeSpan delay)
	{
		var query = text.Trim();
		if (query.Length < MinQueryLength)
		{
			_searchDebouncer.Cancel();
			IsSearching = false;

			if (query.Length == 0)
			{
				Results = [];
				Message = null;
			}

			return Task.CompletedTask;
		}

		// Only the latest text matters: a newer search cancels this one
		return _searchDebouncer.RunAsync(delay, async cancellationToken =>
		{
			IsSearching = true;

			try
			{
				var results = await _musicSource.SearchAsync(query, cancellationToken);

				Results = results;
				Message = results.Count == 0 ? "No results found" : null;
			}
			catch (Exception) when (!cancellationToken.IsCancellationRequested)
			{
				Message = "Search failed. Check your internet connection.";
			}
			finally
			{
				// A cancelled search leaves the indicator to the newer one
				if (!cancellationToken.IsCancellationRequested)
				{
					IsSearching = false;
				}
			}
		});
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
