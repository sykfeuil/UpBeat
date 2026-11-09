using AndroidX.Media3.Common;

namespace UpBeat;

/// <summary>
/// The player given to the media session: forwards everything to ExoPlayer, except previous/next.
/// ExoPlayer only holds the playing track (the queue is managed by the app),
/// so the previous/next buttons of the notification and lock screen are sent to the app's queue.
/// </summary>
public sealed class QueueControlsPlayer : ForwardingPlayer
{
	// Media3 command codes (Player.COMMAND_*), not exposed as named constants by the .NET binding
	private const int CommandSeekToPreviousMediaItem = 6;
	private const int CommandSeekToNext = 9;

	private readonly Action _onPrevious;
	private readonly Action _onNext;

	public QueueControlsPlayer(IPlayer player, Action onPrevious, Action onNext)
		: base(player)
	{
		_onPrevious = onPrevious;
		_onNext = onNext;
	}

	// Always show the previous/next buttons, even for a single track: suggestions always follow it.
	// On OxygenOS, the whole media card is also disabled when "next" is not available.
	public override PlayerCommands? AvailableCommands
	{
		get
		{
			var builder = base.AvailableCommands?.BuildUpon();
			if (builder is null)
			{
				return base.AvailableCommands;
			}

			for (var command = CommandSeekToPreviousMediaItem; command <= CommandSeekToNext; command++)
			{
				builder.Add(command);
			}

			return builder.Build();
		}
	}

	public override bool IsCommandAvailable(int command) =>
		command is >= CommandSeekToPreviousMediaItem and <= CommandSeekToNext
		|| base.IsCommandAvailable(command);

	public override void SeekToPrevious() => _onPrevious();

	public override void SeekToPreviousMediaItem() => _onPrevious();

	public override void SeekToNext() => _onNext();

	public override void SeekToNextMediaItem() => _onNext();
}
