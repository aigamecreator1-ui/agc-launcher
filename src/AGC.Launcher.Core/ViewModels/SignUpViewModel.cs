using System.Text.RegularExpressions;
using AGC.Launcher.Core.Services;
using AGC.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AGC.Launcher.ViewModels;

public sealed partial class SignUpViewModel : ViewModelBase
{
    private readonly IAuthService _authService;

    public SignUpViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    public event EventHandler<AuthResultDto>? SignUpSucceeded;
    public event EventHandler? BackToLoginRequested;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Username { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    private static readonly Regex SimpleEmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    private bool CanSignUp() =>
        !IsBusy && SimpleEmailPattern.IsMatch(Email) && Username.Trim().Length >= 3 && Password.Length > 0;

    [RelayCommand(CanExecute = nameof(CanSignUp))]
    private async Task SignUpAsync()
    {
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            var result = await _authService.SignUpAsync(Email, Username, Password);
            SignUpSucceeded?.Invoke(this, result);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void BackToLogin() => BackToLoginRequested?.Invoke(this, EventArgs.Empty);

    partial void OnEmailChanged(string value) => SignUpCommand.NotifyCanExecuteChanged();

    partial void OnUsernameChanged(string value) => SignUpCommand.NotifyCanExecuteChanged();

    partial void OnPasswordChanged(string value) => SignUpCommand.NotifyCanExecuteChanged();

    partial void OnIsBusyChanged(bool value) => SignUpCommand.NotifyCanExecuteChanged();
}
