using AGC.Launcher.Core.Models;
using AGC.Launcher.Core.Services;
using AGC.Launcher.Core.Services.Http;
using AGC.Shared.Dtos;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AGC.Launcher.ViewModels;

public sealed partial class StoreGameItemViewModel : ViewModelBase
{
    private readonly IPurchaseService _purchaseService;

    public StoreGameItemViewModel(GameDto game, IPurchaseService purchaseService)
    {
        Game = game;
        _purchaseService = purchaseService;
        IsOwned = game.IsOwned;
        _ = LoadThumbnailAsync();
    }

    public GameDto Game { get; }

    public string PriceDisplay => Game.IsPaid ? $"${Game.PriceUsd:F2}" : "FREE";

    public string BuyLabel => Game.IsPaid ? "BUY" : "GET";

    public string PurchasingStatus => Game.IsPaid
        ? "Opened checkout in your browser — waiting for payment confirmation..."
        : "Unlocking...";

    public event EventHandler<GameDto>? Purchased;

    public event EventHandler? OpenDetailRequested;

    [RelayCommand]
    private void OpenDetail() => OpenDetailRequested?.Invoke(this, EventArgs.Empty);

    [ObservableProperty]
    public partial bool IsOwned { get; set; }

    [ObservableProperty]
    public partial bool IsPurchasing { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial Bitmap? Thumbnail { get; set; }

    private async Task LoadThumbnailAsync() => Thumbnail = await ThumbnailLoader.LoadAsync(Game.Id);

    private bool CanBuy() => !IsOwned && !IsPurchasing;

    [RelayCommand(CanExecute = nameof(CanBuy))]
    private async Task BuyAsync()
    {
        ErrorMessage = null;
        IsPurchasing = true;
        try
        {
            var result = await _purchaseService.PurchaseAsync(Game);
            switch (result.Outcome)
            {
                case PurchaseOutcome.Success:
                    IsOwned = true;
                    Purchased?.Invoke(this, Game);
                    break;
                case PurchaseOutcome.Cancelled:
                    break;
                case PurchaseOutcome.Failed:
                    ErrorMessage = result.ErrorMessage ?? "Purchase failed. Please try again.";
                    break;
            }
        }
        finally
        {
            IsPurchasing = false;
        }
    }

    partial void OnIsOwnedChanged(bool value) => BuyCommand.NotifyCanExecuteChanged();

    partial void OnIsPurchasingChanged(bool value) => BuyCommand.NotifyCanExecuteChanged();
}
