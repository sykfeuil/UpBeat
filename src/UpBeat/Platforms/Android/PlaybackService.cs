using Android.App;
using Android.Content.PM;
using AndroidX.Media3.Session;
using UpBeat.Services;

namespace UpBeat;

/// <summary>
/// Android service that keeps the music playing in the background.
/// Media3 runs it in the foreground while music plays, with the media notification and lock screen controls.
/// The player and its session belong to <see cref="AudioPlayerService"/>: the service only exposes them.
/// </summary>
[Service(Exported = true, ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
[IntentFilter([ServiceInterface])]
public class PlaybackService : MediaSessionService
{
	public override MediaSession? OnGetSession(MediaSession.ControllerInfo? controllerInfo)
		=> IPlatformApplication.Current?.Services.GetService<AudioPlayerService>()?.Session;
}
