using AGC.Server.Services;
using AGC.Shared.Dtos;

namespace AGC.Server.Middleware;

/// <summary>
/// While a publish-triggered maintenance window is active, blocks every request
/// except auth (people can still sign in) and the SignalR hub itself, for anyone
/// who isn't the owner. This is the actual enforcement — the SignalR broadcast is
/// just how connected clients find out promptly.
/// </summary>
public sealed class MaintenanceMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, MaintenanceState maintenance)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var exempt = path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/maintenance", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/hubs/maintenance", StringComparison.OrdinalIgnoreCase)
            // Stripe's webhook must always be able to confirm a payment, even mid-lockout.
            || path.StartsWith("/api/webhooks", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/checkout/", StringComparison.OrdinalIgnoreCase);

        var isOwner = context.User.FindFirst(TokenService.IsOwnerClaimType)?.Value == "true";

        if (maintenance.IsActive && !exempt && !isOwner)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(
                new MaintenanceStatusDto(true, maintenance.Message, maintenance.ReopensAtUtc));
            return;
        }

        await next(context);
    }
}
