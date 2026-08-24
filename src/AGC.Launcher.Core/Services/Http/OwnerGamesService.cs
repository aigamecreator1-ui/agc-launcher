using AGC.Shared.Dtos;

namespace AGC.Launcher.Core.Services.Http;

/// <summary>
/// Owner-only: the Games management list and deleting a published title. Deletion is
/// asynchronous on the server — this call just schedules it behind the maintenance
/// lockout; the game is actually removed once that window elapses.
/// </summary>
public sealed class OwnerGamesService(ApiClient api)
{
    public Task<IReadOnlyList<OwnerGameDto>> GetGamesAsync(CancellationToken ct = default)
        => api.GetAsync<IReadOnlyList<OwnerGameDto>>("api/owner/games", ct);

    public Task DeleteGameAsync(string gameId, CancellationToken ct = default)
        => api.DeleteAsync($"api/owner/games/{gameId}", ct);
}
