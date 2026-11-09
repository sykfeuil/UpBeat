namespace UpBeat.Services;

/// <summary>
/// Keeps the stream URLs of the recent tracks (played, or about to be played) for a short time,
/// so that going back to a track, or on to a prefetched one, starts without waiting.
/// URLs are kept in memory only: they expire after a few hours, so they are never stored.
/// Not thread-safe: use it from the UI thread only.
/// </summary>
public sealed class StreamUrlCache
{
	private readonly Func<string, Task<string>> _resolve;
	private readonly TimeProvider _time;
	private readonly int _capacity;
	private readonly TimeSpan _lifetime;

	// URL being resolved or resolved (null if the resolution failed), by track ID
	private readonly Dictionary<string, Entry> _entries = [];

	/// <param name="resolve">Gets the stream URL of a track from its ID.</param>
	/// <param name="time">Gives the current time (replaced in the tests).</param>
	/// <param name="capacity">Maximum number of URLs kept: the oldest one is dropped beyond.</param>
	/// <param name="lifetime">How long a URL is used: YouTube URLs expire after a few hours.</param>
	public StreamUrlCache(Func<string, Task<string>> resolve, TimeProvider time, int capacity, TimeSpan lifetime)
	{
		_resolve = resolve;
		_time = time;
		_capacity = capacity;
		_lifetime = lifetime;
	}

	/// <summary>
	/// Starts resolving the URL of a track in the background, unless a recent one is already known.
	/// A failure is not reported: the URL is simply resolved again when it is needed.
	/// </summary>
	public void Prefetch(string trackId)
	{
		if (TryGetFreshEntry(trackId) is null)
		{
			Add(trackId, TryResolveAsync(trackId));
		}
	}

	/// <summary>
	/// Gets the URL of a track: the known one if it is recent enough, otherwise a new one.
	/// Throws if it cannot be resolved.
	/// </summary>
	public async Task<string> GetAsync(string trackId)
	{
		if (TryGetFreshEntry(trackId) is { } entry && await entry.Url is { } knownUrl)
		{
			return knownUrl;
		}

		var url = await _resolve(trackId);
		Add(trackId, Task.FromResult<string?>(url));
		return url;
	}

	/// <summary>
	/// Forgets the URL of a track, e.g. because it could not be played: it will be resolved again.
	/// </summary>
	public void Invalidate(string trackId) => _entries.Remove(trackId);

	private Entry? TryGetFreshEntry(string trackId)
	{
		if (!_entries.TryGetValue(trackId, out var entry))
		{
			return null;
		}

		if (_time.GetUtcNow() - entry.RequestedAt >= _lifetime)
		{
			_entries.Remove(trackId);
			return null;
		}

		return entry;
	}

	private void Add(string trackId, Task<string?> url)
	{
		_entries[trackId] = new Entry(url, _time.GetUtcNow());

		// Drop the oldest URLs: only the recent tracks are likely to be played again
		while (_entries.Count > _capacity)
		{
			var oldest = _entries.MinBy(pair => pair.Value.RequestedAt);
			_entries.Remove(oldest.Key);
		}
	}

	private async Task<string?> TryResolveAsync(string trackId)
	{
		try
		{
			return await _resolve(trackId);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Unable to prefetch the stream URL: {ex}");
			return null;
		}
	}

	/// <summary>
	/// A URL being resolved or resolved (<c>null</c> if it failed), with the time it was asked for.
	/// </summary>
	private sealed record Entry(Task<string?> Url, DateTimeOffset RequestedAt);
}
