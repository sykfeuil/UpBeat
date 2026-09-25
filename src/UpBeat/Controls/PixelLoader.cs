namespace UpBeat.Controls;

/// <summary>
/// Pixel-art loading indicator: three blocks that light up one after the other.
/// Replaces the round ActivityIndicator.
/// </summary>
public class PixelLoader : HorizontalStackLayout
{
	public static readonly BindableProperty IsRunningProperty = BindableProperty.Create(
		nameof(IsRunning),
		typeof(bool),
		typeof(PixelLoader),
		propertyChanged: (bindable, _, newValue) => ((PixelLoader)bindable).OnIsRunningChanged((bool)newValue));

	private const int BlockCount = 3;
	private const double DimOpacity = 0.25;
	private static readonly TimeSpan StepDuration = TimeSpan.FromMilliseconds(200);

	private readonly BoxView[] _blocks;
	private IDispatcherTimer? _timer;
	private int _litBlock;

	public PixelLoader()
	{
		Spacing = 6;
		IsVisible = false;

		_blocks = new BoxView[BlockCount];
		for (var i = 0; i < BlockCount; i++)
		{
			_blocks[i] = new BoxView
			{
				WidthRequest = 10,
				HeightRequest = 10,
				Color = (Color)Application.Current!.Resources["NeonRed"],
				Opacity = DimOpacity,
			};
			Children.Add(_blocks[i]);
		}
	}

	/// <summary>
	/// Shows and animates the loader while true.
	/// </summary>
	public bool IsRunning
	{
		get => (bool)GetValue(IsRunningProperty);
		set => SetValue(IsRunningProperty, value);
	}

	private void OnIsRunningChanged(bool isRunning)
	{
		IsVisible = isRunning;

		if (isRunning)
		{
			_timer ??= CreateTimer();
			_timer.Start();
		}
		else
		{
			_timer?.Stop();
		}
	}

	private IDispatcherTimer CreateTimer()
	{
		var timer = Dispatcher.CreateTimer();
		timer.Interval = StepDuration;
		timer.Tick += (_, _) =>
		{
			// Light the next block, dim the others
			_litBlock = (_litBlock + 1) % BlockCount;
			for (var i = 0; i < BlockCount; i++)
			{
				_blocks[i].Opacity = i == _litBlock ? 1 : DimOpacity;
			}
		};
		return timer;
	}
}
