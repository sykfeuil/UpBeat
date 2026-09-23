using System.Globalization;

namespace UpBeat.Converters;

/// <summary>
/// Converts a track duration to a short text such as "3:45" or "1:02:30".
/// </summary>
public class DurationConverter : IValueConverter
{
	public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		if (value is not TimeSpan duration)
		{
			return string.Empty;
		}

		return duration.TotalHours >= 1
			? duration.ToString(@"h\:mm\:ss", culture)
			: duration.ToString(@"m\:ss", culture);
	}

	public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> throw new NotSupportedException();
}
