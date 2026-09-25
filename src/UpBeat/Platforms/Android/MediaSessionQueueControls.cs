using System.Reflection;
using AndroidX.Media3.Common;
using AndroidX.Media3.Session;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;

namespace UpBeat;

/// <summary>
/// Enables the previous/next buttons of the media notification and lock screen.
/// MediaElement plays one media at a time, so these buttons do nothing by default: its media session
/// is given a "forwarding" player that sends them to the app's queue instead.
/// </summary>
public static class MediaSessionQueueControls
{
	// Media3 command codes (Player.COMMAND_*), not exposed as named constants by the .NET binding
	private const int CommandSeekToPreviousMediaItem = 6;
	private const int CommandSeekToPrevious = 7;
	private const int CommandSeekToNextMediaItem = 8;
	private const int CommandSeekToNext = 9;

	public static void Attach(MediaElement mediaElement, Action onPrevious, Action onNext)
	{
		if (mediaElement.Handler is null)
		{
			return;
		}

		// MediaElement keeps its media manager, player and session private: they are read by reflection
		const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		var mediaManager = mediaElement.Handler.GetType().GetProperty("MediaManager", Flags)?.GetValue(mediaElement.Handler) as MediaManager;
		if (mediaManager is null)
		{
			return;
		}

		var player = typeof(MediaManager).GetProperty("Player", Flags)?.GetValue(mediaManager) as IPlayer;
		var session = typeof(MediaManager).GetField("session", Flags)?.GetValue(mediaManager) as MediaSession;
		if (player is null || session is null)
		{
			return;
		}

		session.Player = new QueueControlsPlayer(player, onPrevious, onNext);
	}

	/// <summary>
	/// Forwards everything to the real player, except the previous/next commands.
	/// </summary>
	private sealed class QueueControlsPlayer : ForwardingPlayer
	{
		private readonly Action _onPrevious;
		private readonly Action _onNext;

		public QueueControlsPlayer(IPlayer player, Action onPrevious, Action onNext)
			: base(player)
		{
			_onPrevious = onPrevious;
			_onNext = onNext;
		}

		// Always show the previous/next buttons
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
}
