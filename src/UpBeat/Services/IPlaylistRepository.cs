using UpBeat.Models;

namespace UpBeat.Services;

/// <summary>
/// Stores playlists and their tracks.
/// </summary>
public interface IPlaylistRepository
{
	Task<IReadOnlyList<PlaylistSummary>> GetPlaylistsAsync();

	Task<Playlist> CreatePlaylistAsync(string name);

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
