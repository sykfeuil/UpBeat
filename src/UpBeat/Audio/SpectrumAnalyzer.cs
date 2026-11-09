namespace UpBeat.Audio;

/// <summary>
/// Turns the played audio into levels per frequency band, for the visualizer.
/// The audio is received ahead of time (it is processed before it reaches the speaker),
/// so it is kept for a few seconds and analyzed at the position actually heard.
/// Thread-safe: the audio thread writes, the UI thread reads.
/// </summary>
public sealed class SpectrumAnalyzer
{
	/// <summary>Number of samples analyzed at once: about 40 ms of audio, with a resolution of about 20 Hz.</summary>
	public const int FftSize = 2048;

	// Lowest and highest frequencies shown
	private const double MinFrequency = 40;
	private const double MaxFrequency = 16_000;

	// Sound levels shown, in decibels relative to a full-scale sine: quieter is empty, louder is full
	private const double MinDecibels = -50;
	private const double MaxDecibels = 0;

	// Slope added to the levels: 0 at the pivot, more above, less below
	private const double TiltPerOctave = 3;
	private const double TiltPivotFrequency = 1_000;

	// Power of a sine summed over its FFT bins, relative to its peak bin, with a Hann window
	private const double HannPowerSpread = 1.5;

	// Mono audio kept, in frames: about 2.7 s at 48 kHz, more than the delay before the speaker
	private const int HistorySize = 1 << 17;

	private readonly object _lock = new();
	private readonly float[] _history = new float[HistorySize];
	private long _writtenFrames;
	private int _sampleRate;
	private int _channelCount;

	// Position in the track of the first frame received since the last reset (after a seek or a new track)
	private double _startPositionMs;
	private double _nextStartPositionMs;

	// FFT work buffers, used by the reading thread only
	private readonly float[] _window = CreateHannWindow(FftSize);
	private readonly float _windowSum;
	private readonly float[] _real = new float[FftSize];
	private readonly float[] _imaginary = new float[FftSize];

	public SpectrumAnalyzer()
	{
		_windowSum = _window.Sum();
	}

	/// <summary>
	/// Tells where the audio received after the next reset starts in the track (0 for a new track, or the seek position).
	/// </summary>
	public void SetNextStartPosition(TimeSpan position)
	{
		lock (_lock)
		{
			_nextStartPositionMs = position.TotalMilliseconds;
		}
	}

	/// <summary>
	/// Forgets the audio received so far: the next audio starts at the position given by <see cref="SetNextStartPosition"/>.
	/// </summary>
	public void Reset(int sampleRate, int channelCount)
	{
		lock (_lock)
		{
			_sampleRate = sampleRate;
			_channelCount = channelCount;
			_writtenFrames = 0;
			_startPositionMs = _nextStartPositionMs;
		}
	}

	/// <summary>
	/// Adds 16-bit audio, with the channels interleaved (left, right, left, right…).
	/// </summary>
	public void Write(ReadOnlySpan<short> samples)
	{
		lock (_lock)
		{
			var channelCount = Math.Max(1, _channelCount);
			for (var i = 0; i + channelCount <= samples.Length; i += channelCount)
			{
				// Mix the channels into one, from -1 to 1
				var sum = 0f;
				for (var channel = 0; channel < channelCount; channel++)
				{
					sum += samples[i + channel];
				}

				_history[(int)(_writtenFrames % HistorySize)] = sum / (channelCount * 32768f);
				_writtenFrames++;
			}
		}
	}

	/// <summary>
	/// Fills the level of each band (from 0 to 1, low frequencies first) for the audio heard at the given position.
	/// Returns <c>false</c> if this audio is not known (nothing played yet, or not received yet).
	/// </summary>
	public bool TryGetLevels(TimeSpan position, Span<float> levels)
	{
		int sampleRate;

		lock (_lock)
		{
			sampleRate = _sampleRate;
			if (sampleRate == 0)
			{
				return false;
			}

			// The frames that end at the position heard
			var endFrame = (long)((position.TotalMilliseconds - _startPositionMs) * sampleRate / 1000);
			var startFrame = endFrame - FftSize;
			if (startFrame < 0 || endFrame > _writtenFrames || startFrame < _writtenFrames - HistorySize)
			{
				return false;
			}

			for (var i = 0; i < FftSize; i++)
			{
				_real[i] = _history[(int)((startFrame + i) % HistorySize)] * _window[i];
				_imaginary[i] = 0;
			}
		}

		Fft(_real, _imaginary);
		ComputeBandLevels(sampleRate, levels);
		return true;
	}

	private void ComputeBandLevels(int sampleRate, Span<float> levels)
	{
		var maxFrequency = Math.Min(MaxFrequency, sampleRate / 2.0);
		var binWidth = (double)sampleRate / FftSize;

		for (var band = 0; band < levels.Length; band++)
		{
			// Bands are spread evenly on a logarithmic scale, like the notes of a piano
			var lowFrequency = MinFrequency * Math.Pow(maxFrequency / MinFrequency, (double)band / levels.Length);
			var highFrequency = MinFrequency * Math.Pow(maxFrequency / MinFrequency, (double)(band + 1) / levels.Length);
			var lowBin = Math.Max(1, (int)(lowFrequency / binWidth));
			var highBin = Math.Min(FftSize / 2, Math.Max(lowBin + 1, (int)Math.Ceiling(highFrequency / binWidth)));

			// Energy of the whole band: wide bands (treble) gather many frequencies
			var power = 0.0;
			for (var bin = lowBin; bin < highBin; bin++)
			{
				power += _real[bin] * _real[bin] + _imaginary[bin] * _imaginary[bin];
			}

			// Amplitude of the matching sine (1 = full scale): the Hann window spreads a sine over 1.5 bins of power
			var amplitude = 2 * Math.Sqrt(power / HannPowerSpread) / _windowSum;

			// Music is much louder in the bass than in the treble: tilt the levels so that every band moves
			var centerFrequency = Math.Sqrt(lowFrequency * highFrequency);
			var tilt = TiltPerOctave * Math.Log2(centerFrequency / TiltPivotFrequency);

			var decibels = 20 * Math.Log10(Math.Max(amplitude, 1e-9)) + tilt;
			levels[band] = (float)Math.Clamp((decibels - MinDecibels) / (MaxDecibels - MinDecibels), 0, 1);
		}
	}

	/// <summary>
	/// In-place fast Fourier transform (iterative radix-2). The length must be a power of 2.
	/// </summary>
	private static void Fft(float[] real, float[] imaginary)
	{
		var n = real.Length;

		// Bit-reversal permutation
		for (int i = 1, j = 0; i < n; i++)
		{
			var bit = n >> 1;
			for (; (j & bit) != 0; bit >>= 1)
			{
				j ^= bit;
			}

			j ^= bit;
			if (i < j)
			{
				(real[i], real[j]) = (real[j], real[i]);
				(imaginary[i], imaginary[j]) = (imaginary[j], imaginary[i]);
			}
		}

		for (var length = 2; length <= n; length <<= 1)
		{
			var angle = -2 * Math.PI / length;
			var stepReal = (float)Math.Cos(angle);
			var stepImaginary = (float)Math.Sin(angle);

			for (var start = 0; start < n; start += length)
			{
				var wReal = 1f;
				var wImaginary = 0f;

				for (var k = 0; k < length / 2; k++)
				{
					var even = start + k;
					var odd = even + length / 2;
					var oddReal = real[odd] * wReal - imaginary[odd] * wImaginary;
					var oddImaginary = real[odd] * wImaginary + imaginary[odd] * wReal;

					real[odd] = real[even] - oddReal;
					imaginary[odd] = imaginary[even] - oddImaginary;
					real[even] += oddReal;
					imaginary[even] += oddImaginary;

					(wReal, wImaginary) = (wReal * stepReal - wImaginary * stepImaginary, wReal * stepImaginary + wImaginary * stepReal);
				}
			}
		}
	}

	/// <summary>
	/// Hann window: fades the analyzed samples in and out, so that cutting them does not add false frequencies.
	/// </summary>
	private static float[] CreateHannWindow(int size)
	{
		var window = new float[size];
		for (var i = 0; i < size; i++)
		{
			window[i] = (float)(0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (size - 1)));
		}

		return window;
	}
}
