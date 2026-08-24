namespace AGC.Server.Entities;

public enum PayoutStatus
{
    Pending,
    Paid,
    Failed,
}

/// <summary>
/// A withdrawal attempt. AGC is a single-owner catalog, so there's no per-user
/// association — every payout belongs to the one owner balance. Currency is stored
/// per-payout rather than assumed USD, since it's whatever the Stripe account
/// actually settles in — which may not match the USD sales ledger.
/// </summary>
public sealed class Payout
{
    public string Id { get; init; } = Guid.NewGuid().ToString();

    public decimal Amount { get; set; }

    public required string Currency { get; set; }

    public required string StripePayoutId { get; set; }

    public PayoutStatus Status { get; set; } = PayoutStatus.Pending;

    public string? FailureMessage { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
