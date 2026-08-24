using System.Collections.ObjectModel;
using AGC.Launcher.Core.Services.Http;
using AGC.Shared.Dtos;
using AGC.Shared.Formatting;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AGC.Launcher.ViewModels;

public sealed partial class BalanceViewModel : ViewModelBase
{
    private readonly OwnerBalanceService _balanceService;

    public BalanceViewModel(OwnerBalanceService balanceService)
    {
        _balanceService = balanceService;
    }

    public ObservableCollection<LedgerEntryDto> History { get; } = [];

    public string BalanceDisplay => CurrencyFormatter.Format(BalanceUsd, "usd");

    [ObservableProperty]
    public partial decimal BalanceUsd { get; set; }

    /// <summary>The real, live amount Stripe will pay out right now — gates the withdraw input.</summary>
    [ObservableProperty]
    public partial decimal AvailableToWithdraw { get; set; }

    /// <summary>Whatever currency the Stripe account's balance actually holds — not assumed USD.</summary>
    [ObservableProperty]
    public partial string AvailableToWithdrawCurrency { get; set; } = "usd";

    [ObservableProperty]
    public partial decimal WithdrawAmount { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsWithdrawing { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? WithdrawErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? WithdrawSuccessMessage { get; set; }

    public string AvailableToWithdrawDisplay => CurrencyFormatter.Format(AvailableToWithdraw, AvailableToWithdrawCurrency);

    public string WithdrawCurrencyLabel => AvailableToWithdrawCurrency.ToUpperInvariant();

    public async Task LoadAsync()
    {
        ErrorMessage = null;
        IsLoading = true;
        try
        {
            var balanceTask = _balanceService.GetBalanceAsync();
            var availableTask = _balanceService.GetAvailableToWithdrawAsync();
            await Task.WhenAll(balanceTask, availableTask);

            BalanceUsd = balanceTask.Result.BalanceUsd;
            AvailableToWithdraw = availableTask.Result.Amount;
            AvailableToWithdrawCurrency = availableTask.Result.Currency;

            History.Clear();
            foreach (var entry in balanceTask.Result.History)
            {
                History.Add(entry);
            }
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

    private bool CanWithdraw() => !IsWithdrawing && WithdrawAmount > 0 && WithdrawAmount <= AvailableToWithdraw;

    [RelayCommand(CanExecute = nameof(CanWithdraw))]
    private async Task WithdrawAsync()
    {
        WithdrawErrorMessage = null;
        WithdrawSuccessMessage = null;
        IsWithdrawing = true;
        try
        {
            var requested = WithdrawAmount;
            var currency = AvailableToWithdrawCurrency;
            await _balanceService.RequestPayoutAsync(requested);
            WithdrawSuccessMessage = $"Withdrawal of {CurrencyFormatter.Format(requested, currency)} initiated.";
            WithdrawAmount = 0;
            await LoadAsync(); // refresh balance, history, and available-to-withdraw together
        }
        catch (Exception ex)
        {
            WithdrawErrorMessage = ex.Message;
        }
        finally
        {
            IsWithdrawing = false;
        }
    }

    partial void OnBalanceUsdChanged(decimal value) => OnPropertyChanged(nameof(BalanceDisplay));

    partial void OnAvailableToWithdrawChanged(decimal value)
    {
        OnPropertyChanged(nameof(AvailableToWithdrawDisplay));
        WithdrawCommand.NotifyCanExecuteChanged();
    }

    partial void OnAvailableToWithdrawCurrencyChanged(string value)
    {
        OnPropertyChanged(nameof(AvailableToWithdrawDisplay));
        OnPropertyChanged(nameof(WithdrawCurrencyLabel));
    }

    partial void OnWithdrawAmountChanged(decimal value) => WithdrawCommand.NotifyCanExecuteChanged();

    partial void OnIsWithdrawingChanged(bool value) => WithdrawCommand.NotifyCanExecuteChanged();
}
