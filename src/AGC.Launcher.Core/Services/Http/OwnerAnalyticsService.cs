using AGC.Shared.Dtos;

namespace AGC.Launcher.Core.Services.Http;

/// <summary>Owner-only: real launcher- and per-game-level stats for the Analytics tab.</summary>
public sealed class OwnerAnalyticsService(ApiClient api)
{
    public Task<AnalyticsOverviewDto> GetOverviewAsync(CancellationToken ct = default)
        => api.GetAsync<AnalyticsOverviewDto>("api/owner/analytics", ct);
}
