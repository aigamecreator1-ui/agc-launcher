using AGC.Launcher.Core.Services.Http;
using AGC.Shared.Dtos;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AGC.Launcher.ViewModels;

public sealed partial class GameAnalyticsItemViewModel : ViewModelBase
{
    public GameAnalyticsItemViewModel(GameAnalyticsDto game)
    {
        Game = game;
        _ = LoadThumbnailAsync();
    }

    public GameAnalyticsDto Game { get; }

    [ObservableProperty]
    public partial Bitmap? Thumbnail { get; set; }

    public string PublishedDisplay => Game.PublishedAt is { } date ? date.ToString("MMM d, yyyy") : "—";

    private async Task LoadThumbnailAsync() => Thumbnail = await ThumbnailLoader.LoadAsync(Game.GameId);
}
