namespace UpBeat.Controls;

/// <summary>
/// Pixel-art progress bar made of small blocks: red for the elapsed part, muted for the rest.
/// It only draws the progress: a transparent Slider on top handles touches.
/// </summary>
public class PixelProgressBar : GraphicsView, IDrawable
{
	public static readonly BindableProperty ProgressProperty = BindableProperty.Create(
		nameof(Progress),
		typeof(double),
		typeof(PixelProgressBar),
		0.0,
		propertyChanged: (bindable, _, _) => ((PixelProgressBar)bindable).Invalidate());

	private const float SegmentWidth = 6;
	private const float SegmentGap = 2;
	private const float SegmentHeight = 6;

	private readonly Color _filledColor = (Color)Application.Current!.Resources["NeonRed"];
	private readonly Color _emptyColor = (Color)Application.Current!.Resources["Muted"];

	public PixelProgressBar()
	{
		Drawable = this;
		HeightRequest = SegmentHeight;
		InputTransparent = true;
	}

	/// <summary>
	/// Elapsed part, from 0 (start) to 1 (end).
	/// </summary>
	public double Progress
	{
		get => (double)GetValue(ProgressProperty);
		set => SetValue(ProgressProperty, value);
	}

	public void Draw(ICanvas canvas, RectF dirtyRect)
	{
		var segmentCount = (int)((dirtyRect.Width + SegmentGap) / (SegmentWidth + SegmentGap));
		if (segmentCount == 0)
		{
			return;
		}

		var filledCount = (int)Math.Round(Math.Clamp(Progress, 0, 1) * segmentCount);
		var top = dirtyRect.Center.Y - SegmentHeight / 2;

		// Sharp pixels: no smoothing
		canvas.Antialias = false;

		for (var i = 0; i < segmentCount; i++)
		{
			canvas.FillColor = i < filledCount ? _filledColor : _emptyColor;
			canvas.FillRectangle(dirtyRect.Left + i * (SegmentWidth + SegmentGap), top, SegmentWidth, SegmentHeight);
		}
	}
}
