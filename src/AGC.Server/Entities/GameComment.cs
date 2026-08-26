namespace AGC.Server.Entities;

public sealed class GameComment
{
    public string Id { get; init; } = Guid.NewGuid().ToString();

    public required string GameId { get; set; }

    public required string UserId { get; set; }

    public required string Text { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
