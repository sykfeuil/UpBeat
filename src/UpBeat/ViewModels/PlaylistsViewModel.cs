using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UpBeat.Models;
using UpBeat.Services;
using UpBeat.Views;

namespace UpBeat.ViewModels;

public partial class PlaylistsViewModel : ObservableObject
{
	private const string RenameOption = "Rename";
	private const string DeleteOption = "Delete";

	private readonly IPlaylistRepository _repository;
	private readonly IDialogService _dialogs;

	public PlaylistsViewModel(IPlaylistRepository repository, IDialogService dialogs)
	{
		_repository = repository;
		_dialogs = dialogs;
	}

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsEmpty))]
	public partial IReadOnlyList<PlaylistSummary> Playlists { get; set; } = [];

	public bool IsEmpty => Playlists.Count == 0;

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
