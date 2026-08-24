using AGC.Shared.Dtos;
using Microsoft.AspNetCore.SignalR.Client;

namespace AGC.Launcher.Core.Services.Http;

/// <summary>
/// Real-time notifications for the publish-triggered lockout. The server middleware
/// is what actually enforces the lock — this just lets a connected client react
/// promptly instead of discovering it from a failed request.
/// </summary>
public sealed class MaintenanceClient(ApiClient api, ISessionStore sessionStore)
{
    private HubConnection? _connection;

    public event EventHandler<MaintenanceStatusDto>? StatusChanged;

    public Task<MaintenanceStatusDto> CheckStatusNowAsync(CancellationToken ct = default)
        => api.GetAsync<MaintenanceStatusDto>("api/maintenance/status", ct);

    /// <summary>
    /// Connects if not already connected. The hub doesn't require auth (a signed-out
    /// user on the login screen still needs to learn about an active lockout), so this
    /// is safe to call early and keep alive for the whole app session.
    /// </summary>
    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (_connection is not null)
        {
            return;
        }

        _connection = new HubConnectionBuilder()
            .WithUrl($"{ApiConfig.BaseUrl}hubs/maintenance", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(sessionStore.Load()?.Token);
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<MaintenanceStatusDto>("MaintenanceChanged", status => StatusChanged?.Invoke(this, status));

        try
        {
            await _connection.StartAsync(ct);
        }
        catch
        {
            // Best-effort: server-side enforcement still applies even without a live
            // push connection; the client just won't find out quite as promptly.
        }
    }

    public async Task DisconnectAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
