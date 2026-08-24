using AGC.Shared.Formatting;

namespace AGC.Shared.Dtos;

public enum LedgerEntryType
{
    Sale,
    Withdrawal,
}

/// <summary>
/// Amount is signed for display: positive for a sale, negative for a completed/
/// pending withdrawal. A failed withdrawal has no lasting balance effect, so it's
/// shown with Amount 0 and an explanatory Note instead. Currency is per-entry,
/// deliberately — sales are always USD (what customers are charged), but a
/// withdrawal's currency is whatever the Stripe account actually settles in, which
/// can differ. Never assume they match.
/// </summary>
public sealed record LedgerEntryDto(LedgerEntryType Type, string Label, decimal Amount, string Currency, DateTime DateUtc, string? Note)
{
    public string AmountDisplay => CurrencyFormatter.Format(Amount, Currency);
}

/// <summary>BalanceUsd is AGC's own recorded-sales total, always USD (that's what customers
/// are charged) minus any USD-denominated withdrawals. It's informational/historical —
/// distinct from AvailableToWithdrawDto, which is what Stripe will actually pay out right
/// now, in whatever currency the account settles in.</summary>
public sealed record OwnerBalanceDto(decimal BalanceUsd, IReadOnlyList<LedgerEntryDto> History);

/// <summary>The real, live amount and currency Stripe will let you withdraw right now.
/// Currency reflects whatever the account actually holds — not assumed to be USD.</summary>
public sealed record AvailableToWithdrawDto(decimal Amount, string Currency)
{
    public string AmountDisplay => CurrencyFormatter.Format(Amount, Currency);
}

/// <summary>Amount is in whatever currency AvailableToWithdrawDto reported — the server is
/// the source of truth for which currency that is, so the client never specifies it.</summary>
public sealed record CreatePayoutRequestDto(decimal Amount);
