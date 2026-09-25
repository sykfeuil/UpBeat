namespace UpBeat.Helpers;

/// <summary>
/// Runs an action after a delay, cancelling the previous run if a new one starts in between.
/// Used to search while typing without sending a request for every character.
/// </summary>
public sealed class Debouncer
{
	private CancellationTokenSource? _cancellation;

	/// <summary>
	/// Cancels the previous run, waits for the delay, then runs the action.
	/// The action receives a token that is cancelled as soon as a newer run starts.
	/// </summary>
	public async Task RunAsync(TimeSpan delay, Func<CancellationToken, Task> action)
	{
		Cancel();
		var cancellation = _cancellation = new CancellationTokenSource();

		try
		{
			await Task.Delay(delay, cancellation.Token);
			await action(cancellation.Token);
		}
		catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
		{
			// Replaced by a newer run: nothing to do
		}
	}

	/// <summary>
	/// Cancels the pending or running action, if any.
	/// </summary>
	public void Cancel() => _cancellation?.Cancel();
}
