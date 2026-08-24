using System.Collections.ObjectModel;
using AGC.Launcher.Core.Services;
using AGC.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AGC.Launcher.ViewModels;

public sealed partial class StoreViewModel : ViewModelBase
{
    private readonly IGameCatalogService _catalogService;
    private readonly IPurchaseService _purchaseService;
    private readonly List<StoreGameItemViewModel> _allGames = [];

    public StoreViewModel(IGameCatalogService catalogService, IPurchaseService purchaseService)
    {
        _catalogService = catalogService;
        _purchaseService = purchaseService;
    }

    /// <summary>The full catalog, filtered by <see cref="SearchText"/> and <see cref="SelectedGenre"/>.</summary>
    public ObservableCollection<StoreGameItemViewModel> Games { get; } = [];

    public ObservableCollection<string> Genres { get; } = ["ALL GENRES"];

    /// <summary>Newest/most prominent catalog entry, shown as the hero banner above the grid.</summary>
    [ObservableProperty]
    public partial StoreGameItemViewModel? FeaturedGame { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = "";

    [ObservableProperty]
    public partial string SelectedGenre { get; set; } = "ALL GENRES";

    public event EventHandler<GameDto>? GamePurchased;

    public event EventHandler<StoreGameItemViewModel>? OpenDetailRequested;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedGenreChanged(string value) => ApplyFilter();

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var catalog = await _catalogService.GetCatalogAsync();

            _allGames.Clear();
            Genres.Clear();
            Genres.Add("ALL GENRES");

            foreach (var game in catalog)
            {
                var item = new StoreGameItemViewModel(game, _purchaseService);
                item.Purchased += (_, purchasedGame) => GamePurchased?.Invoke(this, purchasedGame);
                item.OpenDetailRequested += (_, _) => OpenDetailRequested?.Invoke(this, item);
                _allGames.Add(item);

                if (!string.IsNullOrWhiteSpace(game.Genre) && !Genres.Contains(game.Genre))
                {
                    Genres.Add(game.Genre);
                }
            }

            FeaturedGame = _allGames.FirstOrDefault();
            ApplyFilter();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter()
    {
        var search = SearchText.Trim();
        var genre = SelectedGenre;

        var filtered = _allGames.Where(g =>
            (string.IsNullOrEmpty(search) || g.Game.Title.Contains(search, StringComparison.OrdinalIgnoreCase)) &&
            (genre == "ALL GENRES" || string.Equals(g.Game.Genre, genre, StringComparison.OrdinalIgnoreCase)));

        Games.Clear();
        foreach (var game in filtered)
        {
            Games.Add(game);
        }
    }
}
