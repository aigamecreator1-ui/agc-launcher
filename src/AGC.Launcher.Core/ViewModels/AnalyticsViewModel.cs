using System.Collections.ObjectModel;
using AGC.Launcher.Core.Services.Http;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AGC.Launcher.ViewModels;

/// <summary>Owner-only: real per-game and launcher-level stats, backed by the real database.</summary>
public sealed partial class AnalyticsViewModel : ViewModelBase
{
    private readonly OwnerAnalyticsService _analyticsService;

    public AnalyticsViewModel(OwnerAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    public ObservableCollection<GameAnalyticsItemViewModel> Games { get; } = [];

    public bool HasAnyGames => Games.Count > 0;

    [ObservableProperty]
    public partial int LauncherOpens { get; set; }

    [ObservableProperty]
    public partial int RegisteredAccounts { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public async Task LoadAsync()
    {
        ErrorMessage = null;
        IsLoading = true;
        try
        {
            var overview = await _analyticsService.GetOverviewAsync();

            LauncherOpens = overview.Launcher.LauncherOpens;
            RegisteredAccounts = overview.Launcher.RegisteredAccounts;

            Games.Clear();
            foreach (var game in overview.Games)
            {
                Games.Add(new GameAnalyticsItemViewModel(game));
            }

            OnPropertyChanged(nameof(HasAnyGames));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
