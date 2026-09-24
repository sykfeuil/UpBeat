namespace UpBeat.Models;

/// <summary>
/// A playlist with its number of tracks, for display in the playlists list.
/// </summary>
public class PlaylistSummary
{
	public int Id { get; set; }

	public string Name { get; set; } = string.Empty;

	public int TrackCount { get; set; }

	public string TrackCountText => TrackCount == 1 ? "1 track" : $"{TrackCount} tracks";
}
