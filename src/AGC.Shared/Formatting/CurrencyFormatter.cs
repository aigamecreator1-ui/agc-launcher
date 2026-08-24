namespace AGC.Shared.Formatting;

/// <summary>
/// Deliberately simple (not culture-aware ICU formatting) — this app only ever
/// displays two currencies today (USD for sales, whatever the Stripe account
/// actually settles in for withdrawals), and being explicit about which currency an
/// amount is in matters more here than locale-perfect formatting.
/// </summary>
public static class CurrencyFormatter
{
    public static string Format(decimal amount, string currencyCode) => currencyCode.ToLowerInvariant() switch
    {
        "usd" => $"${amount:F2}",
        "myr" => $"RM {amount:F2}",
        "eur" => $"€{amount:F2}",
        "gbp" => $"£{amount:F2}",
        _ => $"{amount:F2} {currencyCode.ToUpperInvariant()}",
    };
}
