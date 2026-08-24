using AGC.Launcher.Core.Models;
using AGC.Shared.Dtos;

namespace AGC.Launcher.Core.Services;

/// <summary>
/// Runs a purchase. A real implementation asks the backend to create a Stripe
/// Checkout session, opens it in the system browser, and only reports success once
/// the backend has confirmed payment via a Stripe webhook and recorded the
/// entitlement — the client never sees card data and never decides "did this pay?"
/// on its own.
/// </summary>
public interface IPurchaseService
{
    Task<PurchaseResult> PurchaseAsync(GameDto game, CancellationToken ct = default);
}
