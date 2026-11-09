using UpBeat.Models;
using UpBeat.Services;

namespace UpBeat.Tests;

public class PlaybackQueueTests
{
	private static Track Track(string id) => new(id, $"Title {id}", "Author", TimeSpan.FromMinutes(3), null);

	private static Track[] Tracks(params string[] ids) => ids.Select(Track).ToArray();

	private static string[] Ids(PlaybackQueue queue) => queue.Items.Select(item => item.Track.Id).ToArray();

	[Fact]
	public void New_queue_is_empty()
	{
		var queue = new PlaybackQueue();

		Assert.Null(queue.Current);
		Assert.Null(queue.Next);
		Assert.Equal(-1, queue.CurrentIndex);
		Assert.Equal(0, queue.RemainingCount);
		Assert.False(queue.MoveNext());
		Assert.False(queue.MovePrevious());
	}

	[Fact]
	public void Replace_starts_at_the_given_track()
	{
		var queue = new PlaybackQueue();

		queue.Replace(Tracks("a", "b", "c"), startIndex: 1);

		Assert.Equal("b", queue.Current?.Track.Id);
		Assert.Equal("c", queue.Next?.Track.Id);
		Assert.Equal(1, queue.RemainingCount);
	}

	[Fact]
	public void Replace_rejects_a_start_outside_the_tracks()
	{
		var queue = new PlaybackQueue();

		Assert.Throws<ArgumentOutOfRangeException>(() => queue.Replace(Tracks("a"), startIndex: 1));
		Assert.Throws<ArgumentOutOfRangeException>(() => queue.Replace([], startIndex: 0));
	}

	[Fact]
	public void Replace_changes_the_version()
	{
		var queue = new PlaybackQueue();
		queue.Replace(Tracks("a"), 0);
		var version = queue.Version;

		queue.Replace(Tracks("b"), 0);

		Assert.NotEqual(version, queue.Version);
	}

	[Fact]
	public void MoveNext_stops_at_the_end()
	{
		var queue = new PlaybackQueue();
		queue.Replace(Tracks("a", "b"), 0);

		Assert.True(queue.MoveNext());
		Assert.False(queue.MoveNext());
		Assert.Equal("b", queue.Current?.Track.Id);
	}

	[Fact]
	public void MovePrevious_goes_back_to_played_tracks_and_stops_at_the_first()
	{
		var queue = new PlaybackQueue();
		queue.Replace(Tracks("a", "b"), 1);

		Assert.True(queue.MovePrevious());
		Assert.Equal("a", queue.Current?.Track.Id);
		Assert.False(queue.MovePrevious());
		Assert.Equal("a", queue.Current?.Track.Id);
	}

	[Fact]
	public void InsertNext_plays_the_track_right_after_the_current_one()
	{
		var queue = new PlaybackQueue();
		queue.Replace(Tracks("a", "b", "c"), 0);

		queue.InsertNext(Track("x"));
		queue.InsertNext(Track("y"));

		// The last inserted track is played first, like "Play next" in most players
		Assert.Equal(["a", "y", "x", "b", "c"], Ids(queue));
		Assert.Equal("a", queue.Current?.Track.Id);
	}

	[Fact]
	public void Append_adds_after_the_chosen_tracks_but_before_the_suggestions()
	{
		var queue = new PlaybackQueue();
		queue.Replace(Tracks("a", "b"), 0);
		queue.AppendSuggestions(Tracks("s1", "s2"));

		queue.Append(Track("x"));
		queue.Append(Track("y"));

		Assert.Equal(["a", "b", "x", "y", "s1", "s2"], Ids(queue));
	}

	[Fact]
	public void Append_adds_at_the_end_when_there_is_no_suggestion()
	{
		var queue = new PlaybackQueue();
		queue.Replace(Tracks("a", "b"), 1);

		queue.Append(Track("x"));

		Assert.Equal(["a", "b", "x"], Ids(queue));
		Assert.Equal("x", queue.Next?.Track.Id);
	}

	[Fact]
	public void Append_while_playing_a_suggestion_adds_right_after_it()
	{
		var queue = new PlaybackQueue();
		queue.Replace(Tracks("a"), 0);
		queue.AppendSuggestions(Tracks("s1", "s2"));
		queue.MoveNext();

		queue.Append(Track("x"));

		Assert.Equal(["a", "s1", "x", "s2"], Ids(queue));
	}

	[Fact]
	public void The_same_track_can_be_queued_twice_as_distinct_entries()
	{
		var queue = new PlaybackQueue();
		queue.Replace(Tracks("a"), 0);

		var first = queue.Append(Track("x"));
		var second = queue.Append(Track("x"));

		Assert.NotSame(first, second);
		Assert.Equal(["a", "x", "x"], Ids(queue));
	}

	[Fact]
	public void AppendSuggestions_skips_tracks_already_queued_or_repeated()
	{
		var queue = new PlaybackQueue();
		queue.Replace(Tracks("a", "b"), 1);

		var added = queue.AppendSuggestions(Tracks("a", "s1", "b", "s1", "s2"));

		Assert.Equal(2, added);
		Assert.Equal(["a", "b", "s1", "s2"], Ids(queue));
		Assert.All(queue.Items.Skip(2), item => Assert.True(item.IsSuggestion));
		Assert.All(queue.Items.Take(2), item => Assert.False(item.IsSuggestion));
	}

	[Fact]
	public void TakeMixSeed_uses_the_last_tracks_first_and_each_track_once()
	{
		var queue = new PlaybackQueue();
		queue.Replace(Tracks("a", "b"), 0);

		Assert.Equal("b", queue.TakeMixSeed()?.Id);
		Assert.Equal("a", queue.TakeMixSeed()?.Id);
		Assert.Null(queue.TakeMixSeed());

		// New suggestions bring new seeds
		queue.AppendSuggestions(Tracks("s1"));
		Assert.Equal("s1", queue.TakeMixSeed()?.Id);
	}

	[Fact]
	public void Replace_forgets_the_used_mix_seeds()
	{
		var queue = new PlaybackQueue();
		queue.Replace(Tracks("a"), 0);
		queue.TakeMixSeed();

		queue.Replace(Tracks("a"), 0);

		Assert.Equal("a", queue.TakeMixSeed()?.Id);
	}
}
