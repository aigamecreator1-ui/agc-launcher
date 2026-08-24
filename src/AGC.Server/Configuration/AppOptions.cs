namespace AGC.Server.Configuration;

public sealed class AppOptions
{
    public required string OwnerEmail { get; init; }

    public required string OwnerSecurityCode { get; init; }

    public required string JwtSigningKey { get; init; }

    /// <summary>Null/empty means: fall back to dev-mode codes instead of real email.</summary>
    public string? ResendApiKey { get; init; }

    public required string ResendFromEmail { get; init; }

    /// <summary>
    /// Null/empty (along with <see cref="StripeWebhookSecret"/>) means: no paid-game
    /// features. Checkout and payouts are unavailable rather than the server refusing
    /// to start — publishing/playing free games never needs Stripe at all.
    /// </summary>
    public string? StripeSecretKey { get; init; }

    public string? StripeWebhookSecret { get; init; }

    /// <summary>Both keys must be present — a checkout session with no working webhook would leave purchases stuck Pending forever.</summary>
    public bool IsStripeConfigured =>
        !string.IsNullOrEmpty(StripeSecretKey) && !string.IsNullOrEmpty(StripeWebhookSecret);

    /// <summary>Npgsql connection string for the Postgres database (e.g. a Neon connection string).</summary>
    public required string DatabaseConnectionString { get; init; }

    /// <summary>Project URL for Supabase Storage, e.g. https://xxxxx.supabase.co</summary>
    public required string SupabaseUrl { get; init; }

    /// <summary>
    /// Supabase's service_role key — bypasses Row Level Security. Server-only: never
    /// sent to or embedded in the desktop client, same rule as the Stripe secret key.
    /// </summary>
    public required string SupabaseServiceRoleKey { get; init; }

    /// <summary>Storage bucket that holds uploaded game builds and thumbnails.</summary>
    public required string SupabaseBucket { get; init; }

    public static AppOptions FromConfiguration(IConfiguration configuration)
    {
        string Require(string key) =>
            configuration[key] is { Length: > 0 } value
                ? value
                : throw new InvalidOperationException(
                    $"Required setting '{key}' is missing. Set it in the repo-root .env file.");

        return new AppOptions
        {
            OwnerEmail = Require("OWNER_EMAIL"),
            OwnerSecurityCode = Require("OWNER_SECURITY_CODE"),
            JwtSigningKey = Require("JWT_SIGNING_KEY"),
            ResendApiKey = configuration["RESEND_API_KEY"],
            ResendFromEmail = Require("RESEND_FROM_EMAIL"),
            StripeSecretKey = configuration["STRIPE_SECRET_KEY"],
            StripeWebhookSecret = configuration["STRIPE_WEBHOOK_SECRET"],
            DatabaseConnectionString = Require("DATABASE_URL"),
            SupabaseUrl = Require("SUPABASE_URL"),
            SupabaseServiceRoleKey = Require("SUPABASE_SERVICE_ROLE_KEY"),
            SupabaseBucket = Require("SUPABASE_BUCKET"),
        };
    }
}
