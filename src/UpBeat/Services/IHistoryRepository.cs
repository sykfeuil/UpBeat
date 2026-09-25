using UpBeat.Models;

namespace UpBeat.Services;

/// <summary>
/// Stores the recently played tracks.
/// </summary>
public interface IHistoryRepository
{
	/// <summary>Gets the recently played tracks, the most recent first.</summary>
	Task<IReadOnlyList<HistoryEntry>> GetRecentAsync();

	/// <summary>Adds a track to the history, or moves it to the top if it is already there.</summary>
	Task AddAsync(Track track);
}
