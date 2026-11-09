using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UpBeat.Audio;
using UpBeat.Services;

namespace UpBeat.ViewModels;

/// <summary>
/// Home screen: the equalizer (bass, middle, treble), kept between launches.
/// </summary>
public partial class HomeViewModel : ObservableObject
{
	private readonly ThreeBandEqualizer _equalizer;

	public HomeViewModel(AudioPlayerService audioPlayer)
	{
		_equalizer = audioPlayer.Equalizer;

		// Restore the last settings (the On...Changed methods apply them)
		LowGain = Preferences.Get(PreferenceKey(EqualizerBand.Low), 0.0);
		MidGain = Preferences.Get(PreferenceKey(EqualizerBand.Mid), 0.0);
		HighGain = Preferences.Get(PreferenceKey(EqualizerBand.High), 0.0);
	}

	public double MinGain => ThreeBandEqualizer.MinGainDb;

	public double MaxGain => ThreeBandEqualizer.MaxGainDb;

	[ObservableProperty]
	public partial double LowGain { get; set; }

	[ObservableProperty]
	public partial double MidGain { get; set; }

	[ObservableProperty]
	public partial double HighGain { get; set; }

	partial void OnLowGainChanged(double value) => Apply(EqualizerBand.Low, value);

	partial void OnMidGainChanged(double value) => Apply(EqualizerBand.Mid, value);

	partial void OnHighGainChanged(double value) => Apply(EqualizerBand.High, value);

	/// <summary>
	/// Puts every band back to 0 dB.
	/// </summary>
	[RelayCommand]
	private void Reset()
	{
		LowGain = 0;
		MidGain = 0;
		HighGain = 0;
	}

	private void Apply(EqualizerBand band, double gain)
	{
		_equalizer.SetGain(band, gain);
		Preferences.Set(PreferenceKey(band), gain);
	}

	private static string PreferenceKey(EqualizerBand band) => $"Equalizer{band}Gain";
}
