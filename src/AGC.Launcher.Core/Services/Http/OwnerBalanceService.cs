using AGC.Shared.Dtos;

namespace AGC.Launcher.Core.Services.Http;

public sealed class OwnerBalanceService(ApiClient api)
{
    public Task<OwnerBalanceDto> GetBalanceAsync(CancellationToken ct = default)
        => api.GetAsync<OwnerBalanceDto>("api/owner/balance", ct);

    public Task<AvailableToWithdrawDto> GetAvailableToWithdrawAsync(CancellationToken ct = default)
        => api.GetAsync<AvailableToWithdrawDto>("api/owner/balance/available", ct);

    /// <summary>Amount is in whatever currency GetAvailableToWithdrawAsync reported.</summary>
    public Task<LedgerEntryDto> RequestPayoutAsync(decimal amount, CancellationToken ct = default)
        => api.PostAsync<LedgerEntryDto>("api/owner/payouts", new CreatePayoutRequestDto(amount), ct);
}
