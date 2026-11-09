using System.Runtime.InteropServices;
using Android.Content;
using AndroidX.Media3.Common;
using AndroidX.Media3.Common.Audio;
using AndroidX.Media3.ExoPlayer;
using AndroidX.Media3.ExoPlayer.Audio;
using Java.Nio;
using UpBeat.Audio;

namespace UpBeat;

/// <summary>
/// Gives ExoPlayer an audio output that goes through the app's audio processors (equalizer, spectrum)
/// before reaching the speaker. They see the decoded audio, so no permission is needed.
/// </summary>
public sealed class EffectsRenderersFactory(Context context, IAudioProcessor[] audioProcessors) : DefaultRenderersFactory(context)
{
	protected override IAudioSink? BuildAudioSink(Context? context, bool enableFloatOutput, bool enableAudioTrackPlaybackParams)
		=> new DefaultAudioSink.Builder(context!)
			.SetAudioProcessors(audioProcessors)!
			.Build();
}

/// <summary>
/// Applies the <see cref="ThreeBandEqualizer"/> to the audio played by ExoPlayer (16-bit audio only).
/// Runs on the audio thread.
/// </summary>
public sealed class EqualizerAudioProcessor(ThreeBandEqualizer equalizer) : BaseAudioProcessor
{
	private byte[] _buffer = [];

	protected override AudioProcessorAudioFormat? OnConfigure(AudioProcessorAudioFormat? inputAudioFormat)
	{
		if (inputAudioFormat is null || inputAudioFormat.Encoding != C.EncodingPcm16bit)
		{
			throw new AudioProcessorUnhandledAudioFormatException(inputAudioFormat);
		}

		equalizer.Configure(inputAudioFormat.SampleRate, inputAudioFormat.ChannelCount);
		return inputAudioFormat;
	}

	public override void QueueInput(ByteBuffer? inputBuffer)
	{
		var size = inputBuffer?.Remaining() ?? 0;
		if (size == 0)
		{
			return;
		}

		// The audio is copied into .NET, processed, then copied back to the output buffer
		if (_buffer.Length < size)
		{
			_buffer = new byte[size];
		}

		inputBuffer!.Get(_buffer, 0, size);

		if (!equalizer.IsFlat)
		{
			// The bytes are 16-bit samples in the device's byte order (little-endian on Android)
			equalizer.Process(MemoryMarshal.Cast<byte, short>(_buffer.AsSpan(0, size)));
		}

		var output = ReplaceOutputBuffer(size)!;
		output.Put(_buffer, 0, size);
		output.Flip();
	}

	protected override void OnFlush() => equalizer.Reset();
}

/// <summary>
/// Receives a copy of the played audio (from a TeeAudioProcessor) and gives it to the <see cref="SpectrumAnalyzer"/>.
/// Runs on the audio thread.
/// </summary>
public sealed class SpectrumAudioBufferSink(SpectrumAnalyzer analyzer) : Java.Lang.Object, TeeAudioProcessor.IAudioBufferSink
{
	private byte[] _buffer = [];
	private bool _isSupportedFormat;

	public void Flush(int sampleRateHz, int channelCount, int encoding)
	{
		_isSupportedFormat = encoding == C.EncodingPcm16bit;
		analyzer.Reset(sampleRateHz, channelCount);
	}

	public void HandleBuffer(ByteBuffer? buffer)
	{
		var size = buffer?.Remaining() ?? 0;
		if (!_isSupportedFormat || size == 0)
		{
			return;
		}

		if (_buffer.Length < size)
		{
			_buffer = new byte[size];
		}

		// Read a duplicate, so that the position of the buffer played is left untouched
		buffer!.Duplicate()!.Get(_buffer, 0, size);
		analyzer.Write(MemoryMarshal.Cast<byte, short>(_buffer.AsSpan(0, size)));
	}
}
