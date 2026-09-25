using UpBeat.Models;

namespace UpBeat.Services;

/// <summary>
/// Stores playlists and their tracks.
/// </summary>
public interface IPlaylistRepository
{
	/// <summary>
	/// Raised with the playlist ID when the tracks of a playlist change (added, removed, reordered, deleted).
	/// </summary>
	event EventHandler<int>? PlaylistChanged;

	Task<IReadOnlyList<PlaylistSummary>> GetPlaylistsAsync();

	Task<Playlist> CreatePlaylistAsync(string name);

	/// <summary>Creates a playlist with all the given tracks, in order.</summary>
	Task<Playlist> ImportPlaylistAsync(string name, IReadOnlyList<Track> tracks);

	Task RenamePlaylistAsync(int playlistId, string name);

	/// <summary>Deletes a playlist and all its tracks.</summary>
	Task DeletePlaylistAsync(int playlistId);

	/// <summary>Gets the tracks of a playlist, in order.</summary>
	Task<IReadOnlyList<PlaylistTrack>> GetTracksAsync(int playlistId);

	/// <summary>Adds a track at the end of a playlist.</summary>
	Task AddTrackAsync(int playlistId, Track track);

	Task RemoveTrackAsync(PlaylistTrack track);

	/// <summary>Saves the order of the tracks of a playlist, as given by the list.</summary>
	Task SaveOrderAsync(IReadOnlyList<PlaylistTrack> tracks);
}
