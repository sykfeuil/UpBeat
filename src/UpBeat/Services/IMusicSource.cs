using UpBeat.Models;

namespace UpBeat.Services;

/// <summary>
/// A source of music tracks that can be searched and played.
/// </summary>
public interface IMusicSource
{
	/// <summary>
	/// Searches tracks matching the given query.
	/// </summary>
	Task<IReadOnlyList<Track>> SearchAsync(string query, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the URL of the audio stream of a track.
	/// The URL expires after a few hours, so it must never be stored.
	/// </summary>
	Task<string> GetAudioStreamUrlAsync(string trackId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Searches playlists matching the given query.
	/// </summary>
	Task<IReadOnlyList<PlaylistResult>> SearchPlaylistsAsync(string query, CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all the tracks of a playlist.
	/// </summary>
	Task<IReadOnlyList<Track>> GetPlaylistTracksAsync(string playlistId, CancellationToken cancellationToken = default);
}
