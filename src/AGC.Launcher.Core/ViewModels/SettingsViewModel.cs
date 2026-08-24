using AGC.Launcher.Core.Models;
using AGC.Launcher.Core.Services;
using AGC.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AGC.Launcher.ViewModels;

public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly IPreferencesStore _preferencesStore;
    private readonly bool _isLoaded;

    public SettingsViewModel(UserDto currentUser, IPreferencesStore preferencesStore)
    {
        CurrentUser = currentUser;
        _preferencesStore = preferencesStore;

        var prefs = _preferencesStore.Load();
        LaunchMinimized = prefs.LaunchMinimized;
        NotifyOnNewRelease = prefs.NotifyOnNewRelease;
        AutoInstallPurchases = prefs.AutoInstallPurchases;
        _isLoaded = true;
    }

    public UserDto CurrentUser { get; }

    public string UserInitial => CurrentUser.Username.Length > 0
        ? CurrentUser.Username[..1].ToUpperInvariant()
        : "?";

    public event EventHandler? SignOutRequested;

    [ObservableProperty]
    public partial bool LaunchMinimized { get; set; }

    [ObservableProperty]
    public partial bool NotifyOnNewRelease { get; set; }

    [ObservableProperty]
    public partial bool AutoInstallPurchases { get; set; }

    [ObservableProperty]
    public partial bool ShowSavedConfirmation { get; set; }

    partial void OnLaunchMinimizedChanged(bool value) => Persist();

    partial void OnNotifyOnNewReleaseChanged(bool value) => Persist();

    partial void OnAutoInstallPurchasesChanged(bool value) => Persist();

    private void Persist()
    {
        if (!_isLoaded)
        {
            return;
        }

        _preferencesStore.Save(new AppPreferences
        {
            LaunchMinimized = LaunchMinimized,
            NotifyOnNewRelease = NotifyOnNewRelease,
            AutoInstallPurchases = AutoInstallPurchases,
        });
        _ = FlashSavedConfirmationAsync();
    }

    private async Task FlashSavedConfirmationAsync()
    {
        ShowSavedConfirmation = true;
        await Task.Delay(1400);
        ShowSavedConfirmation = false;
    }

    [RelayCommand]
    private void SignOut() => SignOutRequested?.Invoke(this, EventArgs.Empty);
}
