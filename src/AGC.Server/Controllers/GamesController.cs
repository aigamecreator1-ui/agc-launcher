using AGC.Server.Data;
using AGC.Server.Entities;
using AGC.Server.Extensions;
using AGC.Server.Services;
using AGC.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AGC.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/games")]
public sealed class GamesController(AppDbContext db, GameFileStorage storage) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GameDto>>> GetCatalog(CancellationToken ct)
    {
        var userId = User.RequireUserId();
        var ownedIds = await db.Ownerships
            .Where(o => o.UserId == userId)
            .Select(o => o.GameId)
            .ToListAsync(ct);

        var games = await db.Games
            .Where(g => g.Status == GameStatus.Live)
            .OrderByDescending(g => g.PublishedAt)
            .ToListAsync(ct);

        return Ok(games.Select(g => ToDto(g, ownedIds.Contains(g.Id))).ToList());
    }

    [HttpGet("library")]
    public async Task<ActionResult<IReadOnlyList<GameDto>>> GetLibrary(CancellationToken ct)
    {
        var userId = User.RequireUserId();
        var ownedGameIds = await db.Ownerships
            .Where(o => o.UserId == userId)
            .Select(o => o.GameId)
            .ToListAsync(ct);

        var games = await db.Games
            .Where(g => ownedGameIds.Contains(g.Id))
            .ToListAsync(ct);

        return Ok(games.Select(g => ToDto(g, isOwned: true)).ToList());
    }

    /// <summary>
    /// Grants ownership directly — legitimate for free games. Paid games can't be
    /// claimed this way; that's real Stripe Checkout's job, landing in a later phase.
    /// </summary>
    [HttpPost("{id}/claim")]
    public async Task<ActionResult<ClaimGameResponseDto>> Claim(string id, CancellationToken ct)
    {
        var userId = User.RequireUserId();
        var game = await db.Games.FirstOrDefaultAsync(g => g.Id == id && g.Status == GameStatus.Live, ct);
        if (game is null)
        {
            return NotFound(new ApiErrorDto("Game not found."));
        }

        if (game.IsPaid)
        {
            return BadRequest(new ApiErrorDto("This game requires payment — checkout isn't wired up yet."));
        }

        var alreadyOwned = await db.Ownerships.AnyAsync(o => o.UserId == userId && o.GameId == id, ct);
        if (!alreadyOwned)
        {
            db.Ownerships.Add(new Ownership { UserId = userId, GameId = id });
            await db.SaveChangesAsync(ct);
        }

        return Ok(new ClaimGameResponseDto(true, null));
    }

    /// <summary>
    /// Streams the real build file the Publish flow uploaded, proxied through this
    /// server from Supabase Storage (the bucket is private — this is what actually
    /// enforces the ownership check below, not just the DB row).
    /// </summary>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(string id, CancellationToken ct)
    {
        var userId = User.RequireUserId();
        var game = await db.Games.FirstOrDefaultAsync(g => g.Id == id && g.Status == GameStatus.Live, ct);
        if (game is null)
        {
            return NotFound(new ApiErrorDto("Game not found."));
        }

        var owned = await db.Ownerships.AnyAsync(o => o.UserId == userId && o.GameId == id, ct);
        if (!owned)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ApiErrorDto("You don't own this game."));
        }

        var file = await storage.OpenReadAsync(game.BuildPath, ct);
        if (file is null)
        {
            return NotFound(new ApiErrorDto("The build file is missing on the server."));
        }

        var (content, length, _) = file.Value;
        if (length is not null)
        {
            // The proxied stream isn't seekable, so ASP.NET Core can't infer this on its
            // own — set it explicitly so the client gets real progress instead of falling
            // back to the build size recorded at publish time.
            Response.ContentLength = length;
        }

        return File(content, "application/octet-stream", Path.GetFileName(game.BuildPath));
    }

    [HttpGet("{id}/thumbnail")]
    [AllowAnonymous]
    public async Task<IActionResult> GetThumbnail(string id, CancellationToken ct)
    {
        var game = await db.Games.FirstOrDefaultAsync(g => g.Id == id, ct);
        if (game is null)
        {
            return NotFound();
        }

        var file = await storage.OpenReadAsync(game.ThumbnailPath, ct);
        if (file is null)
        {
            return NotFound();
        }

        var (content, _, contentType) = file.Value;
        return File(content, contentType);
    }

    private static GameDto ToDto(Game g, bool isOwned) => new(
        g.Id,
        g.Title,
        g.Description,
        g.Genre,
        g.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        g.IsPaid,
        g.PriceUsd,
        g.BuildSizeBytes,
        isOwned);
}
