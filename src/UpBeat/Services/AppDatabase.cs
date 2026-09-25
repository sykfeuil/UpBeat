using SQLite;
using UpBeat.Models;

namespace UpBeat.Services;

/// <summary>
/// The local SQLite database of the app, in its private folder. Shared by every repository.
/// </summary>
public class AppDatabase
{
	private const string DatabaseFileName = "upbeat.db3";

	private readonly SQLiteAsyncConnection _connection;

	// Tables are created (or updated with new columns) once, on first use
	private readonly Lazy<Task> _initialization;

	public AppDatabase()
	{
		var databasePath = Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);
		_connection = new SQLiteAsyncConnection(databasePath);
		_initialization = new Lazy<Task>(InitializeAsync);
	}

	/// <summary>
	/// Gets the connection, once the tables are ready.
	/// </summary>
	public async Task<SQLiteAsyncConnection> GetConnectionAsync()
	{
		await _initialization.Value;
		return _connection;
	}

	private async Task InitializeAsync()
	{
		await _connection.CreateTableAsync<Playlist>();
		await _connection.CreateTableAsync<PlaylistTrack>();
		await _connection.CreateTableAsync<HistoryEntry>();
	}
}
