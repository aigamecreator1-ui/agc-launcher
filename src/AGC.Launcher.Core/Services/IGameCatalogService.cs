using AGC.Shared.Dtos;

namespace AGC.Launcher.Core.Services;

/// <summary>
/// Reads the store catalog and the current user's owned-games library. The current
/// user is always inferred server-side from the session token — nothing here passes
/// a user id explicitly.
/// </summary>
public interface IGameCatalogService
{
    Task<IReadOnlyList<GameDto>> GetCatalogAsync(CancellationToken ct = default);

    Task<IReadOnlyList<GameDto>> GetLibraryAsync(CancellationToken ct = default);
}
