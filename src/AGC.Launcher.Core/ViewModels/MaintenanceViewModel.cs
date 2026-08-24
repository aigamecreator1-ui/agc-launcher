using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AGC.Launcher.ViewModels;

public sealed partial class MaintenanceViewModel : ViewModelBase, IDisposable
{
    private readonly Timer _timer;

    public MaintenanceViewModel(string message, DateTime reopensAtUtc)
    {
        Message = message;
        ReopensAtUtc = reopensAtUtc;
        UpdateCountdown();
        _timer = new Timer(_ => Dispatcher.UIThread.Post(UpdateCountdown), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    public string Message { get; }

    public DateTime ReopensAtUtc { get; }

    [ObservableProperty]
    public partial string CountdownDisplay { get; set; } = "--:--";

    private void UpdateCountdown()
    {
        var remaining = ReopensAtUtc - DateTime.UtcNow;
        CountdownDisplay = remaining > TimeSpan.Zero
            ? $"{(int)remaining.TotalMinutes:D2}:{remaining.Seconds:D2}"
            : "00:00";
    }

    public void Dispose() => _timer.Dispose();
}
