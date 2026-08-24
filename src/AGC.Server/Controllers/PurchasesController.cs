using AGC.Server.Data;
using AGC.Server.Entities;
using AGC.Server.Extensions;
using AGC.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe.Checkout;

namespace AGC.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/purchases")]
public sealed class PurchasesController(AppDbContext db) : ControllerBase
{
    [HttpPost("{gameId}/checkout-session")]
    public async Task<ActionResult<CreateCheckoutSessionResponseDto>> CreateCheckoutSession(string gameId, CancellationToken ct)
    {
        var userId = User.RequireUserId();
        var game = await db.Games.FirstOrDefaultAsync(g => g.Id == gameId && g.Status == GameStatus.Live, ct);
        if (game is null)
        {
            return NotFound(new ApiErrorDto("Game not found."));
        }

        if (!game.IsPaid)
        {
            return BadRequest(new ApiErrorDto("This game is free — use the claim endpoint instead."));
        }

        var alreadyOwned = await db.Ownerships.AnyAsync(o => o.UserId == userId && o.GameId == gameId, ct);
        if (alreadyOwned)
        {
            return BadRequest(new ApiErrorDto("You already own this game."));
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var service = new SessionService();
        var session = await service.CreateAsync(new SessionCreateOptions
        {
            Mode = "payment",
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmount = (long)Math.Round(game.PriceUsd * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = game.Title,
                            Description = game.Description,
                        },
                    },
                },
            ],
            SuccessUrl = $"{baseUrl}/checkout/success",
            CancelUrl = $"{baseUrl}/checkout/cancel",
            Metadata = new Dictionary<string, string>
            {
                ["gameId"] = game.Id,
                ["userId"] = userId,
            },
        }, cancellationToken: ct);

        db.Transactions.Add(new Transaction
        {
            UserId = userId,
            GameId = game.Id,
            AmountUsd = game.PriceUsd,
            StripeCheckoutSessionId = session.Id,
            Status = TransactionStatus.Pending,
        });
        await db.SaveChangesAsync(ct);

        return Ok(new CreateCheckoutSessionResponseDto(session.Url));
    }
}
