using AGC.Launcher.Core.Models;
using AGC.Launcher.Core.Services;
using AGC.Launcher.Core.Services.Http;
using AGC.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AGC.Launcher.ViewModels;

/// <summary>
/// Root view model. Owns the top-level navigation between the login/sign-up screens,
/// the signed-in shell, and the maintenance lock screen, and persists the session so
/// the app can resume without a fresh login. This is also the app's composition
/// point — swapping any service implementation happens at construction time, nothing
/// below needs to change.
/// </summary>
public sealed partial class AppViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly IGameCatalogService _catalogService;
    private readonly IPurchaseService _purchaseService;
    private readonly IDownloadService _downloadService;
    private readonly OwnerPublishService _publishService;
    private readonly OwnerBalanceService _balanceService;
    private readonly OwnerGamesService _gamesService;
    private readonly ISessionStore _sessionStore;
    private readonly MaintenanceClient _maintenanceClient;
    private readonly IPreferencesStore _preferencesStore;

    public AppViewModel(
        IAuthService authService,
        IGameCatalogService catalogService,
        IPurchaseService purchaseService,
        IDownloadService downloadService,
        OwnerPublishService publishService,
        OwnerBalanceService balanceService,
        OwnerGamesService gamesService,
        ISessionStore sessionStore,
        MaintenanceClient maintenanceClient,
        IPreferencesStore preferencesStore)
    {
        _authService = authService;
        _catalogService = catalogService;
        _purchaseService = purchaseService;
        _downloadService = downloadService;
        _publishService = publishService;
        _balanceService = balanceService;
        _gamesService = gamesService;
        _sessionStore = sessionStore;
        _maintenanceClient = maintenanceClient;
        _preferencesStore = preferencesStore;
        _maintenanceClient.StatusChanged += OnMaintenanceStatusChanged;

        CurrentViewModel = new SplashViewModel();
        _ = StartupAsync();
    }

    [ObservableProperty]
    public partial ViewModelBase CurrentViewModel { get; set; }

    private async Task StartupAsync()
    {
        // Keep the splash on screen for a minimum, perceptible beat rather than
        // flashing past it the instant the startup checks below happen to be fast.
        var minSplashDuration = Task.Delay(TimeSpan.FromMilliseconds(900));

        // Connect early and keep it alive for the app's lifetime: the hub needs no
        // auth, since even a signed-out user on the login screen must learn about an
        // active lockout.
        _ = _maintenanceClient.ConnectAsync();

        MaintenanceStatusDto status;
        try
        {
            status = await _maintenanceClient.CheckStatusNowAsync();
        }
        catch
        {
            status = new MaintenanceStatusDto(false, null, null);
        }

        await minSplashDuration;

        if (status.IsActive)
        {
            ShowMaintenanceLock(status);
            return;
        }

        var savedSession = _sessionStore.Load();
        CurrentViewModel = savedSession is not null
            ? CreateShellViewModel(savedSession.User)
            : CreateLoginViewModel();
    }

    private LoginViewModel CreateLoginViewModel()
    {
        var login = new LoginViewModel(_authService);
        login.LoginSucceeded += OnAuthSucceeded;
        login.SignUpRequested += (_, _) => CurrentViewModel = CreateSignUpViewModel();
        return login;
    }

    private SignUpViewModel CreateSignUpViewModel()
    {
        var signUp = new SignUpViewModel(_authService);
        signUp.SignUpSucceeded += OnAuthSucceeded;
        signUp.BackToLoginRequested += (_, _) => CurrentViewModel = CreateLoginViewModel();
        return signUp;
    }

    private void OnAuthSucceeded(object? sender, AuthResultDto result)
    {
        _sessionStore.Save(new Session { Token = result.Token, User = result.User });
        CurrentViewModel = CreateShellViewModel(result.User);
    }

    private ShellViewModel CreateShellViewModel(UserDto user)
    {
        var shell = new ShellViewModel(user, _catalogService, _purchaseService, _downloadService, _publishService, _balanceService, _gamesService, _preferencesStore);
        shell.SignedOut += OnSignedOut;
        _ = InitializeShellSafelyAsync(shell);
        return shell;
    }

    private static async Task InitializeShellSafelyAsync(ShellViewModel shell)
    {
        try
        {
            await shell.InitializeAsync();
        }
        catch
        {
            // Swallow: a failed catalog/library refresh shouldn't crash the app. The
            // affected view just stays empty/stale until the user retries an action.
        }
    }

    private void OnSignedOut(object? sender, EventArgs e)
    {
        _sessionStore.Clear();
        CurrentViewModel = CreateLoginViewModel();
    }

    private async void OnMaintenanceStatusChanged(object? sender, MaintenanceStatusDto status)
    {
        if (!status.IsActive)
        {
            CurrentViewModel = CreateLoginViewModel();
            return;
        }

        // "Force-close after a short delay" — give the current screen a moment to
        // show the notification before actually kicking the user out.
        await Task.Delay(TimeSpan.FromSeconds(4));
        ShowMaintenanceLock(status);
    }

    private void ShowMaintenanceLock(MaintenanceStatusDto status)
    {
        _sessionStore.Clear();
        CurrentViewModel = new MaintenanceViewModel(
            status.Message ?? "Launcher closing for a new release.",
            status.ReopensAtUtc ?? DateTime.UtcNow.AddMinutes(5));
    }
}
