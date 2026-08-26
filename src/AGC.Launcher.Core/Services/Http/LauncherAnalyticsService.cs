namespace AGC.Launcher.Core.Services.Http;

/// <summary>Records this account's first-ever launcher open — a no-op on later opens.</summary>
public sealed class LauncherAnalyticsService(ApiClient api)
{
    public Task RecordOpenAsync(CancellationToken ct = default)
        => api.PostAsync<object>("api/analytics/launcher-open", new { }, ct);
}
