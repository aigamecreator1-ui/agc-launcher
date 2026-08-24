using System;
using System.Net.Http;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AGC.Launcher.Core.Services.Http;
using AGC.Launcher.ViewModels;
using AGC.Launcher.Views;

namespace AGC.Launcher;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Composition root. Accounts, the catalog, purchases (real Stripe Checkout
            // for paid games, direct claim for free ones), and downloads all talk to
            // the real AGC.Server backend — same interface-based swap pattern
            // throughout, so nothing above this needs to change if any of these move.
            var sessionStore = new FileSessionStore();
            var httpClient = new HttpClient { BaseAddress = new Uri(ApiConfig.BaseUrl) };
            var apiClient = new ApiClient(httpClient, sessionStore);

            var authService = new HttpAuthService(apiClient);
            var catalogService = new HttpGameCatalogService(apiClient);
            var purchaseService = new HttpPurchaseService(apiClient, catalogService);
            var publishService = new OwnerPublishService(httpClient, sessionStore);
            var balanceService = new OwnerBalanceService(apiClient);
            var gamesService = new OwnerGamesService(apiClient);
            var downloadService = new GameInstallService(httpClient, sessionStore, new FileGameInstallStore());
            var maintenanceClient = new MaintenanceClient(apiClient, sessionStore);
            var preferencesStore = new FilePreferencesStore();

            desktop.MainWindow = new MainWindow
            {
                DataContext = new AppViewModel(
                    authService, catalogService, purchaseService, downloadService,
                    publishService, balanceService, gamesService, sessionStore, maintenanceClient, preferencesStore),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
