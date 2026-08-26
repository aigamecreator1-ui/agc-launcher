namespace AGC.Server.Entities;

public enum GameEngagementKind
{
    View,
    Download,
}

/// <summary>
/// One row per view/download. Counts are derived via GroupBy rather than stored as a
/// mutable counter, matching OwnerController.GetBalance's existing philosophy — see
/// its comment on why derived-from-tables beats a counter that can drift.
/// </summary>
public sealed class GameEngagementEvent
{
    public string Id { get; init; } = Guid.NewGuid().ToString();

    public required string GameId { get; set; }

    public required string UserId { get; set; }

    public GameEngagementKind Kind { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
