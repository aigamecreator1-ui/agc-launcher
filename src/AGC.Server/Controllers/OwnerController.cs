using AGC.Server.Data;
using AGC.Server.Entities;
using AGC.Server.Hubs;
using AGC.Server.Services;
using AGC.Shared.Dtos;
using AGC.Shared.Formatting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace AGC.Server.Controllers;

[ApiController]
[Authorize(Policy = "Owner")]
[Route("api/owner")]
public sealed class OwnerController(
    AppDbContext db,
    GameFileStorage storage,
    MaintenanceState maintenance,
    IHubContext<MaintenanceHub> hub) : ControllerBase
{
    private static readonly TimeSpan PublishMaintenanceWindow = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DeleteMaintenanceWindow = TimeSpan.FromMinutes(2);

    [HttpPost("games/suggest-price")]
    public ActionResult<SuggestPriceResponseDto> SuggestPrice(SuggestPriceRequestDto request)
        => Ok(new SuggestPriceResponseDto(PriceSuggestionService.SuggestPrice(request.BuildSizeBytes)));

    /// <summary>All live titles with the basic per-game stats the Games management screen shows.</summary>
    [HttpGet("games")]
    public async Task<ActionResult<IReadOnlyList<OwnerGameDto>>> GetGames(CancellationToken ct)
    {
        var games = await db.Games
            .Where(g => g.Status == GameStatus.Live)
            .OrderByDescending(g => g.PublishedAt)
            .ToListAsync(ct);

        var ownerCounts = await db.Ownerships
            .GroupBy(o => o.GameId)
            .Select(g => new { GameId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GameId, x => x.Count, ct);

        var revenue = await db.Transactions
            .Where(t => t.Status == TransactionStatus.Completed)
            .GroupBy(t => t.GameId)
            .Select(g => new { GameId = g.Key, Total = g.Sum(t => t.AmountUsd) })
            .ToDictionaryAsync(x => x.GameId, x => x.Total, ct);

        return Ok(games.Select(g => new OwnerGameDto(
            g.Id,
            g.Title,
            g.IsPaid,
            g.PriceUsd,
            g.BuildSizeBytes,
            ownerCounts.GetValueOrDefault(g.Id),
            revenue.GetValueOrDefault(g.Id),
            g.PublishedAt)).ToList());
    }

    /// <summary>
    /// Schedules a game for removal via the same maintenance-lockout flow Publish uses,
    /// just with a shorter window — active users are notified and force-closed, then the
    /// game (row, ownerships, transactions, and files) is actually deleted by
    /// <see cref="MaintenanceReopenService"/> right before access reopens.
    /// </summary>
    [HttpDelete("games/{id}")]
    public async Task<IActionResult> DeleteGame(string id, CancellationToken ct)
    {
        var game = await db.Games.FirstOrDefaultAsync(g => g.Id == id, ct);
        if (game is null)
        {
            return NotFound(new ApiErrorDto("Game not found."));
        }

        var reopensAt = DateTime.UtcNow.Add(DeleteMaintenanceWindow);
        var message = $"Launcher closing while \"{game.Title}\" is removed — reopening in 2 minutes.";
        maintenance.Begin(message, reopensAt, game.Id, MaintenanceAction.Delete);

        await hub.Clients.All.SendAsync(
            "MaintenanceChanged", new MaintenanceStatusDto(true, message, reopensAt), ct);

        return Accepted();
    }

    [HttpPost("games")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(8L * 1024 * 1024 * 1024)] // 8 GB — Unity builds can get large
    public async Task<ActionResult<GameDto>> Publish([FromForm] PublishGameForm form, CancellationToken ct)
    {
        if (form.Build.Length == 0)
        {
            return BadRequest(new ApiErrorDto("Select a build file to publish."));
        }

        if (form.Thumbnail.Length == 0)
        {
            return BadRequest(new ApiErrorDto("Select a thumbnail image."));
        }

        var game = new Game
        {
            Title = form.Title.Trim(),
            Description = form.Description.Trim(),
            Genre = form.Genre.Trim(),
            Tags = form.Tags.Trim(),
            IsPaid = form.IsPaid,
            PriceUsd = form.IsPaid ? form.PriceUsd : 0,
            BuildSizeBytes = form.Build.Length,
            ThumbnailPath = string.Empty,
            BuildPath = string.Empty,
            Status = GameStatus.Publishing,
        };

        await using (var buildStream = form.Build.OpenReadStream())
        {
            game.BuildPath = await storage.SaveAsync(game.Id, "build" + Path.GetExtension(form.Build.FileName), buildStream, ct);
        }

        await using (var thumbStream = form.Thumbnail.OpenReadStream())
        {
            game.ThumbnailPath = await storage.SaveAsync(game.Id, "thumbnail" + Path.GetExtension(form.Thumbnail.FileName), thumbStream, ct);
        }

        db.Games.Add(game);
        await db.SaveChangesAsync(ct);

        var reopensAt = DateTime.UtcNow.Add(PublishMaintenanceWindow);
        const string message = "Launcher closing for a new release — reopening in 5 minutes.";
        maintenance.Begin(message, reopensAt, game.Id, MaintenanceAction.Publish);

        await hub.Clients.All.SendAsync(
            "MaintenanceChanged", new MaintenanceStatusDto(true, message, reopensAt), ct);

        return Ok(new GameDto(
            game.Id, game.Title, game.Description, game.Genre,
            game.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            game.IsPaid, game.PriceUsd, game.BuildSizeBytes, IsOwned: false));
    }

    /// <summary>
    /// AGC's own recorded balance: completed USD sales minus non-failed USD
    /// withdrawals, both derived from our tables rather than stored as a mutable
    /// counter, so it can never drift out of sync. This is informational/historical
    /// — it's distinct from Stripe's real available balance (see
    /// GetAvailableToWithdraw), which is what actually gates a withdrawal, and which
    /// may be in a different currency entirely (see the Currency field on each
    /// withdrawal ledger entry — never assume it matches the USD sales figures).
    /// </summary>
    [HttpGet("balance")]
    public async Task<ActionResult<OwnerBalanceDto>> GetBalance(CancellationToken ct)
    {
        var sales = await db.Transactions
            .Where(t => t.Status == TransactionStatus.Completed)
            .Join(db.Games, t => t.GameId, g => g.Id, (t, g) => new { t.AmountUsd, t.CompletedAt, GameTitle = g.Title })
            .ToListAsync(ct);

        var payouts = await db.Payouts.ToListAsync(ct);

        var usdWithdrawn = payouts
            .Where(p => p.Status != PayoutStatus.Failed && p.Currency == "usd")
            .Sum(p => p.Amount);
        var balance = sales.Sum(s => s.AmountUsd) - usdWithdrawn;

        var history = sales
            .Select(s => new LedgerEntryDto(LedgerEntryType.Sale, s.GameTitle, s.AmountUsd, "usd", s.CompletedAt ?? default, null))
            .Concat(payouts.Select(p => p.Status == PayoutStatus.Failed
                ? new LedgerEntryDto(LedgerEntryType.Withdrawal, "Withdrawal", 0, p.Currency, p.CreatedAt, $"Failed — {p.FailureMessage ?? "unknown error"}")
                : new LedgerEntryDto(LedgerEntryType.Withdrawal, "Withdrawal", -p.Amount, p.Currency, p.CreatedAt, null)))
            .OrderByDescending(e => e.DateUtc)
            .ToList();

        return Ok(new OwnerBalanceDto(balance, history));
    }

    /// <summary>The real amount (and currency) Stripe will currently let you pay out — always
    /// re-verified server-side before actually creating a payout, never trusted from the
    /// client. Currency is whatever the account's balance actually holds, not assumed USD —
    /// see the account-country note in CreatePayout.</summary>
    [HttpGet("balance/available")]
    public async Task<ActionResult<AvailableToWithdrawDto>> GetAvailableToWithdraw(CancellationToken ct)
    {
        var (amount, currency) = await GetPrimaryAvailableBalanceAsync(ct);
        return Ok(new AvailableToWithdrawDto(amount, currency));
    }

    [HttpPost("payouts")]
    public async Task<ActionResult<LedgerEntryDto>> CreatePayout(CreatePayoutRequestDto request, CancellationToken ct)
    {
        if (request.Amount <= 0)
        {
            return BadRequest(new ApiErrorDto("Enter an amount greater than 0."));
        }

        // The Stripe account this app is currently configured against settles in
        // whatever currency its balance holds (not necessarily USD — see the
        // Currency field this returns), so we always pay out in that currency
        // rather than a hardcoded one. Once the account's country/settlement
        // currency is fixed, this automatically starts paying out in USD with no
        // code change needed.
        var (availableAmount, currency) = await GetPrimaryAvailableBalanceAsync(ct);
        if (request.Amount > availableAmount)
        {
            return BadRequest(new ApiErrorDto(
                $"Only {CurrencyFormatter.Format(availableAmount, currency)} is currently available to withdraw from Stripe " +
                "(a recent sale may not have settled yet — this can take a few days)."));
        }

        Stripe.Payout stripePayout;
        try
        {
            stripePayout = await new PayoutService().CreateAsync(new PayoutCreateOptions
            {
                Amount = (long)Math.Round(request.Amount * 100),
                Currency = currency,
            }, cancellationToken: ct);
        }
        catch (StripeException ex)
        {
            return BadRequest(new ApiErrorDto(ex.StripeError?.Message ?? ex.Message));
        }

        var payout = new Entities.Payout
        {
            Amount = request.Amount,
            Currency = currency,
            StripePayoutId = stripePayout.Id,
            Status = stripePayout.Status == "paid" ? PayoutStatus.Paid : PayoutStatus.Pending,
        };
        db.Payouts.Add(payout);
        await db.SaveChangesAsync(ct);

        return Ok(new LedgerEntryDto(LedgerEntryType.Withdrawal, "Withdrawal", -payout.Amount, payout.Currency, payout.CreatedAt, null));
    }

    private static async Task<(decimal Amount, string Currency)> GetPrimaryAvailableBalanceAsync(CancellationToken ct)
    {
        var balance = await new BalanceService().GetAsync(cancellationToken: ct);
        var entry = balance.Available.FirstOrDefault(a => a.Amount > 0) ?? balance.Available.FirstOrDefault();
        return entry is null ? (0, "usd") : (entry.Amount / 100m, entry.Currency);
    }
}
