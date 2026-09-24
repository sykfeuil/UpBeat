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

	public Task<bool> ConfirmAsync(string title, string message, string accept)
		=> CurrentPage.DisplayAlertAsync(title, message, accept, Cancel);

	public Task ShowToastAsync(string message)
		=> Toast.Make(message).Show();
}
