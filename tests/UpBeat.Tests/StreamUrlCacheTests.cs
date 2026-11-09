using UpBeat.Services;

namespace UpBeat.Tests;

public class StreamUrlCacheTests
{
	private static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);

	private readonly ManualTimeProvider _time = new();

	// Number of resolutions per track ID, and IDs whose resolution fails
	private readonly Dictionary<string, int> _resolveCounts = [];
	private readonly HashSet<string> _failingIds = [];

	private StreamUrlCache CreateCache(int capacity = 10) => new(ResolveAsync, _time, capacity, Lifetime);

	private Task<string> ResolveAsync(string trackId)
	{
		_resolveCounts[trackId] = ResolveCount(trackId) + 1;

		return _failingIds.Contains(trackId)
			? Task.FromException<string>(new HttpRequestException("Network error"))
			: Task.FromResult($"https://stream/{trackId}/{ResolveCount(trackId)}");
	}

	private int ResolveCount(string trackId) => _resolveCounts.GetValueOrDefault(trackId);

	[Fact]
	public async Task A_played_track_is_not_resolved_again()
	{
		var cache = CreateCache();

		var first = await cache.GetAsync("a");
		var second = await cache.GetAsync("a");

		Assert.Equal(first, second);
		Assert.Equal(1, ResolveCount("a"));
	}

	[Fact]
	public async Task A_prefetched_track_is_not_resolved_again()
	{
		var cache = CreateCache();

		cache.Prefetch("a");
		cache.Prefetch("a");
		await cache.GetAsync("a");

		Assert.Equal(1, ResolveCount("a"));
	}

	[Fact]
	public async Task An_old_url_is_resolved_again()
	{
		var cache = CreateCache();
		var first = await cache.GetAsync("a");

		_time.Advance(Lifetime);
		var second = await cache.GetAsync("a");

		Assert.NotEqual(first, second);
		Assert.Equal(2, ResolveCount("a"));
	}

	[Fact]
	public async Task A_failed_prefetch_is_resolved_again_when_needed()
	{
		var cache = CreateCache();
		_failingIds.Add("a");
		cache.Prefetch("a");

		_failingIds.Clear();
		var url = await cache.GetAsync("a");

		Assert.Equal("https://stream/a/2", url);
	}

	[Fact]
	public async Task A_failed_resolution_is_reported()
	{
		var cache = CreateCache();
		_failingIds.Add("a");

		await Assert.ThrowsAsync<HttpRequestException>(() => cache.GetAsync("a"));
	}

	[Fact]
	public async Task An_invalidated_url_is_resolved_again()
	{
		var cache = CreateCache();
		await cache.GetAsync("a");

		cache.Invalidate("a");
		await cache.GetAsync("a");

		Assert.Equal(2, ResolveCount("a"));
	}

	[Fact]
	public async Task The_oldest_url_is_dropped_beyond_the_capacity()
	{
		var cache = CreateCache(capacity: 2);
		await cache.GetAsync("a");
		_time.Advance(TimeSpan.FromMinutes(1));
		await cache.GetAsync("b");
		_time.Advance(TimeSpan.FromMinutes(1));
		await cache.GetAsync("c");

		await cache.GetAsync("a");
		await cache.GetAsync("c");

		Assert.Equal(2, ResolveCount("a"));
		Assert.Equal(1, ResolveCount("c"));
	}

	/// <summary>
	/// A clock that only moves when the test says so.
	/// </summary>
	private sealed class ManualTimeProvider : TimeProvider
	{
		private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

		public override DateTimeOffset GetUtcNow() => _now;

		public void Advance(TimeSpan duration) => _now += duration;
	}
}
