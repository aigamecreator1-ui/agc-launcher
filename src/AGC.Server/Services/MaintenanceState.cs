namespace AGC.Server.Services;

public enum MaintenanceAction
{
    Publish,
    Delete,
}

/// <summary>
/// Server-side source of truth for the publish/delete-triggered lockout — the SignalR
/// broadcast is just a notification; this is what actually gets enforced.
/// </summary>
public sealed class MaintenanceState
{
    public bool IsActive { get; private set; }

    public string? Message { get; private set; }

    public DateTime? ReopensAtUtc { get; private set; }

    /// <summary>The game that will flip to Live (Publish) or be removed (Delete) once the window elapses.</summary>
    public string? PendingGameId { get; private set; }

    public MaintenanceAction PendingAction { get; private set; }

    public void Begin(string message, DateTime reopensAtUtc, string pendingGameId, MaintenanceAction action)
    {
        IsActive = true;
        Message = message;
        ReopensAtUtc = reopensAtUtc;
        PendingGameId = pendingGameId;
        PendingAction = action;
    }

    public void End()
    {
        IsActive = false;
        Message = null;
        ReopensAtUtc = null;
        PendingGameId = null;
    }
}
