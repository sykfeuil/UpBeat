using UpBeat.Services;

namespace UpBeat.Controls;

/// <summary>
/// Pixel-art spectrum of the playing audio: one column of blocks per frequency band (bass on the left),
/// with a block that marks the recent peak and falls slowly.
/// Call <see cref="Start"/> and <see cref="Stop"/> when it is shown and hidden, to save the battery.
/// </summary>
public class SpectrumVisualizer : GraphicsView, IDrawable
{
	private const int BandCount = 16;
	private const int BlockRows = 14;
	private const float BlockGap = 4;

	// Animation: about 30 frames per second
	private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(33);

	// Per frame: how much a bar falls, how long a peak stays, then how fast it falls
	private const float BarFall = 0.05f;
	private const int PeakHoldFrames = 15;
	private const float PeakFall = 0.02f;

	private readonly Color _barColor = (Color)Application.Current!.Resources["NeonRed"];
	private readonly Color _peakColor = (Color)Application.Current!.Resources["NeonPurple"];
	private readonly Color _emptyColor = ((Color)Application.Current!.Resources["Muted"]).WithAlpha(0.2f);

	private readonly float[] _newLevels = new float[BandCount];
	private readonly float[] _levels = new float[BandCount];
	private readonly float[] _peaks = new float[BandCount];
	private readonly int[] _peakHolds = new int[BandCount];

	private AudioPlayerService? _audioPlayer;
	private IDispatcherTimer? _timer;

	public SpectrumVisualizer()
	{
		Drawable = this;
		InputTransparent = true;
	}

	/// <summary>
	/// Starts animating.
	/// </summary>
	public void Start()
	{
		_audioPlayer ??= IPlatformApplication.Current?.Services.GetService<AudioPlayerService>();

		if (_timer is null)
		{
			_timer = Dispatcher.CreateTimer();
			_timer.Interval = FrameInterval;
			_timer.Tick += (_, _) => OnFrame();
		}

		_timer.Start();
	}

	/// <summary>
	/// Stops animating.
	/// </summary>
	public void Stop() => _timer?.Stop();

	private void OnFrame()
	{
		// While paused (or loading), the bars fall back to nothing
		var hasAudio = _audioPlayer is { IsPlaying: true }
			&& _audioPlayer.Spectrum.TryGetLevels(_audioPlayer.CurrentPosition, _newLevels);

		var isMoving = false;

		for (var band = 0; band < BandCount; band++)
		{
			var level = hasAudio ? _newLevels[band] : 0;

			// Rise at once, fall slowly: the bars stay readable
			_levels[band] = Math.Max(level, _levels[band] - BarFall);

			if (_levels[band] >= _peaks[band])
			{
				_peaks[band] = _levels[band];
				_peakHolds[band] = PeakHoldFrames;
			}
			else if (_peakHolds[band] > 0)
			{
				_peakHolds[band]--;
			}
			else
			{
				_peaks[band] = Math.Max(0, _peaks[band] - PeakFall);
			}

			isMoving |= _levels[band] > 0 || _peaks[band] > 0;
		}

		// Nothing to redraw once everything has fallen
		if (isMoving || hasAudio)
		{
			Invalidate();
		}
	}

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		var blockWidth = (dirtyRect.Width - BlockGap * (BandCount - 1)) / BandCount;
		var blockHeight = (dirtyRect.Height - BlockGap * (BlockRows - 1)) / BlockRows;
		if (blockWidth <= 0 || blockHeight <= 0)
		{
			return;
		}

		// Sharp pixels: no smoothing
		canvas.Antialias = false;

		for (var band = 0; band < BandCount; band++)
		{
			var litBlocks = (int)Math.Round(_levels[band] * BlockRows);
			var peakBlock = Math.Min(BlockRows - 1, (int)Math.Round(_peaks[band] * BlockRows) - 1);
			var left = dirtyRect.Left + band * (blockWidth + BlockGap);

			for (var row = 0; row < BlockRows; row++)
			{
				canvas.FillColor = row < litBlocks ? _barColor
					: row == peakBlock ? _peakColor
					: _emptyColor;

				// Row 0 is at the bottom
				var top = dirtyRect.Bottom - (row + 1) * blockHeight - row * BlockGap;
				canvas.FillRectangle(left, top, blockWidth, blockHeight);
			}
		}
	}
}
