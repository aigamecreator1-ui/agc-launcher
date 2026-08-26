using AGC.Server.Data;
using AGC.Server.Entities;
using AGC.Server.Extensions;
using AGC.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AGC.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/analytics")]
public sealed class AnalyticsController(AppDbContext db) : ControllerBase
{
    /// <summary>
    /// Records this account's first-ever launcher open. A later call from the same
    /// account is a no-op — the unique index on UserId is the real guarantee, this
    /// check is just to avoid a pointless insert attempt on the common case.
    /// </summary>
    [HttpPost("launcher-open")]
    public async Task<ActionResult<AckDto>> RecordLauncherOpen(CancellationToken ct)
    {
        var userId = User.RequireUserId();
        var alreadyRecorded = await db.LauncherOpenEvents.AnyAsync(e => e.UserId == userId, ct);
        if (!alreadyRecorded)
        {
            db.LauncherOpenEvents.Add(new LauncherOpenEvent { UserId = userId });
            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Unique index race — a concurrent call from the same account already
                // recorded it. Either way, the account's first open is now on record.
            }
        }

        return Ok(new AckDto());
    }
}
