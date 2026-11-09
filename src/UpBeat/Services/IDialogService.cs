namespace UpBeat.Services;

/// <summary>
/// Shows dialogs to the user. ViewModels use it instead of calling pages directly.
/// </summary>
public interface IDialogService
{
	/// <summary>Asks the user to type a text. Returns <c>null</c> if cancelled.</summary>
	Task<string?> PromptAsync(string title, string message, string accept, string? initialValue = null);

	/// <summary>Asks the user to choose among options. Returns <c>null</c> if cancelled.</summary>
	Task<string?> ChooseAsync(string title, IEnumerable<string> options);

	/// <summary>
	/// Asks the user to choose among items, shown with the given label. Returns the chosen item, or <c>null</c> if cancelled.
	/// Items with the same label are told apart by a number (e.g. "Rock (2)"), so the right item is always returned.
	/// </summary>
	Task<T?> ChooseAsync<T>(string title, IReadOnlyList<T> items, Func<T, string> getLabel) where T : class;

	/// <summary>Asks the user to confirm an action.</summary>
	Task<bool> ConfirmAsync(string title, string message, string accept);

	/// <summary>Shows a short message at the bottom of the screen.</summary>
	Task ShowToastAsync(string message);
}
