namespace AGC.Server.Entities;

public sealed class User
{
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>Stored lowercased; lookups always normalize to match.</summary>
    public required string Email { get; set; }

    public required string Username { get; set; }

    public required string PasswordHash { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
