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

        db.GameEngagementEvents.Add(new GameEngagementEvent { GameId = id, UserId = userId, Kind = GameEngagementKind.Download });
        await db.SaveChangesAsync(ct);

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

    /// <summary>Records a detail-page view, then returns the current social snapshot.</summary>
    [HttpPost("{id}/view")]
    public async Task<ActionResult<GameSocialDto>> RecordView(string id, CancellationToken ct)
    {
        var userId = User.RequireUserId();
        var exists = await db.Games.AnyAsync(g => g.Id == id, ct);
        if (!exists)
        {
            return NotFound(new ApiErrorDto("Game not found."));
        }

        db.GameEngagementEvents.Add(new GameEngagementEvent { GameId = id, UserId = userId, Kind = GameEngagementKind.View });
        await db.SaveChangesAsync(ct);

        return Ok(await BuildSocialDtoAsync(id, userId, ct));
    }

    /// <summary>Current likes/dislikes/vote/comments — no recording, used to refresh after voting or commenting.</summary>
    [HttpGet("{id}/social")]
    public async Task<ActionResult<GameSocialDto>> GetSocial(string id, CancellationToken ct)
    {
        var userId = User.RequireUserId();
        var exists = await db.Games.AnyAsync(g => g.Id == id, ct);
        if (!exists)
        {
            return NotFound(new ApiErrorDto("Game not found."));
        }

        return Ok(await BuildSocialDtoAsync(id, userId, ct));
    }

    /// <summary>
    /// Toggle: no existing vote creates one, casting the same vote again removes it,
    /// casting the opposite vote flips it — a player can only ever hold one of
    /// like/dislike/no-vote per game, enforced by the unique (GameId, UserId) index.
    /// </summary>
    [HttpPost("{id}/vote")]
    public async Task<ActionResult<GameSocialDto>> Vote(string id, VoteRequestDto request, CancellationToken ct)
    {
        var userId = User.RequireUserId();
        var exists = await db.Games.AnyAsync(g => g.Id == id, ct);
        if (!exists)
        {
            return NotFound(new ApiErrorDto("Game not found."));
        }

        var existingVote = await db.GameVotes.SingleOrDefaultAsync(v => v.GameId == id && v.UserId == userId, ct);
        if (existingVote is null)
        {
            db.GameVotes.Add(new GameVote { GameId = id, UserId = userId, IsLike = request.IsLike });
        }
        else if (existingVote.IsLike == request.IsLike)
        {
            db.GameVotes.Remove(existingVote);
        }
        else
        {
            existingVote.IsLike = request.IsLike;
        }

        await db.SaveChangesAsync(ct);
        return Ok(await BuildSocialDtoAsync(id, userId, ct));
    }

    [HttpPost("{id}/comments")]
    public async Task<ActionResult<GameSocialDto>> PostComment(string id, PostCommentRequestDto request, CancellationToken ct)
    {
        var userId = User.RequireUserId();
        var exists = await db.Games.AnyAsync(g => g.Id == id, ct);
        if (!exists)
        {
            return NotFound(new ApiErrorDto("Game not found."));
        }

        var text = request.Text.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return BadRequest(new ApiErrorDto("Comment can't be empty."));
        }

        if (text.Length > 2000)
        {
            return BadRequest(new ApiErrorDto("Comment is too long (2000 characters max)."));
        }

        db.GameComments.Add(new GameComment { GameId = id, UserId = userId, Text = text });
        await db.SaveChangesAsync(ct);

        return Ok(await BuildSocialDtoAsync(id, userId, ct));
    }

    private async Task<GameSocialDto> BuildSocialDtoAsync(string gameId, string userId, CancellationToken ct)
    {
        var votes = await db.GameVotes.Where(v => v.GameId == gameId).ToListAsync(ct);
        var likes = votes.Count(v => v.IsLike);
        var dislikes = votes.Count(v => !v.IsLike);
        var userVote = votes.FirstOrDefault(v => v.UserId == userId)?.IsLike;

        var comments = await db.GameComments
            .Where(c => c.GameId == gameId)
            .OrderByDescending(c => c.CreatedAt)
            .Join(db.Users, c => c.UserId, u => u.Id, (c, u) => new GameCommentDto(c.Id, u.Username, c.Text, c.CreatedAt))
            .ToListAsync(ct);

        return new GameSocialDto(likes, dislikes, userVote, comments);
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
