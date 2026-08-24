namespace AGC.Server.Entities;

public sealed class Ownership
{
    public int Id { get; init; }

    public required string UserId { get; set; }

    public required string GameId { get; set; }

    public DateTime AcquiredAt { get; init; } = DateTime.UtcNow;
}
