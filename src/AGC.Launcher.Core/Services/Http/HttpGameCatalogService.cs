using AGC.Shared.Dtos;

namespace AGC.Launcher.Core.Services.Http;

public sealed class HttpGameCatalogService(ApiClient api) : IGameCatalogService
{
    public Task<IReadOnlyList<GameDto>> GetCatalogAsync(CancellationToken ct = default)
        => api.GetAsync<IReadOnlyList<GameDto>>("api/games", ct);

    public Task<IReadOnlyList<GameDto>> GetLibraryAsync(CancellationToken ct = default)
        => api.GetAsync<IReadOnlyList<GameDto>>("api/games/library", ct);
}
