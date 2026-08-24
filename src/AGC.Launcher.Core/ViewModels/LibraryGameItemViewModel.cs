using AGC.Launcher.Core.Services;
using AGC.Launcher.Core.Services.Http;
using AGC.Shared.Dtos;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AGC.Launcher.ViewModels;

public enum InstallState
{
    NotInstalled,
    Installing,
    Installed,
}

public sealed partial class LibraryGameItemViewModel : ViewModelBase
{
    private readonly IDownloadService _downloadService;

    public LibraryGameItemViewModel(GameDto game, IDownloadService downloadService)
    {
        Game = game;
        _downloadService = downloadService;
        State = downloadService.IsInstalled(game.Id) ? InstallState.Installed : InstallState.NotInstalled;
        _ = LoadThumbnailAsync();
    }

    public GameDto Game { get; }

    [ObservableProperty]
    public partial InstallState State { get; set; } = InstallState.NotInstalled;

    [ObservableProperty]
    public partial double InstallProgress { get; set; }

    [ObservableProperty]
    public partial Bitmap? Thumbnail { get; set; }

    [ObservableProperty]
    public partial DateTime? LastPlayedUtc { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public bool IsNotInstalled => State == InstallState.NotInstalled;

    public bool IsInstalling => State == InstallState.Installing;

    public bool IsInstalled => State == InstallState.Installed;

    public event EventHandler? OpenDetailRequested;

    public event EventHandler? Played;

    [RelayCommand]
    private void OpenDetail() => OpenDetailRequested?.Invoke(this, EventArgs.Empty);

    private async Task LoadThumbnailAsync() => Thumbnail = await ThumbnailLoader.LoadAsync(Game.Id);

    private bool CanInstall() => State == InstallState.NotInstalled;

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private async Task InstallAsync()
    {
        ErrorMessage = null;
        State = InstallState.Installing;
        InstallProgress = 0;
        var progress = new Progress<double>(p => InstallProgress = p);
        try
        {
            await _downloadService.InstallAsync(Game, progress);
            State = InstallState.Installed;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            State = InstallState.NotInstalled;
        }
    }

    private bool CanPlay() => State == InstallState.Installed;

    [RelayCommand(CanExecute = nameof(CanPlay))]
    private void Play()
    {
        try
        {
            ErrorMessage = null;
            _downloadService.Launch(Game.Id);
            LastPlayedUtc = DateTime.UtcNow;
            Played?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            State = InstallState.NotInstalled;
        }
    }

    partial void OnStateChanged(InstallState value)
    {
        InstallCommand.NotifyCanExecuteChanged();
        PlayCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(IsNotInstalled));
        OnPropertyChanged(nameof(IsInstalling));
        OnPropertyChanged(nameof(IsInstalled));
    }
}
