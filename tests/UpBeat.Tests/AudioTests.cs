using UpBeat.Audio;

namespace UpBeat.Tests;

public class SpectrumAnalyzerTests
{
	private const int SampleRate = 48_000;
	private const int BandCount = 16;

	private static short[] Sine(double frequency, double amplitude, int frameCount, int channelCount = 2)
	{
		var samples = new short[frameCount * channelCount];
		for (var frame = 0; frame < frameCount; frame++)
		{
			var value = (short)(amplitude * short.MaxValue * Math.Sin(2 * Math.PI * frequency * frame / SampleRate));
			for (var channel = 0; channel < channelCount; channel++)
			{
				samples[frame * channelCount + channel] = value;
			}
		}

		return samples;
	}

	private static SpectrumAnalyzer AnalyzerWith(short[] stereoSamples, TimeSpan startPosition = default)
	{
		var analyzer = new SpectrumAnalyzer();
		analyzer.SetNextStartPosition(startPosition);
		analyzer.Reset(SampleRate, channelCount: 2);
		analyzer.Write(stereoSamples);
		return analyzer;
	}

	[Fact]
	public void Nothing_is_known_before_audio_is_received()
	{
		var analyzer = new SpectrumAnalyzer();

		Assert.False(analyzer.TryGetLevels(TimeSpan.FromSeconds(1), new float[BandCount]));
	}

	[Fact]
	public void A_sine_lights_the_band_of_its_frequency_most()
	{
		var analyzer = AnalyzerWith(Sine(1_000, 0.5, SampleRate));
		var levels = new float[BandCount];

		Assert.True(analyzer.TryGetLevels(TimeSpan.FromMilliseconds(500), levels));

		var loudestBand = Array.IndexOf(levels, levels.Max());
		var lowFrequency = 40 * Math.Pow(16_000 / 40.0, (double)loudestBand / BandCount);
		var highFrequency = 40 * Math.Pow(16_000 / 40.0, (double)(loudestBand + 1) / BandCount);
		Assert.InRange(1_000, lowFrequency, highFrequency);

		// A half-scale sine is at -6 dB: about 0.9 on a 50 dB scale
		Assert.InRange(levels[loudestBand], 0.85f, 0.95f);
		Assert.True(levels[0] < 0.5f);
	}

	[Fact]
	public void Silence_gives_empty_bands()
	{
		var analyzer = AnalyzerWith(new short[SampleRate * 2]);
		var levels = new float[BandCount];

		Assert.True(analyzer.TryGetLevels(TimeSpan.FromMilliseconds(500), levels));
		Assert.All(levels, level => Assert.Equal(0, level));
	}

	[Fact]
	public void Audio_after_a_seek_is_found_at_the_seek_position()
	{
		var analyzer = AnalyzerWith(Sine(1_000, 0.5, SampleRate), startPosition: TimeSpan.FromSeconds(10));
		var levels = new float[BandCount];

		Assert.True(analyzer.TryGetLevels(TimeSpan.FromSeconds(10.5), levels));
		Assert.False(analyzer.TryGetLevels(TimeSpan.FromSeconds(9), levels));
	}

	[Fact]
	public void Audio_not_received_yet_is_unknown()
	{
		var analyzer = AnalyzerWith(Sine(1_000, 0.5, SampleRate));

		Assert.False(analyzer.TryGetLevels(TimeSpan.FromSeconds(2), new float[BandCount]));
	}
}

public class ThreeBandEqualizerTests
{
	private const int SampleRate = 48_000;

	private static short[] Sine(double frequency, double amplitude = 0.2)
	{
		// One second, mono
		var samples = new short[SampleRate];
		for (var i = 0; i < samples.Length; i++)
		{
			samples[i] = (short)(amplitude * short.MaxValue * Math.Sin(2 * Math.PI * frequency * i / SampleRate));
		}

		return samples;
	}

	// Measured on the second half, once the filters have settled
	private static double Rms(short[] samples) =>
		Math.Sqrt(samples.Skip(samples.Length / 2).Average(sample => (double)sample * sample));

	private static double GainOf(ThreeBandEqualizer equalizer, double frequency)
	{
		var input = Sine(frequency);
		var output = (short[])input.Clone();

		equalizer.Configure(SampleRate, channelCount: 1);
		equalizer.Process(output);

		return Rms(output) / Rms(input);
	}

	[Fact]
	public void A_flat_equalizer_leaves_the_audio_unchanged()
	{
		var equalizer = new ThreeBandEqualizer();
		var input = Sine(440);
		var output = (short[])input.Clone();

		equalizer.Configure(SampleRate, channelCount: 1);
		equalizer.Process(output);

		Assert.True(equalizer.IsFlat);
		Assert.Equal(input, output);
	}

	[Fact]
	public void Boosting_the_bass_raises_low_frequencies_only()
	{
		var equalizer = new ThreeBandEqualizer();
		equalizer.SetGain(EqualizerBand.Low, 12);

		// +12 dB is about 4 times louder
		Assert.InRange(GainOf(equalizer, 50), 3.5, 4.2);
		Assert.InRange(GainOf(equalizer, 8_000), 0.95, 1.1);
	}

	[Fact]
	public void Cutting_the_treble_lowers_high_frequencies_only()
	{
		var equalizer = new ThreeBandEqualizer();
		equalizer.SetGain(EqualizerBand.High, -12);

		Assert.InRange(GainOf(equalizer, 12_000), 0.24, 0.3);
		Assert.InRange(GainOf(equalizer, 100), 0.95, 1.05);
	}

	[Fact]
	public void Boosting_the_middle_raises_1_kHz()
	{
		var equalizer = new ThreeBandEqualizer();
		equalizer.SetGain(EqualizerBand.Mid, 6);

		// +6 dB is about 2 times louder
		Assert.InRange(GainOf(equalizer, 1_000), 1.9, 2.1);
	}

	[Fact]
	public void Gains_are_kept_within_the_range()
	{
		var equalizer = new ThreeBandEqualizer();

		equalizer.SetGain(EqualizerBand.Low, 40);
		equalizer.SetGain(EqualizerBand.High, -40);

		Assert.Equal(ThreeBandEqualizer.MaxGainDb, equalizer.GetGain(EqualizerBand.Low));
		Assert.Equal(ThreeBandEqualizer.MinGainDb, equalizer.GetGain(EqualizerBand.High));
		Assert.False(equalizer.IsFlat);
	}

	[Fact]
	public void A_boost_beyond_full_scale_is_clipped_instead_of_wrapping_around()
	{
		var equalizer = new ThreeBandEqualizer();
		equalizer.SetGain(EqualizerBand.Low, 12);
		var samples = Sine(50, amplitude: 0.9);
		var input = (short[])samples.Clone();

		equalizer.Configure(SampleRate, channelCount: 1);
		equalizer.Process(samples);

		// Wrapping around would flip the sign of the loudest samples
		Assert.Contains(short.MaxValue, samples);
		for (var i = samples.Length / 2; i < samples.Length; i++)
		{
			if (Math.Abs((int)input[i]) > 10_000)
			{
				Assert.Equal(Math.Sign(input[i]), Math.Sign(samples[i]));
			}
		}
	}
}
