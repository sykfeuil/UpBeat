using SQLite;

namespace UpBeat.Models;

/// <summary>
/// A track saved in a playlist. Only the track information is stored,
/// never the audio stream URL, which expires.
/// </summary>
[Table("PlaylistTracks")]
public class PlaylistTrack
{
	[PrimaryKey, AutoIncrement]
	public int Id { get; set; }

	[Indexed]
	public int PlaylistId { get; set; }

	/// <summary>Identifier of the track on its source (e.g. the YouTube video ID).</summary>
	[NotNull]
	public string TrackId { get; set; } = string.Empty;

	[NotNull]
	public string Title { get; set; } = string.Empty;

	[NotNull]
	public string Author { get; set; } = string.Empty;

	public TimeSpan? Duration { get; set; }

	public string? ThumbnailUrl { get; set; }

	/// <summary>Order of the track in its playlist (0 = first).</summary>
	public int Position { get; set; }

	public Track ToTrack() => new(TrackId, Title, Author, Duration, ThumbnailUrl);
}
