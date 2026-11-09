using UpBeat.Models;

namespace UpBeat.Services;

/// <summary>
/// One entry of the playback queue. The same track may be queued several times:
/// each entry is a distinct object, told apart by reference.
/// </summary>
public sealed class QueueItem(Track track, bool isSuggestion = false)
{
	/// <summary>
	/// The queued track. Only its ID is used to play it: the stream URL is resolved when needed.
	/// </summary>
	public Track Track { get; } = track;

	/// <summary>
	/// True for a track added automatically from a YouTube mix.
	/// </summary>
	public bool IsSuggestion { get; } = isSuggestion;
}

/// <summary>
/// The tracks to play, in order, with the position of the playing one.
/// Played entries are kept, so that "previous" can go back to them.
/// It is a copy: editing a playlist afterwards does not change the queue, and the other way round.
/// Not thread-safe: use it from the UI thread only.
/// </summary>
public sealed class PlaybackQueue
{
	private readonly List<QueueItem> _items = [];

	// Tracks whose mix has already been loaded, so that the same suggestions are not asked twice
	private readonly HashSet<string> _mixSeedIds = [];

	public IReadOnlyList<QueueItem> Items => _items;

	/// <summary>
	/// Position of the playing entry: -1 before the first one (empty queue, or nothing started yet).
	/// </summary>
	public int CurrentIndex { get; private set; } = -1;

	public QueueItem? Current => CurrentIndex >= 0 ? _items[CurrentIndex] : null;

	public QueueItem? Next => CurrentIndex + 1 < _items.Count ? _items[CurrentIndex + 1] : null;

	/// <summary>
	/// Number of entries after the playing one.
	/// </summary>
	public int RemainingCount => _items.Count - CurrentIndex - 1;

	/// <summary>
	/// Changes each time the queue is replaced, so that work started for an older queue (e.g. loading a mix) can be ignored.
	/// </summary>
	public int Version { get; private set; }

	/// <summary>
	/// Replaces the whole queue, and starts at the given position.
	/// </summary>
	public void Replace(IEnumerable<Track> tracks, int startIndex)
	{
		var items = tracks.Select(track => new QueueItem(track)).ToList();
		ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(startIndex, items.Count);

		_items.Clear();
		_items.AddRange(items);
		_mixSeedIds.Clear();
		CurrentIndex = startIndex;
		Version++;
	}

	/// <summary>
	/// Moves to the next entry. Returns <c>false</c> at the end of the queue.
	/// </summary>
	public bool MoveNext()
	{
		if (Next is null)
		{
			return false;
		}

		CurrentIndex++;
		return true;
	}

	/// <summary>
	/// Moves to the previous entry. Returns <c>false</c> on the first one.
	/// </summary>
	public bool MovePrevious()
	{
		if (CurrentIndex <= 0)
		{
			return false;
		}

		CurrentIndex--;
		return true;
	}

	/// <summary>
	/// Inserts a track right after the playing one ("Play next").
	/// </summary>
	public QueueItem InsertNext(Track track)
	{
		var item = new QueueItem(track);
		_items.Insert(CurrentIndex + 1, item);
		return item;
	}

	/// <summary>
	/// Adds a track after the tracks chosen by the user, but before the upcoming suggestions ("Add to queue"):
	/// otherwise, it would only be played after a whole mix.
	/// </summary>
	public QueueItem Append(Track track)
	{
		var index = CurrentIndex + 1;
		while (index < _items.Count && !_items[index].IsSuggestion)
		{
			index++;
		}

		var item = new QueueItem(track);
		_items.Insert(index, item);
		return item;
	}

	/// <summary>
	/// Adds suggested tracks at the very end, except those already in the queue (played or not).
	/// Returns the number of tracks added.
	/// </summary>
	public int AppendSuggestions(IEnumerable<Track> tracks)
	{
		var queuedIds = _items.Select(item => item.Track.Id).ToHashSet();
		var added = 0;

		foreach (var track in tracks)
		{
			// Add() also rejects a track repeated within the suggestions
			if (queuedIds.Add(track.Id))
			{
				_items.Add(new QueueItem(track, isSuggestion: true));
				added++;
			}
		}

		return added;
	}

	/// <summary>
	/// Picks the track to base the next mix on: the last entry whose mix has not been loaded yet.
	/// Returns <c>null</c> when every entry has been used.
	/// </summary>
	public Track? TakeMixSeed()
	{
		for (var index = _items.Count - 1; index >= 0; index--)
		{
			var track = _items[index].Track;
			if (_mixSeedIds.Add(track.Id))
			{
				return track;
			}
		}

		return null;
	}
}
