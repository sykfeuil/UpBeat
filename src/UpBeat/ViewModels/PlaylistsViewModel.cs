using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UpBeat.Helpers;
using UpBeat.Models;
using UpBeat.Services;
using UpBeat.Views;

namespace UpBeat.ViewModels;

public partial class PlaylistsViewModel : ObservableObject
{
	private const string RenameOption = "Rename";
	private const string DeleteOption = "Delete";

	private const int MinQueryLength = 2;
	private static readonly TimeSpan TypingDelay = TimeSpan.FromMilliseconds(400);

	private readonly IPlaylistRepository _repository;
	private readonly IMusicSource _musicSource;
	private readonly IDialogService _dialogs;
	private readonly Debouncer _searchDebouncer = new();

	public PlaylistsViewModel(IPlaylistRepository repository, IMusicSource musicSource, IDialogService dialogs)
	{
		_repository = repository;
		_musicSource = musicSource;
		_dialogs = dialogs;
	}

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(ShowEmptyState))]
	public partial IReadOnlyList<PlaylistSummary> Playlists { get; set; } = [];

	/// <summary>
	/// "No playlists yet" is shown when the user has no playlist and is not searching.
	/// </summary>
	public bool ShowEmptyState => Playlists.Count == 0 && !IsSearchMode;

	/// <summary>
	/// Text typed in the search field, to find playlists on the music source.
	/// </summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsSearchMode), nameof(IsListMode), nameof(ShowEmptyState))]
	public partial string Query { get; set; } = string.Empty;

	/// <summary>
	/// While searching, the page shows the found playlists instead of the user's playlists.
	/// </summary>
	public bool IsSearchMode => !string.IsNullOrWhiteSpace(Query);

	public bool IsListMode => !IsSearchMode;

	[ObservableProperty]
	public partial IReadOnlyList<PlaylistResult> SearchResults { get; set; } = [];

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasSearchMessage))]
	public partial string? SearchMessage { get; set; }

	public bool HasSearchMessage => !string.IsNullOrEmpty(SearchMessage);

	/// <summary>
	/// True while searching or importing, to show the loading indicator.
	/// </summary>
	[ObservableProperty]
	public partial bool IsBusy { get; set; }

	partial void OnQueryChanged(string value)
	{
		var query = value.Trim();
		if (query.Length < MinQueryLength)
		{
			_searchDebouncer.Cancel();
			IsBusy = false;
			SearchResults = [];
			SearchMessage = null;
			return;
		}

		_ = _searchDebouncer.RunAsync(TypingDelay, async cancellationToken =>
		{
			IsBusy = true;

			try
			{
				var results = await _musicSource.SearchPlaylistsAsync(query, cancellationToken);

				SearchResults = results;
				SearchMessage = results.Count == 0 ? "No playlists found" : null;
			}
			catch (Exception) when (!cancellationToken.IsCancellationRequested)
			{
				SearchMessage = "Search failed. Check your internet connection.";
			}
			finally
			{
				if (!cancellationToken.IsCancellationRequested)
				{
					IsBusy = false;
				}
			}
		});
	}

	/// <summary>
	/// Creates an UpBeat playlist with all the tracks of a found playlist.
	/// </summary>
	[RelayCommand]
	private async Task ImportPlaylistAsync(PlaylistResult result)
	{
		var confirmed = await _dialogs.ConfirmAsync("Import playlist", $"Create \"{result.Title}\" in UpBeat with all its tracks?", "Import");
		if (!confirmed)
		{
			return;
		}

		IsBusy = true;

		try
		{
			var tracks = await _musicSource.GetPlaylistTracksAsync(result.Id);
			if (tracks.Count == 0)
			{
				await _dialogs.ShowToastAsync("This playlist is empty");
				return;
			}

			await _repository.ImportPlaylistAsync(result.Title, tracks);
			await _dialogs.ShowToastAsync($"Imported {tracks.Count} tracks");

			// Back to the user's playlists, where the new one now appears
			Query = string.Empty;
			await LoadAsync();
		}
		catch (Exception)
		{
			await _dialogs.ShowToastAsync("Import failed. Check your internet connection.");
		}
		finally
		{
			IsBusy = false;
		}
	}

	[RelayCommand]
	private async Task LoadAsync()
	{
		Playlists = await _repository.GetPlaylistsAsync();
	}

	[RelayCommand]
	private async Task CreatePlaylistAsync()
	{
		var name = await _dialogs.PromptAsync("New playlist", "Name of the playlist", "Create");
		if (name is null)
		{
			return;
		}

		await _repository.CreatePlaylistAsync(name);
		await LoadAsync();
	}

	[RelayCommand]
	private async Task OpenPlaylistAsync(PlaylistSummary playlist)
	{
		await Shell.Current.GoToAsync(nameof(PlaylistDetailPage), new Dictionary<string, object>
		{
			[PlaylistDetailViewModel.PlaylistIdParameter] = playlist.Id,
			[PlaylistDetailViewModel.PlaylistNameParameter] = playlist.Name,
		});
	}

	/// <summary>
	/// Shows the rename and delete options of a playlist (on long press).
	/// </summary>
	[RelayCommand]
	private async Task ShowPlaylistOptionsAsync(PlaylistSummary playlist)
	{
		var choice = await _dialogs.ChooseAsync(playlist.Name, [RenameOption, DeleteOption]);

		if (choice == RenameOption)
		{
			var name = await _dialogs.PromptAsync("Rename playlist", "New name of the playlist", "Rename", playlist.Name);
			if (name is not null)
			{
				await _repository.RenamePlaylistAsync(playlist.Id, name);
			}
		}
		else if (choice == DeleteOption)
		{
			var confirmed = await _dialogs.ConfirmAsync("Delete playlist", $"Delete \"{playlist.Name}\" and all its tracks?", "Delete");
			if (confirmed)
			{
				await _repository.DeletePlaylistAsync(playlist.Id);
			}
		}

		await LoadAsync();
	}
}
