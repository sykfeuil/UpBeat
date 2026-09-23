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
				.Select(video => new Track(video.Id.Value, video.Title, video.Author.ChannelTitle, video.Duration))
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
}
