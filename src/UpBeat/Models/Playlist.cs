using SQLite;

namespace UpBeat.Models;

/// <summary>
/// A playlist stored in the local database.
/// </summary>
[Table("Playlists")]
public class Playlist
{
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }

	[NotNull]
	public string Name { get; set; } = string.Empty;
}
