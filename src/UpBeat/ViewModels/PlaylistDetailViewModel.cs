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

	public PlaylistDetailViewModel(IPlaylistRepository repository, PlayerViewModel player)
	{
		_repository = repository;
		Player = player;

		Tracks.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsEmpty));
	}

	/// <summary>
	/// ID of the displayed playlist (0 until the navigation provides it).
	/// </summary>
	public int PlaylistId { get; private set; }

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
		PlaylistId = (int)query[PlaylistIdParameter];
		Name = (string)query[PlaylistNameParameter];

		LoadCommand.Execute(null);
	}

	[RelayCommand]
	private async Task LoadAsync()
	{
		var tracks = await _repository.GetTracksAsync(PlaylistId);

		Tracks.Clear();
		foreach (var track in tracks)
		{
			Tracks.Add(track);
		}
	}

	/// <summary>
	/// Plays the whole playlist, starting at the tapped track.
	/// </summary>
	[RelayCommand]
	private Task PlayTrackAsync(PlaylistTrack track)
		=> Player.PlayPlaylistAsync(PlaylistId, track.Id);

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
