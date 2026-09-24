using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UpBeat.Models;
using UpBeat.Services;

namespace UpBeat.ViewModels;

/// <summary>
/// Tracks of one playlist. Receives the playlist from the navigation (see <see cref="ApplyQueryAttributes"/>).
/// </summary>
public partial class PlaylistDetailViewModel : ObservableObject, IQueryAttributable
{
	public const string PlaylistIdParameter = "PlaylistId";
	public const string PlaylistNameParameter = "PlaylistName";

	private readonly IPlaylistRepository _repository;
	private int _playlistId;

	public PlaylistDetailViewModel(IPlaylistRepository repository, PlayerViewModel player)
	{
		_repository = repository;
		Player = player;

		Tracks.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsEmpty));
	}

	public PlayerViewModel Player { get; }

	[ObservableProperty]
	public partial string Name { get; set; } = string.Empty;

	/// <summary>
	/// Observable collection, so that the list can reorder its items in place (drag and drop).
	/// </summary>
	public ObservableCollection<PlaylistTrack> Tracks { get; } = [];

	public bool IsEmpty => Tracks.Count == 0;

	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		_playlistId = (int)query[PlaylistIdParameter];
		Name = (string)query[PlaylistNameParameter];

		LoadCommand.Execute(null);
	}

	[RelayCommand]
	private async Task LoadAsync()
	{
		var tracks = await _repository.GetTracksAsync(_playlistId);

		Tracks.Clear();
		foreach (var track in tracks)
		{
			Tracks.Add(track);
		}
	}

	[RelayCommand]
	private Task PlayTrackAsync(PlaylistTrack track)
		=> Player.PlayTrackCommand.ExecuteAsync(track.ToTrack());

	[RelayCommand]
	private async Task RemoveTrackAsync(PlaylistTrack track)
	{
		await _repository.RemoveTrackAsync(track);
		Tracks.Remove(track);
	}

	/// <summary>
	/// Saves the new order after a drag and drop.
	/// </summary>
	[RelayCommand]
	private Task SaveOrderAsync() => _repository.SaveOrderAsync(Tracks.ToList());

	[RelayCommand]
	private Task GoBackAsync() => Shell.Current.GoToAsync("..");
}
