using Microsoft.AspNetCore.SignalR;

namespace AGC.Server.Hubs;

/// <summary>Server-to-client broadcast only; clients don't call anything on this hub.</summary>
public sealed class MaintenanceHub : Hub;
