namespace AGC.Server.Entities;

public enum GameStatus
{
    Publishing,
    Live,
}

public sealed class Game
{
    public string Id { get; init; } = Guid.NewGuid().ToString();

    public required string Title { get; set; }

    public required string Description { get; set; }

    public required string Genre { get; set; }

    /// <summary>Comma-separated; small enough not to need its own table.</summary>
    public string Tags { get; set; } = string.Empty;

    /// <summary>Path relative to the server's Storage root.</summary>
    public required string ThumbnailPath { get; set; }

    /// <summary>Path relative to the server's Storage root.</summary>
    public required string BuildPath { get; set; }

    public long BuildSizeBytes { get; set; }

    public bool IsPaid { get; set; }

    public decimal PriceUsd { get; set; }

    public GameStatus Status { get; set; } = GameStatus.Publishing;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DateTime? PublishedAt { get; set; }
}
