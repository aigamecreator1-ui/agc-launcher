using AGC.Server.Data;
using AGC.Server.Entities;
using AGC.Server.Hubs;
using AGC.Shared.Dtos;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AGC.Server.Services;

/// <summary>
/// Once the maintenance window elapses, either flips the pending game Live (Publish)
/// or actually removes it — row, ownerships, transactions, and files (Delete) —
/// then clears the lockout.
/// </summary>
public sealed class MaintenanceReopenService(
    MaintenanceState maintenance,
    IServiceScopeFactory scopeFactory,
    IHubContext<MaintenanceHub> hub,
    ILogger<MaintenanceReopenService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (!maintenance.IsActive || maintenance.ReopensAtUtc > DateTime.UtcNow)
            {
                continue;
            }

            var pendingGameId = maintenance.PendingGameId;
            var pendingAction = maintenance.PendingAction;

            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (pendingGameId is not null)
            {
                if (pendingAction == MaintenanceAction.Delete)
                {
                    await DeleteGameAsync(db, scope.ServiceProvider.GetRequiredService<GameFileStorage>(), pendingGameId, stoppingToken);
                }
                else
                {
                    var game = await db.Games.FirstOrDefaultAsync(g => g.Id == pendingGameId, stoppingToken);
                    if (game is not null && game.Status != GameStatus.Live)
                    {
                        game.Status = GameStatus.Live;
                        game.PublishedAt = DateTime.UtcNow;
                        await db.SaveChangesAsync(stoppingToken);
                        logger.LogInformation("Game {GameId} is now live", pendingGameId);
                    }
                }
            }

            maintenance.End();
            await hub.Clients.All.SendAsync(
                "MaintenanceChanged", new MaintenanceStatusDto(false, null, null), stoppingToken);
        }
    }

    private async Task DeleteGameAsync(AppDbContext db, GameFileStorage storage, string gameId, CancellationToken ct)
    {
        var game = await db.Games.FirstOrDefaultAsync(g => g.Id == gameId, ct);
        if (game is null)
        {
            return;
        }

        var filePaths = new[] { game.BuildPath, game.ThumbnailPath };

        db.Ownerships.RemoveRange(await db.Ownerships.Where(o => o.GameId == gameId).ToListAsync(ct));
        db.Transactions.RemoveRange(await db.Transactions.Where(t => t.GameId == gameId).ToListAsync(ct));
        db.Games.Remove(game);
        await db.SaveChangesAsync(ct);

        await storage.DeleteAsync(filePaths, ct);
        logger.LogInformation("Game {GameId} was deleted", gameId);
    }
}
