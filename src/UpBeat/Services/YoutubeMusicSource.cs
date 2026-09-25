using UpBeat.Models;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;

namespace UpBeat.Services;

/// <summary>
/// Music source backed by YouTube, using the YoutubeExplode library.
/// </summary>
public class YoutubeMusicSource : IMusicSource
{
	private const int MaxResults = 20;

	// Thumbnails are shown small (64x36): the smallest image at least this wide is enough
	private const int MinThumbnailWidth = 160;

	private readonly YoutubeClient _youtube = new();

	// YoutubeExplode may do blocking network work (e.g. when closing HTTP responses),
	// which Android forbids on the UI thread: every call is run on a background thread.

	public Task<IReadOnlyList<Track>> SearchAsync(string query, CancellationToken cancellationToken = default)
		=> Task.Run(async () =>
		{
			var videos = await _youtube.Search
				.GetVideosAsync(query, cancellationToken)
				.CollectAsync(MaxResults);

			IReadOnlyList<Track> tracks = videos
				.Select(video => new Track(video.Id.Value, video.Title, video.Author.ChannelTitle, video.Duration, PickThumbnail(video.Thumbnails)))
				.ToList();

			return tracks;
		}, cancellationToken);

	public Task<string> GetAudioStreamUrlAsync(string trackId, CancellationToken cancellationToken = default)
		=> Task.Run(async () =>
		{
			var manifest = await _youtube.Videos.Streams.GetManifestAsync(trackId, cancellationToken);
			var audioStreams = manifest.GetAudioOnlyStreams().ToList();

			// Prefer MP4 (AAC) audio, which every Android version can play
			var stream = audioStreams.Where(s => s.Container == Container.Mp4).TryGetWithHighestBitrate()
				?? audioStreams.GetWithHighestBitrate();

			return stream.Url;
		}, cancellationToken);

	public Task<IReadOnlyList<PlaylistResult>> SearchPlaylistsAsync(string query, CancellationToken cancellationToken = default)
		=> Task.Run(async () =>
		{
			var playlists = await _youtube.Search
				.GetPlaylistsAsync(query, cancellationToken)
				.CollectAsync(MaxResults);

			IReadOnlyList<PlaylistResult> results = playlists
				.Select(playlist => new PlaylistResult(playlist.Id.Value, playlist.Title, playlist.Author?.ChannelTitle, PickThumbnail(playlist.Thumbnails)))
				.ToList();

			return results;
		}, cancellationToken);

	public Task<IReadOnlyList<Track>> GetPlaylistTracksAsync(string playlistId, CancellationToken cancellationToken = default)
		=> Task.Run(async () =>
		{
			var tracks = new List<Track>();

			// Videos are fetched page by page: collect them all
			await foreach (var video in _youtube.Playlists.GetVideosAsync(playlistId, cancellationToken))
			{
				tracks.Add(new Track(video.Id.Value, video.Title, video.Author.ChannelTitle, video.Duration, PickThumbnail(video.Thumbnails)));
			}

			IReadOnlyList<Track> result = tracks;
			return result;
		}, cancellationToken);

	/// <summary>
	/// Picks the smallest thumbnail that is still sharp enough, to keep downloads light.
	/// </summary>
	private static string? PickThumbnail(IReadOnlyList<Thumbnail> thumbnails)
		=> thumbnails
			.OrderBy(t => t.Resolution.Width)
			.FirstOrDefault(t => t.Resolution.Width >= MinThumbnailWidth)?.Url
			?? thumbnails.TryGetWithHighestResolution()?.Url;
}
