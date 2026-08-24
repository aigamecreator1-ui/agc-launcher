using System.Diagnostics;
using AGC.Launcher.Core.Models;
using AGC.Shared.Dtos;

namespace AGC.Launcher.Core.Services.Http;

/// <summary>
/// Free games are granted directly via the claim endpoint (no money involved). Paid
/// games open a real Stripe Checkout session in the system browser — card details
/// never pass through this app — and this polls the library until the backend's
/// webhook has confirmed payment and granted the entitlement.
/// </summary>
public sealed class HttpPurchaseService(ApiClient api, IGameCatalogService catalogService) : IPurchaseService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan PollTimeout = TimeSpan.FromMinutes(3);

    public async Task<PurchaseResult> PurchaseAsync(GameDto game, CancellationToken ct = default)
    {
        if (!game.IsPaid)
        {
            return await ClaimFreeGameAsync(game, ct);
        }

        try
        {
            var session = await api.PostAsync<CreateCheckoutSessionResponseDto>(
                $"api/purchases/{game.Id}/checkout-session", new { }, ct);

            Process.Start(new ProcessStartInfo(session.CheckoutUrl) { UseShellExecute = true });

            return await WaitForEntitlementAsync(game.Id, ct);
        }
        catch (ApiException ex)
        {
            return new PurchaseResult { Outcome = PurchaseOutcome.Failed, ErrorMessage = ex.Message };
        }
    }

    private async Task<PurchaseResult> ClaimFreeGameAsync(GameDto game, CancellationToken ct)
    {
        try
        {
            var result = await api.PostAsync<ClaimGameResponseDto>($"api/games/{game.Id}/claim", new { }, ct);
            return result.Success
                ? new PurchaseResult { Outcome = PurchaseOutcome.Success }
                : new PurchaseResult { Outcome = PurchaseOutcome.Failed, ErrorMessage = result.ErrorMessage };
        }
        catch (ApiException ex)
        {
            return new PurchaseResult { Outcome = PurchaseOutcome.Failed, ErrorMessage = ex.Message };
        }
    }

    private async Task<PurchaseResult> WaitForEntitlementAsync(string gameId, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow + PollTimeout;
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(PollInterval, ct);

            try
            {
                var library = await catalogService.GetLibraryAsync(ct);
                if (library.Any(g => g.Id == gameId))
                {
                    return new PurchaseResult { Outcome = PurchaseOutcome.Success };
                }
            }
            catch
            {
                // Transient — keep polling until the deadline.
            }
        }

        return new PurchaseResult
        {
            Outcome = PurchaseOutcome.Failed,
            ErrorMessage = "Didn't detect a completed payment yet. If you finished checkout, give it a moment and reopen the Store.",
        };
    }
}
