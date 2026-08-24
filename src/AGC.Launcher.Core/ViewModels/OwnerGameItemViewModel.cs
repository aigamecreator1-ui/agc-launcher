using AGC.Launcher.Core.Services.Http;
using AGC.Shared.Dtos;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AGC.Launcher.ViewModels;

public sealed partial class OwnerGameItemViewModel : ViewModelBase
{
    private readonly OwnerGamesService _gamesService;

    public OwnerGameItemViewModel(OwnerGameDto game, OwnerGamesService gamesService)
    {
        Game = game;
        _gamesService = gamesService;
        _ = LoadThumbnailAsync();
    }

    public OwnerGameDto Game { get; }

    [ObservableProperty]
    public partial Bitmap? Thumbnail { get; set; }

    /// <summary>The "..." menu — a small popup with the Delete option.</summary>
    [ObservableProperty]
    public partial bool IsMenuOpen { get; set; }

    /// <summary>The "are you sure?" prompt, shown after Delete is picked from the menu.</summary>
    [ObservableProperty]
    public partial bool IsConfirmingDelete { get; set; }

    [ObservableProperty]
    public partial bool IsDeleting { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public string PriceDisplay => Game.IsPaid ? $"${Game.PriceUsd:F2}" : "FREE";

    public string OwnerCountDisplay => Game.OwnerCount == 1 ? "1 OWNER" : $"{Game.OwnerCount} OWNERS";

    public string RevenueDisplay => Game.IsPaid ? $"${Game.TotalRevenueUsd:F2} EARNED" : "";

    public string ConfirmPrompt => $"Are you sure you want to delete \"{Game.Title}\"? This cannot be undone.";

    private async Task LoadThumbnailAsync() => Thumbnail = await ThumbnailLoader.LoadAsync(Game.Id);

    [RelayCommand]
    private void ToggleMenu() => IsMenuOpen = !IsMenuOpen;

    [RelayCommand]
    private void RequestDelete()
    {
        IsMenuOpen = false;
        ErrorMessage = null;
        IsConfirmingDelete = true;
    }

    [RelayCommand]
    private void CancelDelete() => IsConfirmingDelete = false;

    private bool CanConfirmDelete() => !IsDeleting;

    [RelayCommand(CanExecute = nameof(CanConfirmDelete))]
    private async Task ConfirmDeleteAsync()
    {
        ErrorMessage = null;
        IsDeleting = true;
        try
        {
            await _gamesService.DeleteGameAsync(Game.Id);

            // The DELETE call only *schedules* removal behind the maintenance lockout
            // it just triggered — this client (like every other connected one) is
            // about to be force-closed to the maintenance screen by that broadcast,
            // so there's nothing further to update here.
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            IsConfirmingDelete = false;
        }
        finally
        {
            IsDeleting = false;
        }
    }

    partial void OnIsDeletingChanged(bool value) => ConfirmDeleteCommand.NotifyCanExecuteChanged();
}
