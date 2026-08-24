using System.Text.RegularExpressions;
using AGC.Launcher.Core.Services;
using AGC.Shared.Dtos;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AGC.Launcher.ViewModels;

public enum LoginStage
{
    EnterEmail,
    EnterCode,
    EnterOwnerCode,
}

public sealed partial class LoginViewModel : ViewModelBase
{
    private const string GenericLoginFailure = "No account with that email. You can sign up instead.";

    private readonly IAuthService _authService;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    public event EventHandler<AuthResultDto>? LoginSucceeded;
    public event EventHandler? SignUpRequested;

    [ObservableProperty]
    public partial LoginStage Stage { get; set; } = LoginStage.EnterEmail;

    public bool IsEmailStage => Stage == LoginStage.EnterEmail;

    public bool IsCodeStage => Stage == LoginStage.EnterCode;

    public bool IsOwnerCodeStage => Stage == LoginStage.EnterOwnerCode;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Code { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    /// <summary>Dev-mode only: populated when the server has no email provider configured.</summary>
    [ObservableProperty]
    public partial string? DevModeCodeHint { get; set; }

    private static readonly Regex SimpleEmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    private bool CanContinueWithEmail() => !IsBusy && SimpleEmailPattern.IsMatch(Email);

    [RelayCommand(CanExecute = nameof(CanContinueWithEmail))]
    private async Task ContinueWithEmailAsync()
    {
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            var result = await _authService.RequestLoginAsync(Email);
            switch (result.Status)
            {
                case LoginRequestStatus.EmailCodeSent:
                    DevModeCodeHint = result.DevCode;
                    Stage = LoginStage.EnterCode;
                    break;
                case LoginRequestStatus.OwnerCodeRequired:
                    Stage = LoginStage.EnterOwnerCode;
                    break;
                case LoginRequestStatus.AccountNotFound:
                    ErrorMessage = GenericLoginFailure;
                    break;
            }
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

    private bool CanVerifyCode() => !IsBusy && Code.Trim().Length > 0;

    [RelayCommand(CanExecute = nameof(CanVerifyCode))]
    private async Task VerifyCodeAsync()
    {
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            var result = await _authService.VerifyLoginCodeAsync(Email, Code);
            LoginSucceeded?.Invoke(this, result);
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

    [RelayCommand(CanExecute = nameof(CanVerifyCode))]
    private async Task VerifyOwnerCodeAsync()
    {
        IsBusy = true;
        try
        {
            var result = await _authService.VerifyOwnerCodeAsync(Email, Code);
            LoginSucceeded?.Invoke(this, result);
        }
        catch (Exception)
        {
            // A wrong owner code must be indistinguishable from an unrecognized account —
            // bounce all the way back to the email stage with the same generic message.
            BackToEmail();
            ErrorMessage = GenericLoginFailure;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void BackToEmail()
    {
        Stage = LoginStage.EnterEmail;
        Code = string.Empty;
        ErrorMessage = null;
        DevModeCodeHint = null;
    }

    [RelayCommand]
    private void GoToSignUp() => SignUpRequested?.Invoke(this, EventArgs.Empty);

    partial void OnStageChanged(LoginStage value)
    {
        OnPropertyChanged(nameof(IsEmailStage));
        OnPropertyChanged(nameof(IsCodeStage));
        OnPropertyChanged(nameof(IsOwnerCodeStage));
    }

    partial void OnEmailChanged(string value) => ContinueWithEmailCommand.NotifyCanExecuteChanged();

    partial void OnCodeChanged(string value)
    {
        VerifyCodeCommand.NotifyCanExecuteChanged();
        VerifyOwnerCodeCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsBusyChanged(bool value)
    {
        ContinueWithEmailCommand.NotifyCanExecuteChanged();
        VerifyCodeCommand.NotifyCanExecuteChanged();
        VerifyOwnerCodeCommand.NotifyCanExecuteChanged();
    }
}
