using SQLite;
using UpBeat.Models;

namespace UpBeat.Services;

/// <summary>
/// Playlist storage in a local SQLite database, in the private folder of the app.
/// </summary>
public class PlaylistRepository : IPlaylistRepository
{
	private const string DatabaseFileName = "upbeat.db3";

	private readonly SQLiteAsyncConnection _database;

	// Tables are created once, on first use
	private readonly Lazy<Task> _initialization;

	public PlaylistRepository()
	{
		var databasePath = Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);
		_database = new SQLiteAsyncConnection(databasePath);
		_initialization = new Lazy<Task>(InitializeAsync);
	}

	public async Task<IReadOnlyList<PlaylistSummary>> GetPlaylistsAsync()
	{
		await _initialization.Value;

		return await _database.QueryAsync<PlaylistSummary>(
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
		await _initialization.Value;

		var playlist = new Playlist { Name = name };
		await _database.InsertAsync(playlist);
		return playlist;
	}

	public async Task RenamePlaylistAsync(int playlistId, string name)
	{
		await _initialization.Value;
		await _database.ExecuteAsync("UPDATE Playlists SET Name = ? WHERE Id = ?", name, playlistId);
	}

	public async Task DeletePlaylistAsync(int playlistId)
	{
		await _initialization.Value;

		// Both deletions succeed or fail together
		await _database.RunInTransactionAsync(connection =>
		{
			connection.Execute("DELETE FROM PlaylistTracks WHERE PlaylistId = ?", playlistId);
			connection.Delete<Playlist>(playlistId);
		});
	}

	public async Task<IReadOnlyList<PlaylistTrack>> GetTracksAsync(int playlistId)
	{
		await _initialization.Value;

		return await _database.Table<PlaylistTrack>()
			.Where(t => t.PlaylistId == playlistId)
			.OrderBy(t => t.Position)
			.ToListAsync();
	}

	public async Task AddTrackAsync(int playlistId, Track track)
	{
		await _initialization.Value;

		var lastPosition = await _database.ExecuteScalarAsync<int>(
			"SELECT COALESCE(MAX(Position), -1) FROM PlaylistTracks WHERE PlaylistId = ?", playlistId);

		await _database.InsertAsync(new PlaylistTrack
		{
			PlaylistId = playlistId,
			TrackId = track.Id,
			Title = track.Title,
			Author = track.Author,
			Duration = track.Duration,
			Position = lastPosition + 1,
		});
	}

	public async Task RemoveTrackAsync(PlaylistTrack track)
	{
		await _initialization.Value;
		await _database.DeleteAsync(track);
	}

	public async Task SaveOrderAsync(IReadOnlyList<PlaylistTrack> tracks)
	{
		await _initialization.Value;

		await _database.RunInTransactionAsync(connection =>
		{
			for (var i = 0; i < tracks.Count; i++)
			{
				tracks[i].Position = i;
				connection.Update(tracks[i]);
			}
		});
	}

	private async Task InitializeAsync()
	{
		await _database.CreateTableAsync<Playlist>();
		await _database.CreateTableAsync<PlaylistTrack>();
	}
}
