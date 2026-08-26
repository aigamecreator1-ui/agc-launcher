namespace AGC.Server.Entities;

/// <summary>
/// One row per account, ever — the first time that account opens the launcher.
/// Re-opening on a later day is deliberately NOT counted again; the unique index on
/// UserId is what guarantees that at the database level, not just app-side logic.
/// </summary>
public sealed class LauncherOpenEvent
{
    public string Id { get; init; } = Guid.NewGuid().ToString();

    public required string UserId { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
