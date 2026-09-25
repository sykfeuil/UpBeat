namespace UpBeat.Controls;

/// <summary>
/// Frame shape with pixel-art "staircase" corners, to use as a Border's StrokeShape.
/// The outline is computed for the actual size of the frame, so it fits any Border.
/// </summary>
public class PixelFrameShape : IShape
{
	/// <summary>
	/// Size of one corner step.
	/// </summary>
	public float Step { get; set; } = 4;

	public PathF PathForBounds(Rect bounds)
	{
		var left = (float)bounds.Left;
		var top = (float)bounds.Top;
		var right = (float)bounds.Right;
		var bottom = (float)bounds.Bottom;
		var s = Step;

		var path = new PathF();

		// Top edge, then top-right corner: two steps down
		path.MoveTo(left + 2 * s, top);
		path.LineTo(right - 2 * s, top);
		path.LineTo(right - 2 * s, top + s);
		path.LineTo(right - s, top + s);
		path.LineTo(right - s, top + 2 * s);
		path.LineTo(right, top + 2 * s);

		// Right edge, then bottom-right corner
		path.LineTo(right, bottom - 2 * s);
		path.LineTo(right - s, bottom - 2 * s);
		path.LineTo(right - s, bottom - s);
		path.LineTo(right - 2 * s, bottom - s);
		path.LineTo(right - 2 * s, bottom);

		// Bottom edge, then bottom-left corner
		path.LineTo(left + 2 * s, bottom);
		path.LineTo(left + 2 * s, bottom - s);
		path.LineTo(left + s, bottom - s);
		path.LineTo(left + s, bottom - 2 * s);
		path.LineTo(left, bottom - 2 * s);

		// Left edge, then top-left corner
		path.LineTo(left, top + 2 * s);
		path.LineTo(left + s, top + 2 * s);
		path.LineTo(left + s, top + s);
		path.LineTo(left + 2 * s, top + s);
		path.Close();

		return path;
	}
}
