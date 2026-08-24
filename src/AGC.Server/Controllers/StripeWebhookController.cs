using AGC.Server.Configuration;
using AGC.Server.Data;
using AGC.Server.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace AGC.Server.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/webhooks/stripe")]
public sealed class StripeWebhookController(AppDbContext db, AppOptions options, ILogger<StripeWebhookController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Handle(CancellationToken ct)
    {
        if (!options.IsStripeConfigured)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var json = await new StreamReader(Request.Body).ReadToEndAsync(ct);

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json, Request.Headers["Stripe-Signature"], options.StripeWebhookSecret!); // non-null: IsStripeConfigured checked above
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Rejected a Stripe webhook with an invalid signature.");
            return BadRequest();
        }

        switch (stripeEvent.Type)
        {
            case EventTypes.CheckoutSessionCompleted:
                if (stripeEvent.Data.Object is Session session)
                {
                    await FulfillAsync(session, ct);
                }

                break;

            case EventTypes.PayoutPaid:
            case EventTypes.PayoutFailed:
                if (stripeEvent.Data.Object is Stripe.Payout stripePayout)
                {
                    await UpdatePayoutStatusAsync(stripePayout, ct);
                }

                break;
        }

        return Ok();
    }

    private async Task FulfillAsync(Session session, CancellationToken ct)
    {
        var transaction = await db.Transactions
            .FirstOrDefaultAsync(t => t.StripeCheckoutSessionId == session.Id, ct);

        if (transaction is null)
        {
            logger.LogWarning("Received a completed checkout for an unknown session {SessionId}", session.Id);
            return;
        }

        if (transaction.Status == TransactionStatus.Completed)
        {
            return; // already processed — Stripe can send the same event more than once
        }

        transaction.Status = TransactionStatus.Completed;
        transaction.CompletedAt = DateTime.UtcNow;
        transaction.StripePaymentIntentId = session.PaymentIntentId;

        var alreadyOwned = await db.Ownerships
            .AnyAsync(o => o.UserId == transaction.UserId && o.GameId == transaction.GameId, ct);
        if (!alreadyOwned)
        {
            db.Ownerships.Add(new Ownership { UserId = transaction.UserId, GameId = transaction.GameId });
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            "Fulfilled purchase: user {UserId} now owns game {GameId}", transaction.UserId, transaction.GameId);
    }

    private async Task UpdatePayoutStatusAsync(Stripe.Payout stripePayout, CancellationToken ct)
    {
        var payout = await db.Payouts.FirstOrDefaultAsync(p => p.StripePayoutId == stripePayout.Id, ct);
        if (payout is null)
        {
            logger.LogWarning("Received a payout status update for an unknown payout {PayoutId}", stripePayout.Id);
            return;
        }

        payout.Status = stripePayout.Status == "paid" ? PayoutStatus.Paid : PayoutStatus.Failed;
        payout.FailureMessage = stripePayout.FailureMessage;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Payout {PayoutId} updated to status {Status}", stripePayout.Id, payout.Status);
    }
}
