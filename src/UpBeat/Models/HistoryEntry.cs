using SQLite;

namespace UpBeat.Models;

/// <summary>
/// A track in the listening history. The track ID is the primary key,
/// so a track appears only once: playing it again only updates <see cref="PlayedAt"/>.
/// </summary>
[Table("History")]
public class HistoryEntry
{
	[PrimaryKey]
	public string TrackId { get; set; } = string.Empty;

	[NotNull]
	public string Title { get; set; } = string.Empty;

	[NotNull]
	public string Author { get; set; } = string.Empty;

	public TimeSpan? Duration { get; set; }

	public string? ThumbnailUrl { get; set; }

	/// <summary>Last time the track was played (UTC).</summary>
	[Indexed]
	public DateTime PlayedAt { get; set; }

	public Track ToTrack() => new(TrackId, Title, Author, Duration, ThumbnailUrl);
}
