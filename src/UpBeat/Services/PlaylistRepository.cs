using SQLite;
using UpBeat.Models;

namespace UpBeat.Services;

/// <summary>
/// Playlists stored in the local database.
/// </summary>
public class PlaylistRepository : IPlaylistRepository
{
	private readonly AppDatabase _database;

	public PlaylistRepository(AppDatabase database)
	{
		_database = database;
	}

	public async Task<IReadOnlyList<PlaylistSummary>> GetPlaylistsAsync()
	{
		var db = await _database.GetConnectionAsync();

		return await db.QueryAsync<PlaylistSummary>(
			"""
			SELECT p.Id, p.Name, COUNT(t.Id) AS TrackCount
			FROM Playlists p
			LEFT JOIN PlaylistTracks t ON t.PlaylistId = p.Id
			GROUP BY p.Id, p.Name
			ORDER BY p.Name COLLATE NOCASE
			""");
	}

	public async Task<Playlist> CreatePlaylistAsync(string name)
	{
		var db = await _database.GetConnectionAsync();

		var playlist = new Playlist { Name = name };
		await db.InsertAsync(playlist);
		return playlist;
	}

	public async Task<Playlist> ImportPlaylistAsync(string name, IReadOnlyList<Track> tracks)
	{
		var db = await _database.GetConnectionAsync();
		var playlist = new Playlist { Name = name };

		// The playlist and all its tracks are created together, or not at all
		await db.RunInTransactionAsync(connection =>
		{
			connection.Insert(playlist);
			connection.InsertAll(tracks.Select((track, index) => ToPlaylistTrack(playlist.Id, track, index)), runInTransaction: false);
		});

		return playlist;
	}

	public async Task RenamePlaylistAsync(int playlistId, string name)
	{
		var db = await _database.GetConnectionAsync();
		await db.ExecuteAsync("UPDATE Playlists SET Name = ? WHERE Id = ?", name, playlistId);
	}

	public async Task DeletePlaylistAsync(int playlistId)
	{
		var db = await _database.GetConnectionAsync();

		// Both deletions succeed or fail together
		await db.RunInTransactionAsync(connection =>
		{
			connection.Execute("DELETE FROM PlaylistTracks WHERE PlaylistId = ?", playlistId);
			connection.Delete<Playlist>(playlistId);
		});
	}

	public async Task<IReadOnlyList<PlaylistTrack>> GetTracksAsync(int playlistId)
	{
		var db = await _database.GetConnectionAsync();

		return await db.Table<PlaylistTrack>()
			.Where(t => t.PlaylistId == playlistId)
			.OrderBy(t => t.Position)
			.ToListAsync();
	}

	public async Task AddTrackAsync(int playlistId, Track track)
	{
		var db = await _database.GetConnectionAsync();

		var lastPosition = await db.ExecuteScalarAsync<int>(
			"SELECT COALESCE(MAX(Position), -1) FROM PlaylistTracks WHERE PlaylistId = ?", playlistId);

		await db.InsertAsync(ToPlaylistTrack(playlistId, track, lastPosition + 1));
	}

	public async Task RemoveTrackAsync(PlaylistTrack track)
	{
		var db = await _database.GetConnectionAsync();
		await db.DeleteAsync(track);
	}

	public async Task SaveOrderAsync(IReadOnlyList<PlaylistTrack> tracks)
	{
		var db = await _database.GetConnectionAsync();

		await db.RunInTransactionAsync(connection =>
		{
			for (var i = 0; i < tracks.Count; i++)
			{
				tracks[i].Position = i;
				connection.Update(tracks[i]);
			}
		});
	}

	private static PlaylistTrack ToPlaylistTrack(int playlistId, Track track, int position) => new()
	{
		PlaylistId = playlistId,
		TrackId = track.Id,
		Title = track.Title,
		Author = track.Author,
		Duration = track.Duration,
		ThumbnailUrl = track.ThumbnailUrl,
		Position = position,
	};
}
