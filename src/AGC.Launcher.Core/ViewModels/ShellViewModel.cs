using AGC.Launcher.Core.Services;
using AGC.Launcher.Core.Services.Http;
using AGC.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AGC.Launcher.ViewModels;

public enum ShellTab
{
    Library,
    Store,
    Analytics,
    Balance,
    Publish,
    Games,
    Settings,
}

public sealed partial class ShellViewModel : ViewModelBase
{
    private readonly GameSocialService _socialService;

    public ShellViewModel(
        UserDto user,
        IGameCatalogService catalogService,
        IPurchaseService purchaseService,
        IDownloadService downloadService,
        OwnerPublishService publishService,
        OwnerBalanceService balanceService,
        OwnerGamesService gamesService,
        OwnerAnalyticsService analyticsService,
        GameSocialService socialService,
        IPreferencesStore preferencesStore)
    {
        CurrentUser = user;
        _socialService = socialService;
        Library = new LibraryViewModel(catalogService, downloadService);
        Store = new StoreViewModel(catalogService, purchaseService);
        Store.GamePurchased += (_, game) => Library.AddGameIfMissing(game);

        Analytics = new AnalyticsViewModel(analyticsService);
        Balance = new BalanceViewModel(balanceService);
        Publish = new PublishViewModel(publishService);
        Games = new OwnerGamesViewModel(gamesService);
        Publish.Published += (_, _) =>
        {
            _ = Library.LoadAsync();
            _ = Games.LoadAsync();
        };
        Settings = new SettingsViewModel(user, preferencesStore);
        Settings.SignOutRequested += (_, _) => SignOut();

        Store.OpenDetailRequested += (_, item) => OpenGameDetail(item.Game, item, null);
        Library.OpenDetailRequested += (_, item) => OpenGameDetail(item.Game, null, item);
    }

    public UserDto CurrentUser { get; }

    /// <summary>Drives the owner-only Analytics / Balance / Publish nav entries.</summary>
    public bool IsOwner => CurrentUser.IsOwner;

    public LibraryViewModel Library { get; }

    public StoreViewModel Store { get; }

    public AnalyticsViewModel Analytics { get; }

    public BalanceViewModel Balance { get; }

    public PublishViewModel Publish { get; }

    public OwnerGamesViewModel Games { get; }

    public SettingsViewModel Settings { get; }

    public event EventHandler? SignedOut;

    [ObservableProperty]
    public partial ShellTab SelectedTab { get; set; } = ShellTab.Library;

    [ObservableProperty]
    public partial GameDetailViewModel? DetailViewModel { get; set; }

    public ViewModelBase CurrentTabViewModel => SelectedTab switch
    {
        ShellTab.Library => Library,
        ShellTab.Store => Store,
        ShellTab.Analytics => Analytics,
        ShellTab.Balance => Balance,
        ShellTab.Publish => Publish,
        ShellTab.Games => Games,
        ShellTab.Settings => Settings,
        _ => Library,
    };

    /// <summary>What the shell's content area actually shows: a game detail page takes over
    /// the whole area when open, otherwise it's whatever tab is selected.</summary>
    public ViewModelBase CurrentContentViewModel => DetailViewModel is { } detail ? detail : CurrentTabViewModel;

    partial void OnSelectedTabChanged(ShellTab value) => OnPropertyChanged(nameof(CurrentContentViewModel));

    partial void OnDetailViewModelChanged(GameDetailViewModel? value) => OnPropertyChanged(nameof(CurrentContentViewModel));

    public async Task InitializeAsync()
    {
        var loads = new List<Task> { Library.LoadAsync(), Store.LoadAsync() };
        if (IsOwner)
        {
            loads.Add(Balance.LoadAsync());
            loads.Add(Games.LoadAsync());
            loads.Add(Analytics.LoadAsync());
        }

        await Task.WhenAll(loads);
    }

    private void OpenGameDetail(GameDto game, StoreGameItemViewModel? storeItem, LibraryGameItemViewModel? libraryItem)
    {
        DetailViewModel = new GameDetailViewModel(
            game, storeItem, libraryItem, _socialService,
            onBack: () => DetailViewModel = null,
            onGoToLibrary: () =>
            {
                DetailViewModel = null;
                SelectedTab = ShellTab.Library;
            });
    }

    [RelayCommand]
    private void ShowLibrary()
    {
        DetailViewModel = null;
        SelectedTab = ShellTab.Library;
    }

    [RelayCommand]
    private void ShowStore()
    {
        DetailViewModel = null;
        SelectedTab = ShellTab.Store;
    }

    [RelayCommand]
    private void ShowAnalytics()
    {
        DetailViewModel = null;
        SelectedTab = ShellTab.Analytics;
        _ = Analytics.LoadAsync(); // refresh — new activity may have landed since the shell loaded
    }

    [RelayCommand]
    private void ShowBalance()
    {
        DetailViewModel = null;
        SelectedTab = ShellTab.Balance;
        _ = Balance.LoadAsync(); // refresh — new sales may have landed since the shell loaded
    }

    [RelayCommand]
    private void ShowPublish()
    {
        DetailViewModel = null;
        SelectedTab = ShellTab.Publish;
    }

    [RelayCommand]
    private void ShowGames()
    {
        DetailViewModel = null;
        SelectedTab = ShellTab.Games;
        _ = Games.LoadAsync(); // refresh — a title may have been published or removed since the shell loaded
    }

    [RelayCommand]
    private void ShowSettings()
    {
        DetailViewModel = null;
        SelectedTab = ShellTab.Settings;
    }

    [RelayCommand]
    private void SignOut() => SignedOut?.Invoke(this, EventArgs.Empty);
}
