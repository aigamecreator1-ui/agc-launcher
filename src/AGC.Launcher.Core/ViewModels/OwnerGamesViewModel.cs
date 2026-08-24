using System.Collections.ObjectModel;
using AGC.Launcher.Core.Services.Http;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AGC.Launcher.ViewModels;

/// <summary>Owner-only Games management screen: every published title, with a per-game delete flow.</summary>
public sealed partial class OwnerGamesViewModel : ViewModelBase
{
    private readonly OwnerGamesService _gamesService;

    public OwnerGamesViewModel(OwnerGamesService gamesService)
    {
        _gamesService = gamesService;
    }

    public ObservableCollection<OwnerGameItemViewModel> Games { get; } = [];

    public bool HasAnyGames => Games.Count > 0;

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
            var games = await _gamesService.GetGamesAsync();

            Games.Clear();
            foreach (var game in games)
            {
                Games.Add(new OwnerGameItemViewModel(game, _gamesService));
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
