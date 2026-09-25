using UpBeat.Models;

namespace UpBeat.Services;

/// <summary>
/// Listening history stored in the local database, limited to the most recent tracks.
/// </summary>
public class HistoryRepository : IHistoryRepository
{
	private const int MaxEntries = 100;

	private readonly AppDatabase _database;

	public HistoryRepository(AppDatabase database)
	{
		_database = database;
	}

	public async Task<IReadOnlyList<HistoryEntry>> GetRecentAsync()
	{
		var db = await _database.GetConnectionAsync();

		return await db.Table<HistoryEntry>()
			.OrderByDescending(e => e.PlayedAt)
			.ToListAsync();
	}

	public async Task AddAsync(Track track)
	{
		var db = await _database.GetConnectionAsync();

		// Same track ID = same row: "insert or replace" moves an existing track to the top instead of duplicating it
		await db.InsertOrReplaceAsync(new HistoryEntry
		{
			TrackId = track.Id,
			Title = track.Title,
			Author = track.Author,
			Duration = track.Duration,
			ThumbnailUrl = track.ThumbnailUrl,
			PlayedAt = DateTime.UtcNow,
		});

		// Keep only the most recent entries
		await db.ExecuteAsync(
			"""
			DELETE FROM History
			WHERE TrackId NOT IN (SELECT TrackId FROM History ORDER BY PlayedAt DESC LIMIT ?)
			""",
			MaxEntries);
	}
}
