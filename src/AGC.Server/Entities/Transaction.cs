namespace AGC.Server.Entities;

public enum TransactionStatus
{
    Pending,
    Completed,
    Failed,
}

public sealed class Transaction
{
    public string Id { get; init; } = Guid.NewGuid().ToString();

    public required string UserId { get; set; }

    public required string GameId { get; set; }

    public decimal AmountUsd { get; set; }

    public required string StripeCheckoutSessionId { get; set; }

    public string? StripePaymentIntentId { get; set; }

    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }
}
