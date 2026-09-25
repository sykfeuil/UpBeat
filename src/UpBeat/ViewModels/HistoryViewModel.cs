using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UpBeat.Models;
using UpBeat.Services;

namespace UpBeat.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
	private readonly IHistoryRepository _history;

	public HistoryViewModel(IHistoryRepository history, PlayerViewModel player)
	{
		_history = history;
		Player = player;
	}

	public PlayerViewModel Player { get; }

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsEmpty))]
	public partial IReadOnlyList<HistoryEntry> Entries { get; set; } = [];

	public bool IsEmpty => Entries.Count == 0;

	[RelayCommand]
	private async Task LoadAsync()
	{
		Entries = await _history.GetRecentAsync();
	}

	[RelayCommand]
	private Task PlayTrackAsync(HistoryEntry entry)
		=> Player.PlayTrackCommand.ExecuteAsync(entry.ToTrack());
}
