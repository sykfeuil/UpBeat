namespace UpBeat.Audio;

/// <summary>
/// The bands of the equalizer, like the knobs of a DJ mixer.
/// </summary>
public enum EqualizerBand
{
	/// <summary>Bass (kick, bass line): below about 200 Hz.</summary>
	Low,

	/// <summary>Middle (voices, most instruments): around 1 kHz.</summary>
	Mid,

	/// <summary>Treble (cymbals, hi-hats): above about 4 kHz.</summary>
	High,
}

/// <summary>
/// Three-band equalizer (bass, middle, treble) applied to 16-bit audio.
/// The gains are set from the UI thread and applied by the audio thread.
/// </summary>
public sealed class ThreeBandEqualizer
{
	public const double MinGainDb = -12;
	public const double MaxGainDb = 12;

	private const double LowFrequency = 200;
	private const double MidFrequency = 1_000;
	private const double HighFrequency = 4_000;

	// Width of the middle band: low enough to cover a wide range, like a DJ mixer
	private const double MidQuality = 0.7;

	// Gains in hundredths of a decibel, and a counter changed with each new gain (read by the audio thread)
	private readonly int[] _gains = new int[3];
	private int _version;

	// Used by the audio thread only
	private readonly Biquad[] _filters = [new(), new(), new()];
	private int _appliedVersion = -1;
	private int _sampleRate;
	private int _channelCount;

	/// <summary>
	/// Gain of a band, in decibels, between <see cref="MinGainDb"/> and <see cref="MaxGainDb"/> (0 = unchanged).
	/// </summary>
	public double GetGain(EqualizerBand band) => Volatile.Read(ref _gains[(int)band]) / 100.0;

	public void SetGain(EqualizerBand band, double gainDb)
	{
		var gain = (int)Math.Round(Math.Clamp(gainDb, MinGainDb, MaxGainDb) * 100);
		Volatile.Write(ref _gains[(int)band], gain);
		Interlocked.Increment(ref _version);
	}

	/// <summary>
	/// True when no band is changed: the audio can then be left as it is.
	/// </summary>
	public bool IsFlat
	{
		get
		{
			for (var band = 0; band < _gains.Length; band++)
			{
				if (Volatile.Read(ref _gains[band]) != 0)
				{
					return false;
				}
			}

			return true;
		}
	}

	/// <summary>
	/// Prepares the filters for an audio format. Called by the audio thread.
	/// </summary>
	public void Configure(int sampleRate, int channelCount)
	{
		_sampleRate = sampleRate;
		_channelCount = channelCount;
		_appliedVersion = -1;

		foreach (var filter in _filters)
		{
			filter.SetChannelCount(channelCount);
		}
	}

	/// <summary>
	/// Forgets the previous samples (after a seek or a new track). Called by the audio thread.
	/// </summary>
	public void Reset()
	{
		foreach (var filter in _filters)
		{
			filter.ClearState();
		}
	}

	/// <summary>
	/// Applies the equalizer in place to 16-bit audio, with the channels interleaved. Called by the audio thread.
	/// </summary>
	public void Process(Span<short> samples)
	{
		if (_sampleRate == 0 || _channelCount == 0)
		{
			return;
		}

		var version = Volatile.Read(ref _version);
		if (version != _appliedVersion)
		{
			_filters[0].SetLowShelf(_sampleRate, LowFrequency, GetGain(EqualizerBand.Low));
			_filters[1].SetPeak(_sampleRate, MidFrequency, MidQuality, GetGain(EqualizerBand.Mid));
			_filters[2].SetHighShelf(_sampleRate, HighFrequency, GetGain(EqualizerBand.High));
			_appliedVersion = version;
		}

		for (var i = 0; i < samples.Length; i++)
		{
			var channel = i % _channelCount;
			var value = (double)samples[i];

			foreach (var filter in _filters)
			{
				value = filter.Process(value, channel);
			}

			// A boosted sound may go beyond the 16-bit range: clip it instead of wrapping around
			samples[i] = (short)Math.Clamp(Math.Round(value), short.MinValue, short.MaxValue);
		}
	}

	/// <summary>
	/// Second-order filter ("biquad"), with the formulas of Robert Bristow-Johnson's Audio EQ Cookbook.
	/// </summary>
	private sealed class Biquad
	{
		private double _b0 = 1, _b1, _b2, _a1, _a2;

		// Filter memory, per channel (transposed direct form II)
		private double[] _z1 = [];
		private double[] _z2 = [];

		public void SetChannelCount(int channelCount)
		{
			_z1 = new double[channelCount];
			_z2 = new double[channelCount];
		}

		public void ClearState()
		{
			Array.Clear(_z1);
			Array.Clear(_z2);
		}

		public double Process(double input, int channel)
		{
			var output = _b0 * input + _z1[channel];
			_z1[channel] = _b1 * input - _a1 * output + _z2[channel];
			_z2[channel] = _b2 * input - _a2 * output;
			return output;
		}

		public void SetLowShelf(double sampleRate, double frequency, double gainDb)
		{
			var (a, cos, alpha) = Prepare(sampleRate, frequency, gainDb, quality: Math.Sqrt(0.5));
			var twoSqrtAAlpha = 2 * Math.Sqrt(a) * alpha;

			Set(
				b0: a * ((a + 1) - (a - 1) * cos + twoSqrtAAlpha),
				b1: 2 * a * ((a - 1) - (a + 1) * cos),
				b2: a * ((a + 1) - (a - 1) * cos - twoSqrtAAlpha),
				a0: (a + 1) + (a - 1) * cos + twoSqrtAAlpha,
				a1: -2 * ((a - 1) + (a + 1) * cos),
				a2: (a + 1) + (a - 1) * cos - twoSqrtAAlpha);
		}

		public void SetHighShelf(double sampleRate, double frequency, double gainDb)
		{
			var (a, cos, alpha) = Prepare(sampleRate, frequency, gainDb, quality: Math.Sqrt(0.5));
			var twoSqrtAAlpha = 2 * Math.Sqrt(a) * alpha;

			Set(
				b0: a * ((a + 1) + (a - 1) * cos + twoSqrtAAlpha),
				b1: -2 * a * ((a - 1) + (a + 1) * cos),
				b2: a * ((a + 1) + (a - 1) * cos - twoSqrtAAlpha),
				a0: (a + 1) - (a - 1) * cos + twoSqrtAAlpha,
				a1: 2 * ((a - 1) - (a + 1) * cos),
				a2: (a + 1) - (a - 1) * cos - twoSqrtAAlpha);
		}

		public void SetPeak(double sampleRate, double frequency, double quality, double gainDb)
		{
			var (a, cos, alpha) = Prepare(sampleRate, frequency, gainDb, quality);

			Set(
				b0: 1 + alpha * a,
				b1: -2 * cos,
				b2: 1 - alpha * a,
				a0: 1 + alpha / a,
				a1: -2 * cos,
				a2: 1 - alpha / a);
		}

		private static (double A, double Cos, double Alpha) Prepare(double sampleRate, double frequency, double gainDb, double quality)
		{
			var omega = 2 * Math.PI * Math.Min(frequency, sampleRate * 0.45) / sampleRate;
			return (Math.Pow(10, gainDb / 40), Math.Cos(omega), Math.Sin(omega) / (2 * quality));
		}

		private void Set(double b0, double b1, double b2, double a0, double a1, double a2)
		{
			_b0 = b0 / a0;
			_b1 = b1 / a0;
			_b2 = b2 / a0;
			_a1 = a1 / a0;
			_a2 = a2 / a0;
		}
	}
}
