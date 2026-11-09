using CommunityToolkit.Maui.Alerts;

namespace UpBeat.Services;

/// <summary>
/// Dialogs shown on the current page of the Shell.
/// </summary>
public class DialogService : IDialogService
{
	private const string Cancel = "Cancel";

	private static Page CurrentPage => Shell.Current.CurrentPage;

	public async Task<string?> PromptAsync(string title, string message, string accept, string? initialValue = null)
	{
		var result = await CurrentPage.DisplayPromptAsync(title, message, accept, Cancel, initialValue: initialValue ?? string.Empty);
		return string.IsNullOrWhiteSpace(result) ? null : result.Trim();
	}

	public async Task<string?> ChooseAsync(string title, IEnumerable<string> options)
	{
		var result = await CurrentPage.DisplayActionSheetAsync(title, Cancel, null, options.ToArray());
		return result is null || result == Cancel ? null : result;
	}

	public async Task<T?> ChooseAsync<T>(string title, IReadOnlyList<T> items, Func<T, string> getLabel) where T : class
	{
		// The action sheet only returns the text of the tapped button: each label must be unique
		var labels = MakeUnique(items.Select(getLabel));

		var result = await CurrentPage.DisplayActionSheetAsync(title, Cancel, null, labels);
		var index = result is null ? -1 : Array.IndexOf(labels, result);
		return index < 0 ? null : items[index];
	}

	public Task<bool> ConfirmAsync(string title, string message, string accept)
		=> CurrentPage.DisplayAlertAsync(title, message, accept, Cancel);

	public Task ShowToastAsync(string message)
		=> Toast.Make(message).Show();

	/// <summary>
	/// Adds a number to repeated labels ("Rock", "Rock (2)"…). "Cancel" is taken by the cancel button.
	/// </summary>
	private static string[] MakeUnique(IEnumerable<string> labels)
	{
		var usedLabels = new HashSet<string> { Cancel };
		var uniqueLabels = new List<string>();

		foreach (var label in labels)
		{
			var uniqueLabel = label;
			for (var number = 2; !usedLabels.Add(uniqueLabel); number++)
			{
				uniqueLabel = $"{label} ({number})";
			}

			uniqueLabels.Add(uniqueLabel);
		}

		return [.. uniqueLabels];
	}
}
