using System.Collections.ObjectModel;
using AGC.Launcher.Core.Services;
using AGC.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AGC.Launcher.ViewModels;

public sealed partial class LibraryViewModel : ViewModelBase
{
    private readonly IGameCatalogService _catalogService;
    private readonly IDownloadService _downloadService;
    private readonly List<LibraryGameItemViewModel> _allGames = [];

    public LibraryViewModel(IGameCatalogService catalogService, IDownloadService downloadService)
    {
        _catalogService = catalogService;
        _downloadService = downloadService;
    }

    /// <summary>The owned library, filtered by <see cref="SearchText"/> and <see cref="SelectedGenre"/>.</summary>
    public ObservableCollection<LibraryGameItemViewModel> Games { get; } = [];

    public ObservableCollection<string> Genres { get; } = ["ALL GENRES"];

    /// <summary>Most recently played titles this session, newest first — empty until something is played.</summary>
    public ObservableCollection<LibraryGameItemViewModel> RecentlyPlayed { get; } = [];

    public bool HasAnyGames => _allGames.Count > 0;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string SearchText { get; set; } = "";

    [ObservableProperty]
    public partial string SelectedGenre { get; set; } = "ALL GENRES";

    public event EventHandler<LibraryGameItemViewModel>? OpenDetailRequested;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedGenreChanged(string value) => ApplyFilter();

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var owned = await _catalogService.GetLibraryAsync();

            _allGames.Clear();
            Genres.Clear();
            Genres.Add("ALL GENRES");

            foreach (var game in owned)
            {
                AddToAll(new LibraryGameItemViewModel(game, _downloadService));
            }

            ApplyFilter();
            RefreshRecentlyPlayed();
            OnPropertyChanged(nameof(HasAnyGames));
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void AddGameIfMissing(GameDto game)
    {
        if (_allGames.Any(g => g.Game.Id == game.Id))
        {
            return;
        }

        AddToAll(new LibraryGameItemViewModel(game, _downloadService));
        ApplyFilter();
        OnPropertyChanged(nameof(HasAnyGames));
    }

    private void AddToAll(LibraryGameItemViewModel item)
    {
        item.OpenDetailRequested += (_, _) => OpenDetailRequested?.Invoke(this, item);
        item.Played += (_, _) => RefreshRecentlyPlayed();
        _allGames.Add(item);

        if (!string.IsNullOrWhiteSpace(item.Game.Genre) && !Genres.Contains(item.Game.Genre))
        {
            Genres.Add(item.Game.Genre);
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

    private void RefreshRecentlyPlayed()
    {
        RecentlyPlayed.Clear();
        foreach (var game in _allGames
                     .Where(g => g.LastPlayedUtc is not null)
                     .OrderByDescending(g => g.LastPlayedUtc)
                     .Take(6))
        {
            RecentlyPlayed.Add(game);
        }
    }
}
