namespace AGC.Server.Entities;

public sealed class GameVote
{
    public string Id { get; init; } = Guid.NewGuid().ToString();

    public required string GameId { get; set; }

    public required string UserId { get; set; }

    public bool IsLike { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
