namespace AGC.Server.Configuration;

public sealed class AppOptions
{
    public required string OwnerEmail { get; init; }

    public required string OwnerSecurityCode { get; init; }

    public required string JwtSigningKey { get; init; }

    /// <summary>Null/empty means: fall back to dev-mode codes instead of real email.</summary>
    public string? ResendApiKey { get; init; }

    public required string ResendFromEmail { get; init; }

    public required string StripeSecretKey { get; init; }

    public required string StripeWebhookSecret { get; init; }

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
            StripeSecretKey = Require("STRIPE_SECRET_KEY"),
            StripeWebhookSecret = Require("STRIPE_WEBHOOK_SECRET"),
        };
    }
}
