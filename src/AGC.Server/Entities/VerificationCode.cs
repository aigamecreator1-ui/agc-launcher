namespace AGC.Server.Entities;

/// <summary>
/// A one-time login code emailed to a normal (non-owner) account. The plaintext code
/// is never persisted — only its hash — so a database read alone can't reveal it.
/// </summary>
public sealed class VerificationCode
{
    public int Id { get; init; }

    public required string Email { get; set; }

    public required string CodeHash { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool Consumed { get; set; }

    public int FailedAttempts { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
